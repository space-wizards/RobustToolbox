using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManager
{
    private readonly Dictionary<Type,  ViewVariablesTypeHandler> _typeHandlers = new();

    public ViewVariablesTypeHandler<T> GetTypeHandler<T>()
    {
        if (_typeHandlers.TryGetValue(typeof(T), out var h))
            return (ViewVariablesTypeHandler<T>)h;

        var handler = new ViewVariablesTypeHandler<T>();
        _typeHandlers.Add(typeof(T), handler);
        return handler;
    }

    private void InitializeTypeHandlers()
    {
        GetTypeHandler<EntityUid>()
            .AddHandler(EntityComponentHandler, EntityComponentList)
            .AddPath("Delete",
                uid => new ViewVariablesFakePath(null, null, _ => _entMan.DeleteEntity(uid)))
            .AddPath("QueueDelete",
                uid => new ViewVariablesFakePath(null, null, _ => _entMan.QueueDeleteEntity(uid)));
    }

    private ViewVariablesPath? EntityComponentHandler(EntityUid uid, string relativePath)
    {
        if (!_entMan.EntityExists(uid)
            || !_compFact.TryGetRegistration(relativePath, out var registration, true)
            || !_entMan.TryGetComponent(uid, registration.Idx, out var component))
            return null;

        return new ViewVariablesInstancePath(component);
    }

    private IEnumerable<string> EntityComponentList(EntityUid uid)
    {
        return _entMan.GetComponents(uid)
            .Select(component => _compFact.GetComponentName(component.GetType()));
    }
}
