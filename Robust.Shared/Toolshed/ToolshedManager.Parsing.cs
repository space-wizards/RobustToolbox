using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    private readonly Dictionary<Type, ITypeParser> _consoleTypeParsers = new();
    private readonly Dictionary<Type, Type> _genericTypeParsers = new();

    private void InitializeParser()
    {
        var parsers = _reflection.GetAllChildren<ITypeParser>();

        foreach (var parserType in parsers)
        {
            if (parserType.IsGenericType)
            {
                var t = parserType.BaseType!.GetGenericArguments().First();
                _genericTypeParsers.Add(t.GetGenericTypeDefinition(), parserType);
                _log.Debug($"Setting up {parserType.PrettyName()}, {t.GetGenericTypeDefinition().PrettyName()}");
            }
            else
            {
                var parser = (ITypeParser) _typeFactory.CreateInstanceUnchecked(parserType);
                parser.PostInject();
                _log.Debug($"Setting up {parserType.PrettyName()}, {parser.Parses.PrettyName()}");
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
            if (!_genericTypeParsers.TryGetValue(t.GetGenericTypeDefinition(), out var genParser))
                return null;

            var concreteParser = genParser.MakeGenericType(t.GenericTypeArguments);

            var builtParser = (ITypeParser) _typeFactory.CreateInstanceUnchecked(concreteParser, true);
            builtParser.PostInject();
            _consoleTypeParsers.Add(builtParser.Parses, builtParser);
            return builtParser;
        }

        var baseTy = t.BaseType;

        if (baseTy is not null && baseTy != typeof(object) && baseTy != t.BaseType)
            return GetParserForType(t);

        return null;
    }

    /// <summary>
    ///     Attempts to parse the given type.
    /// </summary>
    /// <param name="parser">The input to parse from.</param>
    /// <param name="parsed">The parsed value, if any.</param>
    /// <param name="error">A console error, if any, that can be reported to explain the parsing failure.</param>
    /// <typeparam name="T">The type to parse from the input.</typeparam>
    /// <returns>Success.</returns>
    public bool TryParse<T>(ForwardParser parser, [NotNullWhen(true)] out object? parsed, out IConError? error)
    {
        return TryParse(parser, typeof(T), out parsed, out error);
    }

    /// <summary>
    ///     iunno man it does autocomplete what more do u want
    /// </summary>
    public ValueTask<(CompletionResult?, IConError?)> TryAutocomplete(ForwardParser parser, Type t, string? argName)
    {
        var impl = GetParserForType(t);

        if (impl is null)
        {
            return ValueTask.FromResult<(CompletionResult?, IConError?)>((null, new UnparseableValueError(t)));
        }

        return impl.TryAutocomplete(parser, argName);
    }

    /// <summary>
    ///     Attempts to parse the given type.
    /// </summary>
    /// <param name="parser">The input to parse from.</param>
    /// <param name="t">The type to parse from the input.</param>
    /// <param name="parsed">The parsed value, if any.</param>
    /// <param name="error">A console error, if any, that can be reported to explain the parsing failure.</param>
    /// <returns>Success.</returns>
    public bool TryParse(ForwardParser parser, Type t, [NotNullWhen(true)] out object? parsed, out IConError? error)
    {
        var impl = GetParserForType(t);

        if (impl is null)
        {
            parsed = null;
            error = new UnparseableValueError(t);
            return false;
        }

        return impl.TryParse(parser, out parsed, out error);
    }
}

/// <summary>
///     Error that's given if a type cannot be parsed due to lack of parser.
/// </summary>
/// <param name="T">The type being parsed.</param>
public record struct UnparseableValueError(Type T) : IConError
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
