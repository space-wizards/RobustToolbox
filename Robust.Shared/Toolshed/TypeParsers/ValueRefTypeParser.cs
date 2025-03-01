using System;
using System.Diagnostics.CodeAnalysis;
using System.Text;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Utility;

#pragma warning disable CS0618 // Type or member is obsolete

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class ValueRefTypeParser<T, TAuto> : TypeParser<ValueRef<T, TAuto>>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out ValueRef<T, TAuto>? result)
    {
        var res = Toolshed.TryParse<ValueRef<T>>(ctx, out var inner);
        result = null;
        if (res)
            result = new ValueRef<T, TAuto>(inner!);
        return res;
    }

    public override CompletionResult? TryAutocomplete(ParserContext parserContext, CommandArgument? arg)
    {
        return Toolshed.TryAutocomplete(parserContext, typeof(ValueRef<T, T>), arg);
    }
}

internal sealed class ValueRefTypeParser<T> : TypeParser<ValueRef<T>>
{
    internal static bool TryParse(
        ToolshedManager shed,
        ParserContext ctx,
        ITypeParser? parser,
        [NotNullWhen(true)] out ValueRef<T>? result)
    {
        result = null;

        ctx.ConsumeWhitespace();
        var rune = ctx.PeekRune();
        if (rune == new Rune('$'))
        {
            if (!shed.TryParse<VarRef<T>>(ctx, out var valRef))
                return false;

            result = valRef;
            return true;
        }

        if (rune == new Rune('{'))
        {
            if (!shed.TryParse<Block<T>>(ctx, out var block))
                return false;

            result = new BlockRef<T>(block);
            return true;
        }

        parser ??= shed.GetParserForType(typeof(T));
        if (parser == null)
        {
            // No parser is available -> must be provided via a variable or block
            if (!ctx.GenerateCompletions)
                ctx.Error = new MustBeVarOrBlock(typeof(T));
            return false;
        }

        if (!parser.TryParse(ctx, out var obj))
            return false;

        if (obj is not T value)
            return false;

        result = new ParsedValueRef<T>(value);
        return true;
    }

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out ValueRef<T>? result)
    {
        return TryParse(Toolshed, ctx, null, out result);
    }

    public static CompletionResult? TryAutocomplete(
        ToolshedManager shed,
        ParserContext ctx,
        CommandArgument? arg,
        ITypeParser? parser)
    {
        ctx.ConsumeWhitespace();
        var rune = ctx.PeekRune();
        if (rune == new Rune('$'))
            return shed.TryAutocomplete(ctx, typeof(VarRef<T>), arg);

        if (rune == new Rune('{'))
        {
            Block<T>.TryParse(ctx, out _);
            return ctx.Completions;
        }

        parser ??= shed.GetParserForType(typeof(T));

        if (parser == null)
            return CompletionResult.FromHint($"<variable or block of type {typeof(T).PrettyName()}>");

        var res = parser.TryAutocomplete(ctx, arg);
        return res ?? CompletionResult.FromHint($"<variable, block, or value of type {typeof(T).PrettyName()}>");
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        return TryAutocomplete(Toolshed, ctx, arg, null);
    }
}

internal sealed class CustomValueRefTypeParser<T, TParser> : CustomTypeParser<ValueRef<T>>
    where TParser : CustomTypeParser<T>, new()
    where T : notnull
{
    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        var parser = Toolshed.GetCustomParser<TParser, T>();
        return ValueRefTypeParser<T>.TryAutocomplete(Toolshed, ctx, arg, parser);
    }

    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out ValueRef<T>? result)
    {
        var parser = Toolshed.GetCustomParser<TParser, T>();
        return ValueRefTypeParser<T>.TryParse(Toolshed, ctx, parser, out result);
    }
}

public sealed class MustBeVarOrBlock(Type T) : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Command expects an argument of type {T.PrettyName()}.\nHowever this type has no parser available, and thus cannot be directly parsed.\nInstead, you have to use a variable or command block to provide it.");
    }
}
