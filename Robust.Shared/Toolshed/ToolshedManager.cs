using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed.Invocation;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

public sealed partial class ToolshedManager
{
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IDynamicTypeFactoryInternal _typeFactory = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ILogManager _logManager = default!;

    private ISawmill _log = default!;

    private readonly Dictionary<string, ToolshedCommand> _commands = new();

    public void Initialize()
    {
        _log = _logManager.GetSawmill("toolshed");
        var watch = new Stopwatch();
        watch.Start();

        var tys = _reflection.FindTypesWithAttribute<ToolshedCommandAttribute>();
        foreach (var ty in tys)
        {
            if (!ty.IsAssignableTo(typeof(ToolshedCommand)))
            {
                _log.Error($"The type {ty.AssemblyQualifiedName} has {nameof(ToolshedCommandAttribute)} without being a child of {nameof(ToolshedCommand)}");
                continue;
            }

            var command = (ToolshedCommand)Activator.CreateInstance(ty)!;
            IoCManager.InjectDependencies(command);

            _commands.Add(command.Name, command);
        }

        InitializeParser();
        InitializeQueries();

        _conHost.RegisterCommand("|", Callback, CompletionCallback);
        _log.Info($"Initialized console in {watch.Elapsed}");
    }

    private async ValueTask<CompletionResult> CompletionCallback(IConsoleShell shell, string[] args, string argstr)
    {
        var parser = new ForwardParser(argstr[2..], this);

        CommandRun.TryParse(true, parser, null, null, false, out _, out var completions, out _);
        if (completions is null)
            return CompletionResult.Empty;

        var (result, _) = await completions.Value;
        if (result is null)
            return CompletionResult.Empty;

        return result;
    }

    public IEnumerable<CommandSpec> AllCommands()
    {
        foreach (var (_, cmd) in _commands)
        {
            if (cmd.HasSubCommands)
            {
                foreach (var subcommand in cmd.Subcommands)
                {
                    yield return new(cmd, subcommand);
                }
            }
            else
            {
                yield return new(cmd, null);
            }
        }
    }

    public ToolshedCommand GetCommand(string commandName) => _commands[commandName];

    public bool TryGetCommand(string commandName, [NotNullWhen(true)] out ToolshedCommand? command)
    {
        return _commands.TryGetValue(commandName, out command);
    }

    private Dictionary<NetUserId, IInvocationContext> _contexts = new();

    private void Callback(IConsoleShell shell, string argstr, string[] args)
    {
        var uid = shell.Player?.UserId ?? new NetUserId();
        if (!_contexts.TryGetValue(uid, out var ctx))
        {
            ctx = new OldShellInvocationContext(shell);
            _contexts[uid] = ctx;
        }

        if (!InvokeCommand(ctx, argstr[2..], null, out var result))
        {
            var errs = ctx.GetErrors().ToList();
            if (errs.Count == 0)
            {
                ctx.WriteLine("Got some unknown error when trying to invoke the command. This is probably a bug!");
            }
            else
            {
                foreach (var err in errs)
                {
                    ctx.WriteLine(err.Describe());
                }
            }

            ctx.ClearErrors();
        }

        shell.WriteLine(FormattedMessage.FromMarkup(PrettyPrintType(result)));
    }

    public bool InvokeCommand(IInvocationContext ctx, string command, object? input, out object? result)
    {
        var parser = new ForwardParser(command, this);
        if (!CommandRun.TryParse(false, parser, input?.GetType(), null, false, out var expr, out _, out var err) || parser.Index < parser.MaxIndex)
        {

            if (err is not null)
                ctx.ReportError(err);

            result = null;
            return false;
        }

        result = expr.Invoke(input, ctx);
        return true;
    }
}

public readonly record struct CommandSpec(ToolshedCommand Cmd, string? SubCommand) : IAsType<ToolshedCommand>
{
    public ToolshedCommand AsType()
    {
        return Cmd;
    }

    public CompletionOption AsCompletion()
    {
        return new CompletionOption(
                $"{Cmd.Name}{(SubCommand is not null ? ":" + SubCommand : "")}",
                Cmd.Description(SubCommand)
            );
    }

    public string FullName() => $"{Cmd.Name}{(SubCommand is not null ? ":" + SubCommand : "")}";

    public override string ToString()
    {
        return Cmd.GetHelp(SubCommand);
    }
}
