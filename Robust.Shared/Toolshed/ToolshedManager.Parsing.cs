using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Security;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    private readonly Dictionary<Type, ITypeParser?> _consoleTypeParsers = new();
    private readonly Dictionary<ITypeParser, ITypeParser?> _argParsers = new();
    private readonly Dictionary<Type, ITypeParser> _customParsers = new();
    private readonly Dictionary<Type, Type> _genericTypeParsers = new();
    private readonly List<(Type, Type)> _constrainedParsers = new();

    private void InitializeParser()
    {
        // This contains both custom parsers, and default type parsers
        var parsers = _reflection.GetAllChildren<ITypeParser>();

        foreach (var parserType in parsers)
        {
            var parent = parserType.BaseType;

            var found = false;
            while (parent != null)
            {
                if (parent.IsGenericType(typeof(TypeParser<>)))
                {
                    found = true;
                    break;
                }
                parent = parent.BaseType;
            }

            if (!found)
                continue;

            if (parserType.IsGenericType)
            {
                var t = parserType.BaseType!.GetGenericArguments().First();
                if (t.IsGenericType)
                {
                    var key = t.GetGenericTypeDefinition();
                    if (!_genericTypeParsers.TryAdd(key, parserType))
                        throw new Exception($"Duplicate toolshed type parser for type: {key}");

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
                if (parser is IPostInjectInit inj)
                    inj.PostInject();

                _log.Verbose($"Setting up {parserType.PrettyName()}, {parser.Parses.PrettyName()}");
                if (!_consoleTypeParsers.TryAdd(parser.Parses, parser))
                {
                    throw new Exception($"Discovered conflicting parsers for type {parser.Parses.PrettyName()}: {parserType.PrettyName()} and {_consoleTypeParsers[parser.Parses]!.GetType().PrettyName()}");
                }
            }
        }
    }

    internal ITypeParser? GetParserForType(Type t)
    {
        if (_consoleTypeParsers.TryGetValue(t, out var parser))
            return parser;

        parser = FindParserForType(t);
        DebugTools.Assert(parser == null || parser.Parses == t);
        _consoleTypeParsers.TryAdd(t, parser);
        return parser;
    }

    /// <summary>
    /// Variant of <see cref="GetParserForType"/> that will return a parser that also attempts to resolve a type from a
    /// variable or block via the <see cref="ValueRef{T}"/> and <see cref="Block"/> parsers.
    /// </summary>
    internal ITypeParser? GetArgumentParser(Type t)
    {
        var parser = GetParserForType(t);
        if (parser != null)
            return GetArgumentParser(parser);

        // Some types are not directly parsable, but can still be passes as arguments by using variables or blocks.
        DebugTools.Assert(!t.IsValueRef() && !t.IsAssignableTo(typeof(Block)));
        return GetParserForType(typeof(ValueRef<>).MakeGenericType(t));
    }

    /// <summary>
    /// Variant of <see cref="GetParserForType"/> that will return a parser that also attempts to resolve a type from a
    /// variable or block via the <see cref="ValueRef{T}"/> parsers. If that fails, it will fall back to using the given
    /// type parser
    /// </summary>
    internal ITypeParser? GetArgumentParser(ITypeParser baseParser)
    {
        if (!baseParser.EnableValueRef)
            return baseParser;

        if (_argParsers.TryGetValue(baseParser, out var parser))
            return parser;

        var t = baseParser.Parses;

        if (t.IsValueRef() || t.IsAssignableTo(typeof(Block)))
            parser = baseParser;
        else if (baseParser.GetType().HasGenericParent(typeof(TypeParser<>)))
            parser = GetParserForType(typeof(ValueRef<>).MakeGenericType(t));
        else
            parser = GetCustomParser(typeof(CustomValueRefTypeParser<,>).MakeGenericType(t, baseParser.GetType()));

        return _argParsers[baseParser] = parser;
    }

    internal TParser GetCustomParser<TParser, T>() where TParser : CustomTypeParser<T>, new() where T : notnull
    {
        return (TParser)GetCustomParser(typeof(TParser));
    }

    /// <summary>
    /// Attempt to fetch the custom parser instance of the given type.
    /// </summary>
    internal ITypeParser GetCustomParser(Type parser)
    {
        if (_customParsers.TryGetValue(parser, out var result))
            return result;

        if (parser.ContainsGenericParameters)
            throw new ArgumentException($"Type cannot contain generic parameters");

        if (!parser.IsCustomParser())
            throw new ArgumentException($"{parser.PrettyName()} does not inherit from {typeof(CustomTypeParser<>).PrettyName()}");

        result = (ITypeParser) _typeFactory.CreateInstanceUnchecked(parser, true);
        if (result is IPostInjectInit inj)
            inj.PostInject();

        return _customParsers[parser] = result;
    }

    private ITypeParser? FindParserForType(Type t)
    {
        // Accidentally using FindParserForType() instead of GetParserForType() can lead to very fun bugs.
        // Hence this assert.
        DebugTools.Assert(!_consoleTypeParsers.ContainsKey(t));

        if (t.IsConstructedGenericType)
        {
            if (_genericTypeParsers.TryGetValue(t.GetGenericTypeDefinition(), out var genParser))
            {
                try
                {
                    var concreteParser = genParser.MakeGenericType(t.GenericTypeArguments);
                    var builtParser = (ITypeParser) _typeFactory.CreateInstanceUnchecked(concreteParser, true);

                    if (builtParser is IPostInjectInit inj)
                        inj.PostInject();

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
        foreach (var (_, genParser) in _constrainedParsers)
        {
            // no, IsAssignableTo isn't useful here. I tried. tfw.
            try
            {
                var concreteParser = genParser.MakeGenericType(t);

                var builtParser = (ITypeParser) _typeFactory.CreateInstanceUnchecked(concreteParser, true);
                if (builtParser is IPostInjectInit inj)
                    inj.PostInject();

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
    public bool TryParse<T>(ParserContext parserContext, [NotNullWhen(true)] out T? parsed)
    {
        var res = TryParse(parserContext, typeof(T), out var p);
        if (p is not null)
            parsed = (T?) p;
        else
            parsed = default;
        return res;
    }

    /// <summary>
    ///     iunno man it does autocomplete what more do u want
    /// </summary>
    public CompletionResult? TryAutocomplete(ParserContext ctx, Type t, CommandArgument? arg)
    {
        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);
        DebugTools.AssertEqual(ctx.GenerateCompletions, true);
        return GetParserForType(t)?.TryAutocomplete(ctx, arg);
    }

    /// <summary>
    /// Attempts to parse the given type directly. Unlike <see cref="TryParseArgument"/> this will not attempt
    /// to resolve variable or command blocks.
    /// </summary>
    /// <param name="parserContext">The input to parse from.</param>
    /// <param name="t">The type to parse from the input.</param>
    /// <param name="parsed">The parsed value, if any.</param>
    /// <returns>Success.</returns>
    public bool TryParse(ParserContext parserContext, Type t, [NotNullWhen(true)] out object? parsed)
    {
        parsed = null;

        if (GetParserForType(t) is not {} impl)
        {
            if (!parserContext.GenerateCompletions)
                parserContext.Error = new UnparseableValueError(t);
            return false;
        }

        if (!impl.TryParse(parserContext, out parsed))
            return false;

        DebugTools.Assert(parsed.GetType().IsAssignableTo(t));
        return true;
    }

    /// <summary>
    /// Variant of <see cref="TryParse{T}"/> that will first attempt to parse the argument as a <see cref="ValueRef{T}"/>
    /// or <see cref="Block"/>, before falling back to the default parser. Note that this generally does not directly
    /// return the requested type.
    /// </summary>
    public bool TryParseArgument(ParserContext parserContext, Type t, [NotNullWhen(true)] out object? parsed)
    {
        parsed = null;
        return GetArgumentParser(t) is { } parser && parser.TryParse(parserContext, out parsed);
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
            var msg = FormattedMessage.FromUnformatted(
                $"The type {T.PrettyName()} has no parser available and cannot be parsed.");
            msg.PushNewline();
            msg.AddText("Please contact a programmer with this error, they'd probably like to see it.");
            msg.PushNewline();
            msg.AddMarkupOrThrow("[bold][color=red]THIS IS A BUG.[/color][/bold]");
            return msg;
        }

        return FormattedMessage.FromUnformatted($"The type {T.PrettyName()} cannot be parsed, as it cannot be constructed.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
