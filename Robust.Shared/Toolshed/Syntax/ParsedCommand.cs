using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Console;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

using Invocable = Func<CommandInvocationArguments, object?>;

public sealed class ParsedCommand
{
    public ToolshedCommand Command => Implementor.Owner;
    public Type ReturnType => Method.Info.ReturnType;

    public Type? PipedType => Bundle.PipedType;
    public string? SubCommand => Bundle.SubCommand;

    internal readonly ToolshedCommandImplementor Implementor;
    internal Invocable Invocable { get; }
    internal CommandArgumentBundle Bundle { get; }

    internal readonly ConcreteCommandMethod Method;

    public static bool TryParse(ParserContext ctx, Type? piped, [NotNullWhen(true)] out ParsedCommand? result)
    {
        var checkpoint = ctx.Save();
        var oldBundle = ctx.Bundle;
        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);
        ctx.Bundle = new CommandArgumentBundle
        {
            Inverted = false,
            PipedType = piped
        };

        ctx.ConsumeWhitespace();

        if (!TryDigestModifiers(ctx))
        {
            result = null;
            ctx.Restore(checkpoint);
            return false;
        }

        // TODO TOOLSHED
        // completion suggestions for modifiers?
        // I.e., if parsing a command name fails, we should take into account that they might be trying to type out
        // "not" or some other command modifier?

        if (!TryParseCommand(ctx, out var invocable, out var method, out var implementor))
        {
            result = null;
            ctx.Restore(checkpoint);
            return false;
        }

        // No errors or completions should have been generated if the parse was successful.
        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);
        result = new(ctx.Bundle, invocable, method.Value, implementor);
        ctx.Bundle = oldBundle;
        return true;
    }

    private ParsedCommand(CommandArgumentBundle bundle, Invocable invocable, ConcreteCommandMethod method, ToolshedCommandImplementor implementor)
    {
        Invocable = invocable;
        Bundle = bundle;
        Implementor = implementor;
        Method = method;
    }

    /// <summary>
    /// Attempt to process any modifer tokens that modify how a command behaves or how it's arguments are parsed and
    /// store the results in the <see cref="CommandArgumentBundle"/>.
    /// </summary>
    private static bool TryDigestModifiers(ParserContext ctx)
    {
        if (ctx.EatMatch("not"))
        {
            ctx.ConsumeWhitespace();
            ctx.Bundle.Inverted = true;
        }

        return true;
    }

    private static bool TryParseCommand(
        ParserContext ctx,
        [NotNullWhen(true)] out Invocable? invocable,
        [NotNullWhen(true)] out ConcreteCommandMethod? method,
        [NotNullWhen(true)] out ToolshedCommandImplementor? implementor)
    {
        invocable = null;
        implementor = null;
        method = null;
        var cmdNameStart = ctx.Index;
        DebugTools.AssertNull(ctx.Error);
        DebugTools.AssertNull(ctx.Completions);

        // Try to parse the command name
        if (!TryParseCommandName(ctx, out var cmdName))
            return false;

        // Attempt to find the command with the given name
        if (!ctx.Environment.TryGetCommand(cmdName, out var command))
        {
            if (ctx.GenerateCompletions)
            {
                if (ctx.OutOfInput)
                    ctx.Completions = ctx.Environment.CommandCompletionsForType(ctx.Bundle.PipedType);
                return false;
            }

            ctx.Error ??= new UnknownCommandError(cmdName);
            ctx.Error.Contextualize(ctx.Input, (cmdNameStart, ctx.Index));
            return false;
        }

        // Attempt to parse the subcommand, if applicable.
        if (!TryParseImplementor(ctx, command, out implementor))
            return false;

        // This is a safeguard to try help prevent information from being accidentally leaked by poorly validated
        // auto completion for commands. I.e., if there is a command that operates on all minds/players, we don't want
        // to send the client a list of all players.
        if (!ctx.CheckInvokable(implementor.Spec))
        {
            if (ctx.GenerateCompletions)
                ctx.Completions = CompletionResult.FromHint($"Insufficient permissions for command: {implementor.FullName}");
            return false;
        }

        // If the name command is currently still being typed, we continue to give command name completions, not
        // argument completions.
        if (ctx.GenerateCompletions && ctx.OutOfInput)
        {
            ctx.Completions = ctx.Bundle.SubCommand == null
                ? ctx.Environment.CommandCompletionsForType(ctx.Bundle.PipedType)
                : ctx.Environment.SubCommandCompletionsForType(ctx.Bundle.PipedType, command);

            // TODO TOOLSHED invalid-fail
            // This technically "fails" to parse what might otherwise be a valid command that takes no argument.
            // However this only happens when generating completions, not when actually executing the command
            // Still, this is pretty janky and I don't know of a good fix.
            return false;
        }

        return implementor.TryParse(ctx, out invocable, out method);
    }

    private static bool TryParseCommandName(ParserContext ctx, [NotNullWhen(true)] out string? name)
    {
        ctx.Bundle.NameStart = ctx.Index;
        name = ctx.GetWord(ParserContext.IsCommandToken);
        if (name != null)
        {
            ctx.Bundle.Command = name;
            ctx.Bundle.NameEnd = ctx.Index;
            return true;
        }

        if (ctx.OutOfInput)
        {
            if (ctx.GenerateCompletions)
            {
                ctx.Completions = ctx.Environment.CommandCompletionsForType(ctx.Bundle.PipedType);
            }
            else
            {
                ctx.Error = new OutOfInputError();
                ctx.Error.Contextualize(ctx.Input, (ctx.Index, ctx.Index));
            }

            return false;
        }

        if (ctx.GenerateCompletions)
            return false;

        ctx.Error = new NotValidCommandError();
        ctx.Error.Contextualize(ctx.Input, (ctx.Bundle.NameStart, ctx.Index+1));
        return false;
    }

    private static bool TryParseImplementor(ParserContext ctx, ToolshedCommand cmd, [NotNullWhen(true)] out ToolshedCommandImplementor? impl)
    {
        if (!cmd.HasSubCommands)
        {
            impl = cmd.CommandImplementors[string.Empty];
            return true;
        }

        impl = null;
        if (!ctx.EatMatch(':'))
        {
            if (ctx.GenerateCompletions)
            {
                ctx.Completions = ctx.Environment.SubCommandCompletionsForType(ctx.Bundle.PipedType, cmd);
                return false;
            }
            ctx.Error = new OutOfInputError();
            ctx.Error.Contextualize(ctx.Input, (ctx.Index, ctx.Index));
            return false;
        }

        var subCmdStart = ctx.Index;
        if (ctx.GetWord(ParserContext.IsToken) is not { } subcmd)
        {
            if (ctx.GenerateCompletions)
            {
                ctx.Completions = ctx.Environment.SubCommandCompletionsForType(ctx.Bundle.PipedType, cmd);
                return false;
            }
            ctx.Error = new OutOfInputError();
            ctx.Error.Contextualize(ctx.Input, (ctx.Index, ctx.Index));
            return false;
        }

        if (!cmd.CommandImplementors.TryGetValue(subcmd, out impl!))
        {
            if (ctx.GenerateCompletions)
            {
                ctx.Completions = ctx.Environment.SubCommandCompletionsForType(ctx.Bundle.PipedType, cmd);
                return false;
            }
            ctx.Error = new UnknownSubcommandError(subcmd, cmd);
            ctx.Error.Contextualize(ctx.Input, (subCmdStart, ctx.Index));
            return false;
        }

        ctx.Bundle.NameEnd = ctx.Index;
        ctx.Bundle.SubCommand = subcmd;
        return true;
    }

    private bool _passedInvokeTest;

    public object? Invoke(object? pipedIn, IInvocationContext ctx)
    {
        if (!_passedInvokeTest && !ctx.CheckInvokable(new CommandSpec(Command, SubCommand), out var error))
        {
            // Could not invoke the command for whatever reason, i.e. permission errors.
            if (error is not null)
                ctx.ReportError(error);
            return null;
        }

        // TODO: This optimization might be dangerous if blocks can be passed to other people through vars.
        // Or not if it can only be done deliberately, but social engineering is a thing.
        _passedInvokeTest = true;

        try
        {
            return Invocable.Invoke(new CommandInvocationArguments()
                {Bundle = Bundle, PipedArgument = pipedIn, Context = ctx});
        }
        catch (Exception e)
        {
            ctx.ReportError(new UnhandledExceptionError(e));
            return null;
        }
    }
}

