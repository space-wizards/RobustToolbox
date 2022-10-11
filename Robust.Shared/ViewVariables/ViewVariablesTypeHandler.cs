using System;
using System.Collections.Generic;

namespace Robust.Shared.ViewVariables;

public delegate ViewVariablesPath? HandleTypePath<in T>(T? obj, string relativePath);
public delegate IEnumerable<string> ListTypeCustomPaths<in T>(T? obj);

public abstract class ViewVariablesTypeHandler
{
    internal abstract ViewVariablesPath? HandlePath(object? obj, string relativePath);
    internal abstract IEnumerable<string> ListPath(object? obj);
}

public sealed class ViewVariablesTypeHandler<T> : ViewVariablesTypeHandler
{
    private readonly List<TypeHandlerData> _handlers = new();
    private readonly Dictionary<string, Func<T?, ViewVariablesFakePath?>> _paths = new();

    internal ViewVariablesTypeHandler()
    {
    }

    /// <summary>
    ///     Adding handler methods allow you to dynamically create and return ViewVariables paths for any sort of path.
    /// </summary>
    /// <remarks>
    ///     The handlers are iterated in the order they were registered in.
    ///     Handlers registered with this method take precedence over handlers registered for specific relative paths.
    /// </remarks>
    /// <returns>The same object instance, so you can chain method calls.</returns>
    public ViewVariablesTypeHandler<T> AddHandler(HandleTypePath<T> handle, ListTypeCustomPaths<T> list)
    {
        _handlers.Add(new TypeHandlerData(handle, list));
        return this;
    }

    /// <summary>
    ///     Remove a specific handler method pair from the type handler.
    /// </summary>
    /// <returns>The same object instance, so you can chain method calls.</returns>
    /// <exception cref="ArgumentException">If the methods specified were not registered.</exception>
    public ViewVariablesTypeHandler<T> RemoveHandler(HandleTypePath<T> handle, ListTypeCustomPaths<T> list)
    {
        for (var i = 0; i < _handlers.Count; i++)
        {
            var data = _handlers[i];

            if (data.Handle != handle || data.List != list)
                continue;

            _handlers.RemoveAt(i);
            return this;
        }

        throw new ArgumentException("The specified arguments were not found in the list!");
    }

    /// <inheritdoc cref="AddPath"/>
    /// <remarks>As opposed to <see cref="AddPath"/>, here the passed object is nullable.</remarks>
    public ViewVariablesTypeHandler<T> AddNullablePath(string path, Func<T?, ViewVariablesFakePath?> handler)
    {
        _paths.Add(path, handler);
        return this;
    }

    /// <summary>
    ///     With this method you can register a handler to handle a specific path relative to the type instance.
    /// </summary>
    /// <returns>The same object instance, so you can chain method calls.</returns>
    public ViewVariablesTypeHandler<T> AddPath(string path, Func<T, ViewVariablesFakePath?> handler)
    {
        ViewVariablesFakePath? Wrapper(T? t)
            => t != null ? handler(t) : null;

        _paths.Add(path, Wrapper);
        return this;
    }

    /// <summary>
    ///     Removes a handler for a specific relative path.
    /// </summary>
    /// <returns>The same object instance, so you can chain method calls.</returns>
    public ViewVariablesTypeHandler<T> RemovePath(string path)
    {
        _paths.Remove(path);
        return this;
    }

    private ViewVariablesPath? HandlePath(T? t, string relativePath)
    {
        // Dynamic handlers take precedence. Iterated by order of registration.
        foreach (var data in _handlers)
        {
            if (data.Handle(t, relativePath) is {} path)
                return path;
        }

        // Finally, try to get a static handler.
        return _paths.TryGetValue(relativePath, out var handler)
            ? handler(t)
            : null;
    }

    private IEnumerable<string> ListPath(T? t)
    {
        foreach (var data in _handlers)
        {
            foreach (var path in data.List(t))
            {
                yield return path;
            }
        }

        foreach (var (path, handler) in _paths)
        {
            if (handler(t) is {})
                yield return path;
        }
    }

    internal override ViewVariablesPath? HandlePath(object? obj, string relativePath) => HandlePath((T?) obj, relativePath);
    internal override IEnumerable<string> ListPath(object? obj) => ListPath((T?) obj);

    private sealed class TypeHandlerData
    {
        public readonly HandleTypePath<T> Handle;
        public readonly ListTypeCustomPaths<T> List;

        public TypeHandlerData(HandleTypePath<T> handle, ListTypeCustomPaths<T> list)
        {
            Handle = handle;
            List = list;
        }
    }
}
