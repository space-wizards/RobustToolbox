using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Log;

namespace Robust.Shared.ViewVariables;

public delegate ViewVariablesPath? HandleTypePath(ViewVariablesPath path, string relativePath);
public delegate ViewVariablesPath? HandleTypePathNullable<in T>(T? obj, string relativePath);
public delegate ViewVariablesPath? HandleTypePathComponent<in TComp>(EntityUid uid, TComp comp, string relativePath);
public delegate ViewVariablesPath? HandleTypePath<in T>(T obj, string relativePath);
public delegate IEnumerable<string> ListTypeCustomPaths(ViewVariablesPath path);
public delegate IEnumerable<string> ListTypeCustomPaths<in T>(T obj);
public delegate IEnumerable<string> ListTypeCustomPathsNullable<in T>(T? obj);
public delegate IEnumerable<string> ListTypeCustomPathsComponent<in TComp>(EntityUid uid, TComp comp);
public delegate ViewVariablesPath? PathHandler(ViewVariablesPath path);
public delegate ViewVariablesPath? PathHandler<in T>(T obj);
public delegate ViewVariablesPath? PathHandlerNullable<in T>(T? obj);
public delegate ViewVariablesPath? PathHandlerComponent<in T>(EntityUid uid, T component);
public delegate TValue ComponentPropertyGetter<in TComp, out TValue>(EntityUid uid, TComp comp);
public delegate void ComponentPropertySetter<in TComp, in TValue>(EntityUid uid, TValue value, TComp? comp);

public abstract class ViewVariablesTypeHandler
{
    internal abstract ViewVariablesPath? HandlePath(ViewVariablesPath path, string relativePath);
    internal abstract IEnumerable<string> ListPath(ViewVariablesPath path);
}

public sealed class ViewVariablesTypeHandler<T> : ViewVariablesTypeHandler
{
    private readonly List<TypeHandlerData> _handlers = new();
    private readonly Dictionary<string, PathHandler> _paths = new();
    private readonly ISawmill _sawmill;

    internal ViewVariablesTypeHandler(ISawmill sawmill)
    {
        _sawmill = sawmill;
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
        ViewVariablesPath? HandleWrapper(ViewVariablesPath path, string relativePath)
            => path.Get() is not {} obj ? null : handle((T)obj, relativePath);

        IEnumerable<string> ListWrapper(ViewVariablesPath path)
            => path.Get() is not {} obj ? Enumerable.Empty<string>() : list((T)obj);

        _handlers.Add(new TypeHandlerData(HandleWrapper, ListWrapper, handle, list));
        return this;
    }

    /// <inheritdoc cref="AddHandler(Robust.Shared.ViewVariables.HandleTypePath{T},Robust.Shared.ViewVariables.ListTypeCustomPaths{T})"/>
    /// <!-- The reason this isn't called "AddHandler" is because it'd cause many ambiguous invocations.-->
    public ViewVariablesTypeHandler<T> AddHandlerNullable(HandleTypePathNullable<T> handle, ListTypeCustomPathsNullable<T> list)
    {
        ViewVariablesPath? HandleWrapper(ViewVariablesPath path, string relativePath)
            => handle((T?)path.Get(), relativePath);

        IEnumerable<string> ListWrapper(ViewVariablesPath path)
            => list((T?) path.Get());

        _handlers.Add(new TypeHandlerData(HandleWrapper, ListWrapper, handle, list));
        return this;
    }

    /// <inheritdoc cref="AddHandler(Robust.Shared.ViewVariables.HandleTypePath{T},Robust.Shared.ViewVariables.ListTypeCustomPaths{T})"/>
    public ViewVariablesTypeHandler<T> AddHandler(HandleTypePathComponent<T> handle, ListTypeCustomPathsComponent<T> list)
    {
        ViewVariablesPath? HandleWrapper(ViewVariablesPath path, string relativePath)
        {
            if (path is not ViewVariablesComponentPath compPath || compPath.Get() is not T comp)
                return null;

            return handle(compPath.Owner, comp, relativePath);
        }

        IEnumerable<string> ListWrapper(ViewVariablesPath path)
        {
            if (path is not ViewVariablesComponentPath compPath || compPath.Get() is not T comp)
                return Enumerable.Empty<string>();

            return list(compPath.Owner, comp);
        }

        _handlers.Add(new TypeHandlerData(HandleWrapper, ListWrapper, handle, list));
        return this;
    }

