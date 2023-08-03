using System;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Syntax;

using Invocable = Func<CommandInvocationArguments, object?>;

public sealed class ParsedCommand
{
    public ToolshedCommand Command { get; }
    public Type? ReturnType { get; }

    public Type? PipedType => Bundle.PipedArgumentType;
    internal Invocable Invocable { get; }
    private CommandArgumentBundle Bundle { get; }
    public string? SubCommand { get; }

    public static bool TryParse(
            bool doAutoComplete,
            ForwardParser parser,
            Type? pipedArgumentType,
            [NotNullWhen(true)] out ParsedCommand? result,
            out IConError? error,
            out bool noCommand,
            out ValueTask<(CompletionResult?, IConError?)>? autocomplete,
            Type? targetType = null
        )
    {
        noCommand = false;
        var checkpoint = parser.Save();
        var bundle = new CommandArgumentBundle()
            {Arguments = new(), Inverted = false, PipedArgumentType = pipedArgumentType, TypeArguments = Array.Empty<Type>()};

        autocomplete = null;
        if (!TryDigestModifiers(parser, bundle, out error)
            || !TryParseCommand(doAutoComplete, parser, bundle, pipedArgumentType, targetType, out var subCommand, out var invocable, out var command, out error, out noCommand, out autocomplete)
            || !command.TryGetReturnType(subCommand, pipedArgumentType, bundle.TypeArguments, out var retType)
            )
        {
            result = null;
            parser.Restore(checkpoint);
            return false;
        }


        result = new(bundle, invocable, command, retType, subCommand);
        return true;
    }

    private ParsedCommand(CommandArgumentBundle bundle, Invocable invocable, ToolshedCommand command, Type? returnType, string? subCommand)
    {
        Invocable = invocable;
        Bundle = bundle;
        Command = command;
        ReturnType = returnType;
        SubCommand = subCommand;
    }

    private static bool TryDigestModifiers(ForwardParser parser, CommandArgumentBundle bundle, out IConError? error)
    {
        error = null;
        if (parser.PeekWord() == "not")
        {
            parser.GetWord(); //yum
            bundle.Inverted = true;
        }

        return true;
    }

    private static bool TryParseCommand(
                bool makeCompletions,
                ForwardParser parser,
                CommandArgumentBundle bundle,
                Type? pipedType,
                Type? targetType,
                out string? subCommand,
                [NotNullWhen(true)] out Invocable? invocable,
                [NotNullWhen(true)] out ToolshedCommand? command,
                out IConError? error,
                out bool noCommand,
                out ValueTask<(CompletionResult?, IConError?)>? autocomplete
            )
    {
        noCommand = false;
        var start = parser.Index;
        var cmd = parser.GetWord(c => c is not ':' and not '{' and not '}' && !char.IsWhiteSpace(c));
        subCommand = null;
        invocable = null;
        command = null;
        if (cmd is null)
        {
            if (parser.PeekChar() is null)
            {
                noCommand = true;
                error = new OutOfInputError();
                error.Contextualize(parser.Input, (parser.Index, parser.Index));
                autocomplete = null;
                if (makeCompletions)
                {
                    var cmds = parser.Toolshed.CommandsTakingType(pipedType ?? typeof(void));
                    autocomplete = ValueTask.FromResult<(CompletionResult?, IConError?)>((CompletionResult.FromHintOptions(cmds.Select(x => x.AsCompletion()), "<command>"), error));
                }

                return false;
            }
            else
            {

                noCommand = true;
                error = new NotValidCommandError(targetType);
                error.Contextualize(parser.Input, (start, parser.Index));
                autocomplete = null;
                return false;
            }
        }

        if (!parser.Toolshed.TryGetCommand(cmd, out var cmdImpl))
        {
            error = new UnknownCommandError(cmd);
            error.Contextualize(parser.Input, (start, parser.Index));
            autocomplete = null;
            if (makeCompletions)
            {
                var cmds = parser.Toolshed.CommandsTakingType(pipedType ?? typeof(void));
                autocomplete = ValueTask.FromResult<(CompletionResult?, IConError?)>((CompletionResult.FromHintOptions(cmds.Select(x => x.AsCompletion()), "<command>"), error));
            }

            return false;
        }

        if (cmdImpl.HasSubCommands)
        {
            error = null;
            autocomplete = null;
            if (makeCompletions)
            {
                var cmds = parser.Toolshed.CommandsTakingType(pipedType ?? typeof(void)).Where(x => x.Cmd.Name == cmd);
                autocomplete = ValueTask.FromResult<(CompletionResult?, IConError?)>((
                    CompletionResult.FromHintOptions(cmds.Select(x => x.AsCompletion()), "<command>"), error));
            }

            if (parser.GetChar() is not ':')
            {
                error = new OutOfInputError();
                error.Contextualize(parser.Input, (parser.Index, parser.Index));
                return false;
            }

            var subCmdStart = parser.Index;

            if (parser.GetWord() is not { } subcmd)
            {
                error = new OutOfInputError();
                error.Contextualize(parser.Input, (parser.Index, parser.Index));
                return false;
            }

            if (!cmdImpl.Subcommands.Contains(subcmd))
            {
                error = new UnknownSubcommandError(cmd, subcmd, cmdImpl);
                error.Contextualize(parser.Input, (subCmdStart, parser.Index));
                return false;
            }

            subCommand = subcmd;
        }

        if (parser.Consume(char.IsWhiteSpace) == 0 && makeCompletions)
        {
            error = null;
            var cmds = parser.Toolshed.CommandsTakingType(pipedType ?? typeof(void));
            autocomplete = ValueTask.FromResult<(CompletionResult?, IConError?)>((CompletionResult.FromHintOptions(cmds.Select(x => x.AsCompletion()), "<command>"), null));
            return false;
        }

        var argsStart = parser.Index;

        if (!cmdImpl.TryParseArguments(makeCompletions, parser, pipedType, subCommand, out var args, out var types, out error, out autocomplete))
        {
            error?.Contextualize(parser.Input, (argsStart, parser.Index));
            return false;
        }

        bundle.TypeArguments = types;

        if (!cmdImpl.TryGetImplementation(bundle.PipedArgumentType, subCommand, types, out var impl))
        {
            error = new NoImplementationError(cmd, types, subCommand, bundle.PipedArgumentType);
            error.Contextualize(parser.Input, (start, parser.Index));
            autocomplete = null;
            return false;
        }

        bundle.Arguments = args;
        invocable = impl;
        command = cmdImpl;
        autocomplete = null;
        return true;
    }

