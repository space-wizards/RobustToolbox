using System.Collections.Generic;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Toolshed.Errors;

namespace Robust.Shared.Toolshed;

/// <summary>
/// <see cref="IInvocationContext"/> that wraps some other context and provides some local variables.
/// </summary>
public sealed class LocalVarInvocationContext(IInvocationContext inner) : IInvocationContext
{
    public bool CheckInvokable(CommandSpec command, out IConError? error)
    {
        return inner.CheckInvokable(command, out error);
    }

    public ICommonSession? Session => inner.Session;
    public ToolshedManager Toolshed => inner.Toolshed;
    public NetUserId? User => inner.User;
    public ToolshedEnvironment Environment => inner.Environment;

    public void WriteLine(string line) => inner.WriteLine(line);
    public void ReportError(IConError err) => inner.ReportError(err);
    public IEnumerable<IConError> GetErrors() => inner.GetErrors();
    public bool HasErrors => inner.HasErrors;
    public void ClearErrors() => inner.ClearErrors();

    public Dictionary<string, object?> LocalVars = new();
    public HashSet<string>? ReadonlyVars;

    public void SetLocal(string name, object? value)
    {
        LocalVars[name] = value;
    }

    public void SetLocal(string name, object? value, bool @readonly)
    {
        LocalVars[name] = value;
        SetReadonly(name, @readonly);
    }

    public void SetReadonly(string name, bool @readonly)
    {
        if (@readonly)
        {
            ReadonlyVars ??= new();
            ReadonlyVars.Add(name);
        }
        else
        {
            ReadonlyVars?.Remove(name);
        }
    }

    public void ClearLocal(string name)
    {
        LocalVars.Remove(name);
        ReadonlyVars?.Remove(name);
    }

    public object? ReadVar(string name)
    {
        return LocalVars.TryGetValue(name, out var obj)
            ? obj
            : inner.ReadVar(name);
    }

    public void WriteVar(string name, object? value)
    {
        if (ReadonlyVars != null && ReadonlyVars.Contains(name))
        {
            ReportError(new ReadonlyVariableError(name));
            return;
        }

        if (LocalVars.ContainsKey(name))
            LocalVars[name] = value;
        else
            inner.WriteVar(name, value);
    }

    public bool IsReadonlyVar(string name) => ReadonlyVars != null && ReadonlyVars.Contains(name);

    public IEnumerable<string> GetVars()
    {
        foreach (var key in LocalVars.Keys)
        {
            yield return key;
        }

        foreach (var key in inner.GetVars())
        {
            if (!LocalVars.ContainsKey(key))
                yield return key;
        }
    }
}
