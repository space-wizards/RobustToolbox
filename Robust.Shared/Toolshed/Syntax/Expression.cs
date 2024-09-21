using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

public sealed class CommandRun
{
    /// <summary>
    /// The original string that contains the substring from which this command run was parsed.
    /// </summary>
    public readonly string OriginalExpr;

    /// <summary>
    /// The list of parsed commands, along with the start and end indices in <see cref="OriginalExpr"/>
    /// </summary>
    public readonly List<(ParsedCommand, Vector2i)> Commands;

    #region Misc Debug Properties

    /// <summary>
    /// The type returned by the last command in <see cref="Commands"/>
    /// </summary>
    public readonly Type? ReturnType;

    /// <summary>
    /// The type that should get piped into the first command in <see cref="Commands"/>
    /// </summary>
    public readonly Type? PipedType;

    /// <summary>
    /// The starting index of the first command in <see cref="Commands"/>
    /// </summary>
    public readonly int StartIndex;

    /// <summary>
    /// The ending index of the last command in <see cref="Commands"/>
    /// </summary>
    public readonly int EndIndex;

    /// <summary>
    /// The substring of <see cref="OriginalExpr"/> from which all of the commands in the run were parsed.
    /// </summary>
    public string SubExpr => OriginalExpr[StartIndex..EndIndex];

    #endregion

    public CommandRun(List<(ParsedCommand, Vector2i)> commands, string originalExpr, Type? returnType, Type? pipedType)
    {
        DebugTools.Assert(commands.Count > 0);
        OriginalExpr = originalExpr;
        Commands = commands;
        ReturnType = returnType;
        PipedType = pipedType;
        StartIndex = commands[0].Item2.X;
        EndIndex = commands[^1].Item2.Y;
        DebugTools.Assert(StartIndex >= 0);
        DebugTools.Assert(EndIndex <= OriginalExpr.Length);
        DebugTools.Assert(EndIndex > StartIndex);
    }

    /// <summary>
    /// Attempt to parse a sequence of commands that initially take in the given piped type.
    /// </summary>
    /// <param name="ctx">The parser context</param>
    /// <param name="pipedType">The type of object being piped into the command that we want to parse, This determines which commands are valid. Null means that the first command takes no piped input</param>
    /// <param name="targetOutput">The desired output type of the final command in the sequence. Null implies no constraint. The <see cref="Void"/> type implies that the final command should not return a value</param>
    /// <param name="expr">The expression that was generated</param>
    /// <returns></returns>
    public static bool TryParse(
        ParserContext ctx,
        Type? pipedType,
        Type? targetOutput,
        [NotNullWhen(true)] out CommandRun? expr)
    {
        expr = null;
        var cmds = new List<(ParsedCommand, Vector2i)>();
        var start = ctx.Index;
        ctx.ConsumeWhitespace();
        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);
        if (pipedType == typeof(void))
            throw new ArgumentException($"Piped type cannot be void");

        if (ctx.PeekBlockTerminator())
        {
            // Trying to parse an empty block as a command run? I.e. " { } "
            ctx.Error = new EmptyCommandRun();
            ctx.Error.Contextualize(ctx.Input, new(start, ctx.Index + 1));
            return false;
        }

        while (true)
        {
            if (!ParsedCommand.TryParse(ctx, pipedType, out var cmd))
            {
                if (ctx.Error is NotValidCommandError err)
                    err.TargetType = targetOutput;
                return false;
            }

            pipedType = cmd.ReturnType;
            cmds.Add((cmd, (start, ctx.Index)));
            ctx.ConsumeWhitespace();

            if (ctx.EatCommandTerminators())
            {
                ctx.ConsumeWhitespace();
                pipedType = null;
            }

            // If the command run encounters a block terminator we exit out.
            // The parser that pushed the block terminator is what should actually eat & pop it, so that it can
            // return appropriate errors if the block was not terminated.
            if (ctx.PeekBlockTerminator())
                break;

            if (ctx.OutOfInput)
                break;

            start = ctx.Index;

            if (pipedType != typeof(void))
                continue;

            // The previously parsed command does not generate any output that can be piped/chained into another
            // command. This can happen if someone tries to provide more arguments than a command accepts.
            // e.g., " i 5 5". In this case, the parsing fails and should make it clear that no more input was expected.
            // Multiple unrelated commands on a single line are still supported via the ';' terminator.
            // I.e., "i 5 i 5" is invalid, but "i 5; i 5" is valid.
            // IMO the latter is also easier to read.
            if (ctx.GenerateCompletions)
                return false;

            ctx.Error = new EndOfCommandError();
            ctx.Error.Contextualize(ctx.Input, (ctx.Index, ctx.Index+1));
            return false;
        }

