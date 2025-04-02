using System.Collections.Generic;
using System.Linq;
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

    /// <inheritdoc />
    public NetUserId? User { get; }

    /// <inheritdoc />
    public ICommonSession? Session => Shell?.Player;

    public OldShellInvocationContext(IConsoleShell shell)
    {
        IoCManager.InjectDependencies(this);
        Shell = shell;
        User = Session?.UserId;
    }

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

    public bool HasErrors => _errors.Count > 0;

    /// <inheritdoc />
    public void ClearErrors()
    {
        _errors.Clear();
    }

    /// <inheritdoc />
    public object? ReadVar(string name)
    {
        if (name == "self" && Session?.AttachedEntity is { } ent)
            return ent;
        return Variables.GetValueOrDefault(name);
    }

    /// <inheritdoc />
    public void WriteVar(string name, object? value)
    {
        if (name == "self")
            ReportError(new ReadonlyVariableError("self"));
        else
            Variables[name] = value;
    }

    /// <inheritdoc />
    public bool IsReadonlyVar(string name) => name == "self";

    /// <inheritdoc />
    public IEnumerable<string> GetVars()
    {
        return Session?.AttachedEntity != null
            ? Variables.Keys.Append("self")
            : Variables.Keys;
    }

    public Dictionary<string, object?> Variables { get; } = new();
}

