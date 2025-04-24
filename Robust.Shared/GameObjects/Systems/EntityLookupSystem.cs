using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

[Flags]
public enum LookupFlags : byte
{
    None = 0,

    /// <summary>
    /// Should we use the approximately intersecting entities or check tighter bounds.
    /// </summary>
    Approximate = 1 << 0,

    /// <summary>
    /// Should we query dynamic physics bodies.
    /// </summary>
    Dynamic = 1 << 1,

    /// <summary>
    /// Should we query static physics bodies.
    /// </summary>
    Static = 1 << 2,

    /// <summary>
    /// Should we query non-collidable physics bodies.
    /// </summary>
    Sundries = 1 << 3,

    /// <summary>
    /// Include entities that are currently in containers.
    /// </summary>
    Contained = 1 << 5,

    /// <summary>
    /// Do we include non-hard fixtures.
    /// </summary>
    Sensors = 1 << 6,

    Uncontained = Dynamic | Static | Sundries | Sensors,

    StaticSundries = Static | Sundries,

    All = Contained | Dynamic | Static | Sundries | Sensors
}

/// <summary>
/// Raised on entities to try to get its WorldAABB.
/// </summary>
[ByRefEvent]
public record struct WorldAABBEvent
{
    public Box2 AABB;
}

public sealed partial class EntityLookupSystem : EntitySystem
{
    [Dependency] private readonly IManifoldManager _manifoldManager = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netMan = default!;
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly FixtureSystem _fixtures = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly SharedPhysicsSystem _physics = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private EntityQuery<BroadphaseComponent> _broadQuery;
    private EntityQuery<ContainerManagerComponent> _containerQuery;
    private EntityQuery<FixturesComponent> _fixturesQuery;
    private EntityQuery<MapComponent> _mapQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<MetaDataComponent> _metaQuery;
    private EntityQuery<PhysicsComponent> _physicsQuery;
    private EntityQuery<PhysicsMapComponent> _physMapQuery;
    private EntityQuery<TransformComponent> _xformQuery;

    /// <summary>
    /// 1 x 1 polygons can overlap neighboring tiles (even without considering the polygon skin around them.
    /// When querying for specific tile fixtures we shrink the bounds by this amount to avoid this overlap.
    /// </summary>
    public const float TileEnlargementRadius = -PhysicsConstants.PolygonRadius * 4f;

    /// <summary>
    /// The minimum size an entity is assumed to be for point purposes.
    /// </summary>
    public const float LookupEpsilon = float.Epsilon * 10f;

    /// <summary>
    /// Returns all non-grid entities. Consider using your own flags if you wish for a faster query.
    /// </summary>
    public const LookupFlags DefaultFlags = LookupFlags.All;

