using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManager
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IReflectionManager _reflectionMan = default!;

    protected static readonly (ViewVariablesPath? Path, string[] Segments) EmptyResolve = (null, Array.Empty<string>());

    protected readonly Dictionary<Guid, WeakReference<object>> _vvObjectStorage = new();

    private void InitializeDomains()
    {
        RegisterDomain("ioc", ResolveIoCObject, ListIoCPaths);
        RegisterDomain("entity", ResolveEntityObject, ListEntityPaths);
        RegisterDomain("system", ResolveEntitySystemObject, ListEntitySystemPaths);
        RegisterDomain("prototype", ResolvePrototypeObject, ListPrototypePaths);
        RegisterDomain("object", ResolveStoredObject, ListStoredObjectPaths);
    }

    private (ViewVariablesPath? Path, string[] Segments) ResolveIoCObject(string path)
    {
        var empty = (new ViewVariablesInstancePath(IoCManager.Instance), Array.Empty<string>());

        if (string.IsNullOrEmpty(path) || IoCManager.Instance == null)
            return empty;

        var segments = path.Split('/');

        if (segments.Length == 0)
            return empty;

        var service = segments[0];

        if (!_reflectionMan.TryLooseGetType(service, out var type))
            return EmptyResolve;

        return IoCManager.Instance.TryResolveType(type, out var obj)
            ? (new ViewVariablesInstancePath(obj), segments[1..])
            : EmptyResolve;
    }

    private string[] ListIoCPaths(string[] segments)
    {
        if (segments.Length > 1 || IoCManager.Instance is not {} deps)
            return Array.Empty<string>();

        if (segments.Length == 1
            && _reflectionMan.TryLooseGetType(segments[0], out var type)
            && deps.TryResolveType(type, out _))
        {
            return Array.Empty<string>();
        }

        return deps.GetRegisteredTypes()
            .Select(t => t.Name)
            .ToArray();
    }

    private (ViewVariablesPath? Path, string[] Segments) ResolveEntityObject(string path)
    {
        if (string.IsNullOrEmpty(path))
            return EmptyResolve;

        var segments = path.Split('/');

        if (segments.Length == 0)
            return EmptyResolve;

        if (!int.TryParse(segments[0], out var num) || num <= 0)
            return EmptyResolve;

        var uid = new EntityUid(num);

        return (new ViewVariablesInstancePath(uid), segments[1..]);
    }

    private string[] ListEntityPaths(string[] segments)
    {
        if (segments.Length > 1)
            return Array.Empty<string>();

        if (segments.Length == 1
            && EntityUid.TryParse(segments[0], out var u)
            && _entMan.EntityExists(u))
        {
            return Array.Empty<string>();
        }

        return _entMan.GetEntities()
            .Select(uid => uid.ToString())
            .ToArray();
    }


    public (ViewVariablesPath? Path, string[] Segments) ResolveEntitySystemObject(string path)
    {
        var entSysMan = _entMan.EntitySysManager;
        var empty = (new ViewVariablesInstancePath(entSysMan), Array.Empty<string>());

        if (string.IsNullOrEmpty(path))
            return empty;

        var segments = path.Split('/');

        if (segments.Length == 0)
            return empty;

        var sys = segments[0];

        if (!_reflectionMan.TryLooseGetType(sys, out var type))
            return EmptyResolve;

        return entSysMan.TryGetEntitySystem(type, out var obj)
            ? (new ViewVariablesInstancePath(obj), segments[1..])
            : EmptyResolve;
    }

    private string[] ListEntitySystemPaths(string[] segments)
    {
        if (segments.Length > 1)
            return Array.Empty<string>();

        var entSysMan = _entMan.EntitySysManager;

        if (segments.Length == 1
            && _reflectionMan.TryLooseGetType(segments[0], out var type)
            && entSysMan.TryGetEntitySystem(type, out _))
        {
            return Array.Empty<string>();
        }

        return _entMan.EntitySysManager
            .GetEntitySystemTypes()
            .Select(t => t.Name)
            .ToArray();
    }

    private (ViewVariablesPath? Path, string[] Segments) ResolvePrototypeObject(string path)
    {
        var empty = (new ViewVariablesInstancePath(_protoMan), Array.Empty<string>());

        if (string.IsNullOrEmpty(path) || IoCManager.Instance == null)
            return empty;

        var segments = path.Split('/');

        if (segments.Length <= 1)
            return empty;

        var kind = segments[0];
        var id = segments[1];

        if (!_protoMan.TryGetVariantType(kind, out var kindType)
            || !_protoMan.TryIndex(kindType, id, out var prototype))
            return EmptyResolve;

        return (new ViewVariablesInstancePath(prototype), segments[2..]);
    }

    private string[] ListPrototypePaths(string[] segments)
    {
        switch (segments.Length)
        {
            case 1 or 2:
            {
                var kind = segments[0];
                var prototype = segments.Length == 1 ? string.Empty : segments[1];

                if(!_protoMan.HasVariant(kind))
                    goto case 0;

                if (_protoMan.TryIndex(_protoMan.GetVariantType(kind), prototype, out _))
                    goto case default;

                return _protoMan.EnumeratePrototypes(kind)
                    .Select(p => $"{kind}/{p.ID}")
                    .ToArray();
            }
            case 0:
            {
                return _protoMan
                    .GetPrototypeKinds()
                    .ToArray();
            }
            default:
            {
                return Array.Empty<string>();
            }
        }
    }

    private (ViewVariablesPath? Path, string[] Segments) ResolveStoredObject(string path)
    {
        if (string.IsNullOrEmpty(path))
            return EmptyResolve;

        var segments = path.Split('/');

        if (segments.Length == 0
            || !Guid.TryParse(segments[0], out var guid)
            || !_vvObjectStorage.TryGetValue(guid, out var weakRef)
            || !weakRef.TryGetTarget(out var obj))
            return EmptyResolve;

        return (new ViewVariablesInstancePath(obj), segments[1..]);
    }

    private string[] ListStoredObjectPaths(string[] segments)
    {
        if (segments.Length > 1)
            return Array.Empty<string>();

        if (segments.Length == 1
            && Guid.TryParse(segments[0], out var guid)
            && _vvObjectStorage.ContainsKey(guid))
        {
            return Array.Empty<string>();
        }

        return _vvObjectStorage.Keys
            .Select(g => g.ToString())
            .ToArray();
    }
}