public record struct UnknownCommandError(string Cmd) : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Got unknown command {Cmd}.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public sealed class NoImplementationError(ParserContext ctx) : ConError
{
    public readonly ToolshedEnvironment Env = ctx.Environment;
    public readonly string Cmd = ctx.Bundle.Command!;
    public readonly string? SubCommand = ctx.Bundle.SubCommand;
    public readonly Type[]? Types = ctx.Bundle.TypeArguments;
    public readonly Type? PipedType = ctx.Bundle.PipedType;

    public override FormattedMessage DescribeInner()
    {
        var msg = FormattedMessage.FromUnformatted($"Could not find an implementation of the '{Cmd}' command given the input type '{PipedType?.PrettyName() ?? "void"}'.\n");

        var cmdImpl = Env.GetCommand(Cmd);
        var accepted = cmdImpl.AcceptedTypes(SubCommand);

        // If one of the signatures just takes T Or IEnumerable<T> we just don't print anything, as it doesn't provide any useful information.
        // TODO TOOLSHED list accepted generic types
        var isGeneric = accepted.Any(x => x.IsGenericParameter);
        if (isGeneric)
            return msg;

        var isGenericEnumerable = accepted.Any(x=> x.IsGenericType
                                             && x.GetGenericTypeDefinition() == typeof(IEnumerable<>)
                                             && x.GetGenericArguments()[0].IsGenericParameter);
        if (isGenericEnumerable)
            return msg;

        msg.AddText($"Accepted types: '{string.Join("','", accepted.Select(x => x.PrettyName()))}'.\n");
        return msg;
    }
}

public record UnknownSubcommandError(string SubCmd, ToolshedCommand Command) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText($"The command group {Command.Name} doesn't have command {SubCmd}.");
        msg.PushNewline();
        msg.AddText($"The valid commands are: {string.Join(", ", Command.Subcommands)}.");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public sealed class NotValidCommandError : ConError
{
    public Type? TargetType;

    public override FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText("Ran into an invalid command, could not parse.");
        if (TargetType is not null && TargetType != typeof(void))
        {
            msg.PushNewline();
            msg.AddText($"The parser was trying to obtain a run of type {TargetType.PrettyName()}, make sure your run actually returns that value.");
        }

        return msg;
    }
}
