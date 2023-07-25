using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Robust.Shared.Console;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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

        var tys = _reflection.FindTypesWithAttribute<RtShellCommandAttribute>();
        foreach (var ty in tys)
        {
            if (!ty.IsAssignableTo(typeof(ToolshedCommand)))
            {
                _log.Error($"The type {ty.AssemblyQualifiedName} has {nameof(RtShellCommandAttribute)} without being a child of {nameof(ToolshedCommand)}");
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
        var parser = new ForwardParser(argstr[2..]);
        var ctx = new OldShellInvocationContext(shell);
        Logger.Debug("awawa");
        CommandRun.TryParse(true, parser, null, null, false, out _, out var completions, out _);
        if (completions is null)
            return CompletionResult.Empty;

        var (result, err) = await completions.Value;
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

    private void Callback(IConsoleShell shell, string argstr, string[] args)
    {
        var parser = new ForwardParser(argstr[2..]);
        var ctx = new OldShellInvocationContext(shell);
        if (!CommandRun.TryParse(false, parser, null, null, false, out var expr, out _, out var err) || parser.Index < parser.MaxIndex)
        {
            if (err is not null)
            {
                ctx.ReportError(err);
                ctx.WriteLine(err.Describe());
            }
            else
            {
                ctx.WriteLine("Got some unknown error while parsing.");
            }

            return;
        }

        var value = expr.Invoke(null, ctx);

        shell.WriteLine(FormattedMessage.FromMarkup(PrettyPrintType(value)));
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

    public override string ToString()
    {
        return Cmd.GetHelp(SubCommand);
    }
}
