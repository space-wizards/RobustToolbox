using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

public sealed class Block
{
    internal CommandRun CommandRun { get; set; }

    public static bool TryParse(
            bool doAutoComplete,
            ParserContext parserContext,
            Type? pipedType,
            [NotNullWhen(true)] out Block? block,
            out ValueTask<(CompletionResult?, IConError?)>? autoComplete,
            out IConError? error
        )
    {
        parserContext.ConsumeWhitespace();

        var enclosed = parserContext.EatMatch('{');

        if (enclosed)
            parserContext.PushTerminator("}");

        CommandRun.TryParse(doAutoComplete, parserContext, pipedType, null, !enclosed, out var expr, out autoComplete, out error);

        if (expr is null)
        {
            block = null;
            return false;
        }

        block = new Block(expr);
        return true;
    }

    public Block(CommandRun expr)
    {
        CommandRun = expr;
    }

    public object? Invoke(object? input, IInvocationContext ctx)
    {
        return CommandRun.Invoke(input, ctx);
    }
}

/// <summary>
/// Something more akin to actual expressions.
/// </summary>
public sealed class Block<T>
{
    internal CommandRun<T> CommandRun { get; set; }

    public static bool TryParse(bool doAutoComplete, ParserContext parserContext, Type? pipedType,
        [NotNullWhen(true)] out Block<T>? block, out ValueTask<(CompletionResult?, IConError?)>? autoComplete, out IConError? error)
    {
        parserContext.ConsumeWhitespace();

        var enclosed = parserContext.EatMatch('{');

        if (enclosed)
            parserContext.PushTerminator("}");

        CommandRun<T>.TryParse(enclosed, doAutoComplete, parserContext, pipedType, !enclosed, out var expr, out autoComplete, out error);

        if (expr is null)
        {
            block = null;
            return false;
        }

        block = new Block<T>(expr);
        return true;
    }

    public Block(CommandRun<T> expr)
    {
        CommandRun = expr;
    }

    public T? Invoke(object? input, IInvocationContext ctx)
    {
        return CommandRun.Invoke(input, ctx);
    }
}

public sealed class Block<TIn, TOut>
{
    internal CommandRun<TIn, TOut> CommandRun { get; set; }

    public static bool TryParse(bool doAutoComplete, ParserContext parserContext, Type? pipedType,
        [NotNullWhen(true)] out Block<TIn, TOut>? block, out ValueTask<(CompletionResult?, IConError?)>? autoComplete, out IConError? error)
    {
        parserContext.ConsumeWhitespace();

        var enclosed = parserContext.EatMatch('{');

        if (enclosed)
            parserContext.PushTerminator("}");

        CommandRun<TIn, TOut>.TryParse(enclosed, doAutoComplete, parserContext, !enclosed, out var expr, out autoComplete, out error);

        if (expr is null)
        {
            block = null;
            return false;
        }

        block = new Block<TIn, TOut>(expr);
        return true;
    }

    public Block(CommandRun<TIn, TOut> expr)
    {
        CommandRun = expr;
    }

    public TOut? Invoke(TIn? input, IInvocationContext ctx)
    {
        return CommandRun.Invoke(input, ctx);
    }
}


public record struct MissingClosingBrace() : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromMarkup("Expected a closing brace.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
