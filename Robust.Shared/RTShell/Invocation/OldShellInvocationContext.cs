using System.Collections.Generic;
using Robust.Shared.Console;
using Robust.Shared.Players;
using Robust.Shared.RTShell.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.RTShell.Invocation;

internal sealed class OldShellInvocationContext : IInvocationContext
{
    public bool CheckInvokable(ConsoleCommand command, string? subCommand, out IConError? error)
    {
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
        _shell = shell;
    }
}
