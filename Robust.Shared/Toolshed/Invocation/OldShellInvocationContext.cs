using System.Collections.Generic;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Invocation;

/// <inheritdoc />
internal sealed class OldShellInvocationContext : IInvocationContext
{
    [field: Dependency]
    public ToolshedManager Toolshed { get; } = default!;

    public ToolshedEnvironment Environment => Toolshed.DefaultEnvironment;

    private readonly List<IConError> _errors = new();

    /// <summary>
    ///     Old system's shell associated with this context. May be null if the player is currently disconnected.
    /// </summary>
    public IConsoleShell? Shell;

    public OldShellInvocationContext(IConsoleShell shell)
    {
        IoCManager.InjectDependencies(this);
        Shell = shell;
        User = Session?.UserId;
    }

    /// <inheritdoc />
    public NetUserId? User { get; }

    /// <inheritdoc />
    public ICommonSession? Session => Shell?.Player;

    /// <inheritdoc />
    public void WriteLine(string line)
    {
        Shell?.WriteLine(line);
    }

    /// <inheritdoc />
    public void WriteLine(FormattedMessage line)
    {
        Shell?.WriteLine(line);
    }

    /// <inheritdoc />
    public void ReportError(IConError err)
    {
        _errors.Add(err);
    }

    /// <inheritdoc />
    public IEnumerable<IConError> GetErrors()
    {
        return _errors;
    }

    /// <inheritdoc />
    public void ClearErrors()
    {
        _errors.Clear();
    }

    /// <inheritdoc />
    public Dictionary<string, object?> Variables { get; } = new();
}