    public override void Initialize()
    {
        base.Initialize();

        _broadQuery = GetEntityQuery<BroadphaseComponent>();
        _containerQuery = GetEntityQuery<ContainerManagerComponent>();
        _fixturesQuery = GetEntityQuery<FixturesComponent>();
        _mapQuery = GetEntityQuery<MapComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _metaQuery = GetEntityQuery<MetaDataComponent>();
        _physicsQuery = GetEntityQuery<PhysicsComponent>();
        _physMapQuery = GetEntityQuery<PhysicsMapComponent>();
        _xformQuery = GetEntityQuery<TransformComponent>();

        SubscribeLocalEvent<BroadphaseComponent, EntityTerminatingEvent>(OnBroadphaseTerminating);
        SubscribeLocalEvent<BroadphaseComponent, ComponentAdd>(OnBroadphaseAdd);
        SubscribeLocalEvent<BroadphaseComponent, ComponentInit>(OnBroadphaseInit);
        SubscribeLocalEvent<GridAddEvent>(OnGridAdd);
        SubscribeLocalEvent<MapCreatedEvent>(OnMapChange);

        _transform.OnBeforeMoveEvent += OnMove;
        EntityManager.EntityInitialized += OnEntityInit;

        SubscribeLocalEvent<TransformComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChange);
        SubscribeLocalEvent<PhysicsComponent, ComponentStartup>(OnBodyStartup);
        SubscribeLocalEvent<CollisionChangeEvent>(OnPhysicsUpdate);
    }

    private void OnBodyStartup(EntityUid uid, PhysicsComponent component, ComponentStartup args)
    {
        UpdatePhysicsBroadphase(uid, Transform(uid), component);
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EntityManager.EntityInitialized -= OnEntityInit;
        _transform.OnBeforeMoveEvent -= OnMove;
    }

    #region DynamicTree

    private void OnBroadphaseTerminating(EntityUid uid, BroadphaseComponent component, ref EntityTerminatingEvent args)
    {
        var xform = _xformQuery.GetComponent(uid);
        var map = xform.MapUid;
        _physMapQuery.TryGetComponent(map, out var physMap);
        RemoveChildrenFromTerminatingBroadphase(xform, component, physMap);
        RemComp(uid, component);
    }

    private void RemoveChildrenFromTerminatingBroadphase(TransformComponent xform,
        BroadphaseComponent component,
        PhysicsMapComponent? map)
    {
        foreach (var child in xform._children)
        {
            if (!_xformQuery.TryGetComponent(child, out var childXform))
                continue;

            if (childXform.GridUid == child)
                continue;

            if (childXform.Broadphase == null)
                continue;

            DebugTools.Assert(childXform.Broadphase.Value.Uid == component.Owner);
            DebugTools.Assert(!_gridQuery.HasComp(child));

            if (childXform.Broadphase.Value.CanCollide && _fixturesQuery.TryGetComponent(child, out var fixtures))
            {
                if (map == null)
                    _physMapQuery.TryGetComponent(childXform.Broadphase.Value.PhysicsMap, out map);

                DebugTools.Assert(map == null || childXform.Broadphase.Value.PhysicsMap == map.Owner);
                var tree = childXform.Broadphase.Value.Static ? component.StaticTree : component.DynamicTree;
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    DestroyProxies(fixture, tree, map);
                }
            }

            childXform.Broadphase = null;
            RemoveChildrenFromTerminatingBroadphase(childXform, component, map);
        }
    }

    private void OnMapChange(MapCreatedEvent ev)
    {
        if (ev.MapId != MapId.Nullspace)
        {
            EnsureComp<BroadphaseComponent>(ev.Uid);
        }
    }

    private void OnGridAdd(GridAddEvent ev)
    {
        // Must be done before initialization as that's when broadphase data starts getting set.
        EnsureComp<BroadphaseComponent>(ev.EntityUid);
    }

    private void OnBroadphaseAdd(Entity<BroadphaseComponent> broadphase, ref ComponentAdd args)
    {
        broadphase.Comp.StaticSundriesTree = new DynamicTree<EntityUid>(
            (in EntityUid value) => GetTreeAABB(value, broadphase.Owner));
        broadphase.Comp.SundriesTree = new DynamicTree<EntityUid>(
            (in EntityUid value) => GetTreeAABB(value, broadphase.Owner));
    }

    private void OnBroadphaseInit(Entity<BroadphaseComponent> broadphase, ref ComponentInit args)
    {
        var xform = Transform(broadphase.Owner);
        _transform.InitializeMapUid(broadphase.Owner, xform);

        // If in broadphase then skip this for now because no physicsmap to init physics entities properly
        // This mainly happens in replays or otherwise spawning grids in nullspace. PhysicsMap is getting dumped in box2c anyway
        if (xform.MapUid == null)
            return;

        if (!_physMapQuery.TryGetComponent(xform.MapUid, out var physMap))
        {
            throw new InvalidOperationException(
                $"Broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(broadphase.Owner)}");
        }

        var ent = new Entity<TransformComponent, BroadphaseComponent>(broadphase, xform, broadphase);
        var map = new Entity<PhysicsMapComponent>(xform.MapUid.Value, physMap);
        var enumerator = xform.ChildEnumerator;
        while (enumerator.MoveNext(out var child))
        {
            if (!_broadQuery.HasComp(child))
                InitializeChild(child, ent, map);
        }
    }

    private void InitializeChild(
        EntityUid child,
        Entity<TransformComponent, BroadphaseComponent> broadphase,
        Entity<PhysicsMapComponent> map)
    {
        if (LifeStage(child) <= EntityLifeStage.PreInit)
            return;

        var xform = Transform(child);

        if (xform.Broadphase != null)
        {
            if (!xform.Broadphase.Value.IsValid())
                return; // Entity is intentionally not on a broadphase (deferred updating?).

            _physMapQuery.TryGetComponent(xform.Broadphase.Value.PhysicsMap, out var oldPhysMap);
            if (!_broadQuery.TryGetComponent(xform.Broadphase.Value.Uid, out var oldBroadphase))
            {
                DebugTools.Assert("Encountered deleted broadphase.");
                if (_fixturesQuery.TryGetComponent(child, out var fixtures))
                {
                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        fixture.ProxyCount = 0;
                        fixture.Proxies = Array.Empty<FixtureProxy>();
                    }
                }

                xform.Broadphase = null;
            }
            else if (oldBroadphase != broadphase.Comp2)
            {
                RemoveFromEntityTree(xform.Broadphase.Value.Uid, oldBroadphase, ref oldPhysMap, child, xform);
            }
        }

        DebugTools.Assert(xform.Broadphase is not {} x || x.Uid == broadphase.Owner && (!x.CanCollide || x.PhysicsMap == map.Owner));
        AddOrUpdateEntityTree(
            broadphase.Owner,
            broadphase.Comp2,
            broadphase.Comp1,
            map.Comp,
            child,
            xform);
    }

    private Box2 GetTreeAABB(EntityUid entity, EntityUid tree)
    {
        if (!_xformQuery.TryGetComponent(entity, out var xform))
        {
            Log.Error($"Entity tree contains a deleted entity? Tree: {ToPrettyString(tree)}, entity: {entity}");
            return default;
        }

        if (xform.ParentUid == tree)
            return GetAABBNoContainer(entity, xform.LocalPosition, xform.LocalRotation);

        if (!_xformQuery.TryGetComponent(tree, out var treeXform))
        {
            Log.Error($"Entity tree has no transform? Tree Uid: {tree}");
            return default;
        }

        return _transform.GetInvWorldMatrix(treeXform).TransformBox(GetWorldAABB(entity, xform));
    }

    internal void CreateProxies(EntityUid uid, string fixtureId, Fixture fixture, TransformComponent xform,
        PhysicsComponent body)
    {
        if (!TryGetCurrentBroadphase(xform, out var broadphase))
            return;

        if (!_physMapQuery.TryGetComponent(xform.MapUid, out var physMap))
            throw new InvalidOperationException();

        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
        var mapTransform = new Transform(worldPos, worldRot);

        var (_, broadWorldRot, _, broadInvMatrix) = _transform.GetWorldPositionRotationMatrixWithInv(broadphase.Owner);
        var broadphaseTransform = new Transform(Vector2.Transform(mapTransform.Position, broadInvMatrix), mapTransform.Quaternion2D.Angle - broadWorldRot);
        var tree = body.BodyType == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;
        DebugTools.Assert(fixture.ProxyCount == 0);

        AddOrMoveProxies(uid, fixtureId, fixture, body, tree, broadphaseTransform, mapTransform, physMap.MoveBuffer);
    }

    internal void DestroyProxies(EntityUid uid, string fixtureId, Fixture fixture, TransformComponent xform, BroadphaseComponent broadphase, PhysicsMapComponent? physicsMap)
    {
        DebugTools.AssertNotNull(xform.Broadphase);
        DebugTools.Assert(xform.Broadphase!.Value.Uid == broadphase.Owner);

        if (!xform.Broadphase.Value.CanCollide || xform.GridUid == uid)
            return;

        if (fixture.ProxyCount == 0)
        {
            Log.Warning($"Tried to destroy fixture {fixtureId} on {ToPrettyString(uid)} that already has no proxies?");
            return;
        }

        var tree = xform.Broadphase.Value.Static ? broadphase.StaticTree : broadphase.DynamicTree;
        DestroyProxies(fixture, tree, physicsMap);
    }

    #endregion

    #region Entity events

    private void OnPhysicsUpdate(ref CollisionChangeEvent ev)
    {
        var xform = Transform(ev.BodyUid);
        UpdatePhysicsBroadphase(ev.BodyUid, xform, ev.Body);

        // ensure that the cached broadphase is correct.
        DebugTools.Assert(_timing.ApplyingState
                          || xform.Broadphase == null
                          || ev.Body.LifeStage <= ComponentLifeStage.Initializing
                          || !xform.Broadphase.Value.IsValid()
                          || ((xform.Broadphase.Value.CanCollide == ev.Body.CanCollide)
                              && (xform.Broadphase.Value.Static == (ev.Body.BodyType == BodyType.Static))));
    }

    private void OnBodyTypeChange(EntityUid uid, TransformComponent xform, ref PhysicsBodyTypeChangedEvent args)
    {
        // only matters if we swapped from static to non-static or vice versa.
        if (args.Old != BodyType.Static && args.New != BodyType.Static)
            return;

        UpdatePhysicsBroadphase(uid, xform, args.Component);
    }

    private void UpdatePhysicsBroadphase(EntityUid uid, TransformComponent xform, PhysicsComponent body)
    {
        if (body.LifeStage <= ComponentLifeStage.Initializing)
            return;

        if (xform.GridUid == uid)
            return;
        DebugTools.Assert(!HasComp<MapGridComponent>(uid));

        if (xform.Broadphase is not { Valid: true } old)
            return; // entity is not on any broadphase

        xform.Broadphase = null;

        if (!_broadQuery.TryGetComponent(old.Uid, out var broadphase))
            return; // broadphase probably got deleted.

        // remove from the old broadphase
        var fixtures = Comp<FixturesComponent>(uid);
        if (old.CanCollide)
        {
            _physMapQuery.TryGetComponent(old.PhysicsMap, out var physicsMap);
            RemoveBroadTree(broadphase, fixtures, old.Static, physicsMap);
        }
        else
            (old.Static ? broadphase.StaticSundriesTree : broadphase.SundriesTree).Remove(uid);

        // Add to new broadphase
        if (body.CanCollide)
            AddPhysicsTree(uid, old.Uid, broadphase, xform, body, fixtures);
        else
            AddOrUpdateSundriesTree(old.Uid, broadphase, uid, xform, body.BodyType == BodyType.Static);
    }

    private void RemoveBroadTree(BroadphaseComponent lookup, FixturesComponent manager, bool staticBody, PhysicsMapComponent? map)
    {
        var tree = staticBody ? lookup.StaticTree : lookup.DynamicTree;
        foreach (var fixture in manager.Fixtures.Values)
        {
            DestroyProxies(fixture, tree, map);
        }
    }

    internal void DestroyProxies(Fixture fixture, IBroadPhase tree, PhysicsMapComponent? map)
    {
        var buffer = map?.MoveBuffer;
        for (var i = 0; i < fixture.ProxyCount; i++)
        {
            var proxy = fixture.Proxies[i];
            tree.RemoveProxy(proxy.ProxyId);
            buffer?.Remove(proxy);
        }

        fixture.ProxyCount = 0;
        fixture.Proxies = Array.Empty<FixtureProxy>();
    }

    private void AddPhysicsTree(EntityUid uid, EntityUid broadUid, BroadphaseComponent broadphase, TransformComponent xform, PhysicsComponent body, FixturesComponent fixtures)
    {
        var broadphaseXform = _xformQuery.GetComponent(broadUid);

        if (broadphaseXform.MapID == MapId.Nullspace)
            return;

        if (!_physMapQuery.TryGetComponent(broadphaseXform.MapUid, out var physMap))
            throw new InvalidOperationException($"Physics Broadphase is missing physics map. {ToPrettyString(broadUid)}");

        AddOrUpdatePhysicsTree(uid, broadUid, broadphase, broadphaseXform, physMap, xform, body, fixtures);
    }

    private void AddOrUpdatePhysicsTree(
        EntityUid uid,
        EntityUid broadUid,
        BroadphaseComponent broadphase,
        TransformComponent broadphaseXform,
        PhysicsMapComponent physicsMap,
        TransformComponent xform,
        PhysicsComponent body,
        FixturesComponent manager)
    {
        DebugTools.Assert(!_container.IsEntityOrParentInContainer(body.Owner, null, xform));
        DebugTools.Assert(xform.Broadphase == null || xform.Broadphase == new BroadphaseData(broadphase.Owner, physicsMap.Owner, body.CanCollide, body.BodyType == BodyType.Static));
        DebugTools.Assert(broadphase.Owner == broadUid);

        xform.Broadphase ??= new(broadUid, physicsMap.Owner, body.CanCollide, body.BodyType == BodyType.Static);
        var tree = body.BodyType == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;

        // TOOD optimize this. This function iterates UP through parents, while we are currently iterating down.
        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
        var mapTransform = new Transform(worldPos, worldRot);

        // TODO BROADPHASE PARENTING this just assumes local = world
        var broadphaseTransform = new Transform(Vector2.Transform(mapTransform.Position, broadphaseXform.InvLocalMatrix), mapTransform.Quaternion2D.Angle - broadphaseXform.LocalRotation);

        foreach (var (id, fixture) in manager.Fixtures)
        {
            AddOrMoveProxies(uid, id, fixture, body, tree, broadphaseTransform, mapTransform, physicsMap.MoveBuffer);
        }
    }

    private void AddOrMoveProxies(
        EntityUid uid,
        string fixtureId,
        Fixture fixture,
        PhysicsComponent body,
        IBroadPhase tree,
        Transform broadphaseTransform,
        Transform mapTransform,
        Dictionary<FixtureProxy, Box2> moveBuffer)
    {
        // Moving
        if (fixture.ProxyCount > 0)
        {
            for (var i = 0; i < fixture.ProxyCount; i++)
            {
                var bounds = fixture.Shape.ComputeAABB(broadphaseTransform, i);
                var proxy = fixture.Proxies[i];
                tree.MoveProxy(proxy.ProxyId, bounds);
                proxy.AABB = bounds;
                moveBuffer[proxy] = fixture.Shape.ComputeAABB(mapTransform, i);
            }

            return;
        }

        var count = fixture.Shape.ChildCount;
        var proxies = new FixtureProxy[count];

        for (var i = 0; i < count; i++)
        {
            var bounds = fixture.Shape.ComputeAABB(broadphaseTransform, i);
            var proxy = new FixtureProxy(uid, body, bounds, fixtureId, fixture, i);
            proxy.ProxyId = tree.AddProxy(ref proxy);
            proxy.AABB = bounds;
            proxies[i] = proxy;
            moveBuffer[proxy] = fixture.Shape.ComputeAABB(mapTransform, i);
        }

        fixture.Proxies = proxies;
        fixture.ProxyCount = count;
    }

    private void AddOrUpdateSundriesTree(EntityUid broadUid, BroadphaseComponent broadphase, EntityUid uid, TransformComponent xform, bool staticBody, Box2? aabb = null)
    {
        DebugTools.Assert(!_container.IsEntityOrParentInContainer(uid));
        DebugTools.Assert(xform.Broadphase == null || xform.Broadphase == new BroadphaseData(broadUid, default, false, staticBody));
        xform.Broadphase ??= new(broadUid, default, false, staticBody);
        (staticBody ? broadphase.StaticSundriesTree : broadphase.SundriesTree).AddOrUpdate(uid, aabb);
    }

    private void OnEntityInit(Entity<MetaDataComponent> uid)
    {
        if (_container.IsEntityOrParentInContainer(uid, uid) || _mapQuery.HasComp(uid) || _gridQuery.HasComp(uid))
            return;

        // TODO can this just be done implicitly via transform startup?
        // or do things need to be in trees for other component startup logic?
        FindAndAddToEntityTree(uid, false);
    }

    private void OnMove(ref MoveEvent args)
    {
        if (args.Component.GridUid == args.Sender)
        {
            if (args.ParentChanged) // grid changed maps, need to update children and clear the move buffer.
                OnGridChangedMap(args);
            return;
        }
        DebugTools.Assert(!_gridQuery.HasComp(args.Sender));

        if (args.Component.MapUid == args.Sender)
            return;
        DebugTools.Assert(!_mapQuery.HasComp(args.Sender));

        if (args.ParentChanged)
            UpdateParent(args.Sender, args.Component);
        else
            UpdateEntityTree(args.Sender, args.Component);
    }

    private void OnGridChangedMap(MoveEvent args)
    {
        var newMap = args.NewPosition.EntityId;
        var oldMap = args.OldPosition.EntityId;

        if (Terminating(oldMap))
            return;

        // We need to recursively update the cached data and remove children from the move buffer
        DebugTools.Assert(_gridQuery.HasComp(args.Sender));
        DebugTools.Assert(!newMap.IsValid() || _mapQuery.HasComp(newMap));
        DebugTools.Assert(!oldMap.IsValid() || _mapQuery.HasComp(oldMap));

        var oldBuffer = _physMapQuery.CompOrNull(oldMap)?.MoveBuffer;
        var newBuffer = _physMapQuery.CompOrNull(newMap)?.MoveBuffer;

        foreach (var child in args.Component._children)
        {
            RecursiveOnGridChangedMap(child, oldMap, newMap, oldBuffer, newBuffer);
        }
    }

    private void RecursiveOnGridChangedMap(
        EntityUid uid,
        EntityUid oldMap,
        EntityUid newMap,
        Dictionary<FixtureProxy, Box2>? oldBuffer,
        Dictionary<FixtureProxy, Box2>? newBuffer)
    {
        if (!_xformQuery.TryGetComponent(uid, out var xform))
            return;

        foreach (var child in xform._children)
        {
            RecursiveOnGridChangedMap(child, oldMap, newMap, oldBuffer, newBuffer);
        }

        if (xform.Broadphase == null || !xform.Broadphase.Value.CanCollide)
            return;

        DebugTools.Assert(_netMan.IsClient || !xform.Broadphase.Value.PhysicsMap.IsValid() || xform.Broadphase.Value.PhysicsMap == oldMap);
        xform.Broadphase = xform.Broadphase.Value with { PhysicsMap = newMap };

        if (!_fixturesQuery.TryGetComponent(uid, out var fixtures))
            return;

        if (oldBuffer != null)
        {
            foreach (var fix in fixtures.Fixtures.Values)
            foreach (var prox in fix.Proxies)
            {
                oldBuffer.Remove(prox);
            }
        }

        if (newBuffer == null)
            return;

        // TODO PERFORMANCE
        // track world position while recursively iterating down through children.
        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
        var mapTransform = new Transform(worldPos, worldRot);

        foreach (var fixture in fixtures.Fixtures.Values)
        {
            for (var i = 0; i < fixture.ProxyCount; i++)
            {
                var proxy = fixture.Proxies[i];
                newBuffer[proxy] = fixture.Shape.ComputeAABB(mapTransform, i);
            }
        }
    }

    private void UpdateParent(EntityUid uid, TransformComponent xform)
    {
        BroadphaseComponent? oldBroadphase = null;
        PhysicsMapComponent? oldPhysMap = null;
        if (xform.Broadphase != null)
        {
            if (!xform.Broadphase.Value.IsValid())
                return; // Entity is intentionally not on a broadphase (deferred updating?).

            _physMapQuery.TryGetComponent(xform.Broadphase.Value.PhysicsMap, out oldPhysMap);

            if (!_broadQuery.TryGetComponent(xform.Broadphase.Value.Uid, out oldBroadphase))
            {

                DebugTools.Assert("Encountered deleted broadphase.");

                // broadphase was probably deleted.
                if (_fixturesQuery.TryGetComponent(uid, out var fixtures))
                {
                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        fixture.ProxyCount = 0;
                        fixture.Proxies = Array.Empty<FixtureProxy>();
                    }
                }

                xform.Broadphase = null;
            }
        }

        TryFindBroadphase(xform, out var newBroadphase);

        if (oldBroadphase != null && oldBroadphase != newBroadphase)
        {
            RemoveFromEntityTree(oldBroadphase.Owner, oldBroadphase, ref oldPhysMap, uid, xform);
        }

        if (newBroadphase == null)
            return;

        var newBroadphaseXform = _xformQuery.GetComponent(newBroadphase.Owner);
        if (!_physMapQuery.TryGetComponent(newBroadphaseXform.MapUid, out var physMap))
        {
            throw new InvalidOperationException(
                $"Broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(newBroadphase.Owner)}");
        }

        AddOrUpdateEntityTree(
            newBroadphase.Owner,
            newBroadphase,
            newBroadphaseXform,
            physMap,
            uid,
            xform);
    }

    public void FindAndAddToEntityTree(EntityUid uid, bool recursive = true, TransformComponent? xform = null)
    {
        if (!_xformQuery.Resolve(uid, ref xform))
            return;

        if (TryFindBroadphase(xform, out var broadphase))
            AddOrUpdateEntityTree(broadphase.Owner, broadphase, uid, xform, recursive);
    }

    /// <summary>
    ///     Variant of <see cref="FindAndAddToEntityTree(EntityUid, TransformComponent?)"/> that just re-adds the entity to the current tree (updates positions).
    /// </summary>
    public void UpdateEntityTree(EntityUid uid, TransformComponent? xform = null)
    {
        if (!_xformQuery.Resolve(uid, ref xform))
            return;

        if (!TryGetCurrentBroadphase(xform, out var broadphase))
            return;

        AddOrUpdateEntityTree(broadphase.Owner, broadphase, uid, xform);
    }

    private void AddOrUpdateEntityTree(EntityUid broadUid,
        BroadphaseComponent broadphase,
        EntityUid uid,
        TransformComponent xform,
        bool recursive = true)
    {
        var broadphaseXform = _xformQuery.GetComponent(broadphase.Owner);
        if (!_physMapQuery.TryGetComponent(broadphaseXform.MapUid, out var physMap))
        {
            throw new InvalidOperationException(
                $"Broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(broadphase.Owner)}");
        }

        AddOrUpdateEntityTree(
            broadUid,
            broadphase,
            broadphaseXform,
            physMap,
            uid,
            xform,
            recursive);
    }

    private void AddOrUpdateEntityTree(
        EntityUid broadUid,
        BroadphaseComponent broadphase,
        TransformComponent broadphaseXform,
        PhysicsMapComponent physicsMap,
        EntityUid uid,
        TransformComponent xform,
        bool recursive = true)
    {
        if (xform.Broadphase != null && !xform.Broadphase.Value.IsValid())
        {
            // This entity was explicitly removed from lookup trees, possibly because it is in a container or has
            // been detached by the PVS system. Do nothing.
            return;
        }

        if (!_physicsQuery.TryGetComponent(uid, out var body) || !body.CanCollide)
        {
            // TODO optimize this. This function iterates UP through parents, while we are currently iterating down.
            var (coordinates, rotation) = _transform.GetMoverCoordinateRotation(uid, xform);

            // TODO BROADPHASE PARENTING this just assumes local = world
            var relativeRotation = rotation - broadphaseXform.LocalRotation;

            var aabb = GetAABBNoContainer(uid, coordinates.Position, relativeRotation);
            AddOrUpdateSundriesTree(broadUid, broadphase, uid, xform, body?.BodyType == BodyType.Static, aabb);
        }
        else
        {
            AddOrUpdatePhysicsTree(uid, broadUid, broadphase, broadphaseXform, physicsMap, xform, body, _fixturesQuery.GetComponent(uid));
        }

        if (xform.ChildCount == 0 || !recursive)
            return;

        // TODO can this be removed?
        // AFAIK the separate container check is redundant now that we check for an invalid broadphase at the beginning of this function.
        if (!_containerQuery.HasComponent(uid))
        {
            foreach (var child in xform._children)
            {
                var childXform = _xformQuery.GetComponent(child);
                AddOrUpdateEntityTree(broadUid, broadphase, broadphaseXform, physicsMap, child, childXform, recursive);
            }
            return;
        }

        foreach (var child in xform._children)
        {
            if ((_metaQuery.GetComponent(child).Flags & MetaDataFlags.InContainer) != 0x0)
                continue;

            var childXform = _xformQuery.GetComponent(child);
            AddOrUpdateEntityTree(broadUid, broadphase, broadphaseXform, physicsMap, child, childXform, recursive);
        }
    }

    /// <summary>
    /// Recursively iterates through this entity's children and removes them from the BroadphaseComponent.
    /// </summary>
    public void RemoveFromEntityTree(EntityUid uid, TransformComponent xform)
    {
        if (!TryGetCurrentBroadphase(xform, out var broadphase))
            return;

        DebugTools.Assert(!_gridQuery.HasComp(uid));
        DebugTools.Assert(!_mapQuery.HasComp(uid));
        PhysicsMapComponent? physMap = null;
        if (xform.Broadphase!.Value.PhysicsMap is { Valid: true } map && !_physMapQuery.TryGetComponent(map, out physMap))
        {
            throw new InvalidOperationException(
                $"Broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(broadphase.Owner)}");
        }

        RemoveFromEntityTree(broadphase.Owner, broadphase, ref physMap, uid, xform);
    }

    /// <summary>
    /// Recursively iterates through this entity's children and removes them from the BroadphaseComponent.
    /// </summary>
    private void RemoveFromEntityTree(
        EntityUid broadUid,
        BroadphaseComponent broadphase,
        ref PhysicsMapComponent? physicsMap,
        EntityUid uid,
        TransformComponent xform,
        bool recursive = true)
    {
        if (xform.Broadphase is not { Valid: true } old)
        {
            // this entity was probably inside of a container during a recursive iteration. This should mean all of
            // its own children are also not on any broadphase.
            return;
        }

        if (old.Uid != broadUid)
        {
            // Because this gets called recursively, and because we cache the map & broadphase data, this may fail
            // when the client has deferred broadphase updates, where maybe an entity from one broadphase was
            // parented to one from another.
            DebugTools.Assert(_netMan.IsClient);
            broadUid = old.Uid;
        }

        if (old.PhysicsMap.IsValid() && physicsMap?.Owner != old.PhysicsMap)
        {
            if (!_physMapQuery.TryGetComponent(old.PhysicsMap, out physicsMap))
                Log.Error($"Entity {ToPrettyString(uid)} has missing physics map?");
        }

        if (old.CanCollide)
        {
            DebugTools.Assert(old.PhysicsMap == (physicsMap?.Owner ?? default));
            RemoveBroadTree(broadphase, _fixturesQuery.GetComponent(uid), old.Static, physicsMap);
        }
        else if (old.Static)
            broadphase.StaticSundriesTree.Remove(uid);
        else
            broadphase.SundriesTree.Remove(uid);

        xform.Broadphase = null;
        if (!recursive)
            return;

        foreach (var child in xform._children)
        {
            RemoveFromEntityTree(
                broadUid,
                broadphase,
                ref physicsMap,
                child,
                _xformQuery.GetComponent(child));
        }
    }

    public bool TryGetCurrentBroadphase(TransformComponent xform, [NotNullWhen(true)] out BroadphaseComponent? broadphase)
    {
        broadphase = null;
        if (xform.Broadphase is not { Valid: true } old)
            return false;

        if (!_broadQuery.TryGetComponent(old.Uid, out broadphase))
        {
            // broadphase was probably deleted
            DebugTools.Assert("Encountered deleted broadphase.");

            if (_fixturesQuery.TryGetComponent(xform.Owner, out FixturesComponent? fixtures))
            {
                foreach (var fixture in fixtures.Fixtures.Values)
                {
                    fixture.ProxyCount = 0;
                    fixture.Proxies = Array.Empty<FixtureProxy>();
                }
            }

            xform.Broadphase = null;
            return false;
        }

        return true;
    }

    public BroadphaseComponent? GetCurrentBroadphase(TransformComponent xform)
    {
        TryGetCurrentBroadphase(xform, out var broadphase);
        return broadphase;
    }

    public BroadphaseComponent? FindBroadphase(EntityUid uid)
    {
        TryFindBroadphase(uid, out var broadphase);
        return broadphase;
    }

    public bool TryFindBroadphase(EntityUid uid, [NotNullWhen(true)] out BroadphaseComponent? broadphase)
    {
        return TryFindBroadphase(_xformQuery.GetComponent(uid), out broadphase);
    }

    public bool TryFindBroadphase(
        TransformComponent xform,
        [NotNullWhen(true)] out BroadphaseComponent? broadphase)
    {
        if (xform.MapID == MapId.Nullspace || _container.IsEntityOrParentInContainer(xform.Owner, null, xform))
        {
            broadphase = null;
            return false;
        }

        var parent = xform.ParentUid;

        // TODO provide variant that also returns world rotation (and maybe position). Avoids having to iterate though parents twice.
        while (parent.IsValid())
        {
            if (_broadQuery.TryGetComponent(parent, out broadphase))
                return true;

            parent = _xformQuery.GetComponent(parent).ParentUid;
        }

        broadphase = null;
        return false;
    }
    #endregion

    #region Bounds

    /// <summary>
    /// Get the AABB of an entity with the supplied position and angle. Tries to consider if the entity is in a container.
    /// </summary>
    public Box2 GetAABB(EntityUid uid, Vector2 position, Angle angle, TransformComponent xform, EntityQuery<TransformComponent> xformQuery)
    {
        // If we're in a container then we just use the container's bounds.
        if (_container.TryGetOuterContainer(uid, xform, out var container, xformQuery))
        {
            return GetAABBNoContainer(container.Owner, position, angle);
        }

        return GetAABBNoContainer(uid, position, angle);
    }

    /// <summary>
    /// Get the AABB of an entity with the supplied position and angle without considering containers.
    /// </summary>
    public Box2 GetAABBNoContainer(EntityUid uid, Vector2 position, Angle angle)
    {
        if (_fixturesQuery.TryGetComponent(uid, out var fixtures))
        {
            var transform = new Transform(position, angle);

            var bounds = new Box2(transform.Position, transform.Position);
            // TODO cache this to speed up entity lookups & tree updating
            foreach (var fixture in fixtures.Fixtures.Values)
            {
                for (var i = 0; i < fixture.Shape.ChildCount; i++)
                {
                    // TODO don't transform each fixture, just transform the final AABB
                    var boundy = fixture.Shape.ComputeAABB(transform, i);
                    bounds = bounds.Union(boundy);
                }
            }

            return bounds;
        }

        var ev = new WorldAABBEvent()
        {
            AABB = new Box2(position, position),
        };

        RaiseLocalEvent(uid, ref ev);
        return ev.AABB;
    }

    public Box2 GetWorldAABB(EntityUid uid, TransformComponent? xform = null)
    {
        var xformQuery = GetEntityQuery<TransformComponent>();
        xform ??= xformQuery.GetComponent(uid);
        var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery);

        return GetAABB(uid, worldPos, worldRot, xform, xformQuery);
    }

    #endregion
}
