using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.BroadPhase;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.GameObjects
{
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

        Uncontained = Dynamic | Static | Sundries,

        StaticSundries = Static | Sundries,
    }

    public sealed partial class EntityLookupSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly INetManager _netMan = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        /// <summary>
        /// Returns all non-grid entities. Consider using your own flags if you wish for a faster query.
        /// </summary>
        public const LookupFlags DefaultFlags = LookupFlags.Contained | LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BroadphaseComponent, EntityTerminatingEvent>(OnBroadphaseTerminating);
            SubscribeLocalEvent<BroadphaseComponent, ComponentAdd>(OnBroadphaseAdd);
            SubscribeLocalEvent<GridAddEvent>(OnGridAdd);
            SubscribeLocalEvent<MapChangedEvent>(OnMapChange);

            SubscribeLocalEvent<MoveEvent>(OnMove);

            SubscribeLocalEvent<TransformComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChange);
            SubscribeLocalEvent<CollisionChangeEvent>(OnPhysicsUpdate);

            EntityManager.EntityInitialized += OnEntityInit;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntityManager.EntityInitialized -= OnEntityInit;
        }
        #region DynamicTree

        private void OnBroadphaseTerminating(EntityUid uid, BroadphaseComponent component, ref EntityTerminatingEvent args)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var fixtureQuery = GetEntityQuery<FixturesComponent>();
            var physicsMapQuery = GetEntityQuery<PhysicsMapComponent>();
            var xform = xformQuery.GetComponent(uid);
            var map = xform.MapUid;
            physicsMapQuery.TryGetComponent(map, out var physMap);
            RemoveChildrenFromTerminatingBroadphase(xform, component, physMap, xformQuery, fixtureQuery, physicsMapQuery);
            RemComp(uid, component);
        }

        private void RemoveChildrenFromTerminatingBroadphase(TransformComponent xform,
            BroadphaseComponent component,
            PhysicsMapComponent? map,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<FixturesComponent> fixtureQuery,
            EntityQuery<PhysicsMapComponent> physicsMapQuery)
        {
            var childEnum = xform.ChildEnumerator;

            while (childEnum.MoveNext(out var child))
            {
                if (!xformQuery.TryGetComponent(child.Value, out var childXform))
                    continue;

                if (childXform.GridUid == child)
                    continue;

                if (childXform.Broadphase == null)
                    continue;

                DebugTools.Assert(childXform.Broadphase.Value.Uid == component.Owner);
                DebugTools.Assert(!_mapManager.IsGrid(child.Value));

                if (childXform.Broadphase.Value.CanCollide && fixtureQuery.TryGetComponent(child.Value, out var fixtures))
                {
                    if (map == null)
                        physicsMapQuery.TryGetComponent(childXform.Broadphase.Value.MapUid, out map);

                    DebugTools.Assert(map == null || childXform.Broadphase.Value.MapUid == map.Owner);
                    var tree = childXform.Broadphase.Value.Static ? component.StaticTree : component.DynamicTree;
                    foreach (var fixture in fixtures.Fixtures.Values)
                    {
                        DestroyProxies(fixture, tree, map);
                    }
                }

                childXform.Broadphase = null;
                RemoveChildrenFromTerminatingBroadphase(childXform, component, map, xformQuery, fixtureQuery, physicsMapQuery);
            }
        }

        private void OnMapChange(MapChangedEvent ev)
        {
            if (ev.Created && ev.Map != MapId.Nullspace)
            {
                EnsureComp<BroadphaseComponent>(ev.Uid);
            }
        }

        private void OnGridAdd(GridAddEvent ev)
        {
            // Must be done before initialization as that's when broadphase data starts getting set.
            EnsureComp<BroadphaseComponent>(ev.EntityUid);
        }

        private void OnBroadphaseAdd(EntityUid uid, BroadphaseComponent component, ComponentAdd args)
        {
            component.DynamicTree = new DynamicTreeBroadPhase();
            component.StaticTree = new DynamicTreeBroadPhase();
            component.StaticSundriesTree = new DynamicTree<EntityUid>(
                (in EntityUid value) => GetTreeAABB(value, component.Owner));
            component.SundriesTree = new DynamicTree<EntityUid>(
                (in EntityUid value) => GetTreeAABB(value, component.Owner));
        }

        private Box2 GetTreeAABB(EntityUid entity, EntityUid tree)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();

            if (!xformQuery.TryGetComponent(entity, out var xform))
            {
                Logger.Error($"Entity tree contains a deleted entity? Tree: {ToPrettyString(tree)}, entity: {entity}");
                return default;
            }

            if (xform.ParentUid == tree)
                return GetAABBNoContainer(entity, xform.LocalPosition, xform.LocalRotation);

            if (!xformQuery.TryGetComponent(tree, out var treeXform))
            {
                Logger.Error($"Entity tree has no transform? Tree Uid: {tree}");
                return default;
            }

            return treeXform.InvWorldMatrix.TransformBox(GetWorldAABB(entity, xform));
        }

        internal void CreateProxies(TransformComponent xform, Fixture fixture)
        {
            if (!TryGetCurrentBroadphase(xform, out var broadphase))
                return;

            if (!TryComp(xform.MapUid, out PhysicsMapComponent? physMap))
                throw new InvalidOperationException();

            var xformQuery = GetEntityQuery<TransformComponent>();
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery);
            var mapTransform = new Transform(worldPos, worldRot);

            var (_, broadWorldRot, _, broadInvMatrix) = xformQuery.GetComponent(broadphase.Owner).GetWorldPositionRotationMatrixWithInv();
            var broadphaseTransform = new Transform(broadInvMatrix.Transform(mapTransform.Position), mapTransform.Quaternion2D.Angle - broadWorldRot);
            var tree = fixture.Body.BodyType == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;
            DebugTools.Assert(fixture.ProxyCount == 0);

            AddOrMoveProxies(fixture, tree, broadphaseTransform, mapTransform, physMap.MoveBuffer);
        }

        internal void DestroyProxies(Fixture fixture, TransformComponent xform, BroadphaseComponent broadphase, PhysicsMapComponent? physicsMap)
        {
            DebugTools.AssertNotNull(xform.Broadphase);
            DebugTools.Assert(xform.Broadphase!.Value.Uid == broadphase.Owner);

            if (!xform.Broadphase.Value.CanCollide || xform.GridUid == xform.Owner)
                return;

            if (fixture.ProxyCount == 0)
            {
                Logger.Warning($"Tried to destroy fixture {fixture.ID} on {ToPrettyString(fixture.Body.Owner)} that already has no proxies?");
                return;
            }

            var tree = xform.Broadphase.Value.Static ? broadphase.StaticTree : broadphase.DynamicTree;
            DestroyProxies(fixture, tree, physicsMap);
        }

        #endregion

        #region Entity events

        private void OnPhysicsUpdate(ref CollisionChangeEvent ev)
        {
            var xform = Transform(ev.Body.Owner);
            UpdatePhysicsBroadphase(ev.Body.Owner, xform, ev.Body);

            // ensure that the cached broadphase is correct.
            DebugTools.Assert(_timing.ApplyingState
                || xform.Broadphase == null
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
            if (xform.GridUid == uid)
                return;
            DebugTools.Assert(!_mapManager.IsGrid(uid));

            if (xform.Broadphase is not { Valid: true } old)
                return; // entity is not on any broadphase

            xform.Broadphase = null;

            if (!TryComp(old.Uid, out BroadphaseComponent? broadphase))
                return; // broadphase probably got deleted.

            // remove from the old broadphase
            var fixtures = Comp<FixturesComponent>(uid);
            if (old.CanCollide)
            {
                TryComp(old.MapUid, out PhysicsMapComponent? physicsMap);
                RemoveBroadTree(broadphase, fixtures, old.Static, physicsMap);
            }
            else
                (old.Static ? broadphase.StaticSundriesTree : broadphase.SundriesTree).Remove(uid);

            // Add to new broadphase
            if (body.CanCollide)
                AddPhysicsTree(old.Uid, broadphase, xform, body, fixtures);
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

        private void AddPhysicsTree(EntityUid broadUid, BroadphaseComponent broadphase, TransformComponent xform, PhysicsComponent body, FixturesComponent fixtures)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var broadphaseXform = xformQuery.GetComponent(broadUid);

            if (broadphaseXform.MapID == MapId.Nullspace)
                return;

            if (!TryComp(broadphaseXform.MapUid, out PhysicsMapComponent? physMap))
                throw new InvalidOperationException($"Physics Broadphase is missing physics map. {ToPrettyString(broadUid)}");

            AddOrUpdatePhysicsTree(broadUid, broadphase, broadphaseXform, physMap, xform, body, fixtures, xformQuery);
        }

        private void AddOrUpdatePhysicsTree(
            EntityUid broadUid,
            BroadphaseComponent broadphase,
            TransformComponent broadphaseXform,
            PhysicsMapComponent physicsMap,
            TransformComponent xform,
            PhysicsComponent body,
            FixturesComponent manager,
            EntityQuery<TransformComponent> xformQuery)
        {
            DebugTools.Assert(!_container.IsEntityOrParentInContainer(body.Owner, null, xform, null, xformQuery));
            DebugTools.Assert(xform.Broadphase == null || xform.Broadphase == new BroadphaseData(broadphase.Owner, physicsMap.Owner, body.CanCollide, body.BodyType == BodyType.Static));
            DebugTools.Assert(broadphase.Owner == broadUid);

            xform.Broadphase ??= new(broadUid, physicsMap.Owner, body.CanCollide, body.BodyType == BodyType.Static);
            var tree = body.BodyType == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;

            // TOOD optimize this. This function iterates UP through parents, while we are currently iterating down.
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery);
            var mapTransform = new Transform(worldPos, worldRot);

            // TODO BROADPHASE PARENTING this just assumes local = world
            var broadphaseTransform = new Transform(broadphaseXform.InvLocalMatrix.Transform(mapTransform.Position), mapTransform.Quaternion2D.Angle - broadphaseXform.LocalRotation);

            foreach (var fixture in manager.Fixtures.Values)
            {
                AddOrMoveProxies(fixture, tree, broadphaseTransform, mapTransform, physicsMap.MoveBuffer);
            }
        }

        private void AddOrMoveProxies(
            Fixture fixture,
            IBroadPhase tree,
            Transform broadphaseTransform,
            Transform mapTransform,
            Dictionary<FixtureProxy, Box2> moveBuffer)
        {
            DebugTools.Assert(fixture.Body.CanCollide);

            // Moving
            if (fixture.ProxyCount > 0)
            {
                for (var i = 0; i < fixture.ProxyCount; i++)
                {
                    var bounds = fixture.Shape.ComputeAABB(broadphaseTransform, i);
                    var proxy = fixture.Proxies[i];
                    tree.MoveProxy(proxy.ProxyId, bounds, Vector2.Zero);
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
                var proxy = new FixtureProxy(bounds, fixture, i);
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

        private void OnEntityInit(EntityUid uid)
        {
            if (_container.IsEntityOrParentInContainer(uid) || _mapManager.IsMap(uid) || _mapManager.IsGrid(uid))
                return;

            // TODO can this just be done implicitly via transform startup?
            // or do things need to be in trees for other component startup logic?
            FindAndAddToEntityTree(uid);
        }

        private void OnMove(ref MoveEvent args)
        {
            if (args.Component.GridUid == args.Sender)
            {
                if (args.ParentChanged) // grid changed maps, need to update children and clear the move buffer.
                    OnGridChangedMap(args);
                return;
            }
            DebugTools.Assert(!_mapManager.IsGrid(args.Sender));

            if (args.Component.MapUid == args.Sender)
                return;
            DebugTools.Assert(!_mapManager.IsMap(args.Sender));

            if (args.ParentChanged)
                UpdateParent(args.Sender, args.Component, args.OldPosition.EntityId);
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
            var xformQuery = GetEntityQuery<TransformComponent>();
            var fixturesQuery = GetEntityQuery<FixturesComponent>();

            DebugTools.Assert(HasComp<MapGridComponent>(args.Sender));
            DebugTools.Assert(!newMap.IsValid() || HasComp<MapComponent>(newMap));
            DebugTools.Assert(!oldMap.IsValid() || HasComp<MapComponent>(oldMap));

            var oldBuffer = CompOrNull<PhysicsMapComponent>(oldMap)?.MoveBuffer;
            var newBuffer = CompOrNull<PhysicsMapComponent>(newMap)?.MoveBuffer;

            var enumerator = args.Component.ChildEnumerator;
            while (enumerator.MoveNext(out var child))
            {
                RecursiveOnGridChangedMap(child.Value, oldMap, newMap, oldBuffer, newBuffer, xformQuery, fixturesQuery);
            }
        }

        private void RecursiveOnGridChangedMap(
            EntityUid uid,
            EntityUid oldMap,
            EntityUid newMap,
            Dictionary<FixtureProxy, Box2>? oldBuffer,
            Dictionary<FixtureProxy, Box2>? newBuffer,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<FixturesComponent> fixturesQuery)
        {
            if (!xformQuery.TryGetComponent(uid, out var xform))
                return;

            var enumerator = xform.ChildEnumerator;
            while (enumerator.MoveNext(out var child))
            {
                RecursiveOnGridChangedMap(child.Value, oldMap, newMap, oldBuffer, newBuffer, xformQuery, fixturesQuery);
            }

            if (xform.Broadphase == null || !xform.Broadphase.Value.CanCollide)
                return;

            DebugTools.Assert(_netMan.IsClient || !xform.Broadphase.Value.MapUid.IsValid() || xform.Broadphase.Value.MapUid == oldMap);
            xform.Broadphase = xform.Broadphase.Value with { MapUid = newMap };

            if (!fixturesQuery.TryGetComponent(uid, out var fixtures))
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
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery);
            var mapTransform = new Transform(worldPos, worldRot);

            foreach (var fixture in fixtures.Fixtures.Values)
                for (var i = 0; i < fixture.ProxyCount; i++)
                {
                    var proxy = fixture.Proxies[i];
                    newBuffer[proxy] = fixture.Shape.ComputeAABB(mapTransform, i);
                }
        }

        private void UpdateParent(EntityUid uid, TransformComponent xform, EntityUid oldParent)
        {
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var fixturesQuery = GetEntityQuery<FixturesComponent>();

            BroadphaseComponent? oldBroadphase = null;
            PhysicsMapComponent? oldPhysMap = null;
            if (xform.Broadphase != null)
            {
                if (!xform.Broadphase.Value.IsValid())
                    return; // Entity is intentionally not on a broadphase (deferred updating?).

                TryComp(xform.Broadphase.Value.MapUid, out oldPhysMap);

                if (!broadQuery.TryGetComponent(xform.Broadphase.Value.Uid, out oldBroadphase))
                {

                    DebugTools.Assert("Encountered deleted broadphase.");

                    // broadphase was probably deleted.
                    if (fixturesQuery.TryGetComponent(uid, out var fixtures))
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

            if (oldBroadphase != null && xformQuery.GetComponent(oldParent).MapID == MapId.Nullspace)
            {
                oldBroadphase = null;
                // Note that the parentXform.MapID != MapId.Nullspace is required because currently grids are not allowed to
                // ever enter null-space. If they are in null-space, we assume that the grid is being deleted, as otherwise
                // RemoveFromEntityTree() will explode. This may eventually have to change if we stop universally sending
                // all grids to all players (i.e., out-of view grids will need to get sent to null-space)
                //
                // This also means the queries above can be reverted (check broadQuery, then xformQuery, as this will
                // generally save a component lookup.
            }

            TryFindBroadphase(xform, broadQuery, xformQuery, out var newBroadphase);

            var physicsQuery = GetEntityQuery<PhysicsComponent>();
            if (oldBroadphase != null && oldBroadphase != newBroadphase)
            {
                RemoveFromEntityTree(oldBroadphase.Owner, oldBroadphase, ref oldPhysMap, uid, xform, xformQuery, fixturesQuery);
            }

            if (newBroadphase == null)
                return;

            var metaQuery = GetEntityQuery<MetaDataComponent>();
            var contQuery = GetEntityQuery<ContainerManagerComponent>();

            var newBroadphaseXform = xformQuery.GetComponent(newBroadphase.Owner);
            if (!TryComp(newBroadphaseXform.MapUid, out PhysicsMapComponent? physMap))
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
                xform,
                xformQuery,
                metaQuery,
                contQuery,
                physicsQuery,
                fixturesQuery);
        }

        public void FindAndAddToEntityTree(EntityUid uid, TransformComponent? xform = null)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            if (!xformQuery.Resolve(uid, ref xform))
                return;

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            if (TryFindBroadphase(xform, broadQuery, xformQuery, out var broadphase))
                AddOrUpdateEntityTree(broadphase, uid, xform, xformQuery);
        }

        public void FindAndAddToEntityTree(EntityUid uid,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<MetaDataComponent> metaQuery,
            EntityQuery<ContainerManagerComponent> contQuery,
            EntityQuery<PhysicsComponent> physicsQuery,
            EntityQuery<FixturesComponent> fixturesQuery,
            EntityQuery<BroadphaseComponent> broadQuery)
        {
            if (TryFindBroadphase(xform, broadQuery, xformQuery, out var broadphase))
                AddOrUpdateEntityTree(broadphase.Owner, broadphase, uid, xform, xformQuery, metaQuery, contQuery, physicsQuery, fixturesQuery, true);
        }

        /// <summary>
        ///     Variant of <see cref="FindAndAddToEntityTree(EntityUid, TransformComponent?)"/> that just re-adds the entity to the current tree (updates positions).
        /// </summary>
        public void UpdateEntityTree(EntityUid uid, TransformComponent? xform = null)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            if (!xformQuery.Resolve(uid, ref xform))
                return;

            if (!TryGetCurrentBroadphase(xform, out var broadphase))
                return;

            AddOrUpdateEntityTree(broadphase, uid, xform, xformQuery);
        }

        private void AddOrUpdateEntityTree(
            BroadphaseComponent broadphase,
            EntityUid uid,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            bool recursive = true)
        {
            var metaQuery = GetEntityQuery<MetaDataComponent>();
            var contQuery = GetEntityQuery<ContainerManagerComponent>();
            var physicsQuery = GetEntityQuery<PhysicsComponent>();
            var fixturesQuery = GetEntityQuery<FixturesComponent>();

            AddOrUpdateEntityTree(broadphase.Owner, broadphase, uid, xform, xformQuery, metaQuery, contQuery, physicsQuery, fixturesQuery, recursive);
        }

        private void AddOrUpdateEntityTree(EntityUid broadUid,
            BroadphaseComponent broadphase,
            EntityUid uid,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<MetaDataComponent> metaQuery,
            EntityQuery<ContainerManagerComponent> contQuery,
            EntityQuery<PhysicsComponent> physicsQuery,
            EntityQuery<FixturesComponent> fixturesQuery,
            bool recursive)
        {
            var broadphaseXform = xformQuery.GetComponent(broadphase.Owner);
            if (!TryComp(broadphaseXform.MapUid, out PhysicsMapComponent? physMap))
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
                xformQuery,
                metaQuery,
                contQuery,
                physicsQuery,
                fixturesQuery,
                recursive);
        }

        private void AddOrUpdateEntityTree(
            EntityUid broadUid,
            BroadphaseComponent broadphase,
            TransformComponent broadphaseXform,
            PhysicsMapComponent physicsMap,
            EntityUid uid,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<MetaDataComponent> metaQuery,
            EntityQuery<ContainerManagerComponent> contQuery,
            EntityQuery<PhysicsComponent> physicsQuery,
            EntityQuery<FixturesComponent> fixturesQuery,
            bool recursive = true)
        {
            if (!physicsQuery.TryGetComponent(uid, out var body) || !body.CanCollide)
            {
                // TOOD optimize this. This function iterates UP through parents, while we are currently iterating down.
                var (coordinates, rotation) = _transform.GetMoverCoordinateRotation(xform, xformQuery);

                // TODO BROADPHASE PARENTING this just assumes local = world
                var relativeRotation = rotation - broadphaseXform.LocalRotation;

                var aabb = GetAABBNoContainer(uid, coordinates.Position, relativeRotation);
                AddOrUpdateSundriesTree(broadUid, broadphase, uid, xform, body?.BodyType == BodyType.Static, aabb);
            }
            else
            {
                AddOrUpdatePhysicsTree(broadUid, broadphase, broadphaseXform, physicsMap, xform, body, fixturesQuery.GetComponent(uid), xformQuery);
            }

            var childEnumerator = xform.ChildEnumerator;
            if (xform.ChildCount == 0 || !recursive)
                return;

            if (!contQuery.HasComponent(xform.Owner))
            {
                while (childEnumerator.MoveNext(out var child))
                {
                    var childXform = xformQuery.GetComponent(child.Value);
                    AddOrUpdateEntityTree(broadUid, broadphase, broadphaseXform, physicsMap, child.Value, childXform, xformQuery, metaQuery, contQuery, physicsQuery, fixturesQuery);
                }
                return;
            }

            while (childEnumerator.MoveNext(out var child))
            {
                if ((metaQuery.GetComponent(child.Value).Flags & MetaDataFlags.InContainer) != 0x0)
                    continue;

                var childXform = xformQuery.GetComponent(child.Value);
                AddOrUpdateEntityTree(broadUid, broadphase, broadphaseXform, physicsMap, child.Value, childXform, xformQuery, metaQuery, contQuery, physicsQuery, fixturesQuery);
            }
        }

        /// <summary>
        /// Recursively iterates through this entity's children and removes them from the BroadphaseComponent.
        /// </summary>
        public void RemoveFromEntityTree(EntityUid uid, TransformComponent xform, EntityQuery<TransformComponent> xformQuery)
        {
            if (!TryGetCurrentBroadphase(xform, out var broadphase))
                return;

            var physicsQuery = GetEntityQuery<PhysicsComponent>();
            var fixturesQuery = GetEntityQuery<FixturesComponent>();

            PhysicsMapComponent? physMap = null;
            if (xform.Broadphase!.Value.MapUid is { Valid: true } map && !TryComp(map, out physMap))
            {
                throw new InvalidOperationException(
                    $"Broadphase's map is missing a physics map comp. Broadphase: {ToPrettyString(broadphase.Owner)}");
            }

            RemoveFromEntityTree(broadphase.Owner, broadphase, ref physMap, uid, xform, xformQuery, fixturesQuery);
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
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<FixturesComponent> fixturesQuery,
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

            if (old.MapUid.IsValid() && physicsMap?.Owner != old.MapUid)
            {
                if (!TryComp(old.MapUid, out physicsMap))
                    Logger.Error($"Entity {ToPrettyString(uid)} has missing physics map?");
            }

            if (old.CanCollide)
            {
                DebugTools.Assert(old.MapUid == (physicsMap?.Owner ?? default));
                RemoveBroadTree(broadphase, fixturesQuery.GetComponent(uid), old.Static, physicsMap);
            }
            else if (old.Static)
                broadphase.StaticSundriesTree.Remove(uid);
            else
                broadphase.SundriesTree.Remove(uid);

            xform.Broadphase = null;
            if (!recursive)
                return;

            var childEnumerator = xform.ChildEnumerator;
            while (childEnumerator.MoveNext(out var child))
            {
                RemoveFromEntityTree(
                    broadUid,
                    broadphase,
                    ref physicsMap,
                    child.Value,
                    xformQuery.GetComponent(child.Value),
                    xformQuery,
                    fixturesQuery);
            }
        }

        public bool TryGetCurrentBroadphase(TransformComponent xform, [NotNullWhen(true)] out BroadphaseComponent? broadphase)
        {
            broadphase = null;
            if (xform.Broadphase is not { Valid: true } old)
                return false;

            if (!TryComp(old.Uid, out broadphase))
            {
                // broadphase was probably deleted
                DebugTools.Assert("Encountered deleted broadphase.");

                if (TryComp(xform.Owner, out FixturesComponent? fixtures))
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
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            return TryFindBroadphase(xformQuery.GetComponent(uid), broadQuery, xformQuery, out broadphase);
        }

        public bool TryFindBroadphase(
            TransformComponent xform,
            EntityQuery<BroadphaseComponent> broadQuery,
            EntityQuery<TransformComponent> xformQuery,
            [NotNullWhen(true)] out BroadphaseComponent? broadphase)
        {
            if (xform.MapID == MapId.Nullspace || _container.IsEntityOrParentInContainer(xform.Owner, null, xform, null, xformQuery))
            {
                broadphase = null;
                return false;
            }

            var parent = xform.ParentUid;

            // TODO provide variant that also returns world rotation (and maybe position). Avoids having to iterate though parents twice.
            while (parent.IsValid())
            {
                if (broadQuery.TryGetComponent(parent, out broadphase))
                    return true;

                parent = xformQuery.GetComponent(parent).ParentUid;
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
            if (TryComp<ILookupWorldBox2Component>(uid, out var worldLookup))
            {
                var transform = new Transform(position, angle);
                return worldLookup.GetAABB(transform);
            }
            else
            {
                return new Box2(position, position);
            }
        }

        public Box2 GetWorldAABB(EntityUid uid, TransformComponent? xform = null)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            xform ??= xformQuery.GetComponent(uid);
            var (worldPos, worldRot) = xform.GetWorldPositionRotation(xformQuery);

            return GetAABB(uid, worldPos, worldRot, xform, xformQuery);
        }

        #endregion
    }
}
