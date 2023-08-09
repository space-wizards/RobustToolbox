#pragma warning restore CS1591

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
using Robust.Shared.Players;
using Robust.Shared.Reflection;
using Robust.Shared.Timing;
using Robust.Shared.Toolshed.Invocation;
using Robust.Shared.Toolshed.Syntax;
using Robust.Shared.Toolshed.TypeParsers;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

/// <summary>
///     The overarching controller for Toolshed, providing invocation, reflection, commands, parsing, and other tools used by the language.
///     <see href="https://docs.spacestation14.io/">External documentation</see> has a more in-depth look.
/// </summary>
/// <seealso cref="ToolshedCommand"/>
/// <seealso cref="IInvocationContext"/>
public sealed partial class ToolshedManager
{
    [Dependency] private readonly IDynamicTypeFactoryInternal _typeFactory = default!;
    [Dependency] private readonly IEntityManager _entity = default!;
    [Dependency] private readonly IReflectionManager _reflection = default!;
    [Dependency] private readonly ILogManager _logManager = default!;
    [Dependency] private readonly INetManager _net = default!;

    private ISawmill _log = default!;

    private readonly Dictionary<string, ToolshedCommand> _commands = new();

    /// <summary>
    ///     If you're not an engine developer, you probably shouldn't call this.
    /// </summary>
    public void Initialize()
    {
#if !CLIENT_SCRIPTING
        if (_net.IsClient)
            throw new NotImplementedException("Toolshed is not yet ready for client-side use.");
#endif

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

        _log.Info($"Initialized console in {watch.Elapsed}");
    }

    /// <summary>
    ///     Provides every registered command, including subcommands.
    /// </summary>
    /// <returns>Enumerable of every command.</returns>
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

    /// <summary>
    ///     Gets a command's object by name.
    /// </summary>
    /// <param name="commandName">Command to get.</param>
    /// <returns>A command object.</returns>
    /// <exception cref="IndexOutOfRangeException">Thrown when there is no command of the given name.</exception>
    public ToolshedCommand GetCommand(string commandName) => _commands[commandName];

    /// <summary>
    ///     Attempts to get a command's object by name.
    /// </summary>
    /// <param name="commandName">Command to get.</param>
    /// <param name="command">The command obtained, if any.</param>
    /// <returns>Success.</returns>
    public bool TryGetCommand(string commandName, [NotNullWhen(true)] out ToolshedCommand? command)
    {
        return _commands.TryGetValue(commandName, out command);
    }

    private Dictionary<NetUserId, IInvocationContext> _contexts = new();

    /// <summary>
    ///     Invokes a command as the given user.
    /// </summary>
    /// <param name="session">User to run as.</param>
    /// <param name="command">Command to invoke.</param>
    /// <param name="input">An input value to use, if any.</param>
    /// <param name="result">The resulting value, if any.</param>
    /// <returns>Invocation success.</returns>
    /// <example><code>
    ///     ToolshedManager toolshed = ...;
    ///     ICommonSession ctx = ...;
    ///     // Now run some user provided command and get a result!
    ///     toolshed.InvokeCommand(ctx, userCommand, "my input value", out var result);
    /// </code></example>
    /// <remarks>
    ///     This will use the same IInvocationContext as the one used by the user for debug console commands.
    /// </remarks>
    public bool InvokeCommand(ICommonSession session, string command, object? input, out object? result)
    {
        if (!_contexts.TryGetValue(session.UserId, out var ctx))
        {
            // Can't get a shell here.
            result = null;
            return false;
        }

        ctx.ClearErrors();

        return InvokeCommand(ctx, command, input, out result);
    }

    /// <summary>
    ///     Invokes a command as the given user.
    /// </summary>
    /// <param name="session">User to run as.</param>
    /// <param name="command">Command to invoke.</param>
    /// <param name="input">An input value to use, if any.</param>
    /// <param name="result">The resulting value, if any.</param>
    /// <returns>Invocation success.</returns>
   /// <example><code>
   ///     ToolshedManager toolshed = ...;
   ///     IConsoleShell ctx = ...;
   ///     // Now run some user provided command and get a result!
   ///     toolshed.InvokeCommand(ctx, userCommand, "my input value", out var result);
   /// </code></example>
    /// <remarks>
    ///     This will use the same IInvocationContext as the one used by the user for debug console commands.
    /// </remarks>
    public bool InvokeCommand(IConsoleShell session, string command, object? input, out object? result, out IInvocationContext ctx)
    {
        var idx = session.Player?.UserId ?? new NetUserId();
        if (!_contexts.TryGetValue(idx, out var ourCtx))
        {
            ourCtx = new OldShellInvocationContext(session);
            _contexts[idx] = ourCtx;
        }

        ourCtx.ClearErrors();
        ctx = ourCtx;

        return InvokeCommand(ctx, command, input, out result);
    }


    /// <summary>
    ///     Invokes a command with the given context.
    /// </summary>
    /// <param name="ctx">The context to run in.</param>
    /// <param name="command">Command to invoke.</param>
    /// <param name="input">An input value to use, if any.</param>
    /// <param name="result">The resulting value, if any.</param>
    /// <returns>Invocation success.</returns>
    /// <example><code>
    ///     ToolshedManager toolshed = ...;
    ///     IInvocationContext ctx = ...;
    ///     // Now run some user provided command and get a result!
    ///     toolshed.InvokeCommand(ctx, userCommand, "my input value", out var result);
    /// </code></example>
    public bool InvokeCommand(IInvocationContext ctx, string command, object? input, out object? result)
    {
        ctx.ClearErrors();

        var parser = new ForwardParser(command, this);
        if (!CommandRun.TryParse(false, false, parser, input?.GetType(), null, false, out var expr, out _, out var err) || parser.Index < parser.MaxIndex)
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

/// <summary>
///     A command specification, containing both the command object and the selected subcommand if any.
/// </summary>
/// <param name="Cmd">Command object.</param>
/// <param name="SubCommand">Subcommand, if any.</param>
public readonly record struct CommandSpec(ToolshedCommand Cmd, string? SubCommand) : IAsType<ToolshedCommand>
{
    /// <inheritdoc/>
    public ToolshedCommand AsType()
    {
        return Cmd;
    }

    /// <summary>
    ///     Returns a completion option for this command, for use in autocomplete.
    /// </summary>
    public CompletionOption AsCompletion()
    {
        return new CompletionOption(
                $"{Cmd.Name}{(SubCommand is not null ? ":" + SubCommand : "")}",
                Cmd.Description(SubCommand)
            );
    }

    /// <summary>
    ///     Returns the full name of the command.
    /// </summary>
    public string FullName() => $"{Cmd.Name}{(SubCommand is not null ? ":" + SubCommand : "")}";

    /// <summary>
    ///     Returns the localization string for the description of this command.
    /// </summary>
    public string DescLocStr() => Cmd.UnlocalizedDescription(SubCommand);

    /// <inheritdoc/>
    public override string ToString()
    {
        return Cmd.GetHelp(SubCommand);
    }
}
