using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class BlockTypeParser : TypeParser<Block>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Block? result)
    {
        return Block.TryParse(ctx, out result);
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        Block.TryParse(ctx, out _);
        return ctx.Completions;
    }
}

internal sealed class BlockTypeParser<T> : TypeParser<Block<T>>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Block<T>? result)
    {
        return Block<T>.TryParse(ctx, out result);
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        Block<T>.TryParse(ctx, out _);
        return ctx.Completions;
    }
}

internal sealed class BlockTypeParser<TIn, TOut> : TypeParser<Block<TIn, TOut>>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Block<TIn, TOut>? result)
    {
        return Block<TIn, TOut>.TryParse(ctx, out result);
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        Block<TIn, TOut>.TryParse(ctx, out _);
        return ctx.Completions;
    }
}
