using System.Collections.Generic;
using Robust.Shared.Players;
using Robust.Shared.Toolshed.Errors;
using Robust.Shared.Utility;

namespace Robust.Shared.Toolshed;

public interface IInvocationContext
{
    public bool CheckInvokable(CommandSpec command, out IConError? error);

    ICommonSession? Session { get; }

    public void WriteLine(string line);

    public void WriteLine(FormattedMessage line)
    {
        // Cut markup for server.
        if (Session is null)
        {
            WriteLine(line.ToString());
            return;
        }

        WriteLine(line.ToMarkup());
    }

    public void WriteMarkup(string markup)
    {
        WriteLine(FormattedMessage.FromMarkup(markup));
    }

    public void WriteError(IConError error)
    {
        WriteLine(error.Describe());
    }

    public void ReportError(IConError err);

    public IEnumerable<IConError> GetErrors();

    public void ClearErrors();

    protected Dictionary<string, object?> Variables { get; }

    public object? ReadVar(string name)
    {
        Variables.TryGetValue(name, out var res);
        return res;
    }

    public void WriteVar(string name, object? value)
    {
        Variables[name] = value;
    }
}
