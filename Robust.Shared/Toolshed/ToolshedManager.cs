using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.Console;
using Robust.Shared.Enums;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Reflection;
using Robust.Shared.Toolshed.Errors;
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
#if !CLIENT_SCRIPTING
    [Dependency] private readonly INetManager _net = default!;
#endif
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;

    private ISawmill _log = default!;

    private Dictionary<NetUserId, OldShellInvocationContext> _contexts = new();

    /// <summary>
    ///     If you're not an engine developer, you probably shouldn't call this.
    /// </summary>
    public void Initialize()
    {
        _log = _logManager.GetSawmill("toolshed");

        InitializeParser();
        _player.PlayerStatusChanged += OnStatusChanged;
    }

    private void OnStatusChanged(object? sender, SessionStatusEventArgs e)
    {
        if (!_contexts.TryGetValue(e.Session.UserId, out var ctx))
            return;

        DebugTools.Assert(ctx.User == e.Session.UserId);
        if (e.NewStatus == SessionStatus.Disconnected)
        {
            DebugTools.Assert(ctx.Session == e.Session);
            ctx.Shell = null;
        }

        if (e.NewStatus == SessionStatus.InGame)
        {
            DebugTools.AssertNull(ctx.Session);
            DebugTools.AssertNull(ctx.Shell);
            ctx.Shell = new ConsoleShell(_conHost, e.Session, false);
        }
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
            var shell = new ConsoleShell(_conHost, session, false);
            _contexts[session.UserId] = ctx = new(shell);
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
        result = null;

        var parser = new ParserContext(command, this, ctx);
        if (!CommandRun.TryParse(parser, input?.GetType(), null, out var expr))
        {
            ctx.ReportError(parser.Error ?? new FailedToParseError());
            return false;
        }

        result = expr.Invoke(input, ctx);
        return true;
    }

    public CompletionResult? GetCompletions(ConsoleShell shell, string command)
    {
        var idx = shell.Player?.UserId ?? new NetUserId();
        if (!_contexts.TryGetValue(idx, out var ourCtx))
            ourCtx = _contexts[idx] = new OldShellInvocationContext(shell);

        return GetCompletions(ourCtx, command);
    }

    public CompletionResult? GetCompletions(IInvocationContext ctx, string command)
    {
        ctx.ClearErrors();
        var parser = new ParserContext(command, this, ctx);
        parser.GenerateCompletions = true;
        CommandRun.TryParse(parser, null, null, out _);
        return parser.Completions;
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
        return new CompletionOption(FullName(), Cmd.Description(SubCommand));
    }

    /// <summary>
    ///     Returns the full name of the command.
    /// </summary>
    public string FullName() => SubCommand == null ? Cmd.Name : $"{Cmd.Name}:{SubCommand}";

    /// <summary>
    ///     Returns the localization string for the description of this command.
    /// </summary>
    public string DescLocStr() => Cmd.DescriptionLocKey(SubCommand);

    /// <inheritdoc/>
    public override string ToString() => FullName();
}

public record struct FailedToParseError() : IConError
{
    public FormattedMessage DescribeInner()
    {
        return FormattedMessage.FromUnformatted($"Failed to parse toolshed command");
    }

    public string? Expression { get; set; }
    public Vector2i? IssueSpan { get; set; }
    public StackTrace? Trace { get; set; }
}
