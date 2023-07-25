using System.Collections.Generic;
using Robust.Shared.Console;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed.Invocation;

internal sealed class OldShellInvocationContext : IInvocationContext
{
    [Dependency] private readonly ToolshedManager _toolshed = default!;

    public bool CheckInvokable(CommandSpec command, out IConError? error)
    {
        if (_toolshed.ActivePermissionController is { } controller)
        {
            return controller.CheckInvokable(command, Session, out error);
        }

        error = null;
        return true;
    }

    public ICommonSession? Session => _shell.Player;

    private IConsoleShell _shell;
    private List<IConError> _errors = new();

    public void WriteLine(string line)
    {
        _shell.WriteLine(line);
    }

    public void WriteLine(FormattedMessage line)
    {
        _shell.WriteLine(line);
    }

    public void ReportError(IConError err)
    {
        _errors.Add(err);
    }

    public IEnumerable<IConError> GetErrors() => _errors;

    public void ClearErrors()
    {
        _errors.Clear();
    }

    public OldShellInvocationContext(IConsoleShell shell)
    {
        IoCManager.InjectDependencies(this);
        _shell = shell;
    }
}
