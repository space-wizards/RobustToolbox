using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Toolshed.Syntax;

namespace Robust.Shared.Toolshed.TypeParsers;

internal sealed class CommandRunTypeParser : TypeParser<CommandRun>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out CommandRun? result)
    {
        return CommandRun.TryParse(ctx, null, null, out result);
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        CommandRun.TryParse(ctx, null, null, out _);
        return ctx.Completions;
    }
}

internal sealed class ExpressionTypeParser<T> : TypeParser<CommandRun<T>>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out CommandRun<T>? result)
    {
        return CommandRun<T>.TryParse(ctx, null, out result);
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        CommandRun<T>.TryParse(ctx, null, out _);
        return ctx.Completions;
    }
}

internal sealed class ExpressionTypeParser<TIn, TOut> : TypeParser<CommandRun<TIn, TOut>>
{
    public override bool TryParse(ParserContext ctx, [NotNullWhen(true)] out CommandRun<TIn, TOut>? result)
    {
        return CommandRun<TIn, TOut>.TryParse(ctx, out result);
    }

    public override CompletionResult? TryAutocomplete(ParserContext ctx, CommandArgument? arg)
    {
        CommandRun<TIn, TOut>.TryParse(ctx, out _);
        return ctx.Completions;
    }
}