    /// <inheritdoc cref="AddHandler(Robust.Shared.ViewVariables.HandleTypePath{T},Robust.Shared.ViewVariables.ListTypeCustomPaths{T})"/>
    public ViewVariablesTypeHandler<T> AddHandler(HandleTypePath handle, ListTypeCustomPaths list)
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
        return RemoveHandlerInternal(handle, list, true);
    }

    /// <inheritdoc cref="RemoveHandler(Robust.Shared.ViewVariables.HandleTypePath{T},Robust.Shared.ViewVariables.ListTypeCustomPaths{T})"/>
    public ViewVariablesTypeHandler<T> RemoveHandlerNullable(HandleTypePathNullable<T> handle, ListTypeCustomPathsNullable<T> list)
    {
        return RemoveHandlerInternal(handle, list, true);
    }

    /// <inheritdoc cref="RemoveHandler(Robust.Shared.ViewVariables.HandleTypePath{T},Robust.Shared.ViewVariables.ListTypeCustomPaths{T})"/>
    public ViewVariablesTypeHandler<T> RemoveHandler(HandleTypePathComponent<T> handle, ListTypeCustomPathsComponent<T> list)
    {
        return RemoveHandlerInternal(handle, list, true);
    }

    /// <inheritdoc cref="RemoveHandler(Robust.Shared.ViewVariables.HandleTypePath{T},Robust.Shared.ViewVariables.ListTypeCustomPaths{T})"/>
    public ViewVariablesTypeHandler<T> RemoveHandler(HandleTypePath handle, ListTypeCustomPaths list)
    {
        return RemoveHandlerInternal(handle, list);
    }

    private ViewVariablesTypeHandler<T> RemoveHandlerInternal(object handle, object list, bool originalCompare = false)
    {
        for (var i = 0; i < _handlers.Count; i++)
        {
            var data = _handlers[i];

            if (!originalCompare && !(handle.Equals(data.Handle) && list.Equals(data.List)))
                continue;

            if (originalCompare && !(handle.Equals(data.OriginalHandle) && list.Equals(data.OriginalList)))
                continue;

            _handlers.RemoveAt(i);
            return this;
        }

        throw new ArgumentException("The specified arguments were not found in the list!");
    }

    /// <summary>
    ///     With this method you can register a handler to handle a specific path relative to the type instance.
    /// </summary>
    /// <returns>The same object instance, so you can chain method calls.</returns>
    public ViewVariablesTypeHandler<T> AddPath(string path, PathHandler<T> handler)
    {
        ViewVariablesPath? Wrapper(T? t)
            => t != null ? handler(t) : null;

        return AddPathNullable(path, (PathHandlerNullable<T>) Wrapper);
    }

    /// <inheritdoc cref="AddPath(string,PathHandler)"/>
    /// <remarks>As opposed to <see cref="AddPath(string,PathHandler)"/>, here the passed object is nullable.</remarks>
    /// <!-- The reason this isn't called "AddPath" is because it'd cause many ambiguous invocations.-->
    public ViewVariablesTypeHandler<T> AddPathNullable(string path, PathHandlerNullable<T> handler)
    {
        ViewVariablesPath? Wrapper(ViewVariablesPath p)
            => handler((T?) p.Get());

        return AddPath(path, Wrapper);
    }

    /// <inheritdoc cref="AddPath(string,PathHandler)"/>
    /// <remarks>As opposed to the rest of "AddPath" methods, this one is specific to entity components.</remarks>
    public ViewVariablesTypeHandler<T> AddPath(string path, PathHandlerComponent<T> handler)
    {
        ViewVariablesPath? Wrapper(ViewVariablesPath p)
        {
            if (p is not ViewVariablesComponentPath pc || pc.Get() is not {} obj)
                return null;

            return handler(pc.Owner, (T) obj);
        }

        return AddPath(path, Wrapper);
    }

    /// <inheritdoc cref="AddPath(string,PathHandler)"/>
    public ViewVariablesTypeHandler<T> AddPath<TValue>(string path, ComponentPropertyGetter<T, TValue> getter,
        ComponentPropertySetter<T, TValue>? setter = null)
    {
        // Gee, these wrappers are getting more and more complicated...
        ViewVariablesPath? Wrapper(ViewVariablesPath p)
        {
            if (p is not ViewVariablesComponentPath pc || pc.Get() is not {} obj)
                return null;

            var comp = (T) obj;

            var newPath = ViewVariablesPath.FromGetter(() => getter(pc.Owner, comp), typeof(TValue));

            if (setter != null)
            {
                newPath = newPath.WithSetter(value =>
                {
                    // In case it explodes with a NRE or something!
                    try
                    {
                        setter(pc.Owner, (TValue) value!, comp);
                    }
                    catch (NullReferenceException e)
                    {
                        _sawmill.Log(
                            LogLevel.Error,
                            e,
                            $"NRE caught in setter for path \"{path}\" for type \"{typeof(T).Name}\"...");
                    }
                });
            }

            return newPath;
        }

        return AddPath(path, Wrapper);
    }

    /// <inheritdoc cref="AddPath(string,PathHandler)"/>
    public ViewVariablesTypeHandler<T> AddPath(string path, PathHandler handler)
    {
        _paths.Add(path, handler);
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

    internal override ViewVariablesPath? HandlePath(ViewVariablesPath path, string relativePath)
    {
        // Dynamic handlers take precedence. Iterated by order of registration.
        foreach (var data in _handlers)
        {
            if (data.Handle(path, relativePath) is {} dynPath)
                return dynPath;
        }

        // Finally, try to get a static handler.
        return _paths.TryGetValue(relativePath, out var handler)
            ? handler(path)
            : null;
    }

    internal override IEnumerable<string> ListPath(ViewVariablesPath path)
    {
        foreach (var data in _handlers)
        {
            foreach (var p in data.List(path))
            {
                yield return p;
            }
        }

        foreach (var (p, handler) in _paths)
        {
            if (handler(path) is {})
                yield return p;
        }
    }

    private sealed class TypeHandlerData
    {
        public readonly HandleTypePath Handle;
        public readonly ListTypeCustomPaths List;

        public readonly object? OriginalHandle;
        public readonly object? OriginalList;

        public TypeHandlerData(HandleTypePath handle, ListTypeCustomPaths list,
            object? origHandle = null, object? origList = null)
        {
            Handle = handle;
            List = list;
            OriginalHandle = origHandle;
            OriginalList = origList;
        }
    }
}