        if (ctx.Error != null || cmds.Count == 0)
        {
            expr = null;
            return false;
        }

        // Return the last type, even if the command ended with a ';'
        var returnType = cmds[^1].Item1.ReturnType;
        if (targetOutput != null && !returnType.IsAssignableTo(targetOutput))
        {
            ctx.Error = new WrongCommandReturn(targetOutput, returnType);
            expr = null;
            return false;
        }

        expr = new CommandRun(cmds, ctx.Input, returnType, pipedType);
        return true;
    }

    public object? Invoke(object? input, IInvocationContext ctx, bool reportErrors = true)
    {
        // TODO TOOLSHED Improve error handling
        // Most expression invokers don't bother to check for errors.
        // This especially applies to all map / emplace / sort commands.
        // A simple error while enumerating entities could lock up the server.

        if (ctx.HasErrors)
        {
            // Attempt to prevent O(n^2) growth in errors due to people repeatedly evaluating expressions without
            // checking for errors.
            throw new Exception($"Improperly handled Toolshed errors");
        }

        var ret = input;
        foreach (var (cmd, span) in Commands)
        {
            ret = cmd.Invoke(ret, ctx);
            if (!ctx.HasErrors)
                continue;

            if (!reportErrors)
                return null;

            foreach (var err in ctx.GetErrors())
            {
                err.Contextualize(OriginalExpr, span);
                ctx.WriteLine(err.Describe());
            }

            return null;
        }

        return ret;
    }

    public override string ToString()
    {
        return SubExpr;
    }
}

public sealed class CommandRun<TIn, TOut>
{
    internal readonly CommandRun InnerCommandRun;

    public static bool TryParse(ParserContext ctx, [NotNullWhen(true)] out CommandRun<TIn, TOut>? expr)
    {
        if (!CommandRun.TryParse(ctx, typeof(TIn), typeof(TOut), out var innerExpr))
        {
            expr = null;
            return false;
        }

        expr = new CommandRun<TIn, TOut>(innerExpr);
        return true;
    }

    public TOut? Invoke(object? input, IInvocationContext ctx)
    {
        var res = InnerCommandRun.Invoke(input, ctx);
        if (res is null)
            return default;
        return (TOut?) res;
    }

    internal CommandRun(CommandRun commandRun)
    {
        InnerCommandRun = commandRun;
    }

    public override string ToString()
    {
        return InnerCommandRun.ToString();
    }
}

public sealed class CommandRun<TRes>
{
    internal readonly CommandRun InnerCommandRun;

    public static bool TryParse(
        ParserContext ctx,
        Type? pipedType,
        [NotNullWhen(true)] out CommandRun<TRes>? expr)
    {
        if (!CommandRun.TryParse(ctx, pipedType, typeof(TRes), out var innerExpr))
        {
            expr = null;
            return false;
        }

        expr = new CommandRun<TRes>(innerExpr);
        return true;
    }

    public TRes? Invoke(object? input, IInvocationContext ctx)
    {
        var res = InnerCommandRun.Invoke(input, ctx);
        if (res is null)
            return default;
        return (TRes?) res;
    }

    internal CommandRun(CommandRun commandRun)
    {
        InnerCommandRun = commandRun;
    }

    public override string ToString()
    {
        return InnerCommandRun.ToString();
    }
}

public record struct WrongCommandReturn(Type Expected, Type Got) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromUnformatted(
            $"Expected an command run that returns type {Expected.PrettyName()}, but got {Got.PrettyName()}");

        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public sealed class EmptyCommandRun : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromUnformatted($"Empty command block");

        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}


public record struct MissingClosingBrace : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Expected a closing brace, }.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct MissingOpeningBrace : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Expected an opening brace, {.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public sealed class EndOfCommandError : ConError
{
    public override FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted("Expected an end of command (;)");
    }
}