    private bool _passedInvokeTest = false;

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
        return FormattedMessage.FromMarkup($"Got unknown command {Cmd}.");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct NoImplementationError(string Cmd, Type[] Types, string? SubCommand, Type? PipedType) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var newCon = IoCManager.Resolve<ToolshedManager>();

        var msg = FormattedMessage.FromMarkup($"Could not find an implementation for {Cmd} given the input type {PipedType?.PrettyName() ?? "void"}.");
        msg.PushNewline();

        var typeArgs = "";

        if (Types.Length != 0)
        {
            typeArgs = "<" + string.Join(",", Types.Select(ReflectionExtensions.PrettyName)) + ">";
        }

        msg.AddText($"Signature: {Cmd}{(SubCommand is not null ? $":{SubCommand}" : "")}{typeArgs} {PipedType?.PrettyName() ?? "void"} -> ???");

        var piped = PipedType ?? typeof(void);
        var cmdImpl = newCon.GetCommand(Cmd);
        var accepted = cmdImpl.AcceptedTypes(SubCommand).ToHashSet();

        foreach (var (command, subCommand) in newCon.CommandsTakingType(piped))
        {
            if (!command.TryGetReturnType(subCommand, piped, Array.Empty<Type>(), out var retType) || !accepted.Any(x => retType.IsAssignableTo(x)))
                continue;

            if (!cmdImpl.TryGetReturnType(SubCommand, retType, Types, out var myRetType))
                continue;

            msg.PushNewline();
            msg.AddText($"The command {command.Name}{(subCommand is not null ? $":{subCommand}" : "")} can convert from {piped.PrettyName()} to {retType.PrettyName()}.");
            msg.PushNewline();
            msg.AddText($"With this fix, the new signature will be: {Cmd}{(SubCommand is not null ? $":{SubCommand}" : "")}{typeArgs} {retType?.PrettyName() ?? "void"} -> {myRetType?.PrettyName() ?? "void"}.");
        }

        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct UnknownSubcommandError(string Cmd, string SubCmd, ToolshedCommand Command) : IConError
{
    public FormattedMessage DescribeInner()
    {
        var msg = new FormattedMessage();
        msg.AddText($"The command group {Cmd} doesn't have command {SubCmd}.");
        msg.PushNewline();
        msg.AddText($"The valid commands are: {string.Join(", ", Command.Subcommands)}.");
        return msg;
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}

public record struct NotValidCommandError(Type? TargetType) : IConError
{
    public FormattedMessage DescribeInner()
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

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
