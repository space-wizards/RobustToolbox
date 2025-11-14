using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.ViewVariables;

public delegate (ViewVariablesPath? path, string[] segments) DomainResolveObject(string path);
public delegate IEnumerable<string>? DomainListPaths(string[] segments);

internal abstract partial class ViewVariablesManager
{
    protected static readonly (ViewVariablesPath? Path, string[] Segments) EmptyResolve = (null, Array.Empty<string>());

    private readonly Dictionary<string, DomainData> _registeredDomains = new();
    protected readonly Dictionary<Guid, WeakReference<object>> _vvObjectStorage = new();

    public void RegisterDomain(string domain, DomainResolveObject resolveObject, DomainListPaths list)
    {
        _registeredDomains.Add(domain, new DomainData(resolveObject, list));
    }

    public bool UnregisterDomain(string domain)
    {
        return _registeredDomains.Remove(domain);
    }

    private void InitializeDomains()
    {
        RegisterDomain("ioc", ResolveIoCObject, ListIoCPaths);
        RegisterDomain("entity", ResolveEntityObject, ListEntityPaths);
        RegisterDomain("system", ResolveEntitySystemObject, ListEntitySystemPaths);
        RegisterDomain("prototype", ResolvePrototypeObject, ListPrototypePaths);
        RegisterDomain("object", ResolveStoredObject, ListStoredObjectPaths);
        RegisterDomain("vvtest", ResolveVvTestObject, ListVvTestObjectPaths);
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

    private IEnumerable<string>? ListIoCPaths(string[] segments)
    {
        if (segments.Length > 1 || IoCManager.Instance is not {} deps)
            return null;

        if (segments.Length == 1
            && _reflectionMan.TryLooseGetType(segments[0], out var type)
            && deps.TryResolveType(type, out _))
        {
            return null;
        }

        return deps.GetRegisteredTypes()
            .Select(t => t.Name);
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

    private IEnumerable<string>? ListEntityPaths(string[] segments)
    {
        if (segments.Length > 1)
            return null;

        if (segments.Length == 1
            && NetEntity.TryParse(segments[0], out var uNet)
            && _entMan.TryGetEntity(uNet, out var u)
            && _entMan.EntityExists(u))
        {
            return null;
        }

        return _entMan.GetEntities()
            .Select(uid => uid.ToString());
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

    private IEnumerable<string>? ListEntitySystemPaths(string[] segments)
    {
        if (segments.Length > 1)
            return null;

        var entSysMan = _entMan.EntitySysManager;

        if (segments.Length == 1
            && _reflectionMan.TryLooseGetType(segments[0], out var type)
            && entSysMan.TryGetEntitySystem(type, out _))
        {
            return null;
        }

        return _entMan.EntitySysManager
            .GetEntitySystemTypes()
            .Select(t => t.Name);
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

        if (!_protoMan.TryGetKindType(kind, out var kindType)
            || !_protoMan.TryIndex(kindType, id, out var prototype))
            return EmptyResolve;

        return (new ViewVariablesInstancePath(prototype), segments[2..]);
    }

    private IEnumerable<string>? ListPrototypePaths(string[] segments)
    {
        switch (segments.Length)
        {
            case 1 or 2:
            {
                var kind = segments[0];
                var prototype = segments.Length == 1 ? string.Empty : segments[1];

                if(!_protoMan.HasKind(kind))
                    goto case 0;

                if (_protoMan.TryIndex(_protoMan.GetKindType(kind), prototype, out _))
                    goto case default;

                return _protoMan.EnumeratePrototypes(kind)
                    .Select(p => $"{kind}/{p.ID}");
            }
            case 0:
            {
                return _protoMan
                    .GetPrototypeKinds();
            }
            default:
            {
                return null;
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

    private IEnumerable<string>? ListStoredObjectPaths(string[] segments)
    {
        if (segments.Length > 1)
            return null;

        if (segments.Length == 1
            && Guid.TryParse(segments[0], out var guid)
            && _vvObjectStorage.ContainsKey(guid))
        {
            return null;
        }

        return _vvObjectStorage.Keys
            .Select(g => g.ToString());
    }

    private (ViewVariablesPath? path, string[] segments) ResolveVvTestObject(string path)
    {
        var segments = path.Split('/');

        return (new ViewVariablesInstancePath(new VvTest()), segments);
    }

    private IEnumerable<string>? ListVvTestObjectPaths(string[] segments)
    {
        return null;
    }

    /// <summary>
    ///     Test class to test local VV easily without connecting to the server.
    /// </summary>
    [DataDefinition] // For VV path reading purposes.
    [SuppressMessage("ReSharper", "UnusedMember.Local")]
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    private sealed partial class VvTest : IEnumerable<object>
    {
        [DataField("x")]
        [ViewVariables(VVAccess.ReadWrite)]
        private int X = 10;

        // Note: DataField implies VV read-only already.
        [ViewVariables] public Dictionary<object, object> Dict = new() {{"a", "b"}, {"c", "d"}};

        [ViewVariables] public List<object> List => new() {1, 2, 3, 4, 5, 6, 7, 8, 9, X, 11, 12, 13, 14, 15, this};


        [DataField("multiDimensionalArray")] public int[,] MultiDimensionalArray = new int[5, 2] {{1, 2}, {3, 4}, {5, 6}, {7, 8}, {9, 0}};


        [DataField("vector")]
        [ViewVariables(VVAccess.ReadWrite)]
        private Vector2 Vector = new(50, 50);

        [DataField("data")]
        [ViewVariables(VVAccess.ReadWrite)]
        private ComplexDataStructure Data = new();

        public IEnumerator<object> GetEnumerator()
        {
            return List.GetEnumerator();
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        [DataDefinition]
        private partial struct ComplexDataStructure
        {
            // VV3 uses our serialization system internally, so this allows these values to be changed.
            [DataField("X")]
            [ViewVariables(VVAccess.ReadWrite)]
            public int X;

            [DataField("Y")]
            [ViewVariables(VVAccess.ReadWrite)]
            public int Y;

            public ComplexDataStructure()
            {
                X = 5;
                Y = 15;
            }
        }
    }

    internal sealed class DomainData
    {
        public readonly DomainResolveObject ResolveObject;
        public readonly DomainListPaths List;

        public DomainData(DomainResolveObject resolveObject, DomainListPaths list)
        {
            ResolveObject = resolveObject;
            List = list;
        }
    }
}
