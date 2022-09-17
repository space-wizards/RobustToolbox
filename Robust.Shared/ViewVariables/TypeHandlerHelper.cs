using System;
using System.Collections.Generic;

namespace Robust.Shared.ViewVariables;

public sealed class TypeHandlerHelper<T>
{
    private readonly Dictionary<Type, ViewVariablesManager.TypeHandlerData> _handlers = new();
    private readonly Dictionary<string, Func<T?, ViewVariablesFakePath?>> _paths = new();

    public TypeHandlerHelper()
    {
    }

    public TypeHandlerHelper(Dictionary<string, Func<T?, ViewVariablesFakePath?>> paths)
    {
        _paths = paths;
    }

    internal ViewVariablesPath? HandlePath(T? t, string relativePath)
    {
        return _paths.TryGetValue(relativePath, out var handler)
            ? handler(t)
            : null;
    }

    internal IEnumerable<string> ListPath(T? t)
    {
        foreach (var (path, handler) in _paths)
        {
            if (handler(t) is {})
                yield return path;
        }
    }

    public bool AddPath(string path, Func<T?, ViewVariablesFakePath?> handler)
    {
        if (_paths.ContainsKey(path))
            return false;

        _paths.Add(path, handler);

        return true;
    }

    public bool RemovePath(string path)
    {
        return _paths.Remove(path);
    }
}
