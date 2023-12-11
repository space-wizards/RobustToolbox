using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    private readonly Dictionary<Type, ITypeParser> _consoleTypeParsers = new();
    private readonly Dictionary<Type, Type> _genericTypeParsers = new();
    private readonly List<(Type, Type)> _constrainedParsers = new();

    private void InitializeParser()
    {
        var parsers = _reflection.GetAllChildren<ITypeParser>();

        foreach (var parserType in parsers)
        {
            if (parserType.IsGenericType)
            {
                var t = parserType.BaseType!.GetGenericArguments().First();
                if (t.IsGenericType)
                {
                    _genericTypeParsers.Add(t.GetGenericTypeDefinition(), parserType);
                    _log.Verbose($"Setting up {parserType.PrettyName()}, {t.GetGenericTypeDefinition().PrettyName()}");
                }
                else if (t.IsGenericParameter)
                {
                    _constrainedParsers.Add((t, parserType));
                    _log.Verbose($"Setting up {parserType.PrettyName()}, for T where T: {string.Join(", ", t.GetGenericParameterConstraints().Select(x => x.PrettyName()))}");
                }
            }
            else
            {
                var parser = (ITypeParser) _typeFactory.CreateInstanceUnchecked(parserType, oneOff: true);
                parser.PostInject();
                _log.Verbose($"Setting up {parserType.PrettyName()}, {parser.Parses.PrettyName()}");
                _consoleTypeParsers.Add(parser.Parses, parser);
            }
        }
    }

    private ITypeParser? GetParserForType(Type t)
    {
        if (_consoleTypeParsers.TryGetValue(t, out var parser))
            return parser;

        if (t.IsConstructedGenericType)
        {
            if (_genericTypeParsers.TryGetValue(t.GetGenericTypeDefinition(), out var genParser))
            {
                try
                {
                    var concreteParser = genParser.MakeGenericType(t.GenericTypeArguments);

                    var builtParser = (ITypeParser) _typeFactory.CreateInstanceUnchecked(concreteParser, true);
                    builtParser.PostInject();
                    _consoleTypeParsers.Add(builtParser.Parses, builtParser);
                    return builtParser;
                }
                catch (SecurityException)
                {
                    _log.Info($"Couldn't use {genParser.PrettyName()} to parse {t.PrettyName()}");
                    // Oops, try the other thing.
                }
            }
        }

        // augh, slow path!
        foreach (var (param, genParser) in _constrainedParsers)
        {
            // no, IsAssignableTo isn't useful here. I tried. tfw.
            try
            {
                var concreteParser = genParser.MakeGenericType(t);

                var builtParser = (ITypeParser) _typeFactory.CreateInstanceUnchecked(concreteParser, true);
                builtParser.PostInject();
                _consoleTypeParsers.Add(builtParser.Parses, builtParser);
                return builtParser;
            }
            catch (SecurityException)
            {
                // Oops, try another.
            }
            catch (ArgumentException)
            {
                // oogh  C# why do i have to do this with exception handling.
            }
        }

        // ough, slower path!
        var baseTy = t.BaseType;

        if (baseTy is not null && baseTy != typeof(object) && baseTy != typeof(ValueType) && GetParserForType(baseTy) is {} retTy)
            return retTy;

        foreach (var i in t.GetInterfaces())
        {
            if (GetParserForType(i) is { } o)
                return o;
        }

        return null;
    }

    /// <summary>
    ///     Attempts to parse the given type.
    /// </summary>
    /// <param name="parserContext">The input to parse from.</param>
    /// <param name="parsed">The parsed value, if any.</param>
    /// <param name="error">A console error, if any, that can be reported to explain the parsing failure.</param>
    /// <typeparam name="T">The type to parse from the input.</typeparam>
    /// <returns>Success.</returns>
    public bool TryParse<T>(ParserContext parserContext, [NotNullWhen(true)] out T? parsed, out IConError? error)
    {
        var res = TryParse(parserContext, typeof(T), out var p, out error);
        if (p is not null)
            parsed = (T?) p;
        else
            parsed = default(T);
        return res;
    }

    /// <summary>
    ///     iunno man it does autocomplete what more do u want
    /// </summary>
    public ValueTask<(CompletionResult?, IConError?)> TryAutocomplete(ParserContext parserContext, Type t, string? argName)
    {
        var impl = GetParserForType(t);

        if (impl is null)
        {
            return ValueTask.FromResult<(CompletionResult?, IConError?)>((null, new UnparseableValueError(t)));
        }

        return impl.TryAutocomplete(parserContext, argName);
    }

    /// <summary>
    ///     Attempts to parse the given type.
    /// </summary>
    /// <param name="parserContext">The input to parse from.</param>
    /// <param name="t">The type to parse from the input.</param>
    /// <param name="parsed">The parsed value, if any.</param>
    /// <param name="error">A console error, if any, that can be reported to explain the parsing failure.</param>
    /// <returns>Success.</returns>
    public bool TryParse(ParserContext parserContext, Type t, [NotNullWhen(true)] out object? parsed, out IConError? error)
    {
        var impl = GetParserForType(t);

        if (impl is null)
        {
            parsed = null;
            error = new UnparseableValueError(t);
            return false;
        }

        return impl.TryParse(parserContext, out parsed, out error);
    }
}

/// <summary>
///     Error that's given if a type cannot be parsed due to lack of parser.
/// </summary>
/// <param name="T">The type being parsed.</param>
public record UnparseableValueError(Type T) : IConError
{
    public FormattedMessage DescribeInner()
    {

        if (T.Constructable())
        {
            var msg = FormattedMessage.FromMarkup(
                $"The type {T.PrettyName()} has no parser available and cannot be parsed.");
            msg.PushNewline();
            msg.AddText("Please contact a programmer with this error, they'd probably like to see it.");
            msg.PushNewline();
            msg.AddMarkup("[bold][color=red]THIS IS A BUG.[/color][/bold]");
            return msg;
        }
        else
        {
            return FormattedMessage.FromMarkup($"The type {T.PrettyName()} cannot be parsed, as it cannot be constructed.");
        }
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
