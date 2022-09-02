using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Players;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;

namespace Robust.Shared.ViewVariables;

internal abstract partial class ViewVariablesManagerShared
{
    [Dependency] private readonly IEntityManager _entMan = default!;
    [Dependency] private readonly IComponentFactory _compFact = default!;
    [Dependency] private readonly IPrototypeManager _protoMan = default!;
    [Dependency] private readonly IReflectionManager _reflectionMan = default!;

    protected static readonly (object?, string[]) EmptyResolve = (null, Array.Empty<string>());

    protected readonly Dictionary<Guid, WeakReference<object>> _vvObjectStorage = new();

    private void InitializeDomains()
    {
        RegisterDomain("ioc", ResolveIoCObject);
        RegisterDomain("entity", ResolveEntityObject);
        RegisterDomain("system", ResolveEntitySystemObject);
        RegisterDomain("prototype", ResolvePrototypeObject);
        RegisterDomain("object", ResolveStoredObject);
    }

    public (object? obj, string[] segments) ResolveIoCObject(string path)
    {
        var empty = (IoCManager.Instance, Array.Empty<string>());

        if (string.IsNullOrEmpty(path) || IoCManager.Instance == null)
            return empty;

        var segments = path.Split('/');

        if (segments.Length == 0)
            return empty;

        var service = segments[0];

        if (!_reflectionMan.TryLooseGetType(service, out var type))
            return EmptyResolve;

        return IoCManager.Instance.TryResolveType(type, out var obj)
            ? (obj, segments[1..])
            : EmptyResolve;
    }

    public (object? obj, string[] segments) ResolveEntityObject(string path)
    {
        if (string.IsNullOrEmpty(path))
            return EmptyResolve;

        var segments = path.Split('/');

        if (segments.Length == 0)
            return EmptyResolve;

        if (!int.TryParse(segments[0], out var num) || num <= 0)
            return EmptyResolve;

        var uid = new EntityUid(num);

        if (segments.Length == 1)
            return (uid, Array.Empty<string>());

        var componentName = segments[1];

        if (!_entMan.EntityExists(uid)
            || !_compFact.TryGetRegistration(componentName, out var registration, true)
            || !_entMan.TryGetComponent(uid, registration.Idx, out var component))
            return (null, Array.Empty<string>());

        return (component, segments[2..]);
    }

    public (object? obj, string[] segments) ResolveEntitySystemObject(string path)
    {
        var entSysMan = _entMan.EntitySysManager;
        var empty = (entSysMan, Array.Empty<string>());

        if (string.IsNullOrEmpty(path))
            return empty;

        var segments = path.Split('/');

        if (segments.Length == 0)
            return empty;

        var sys = segments[0];

        if (!_reflectionMan.TryLooseGetType(sys, out var type))
            return EmptyResolve;

        return entSysMan.TryGetEntitySystem(type, out var obj)
            ? (obj, segments[1..])
            : EmptyResolve;
    }

    private (object? obj, string[] segments) ResolvePrototypeObject(string path)
    {
        var empty = (_protoMan, Array.Empty<string>());

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

        return (prototype, segments[2..]);
    }

    private (object? obj, string[] segments) ResolveStoredObject(string path)
    {
        if (string.IsNullOrEmpty(path))
            return EmptyResolve;

        var segments = path.Split('/');

        if (segments.Length == 0
            || !Guid.TryParse(segments[0], out var guid)
            || !_vvObjectStorage.TryGetValue(guid, out var weakRef)
            || !weakRef.TryGetTarget(out var obj))
            return EmptyResolve;

        return (obj, segments[1..]);
    }
}
