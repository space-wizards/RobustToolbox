using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManager
{
    private readonly Dictionary<Type,  TypeHandlerData> _registeredTypeHandlers = new();

    public void RegisterTypeHandler<T>(HandleTypePath<T> handler, ListTypeCustomPaths<T> list)
    {
        ViewVariablesPath? Handler(object? obj, string relPath)
            => handler((T?) obj, relPath);

        IEnumerable<string> ListHandler(object? obj)
            => list((T?) obj);

        RegisterTypeHandler(typeof(T), Handler, ListHandler);
    }

    public void RegisterTypeHandler(Type type, HandleTypePath handler, ListTypeCustomPaths list)
    {
        if (_registeredTypeHandlers.ContainsKey(type))
            throw new Exception("Duplicated registration!");

        _registeredTypeHandlers[type] = new TypeHandlerData(handler, list);
    }

    public bool UnregisterTypeHandler<T>()
    {
        return UnregisterTypeHandler(typeof(T));
    }

    public bool UnregisterTypeHandler(Type type)
    {
        return _registeredTypeHandlers.Remove(type);
    }

    private void InitializeTypeHandlers()
    {
        RegisterTypeHandler<EntityUid>(HandleEntityPath, ListEntityTypeHandlerPaths);
    }

    private ViewVariablesPath? HandleEntityPath(EntityUid uid, string relativePath)
    {
        if (!_entMan.EntityExists(uid)
            || !_compFact.TryGetRegistration(relativePath, out var registration, true)
            || !_entMan.TryGetComponent(uid, registration.Idx, out var component))
            return null;

        return new ViewVariablesInstancePath(component);
    }

    private IEnumerable<string> ListEntityTypeHandlerPaths(EntityUid uid)
    {
        return _entMan.GetComponents(uid)
            .Select(component => _compFact.GetComponentName(component.GetType()));
    }

    internal sealed class TypeHandlerData
    {
        public readonly HandleTypePath Handle;
        public readonly ListTypeCustomPaths List;

        public TypeHandlerData(HandleTypePath handle, ListTypeCustomPaths list)
        {
            Handle = handle;
            List = list;
        }
    }
}
