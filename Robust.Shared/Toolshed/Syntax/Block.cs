using System;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Console;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

/// <summary>
/// A simple block of commands.
/// </summary>
[Virtual]
public class Block(CommandRun expr)
{
    public readonly CommandRun Run = expr;

    public static bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Block? block)
    {
        block = null;
        if (!TryParseBlock(ctx, null, null, out var run))
            return false;

        block = new Block(run);
        return true;
    }

    public object? Invoke(object? input, IInvocationContext ctx)
    {
        return Run.Invoke(input, ctx);
    }

    public static bool TryParseBlock(
        ParserContext ctx,
        Type? pipedType,
        Type? targetOutput,
        [NotNullWhen(true)] out CommandRun? run)
    {
        run = null;
        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);

        ctx.ConsumeWhitespace();
        if (!ctx.EatMatch('{'))
        {
            if (ctx.GenerateCompletions)
                ctx.Completions = CompletionResult.FromOptions([new CompletionOption("{")]);
            else
                ctx.Error = new MissingOpeningBrace();

            return false;
        }

        ctx.PushBlockTerminator('}');
        if (!CommandRun.TryParse(ctx, pipedType, targetOutput, out run))
        {
            return false;
        }

        if (ctx.EatBlockTerminator())
            return true;

        ctx.ConsumeWhitespace();
        if (!ctx.GenerateCompletions)
        {
            ctx.Error = new MissingClosingBrace();
            return false;
        }

        if (ctx.OutOfInput)
            ctx.Completions = CompletionResult.FromOptions([new CompletionOption("}")]);
        return false;
    }

    public override string ToString()
    {
        return $"{{ {Run} }}";
    }
}

/// <summary>
/// A block of commands that take in no input, and return <see cref="T"/>.
/// </summary>
[Virtual]
public class Block<T>(CommandRun expr) : Block(expr)
{
    public static bool TryParse(ParserContext ctx,
        [NotNullWhen(true)] out Block<T>? block
    )
    {
        block = null;
        if (!TryParseBlock(ctx, null, typeof(T), out var run))
            return false;

        block = new Block<T>(run);
        return true;
    }

    public T? Invoke(IInvocationContext ctx)
    {
        var res = Run.Invoke(null, ctx);
        if (res is null)
            return default;
        return (T?) res;
    }
}

/// <summary>
/// A block of commands that take in <see cref="TIn"/>, and return <see cref="TOut"/>.
/// </summary>
[Virtual]
public class Block<TIn, TOut>(CommandRun expr) : Block(expr)
{
    public static bool TryParse(ParserContext ctx, [NotNullWhen(true)] out Block<TIn, TOut>? block)
    {
        block = null;
        if (!TryParseBlock(ctx, typeof(TIn), typeof(TOut), out var run))
            return false;

        block = new Block<TIn, TOut>(run);
        return true;
    }

    public TOut? Invoke(TIn? input, IInvocationContext ctx)
    {
        var res = Run.Invoke(input, ctx);
        if (res is null)
            return default;
        return (TOut?) res;
    }
}
