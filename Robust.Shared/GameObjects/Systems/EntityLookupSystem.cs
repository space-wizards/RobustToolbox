using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.BroadPhase;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
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
        /// Also return entities from an anchoring query.
        /// </summary>
        [Obsolete("Use Static")]
        Anchored = 1 << 4,

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
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        /// <summary>
        /// Returns all non-grid entities. Consider using your own flags if you wish for a faster query.
        /// </summary>
        public const LookupFlags DefaultFlags = LookupFlags.Contained | LookupFlags.Dynamic | LookupFlags.Static | LookupFlags.Sundries;

        public override void Initialize()
        {
            base.Initialize();
            var configManager = IoCManager.Resolve<IConfigurationManager>();

            SubscribeLocalEvent<BroadphaseComponent, ComponentAdd>(OnBroadphaseAdd);
            SubscribeLocalEvent<GridAddEvent>(OnGridAdd);
            SubscribeLocalEvent<MapChangedEvent>(OnMapChange);

            SubscribeLocalEvent<MoveEvent>(OnMove);

            SubscribeLocalEvent<TransformComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChange);
            SubscribeLocalEvent<TransformComponent, CollisionChangeEvent>(OnPhysicsUpdate);

            EntityManager.EntityInitialized += OnEntityInit;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntityManager.EntityInitialized -= OnEntityInit;
        }
        #region DynamicTree

        private void OnMapChange(MapChangedEvent ev)
        {
            if (ev.Created && ev.Map != MapId.Nullspace)
            {
                EnsureComp<BroadphaseComponent>(_mapManager.GetMapEntityId(ev.Map));
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

        internal void CreateProxies(Fixture fixture, Vector2 worldPos, Angle worldRot)
        {
            // TODO: Grids on broadphasecomponent
            if (_mapManager.IsGrid(fixture.Body.Owner))
                return;

            var xformQuery = GetEntityQuery<TransformComponent>();
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xform = xformQuery.GetComponent(fixture.Body.Owner);

            if (!TryComp(xform.MapUid, out SharedPhysicsMapComponent? physMap))
                throw new InvalidOperationException();

            if (!TryFindBroadphase(xformQuery.GetComponent(fixture.Body.Owner), broadQuery, xformQuery, out var broadphase))
                throw new InvalidOperationException();

            var mapTransform = new Transform(worldPos, worldRot);
            var (_, broadWorldRot, _, broadInvMatrix) = xformQuery.GetComponent(broadphase.Owner).GetWorldPositionRotationMatrixWithInv();
            var broadphaseTransform = new Transform(broadInvMatrix.Transform(mapTransform.Position), mapTransform.Quaternion2D.Angle - broadWorldRot);
            var tree = fixture.Body.BodyType == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;
            DebugTools.Assert(fixture.ProxyCount == 0);

            AddOrMoveProxies(fixture, tree, broadphaseTransform, mapTransform, physMap.MoveBuffer);
        }

        internal void DestroyProxies(Fixture fixture, TransformComponent xform)
        {
            if (_mapManager.IsGrid(fixture.Body.Owner))
                return;

            if (fixture.ProxyCount == 0)
            {
                Logger.Warning($"Tried to destroy fixture {fixture.ID} on {ToPrettyString(fixture.Body.Owner)} that already has no proxies?");
                return;
            }

            var xformQuery = GetEntityQuery<TransformComponent>();
            var broadQuery = GetEntityQuery<BroadphaseComponent>();

            if (!TryComp(xform.MapUid, out SharedPhysicsMapComponent? physMap))
                throw new InvalidOperationException();

            if (!TryFindBroadphase(xformQuery.GetComponent(fixture.Body.Owner), broadQuery, xformQuery, out var broadphase))
                throw new InvalidOperationException();

            var tree = fixture.Body.BodyType == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;
            DestroyProxies(fixture, tree, physMap.MoveBuffer);
        }

        #endregion

        #region Entity events

        private void OnPhysicsUpdate(EntityUid uid, TransformComponent xform, ref CollisionChangeEvent ev)
        {
            UpdatePhysicsBroadphase(uid, xform, ev.Body);
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

            if (xform.Broadphase is not { } old)
                return; // entity is not on any broadphase

            xform.Broadphase = null;

            if (!TryComp(old.Uid, out BroadphaseComponent? broadphase))
                return; // broadphase probably got deleted.

            // remove from the old broadphase
            var fixtures = Comp<FixturesComponent>(uid);
            if (old.CanCollide)
                RemoveBroadTree(fixtures, broadphase, old.Static);
            else
                (old.Static ? broadphase.StaticSundriesTree : broadphase.SundriesTree).Remove(uid);
            
            // Add to new broadphase
            if (body.CanCollide)
                AddPhysicsTree(old.Uid, broadphase, xform, body, fixtures);
            else
                AddSundriesTree(uid, xform, broadphase, body.BodyType == BodyType.Static);
        }

        private void RemoveBroadTree(FixturesComponent manager, BroadphaseComponent lookup, bool staticBody)
        {
            if (!TryComp<TransformComponent>(lookup.Owner, out var lookupXform))
            {
                throw new InvalidOperationException("Lookup does not exist?");
            }

            var map = lookupXform.MapUid;
            if (map == null)
            {
                // See the comments in UpdateParent()
                throw new NotSupportedException("Nullspace lookups are not supported.");
            }

            var tree = staticBody ? lookup.StaticTree : lookup.DynamicTree;
            var moveBuffer = Comp<SharedPhysicsMapComponent>(map.Value).MoveBuffer;

            foreach (var fixture in manager.Fixtures.Values)
            {
                DestroyProxies(fixture, tree, moveBuffer);
            }
        }

        private void DestroyProxies(Fixture fixture, IBroadPhase tree, Dictionary<FixtureProxy, Box2> moveBuffer)
        {
            for (var i = 0; i < fixture.ProxyCount; i++)
            {
                var proxy = fixture.Proxies[i];
                tree.RemoveProxy(proxy.ProxyId);
                moveBuffer.Remove(proxy);
            }

            fixture.ProxyCount = 0;
            fixture.Proxies = Array.Empty<FixtureProxy>();
        }

        private void AddPhysicsTree(EntityUid broadUid, BroadphaseComponent broadphase, TransformComponent xform, PhysicsComponent body, FixturesComponent fixtures)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var broadphaseXform = xformQuery.GetComponent(broadUid);

            if (!TryComp(broadUid, out SharedPhysicsMapComponent? physMap))
                throw new InvalidOperationException($"Physics Broadphase is missing physics map. {ToPrettyString(broadUid)}");

            AddPhysicsTree(broadUid, broadphase, broadphaseXform, physMap, xform, body, fixtures, xformQuery);
        }

        private void AddPhysicsTree(
            EntityUid broadUid,
            BroadphaseComponent broadphase,
            TransformComponent broadphaseXform,
            SharedPhysicsMapComponent physicsMap,
            TransformComponent xform,
            PhysicsComponent body,
            FixturesComponent manager,
            EntityQuery<TransformComponent> xformQuery)
        {
            DebugTools.Assert(!_container.IsEntityOrParentInContainer(body.Owner, null, xform, null, xformQuery));
            DebugTools.Assert(xform.Broadphase == null || xform.Broadphase == new BroadphaseData(broadphase.Owner, body.CanCollide, body.BodyType == BodyType.Static));
            DebugTools.Assert(broadphase.Owner == broadUid);

            xform.Broadphase ??= new(broadUid, body.CanCollide, body.BodyType == BodyType.Static);
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

        private void AddSundriesTree(EntityUid uid, TransformComponent xform, BroadphaseComponent broadphase, bool staticBody, Box2? aabb = null)
        {
            DebugTools.Assert(!_container.IsEntityOrParentInContainer(uid));
            DebugTools.Assert(xform.Broadphase == null || xform.Broadphase == new BroadphaseData(broadphase.Owner, false, staticBody));
            xform.Broadphase ??= new(broadphase.Owner, false, staticBody);
            (staticBody ? broadphase.StaticSundriesTree : broadphase.SundriesTree).Add(uid, aabb);
        }

        private void OnEntityInit(EntityUid uid)
        {
            if (_container.IsEntityOrParentInContainer(uid) || _mapManager.IsMap(uid) || _mapManager.IsGrid(uid))
                return;

            // TODO can this just be done implicitly via transform startup?
            // or do things need to be in trees for other component startup logic?
            AddToEntityTree(uid);
        }

        private void OnMove(ref MoveEvent args)
        {
            if (args.Component.GridUid == args.Sender)
                return;
            DebugTools.Assert(!_mapManager.IsGrid(args.Sender));

            if (args.Component.MapUid == args.Sender)
                return;
            DebugTools.Assert(!_mapManager.IsMap(args.Sender));

            if (args.ParentChanged)
                UpdateParent(args.Sender, args.Component, args.OldPosition.EntityId);
            else
                UpdateEntityTree(args.Sender, args.Component);
        }

        private void UpdateParent(EntityUid uid, TransformComponent xform, EntityUid oldParent)
        {
            if (!TryGetCurrentBroadphase(xform, out var oldBroadphase))
                return; // If the entity was not already in a broadphase, parent changes will not automatically add it.

            if (oldBroadphase != null && Transform(oldParent).MapID == MapId.Nullspace)
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

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var physicsQuery = GetEntityQuery<PhysicsComponent>();
            var fixturesQuery = GetEntityQuery<FixturesComponent>();
            TryFindBroadphase(xform, broadQuery, xformQuery, out var newBroadphase);

            if (oldBroadphase != null && oldBroadphase != newBroadphase)
                RemoveFromEntityTree(uid, xform, oldBroadphase, xformQuery, physicsQuery, fixturesQuery);

            if (newBroadphase == null)
                return;

            var metaQuery = GetEntityQuery<MetaDataComponent>();
            var contQuery = GetEntityQuery<ContainerManagerComponent>();

            AddToEntityTree(
                newBroadphase.Owner,
                newBroadphase,
                xformQuery.GetComponent(newBroadphase.Owner),
                Comp<SharedPhysicsMapComponent>(newBroadphase.Owner),
                uid,
                xform,
                xformQuery,
                metaQuery,
                contQuery,
                physicsQuery,
                fixturesQuery);
        }

        internal void AddToEntityTree(EntityUid uid, TransformComponent? xform = null)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            if (!xformQuery.Resolve(uid, ref xform))
                return;

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            if (!TryFindBroadphase(xform, broadQuery, xformQuery, out var broadphase))
                return;

            AddToEntityTree(uid, broadphase, xform, xformQuery);
        }

        /// <summary>
        ///     Variant of <see cref="AddToEntityTree(EntityUid, TransformComponent?)"/> that just re-adds the entity to the current tree (updates positions).
        /// </summary>
        internal void UpdateEntityTree(EntityUid uid, TransformComponent? xform = null)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            if (!xformQuery.Resolve(uid, ref xform))
                return;

            if (!TryGetCurrentBroadphase(xform, out var broadpase))
                return;

            AddToEntityTree(uid, broadpase, xform, xformQuery);
        }

        private void AddToEntityTree(
            EntityUid uid,
            BroadphaseComponent broadphase,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            bool recursive = true)
        {
            var metaQuery = GetEntityQuery<MetaDataComponent>();
            var contQuery = GetEntityQuery<ContainerManagerComponent>();
            var physicsQuery = GetEntityQuery<PhysicsComponent>();
            var fixturesQuery = GetEntityQuery<FixturesComponent>();
            AddToEntityTree(
                broadphase.Owner,
                broadphase,
                xformQuery.GetComponent(broadphase.Owner),
                Comp<SharedPhysicsMapComponent>(broadphase.Owner),
                uid,
                xform,
                xformQuery,
                metaQuery,
                contQuery,
                physicsQuery,
                fixturesQuery,
                recursive);
        }

        private void AddToEntityTree(
            EntityUid broadUid,
            BroadphaseComponent broadphase,
            TransformComponent broadphaseXform,
            SharedPhysicsMapComponent physicsMap,
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
                AddSundriesTree(uid, xform, broadphase, body?.BodyType == BodyType.Static, aabb);
            }
            else
            {
                AddPhysicsTree(broadUid, broadphase, broadphaseXform, physicsMap, xform, body, fixturesQuery.GetComponent(uid), xformQuery);
            }

            var childEnumerator = xform.ChildEnumerator;
            if (xform.ChildCount == 0 || !recursive)
                return;

            if (!contQuery.HasComponent(xform.Owner))
            {
                while (childEnumerator.MoveNext(out var child))
                {
                    var childXform = xformQuery.GetComponent(child.Value);
                    AddToEntityTree(broadUid, broadphase, broadphaseXform, physicsMap, child.Value, childXform, xformQuery, metaQuery, contQuery, physicsQuery, fixturesQuery);
                }
                return;
            }

            while (childEnumerator.MoveNext(out var child))
            {
                if ((metaQuery.GetComponent(child.Value).Flags & MetaDataFlags.InContainer) != 0x0)
                    continue;

                var childXform = xformQuery.GetComponent(child.Value);
                AddToEntityTree(broadUid, broadphase, broadphaseXform, physicsMap, child.Value, childXform, xformQuery, metaQuery, contQuery, physicsQuery, fixturesQuery);
            }
        }

        /// <summary>
        /// Recursively iterates through this entity's children and removes them from the BroadphaseComponent.
        /// </summary>
        internal void RemoveFromEntityTree(EntityUid uid, TransformComponent? xform = null)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            if (!xformQuery.Resolve(uid, ref xform))
                return;

            if (!TryGetCurrentBroadphase(xform, out var broadphase))
                return;

            var physicsQuery = GetEntityQuery<PhysicsComponent>();
            var fixturesQuery = GetEntityQuery<FixturesComponent>();
            RemoveFromEntityTree(uid, xform, broadphase, xformQuery, physicsQuery, fixturesQuery);
        }

        /// <summary>
        /// Recursively iterates through this entity's children and removes them from the BroadphaseComponent.
        /// </summary>
        private void RemoveFromEntityTree(
            EntityUid uid,
            TransformComponent xform,
            BroadphaseComponent lookup,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<PhysicsComponent> physicsQuery,
            EntityQuery<FixturesComponent> fixturesQuery,
            bool recursive = true)
        {
            if (xform.Broadphase == null)
            {
                // this entity was probably inside of a container during a recursive iteration. This should mean all of
                // its own children are also not on any broadphase.
                return;
            }

            if (!physicsQuery.TryGetComponent(uid, out var body) || !body.CanCollide)
            {
                if (body?.BodyType == BodyType.Static)
                {
                    DebugTools.Assert(xform.Broadphase == new BroadphaseData(lookup.Owner, false, true));
                    lookup.StaticSundriesTree.Remove(uid);
                }
                else
                {
                    DebugTools.Assert(xform.Broadphase == new BroadphaseData(lookup.Owner, false, false));
                    lookup.SundriesTree.Remove(uid);
                }
            }
            else
            {
                DebugTools.Assert(xform.Broadphase == new BroadphaseData(lookup.Owner, body.CanCollide, body.BodyType == BodyType.Static));
                RemoveBroadTree(fixturesQuery.GetComponent(uid), lookup, body.BodyType == BodyType.Static);
            }

            xform.Broadphase = null;
            if (!recursive)
                return;

            var childEnumerator = xform.ChildEnumerator;
            while (childEnumerator.MoveNext(out var child))
            {
                RemoveFromEntityTree(uid, xformQuery.GetComponent(child.Value), lookup, xformQuery, physicsQuery, fixturesQuery);
            }
        }

        public bool TryGetCurrentBroadphase(TransformComponent xform, [NotNullWhen(true)] out BroadphaseComponent? broadphase)
        {
            broadphase = null;
            if (xform.Broadphase == null)
                return false;

            if (!TryComp(xform.Broadphase.Value.Uid, out broadphase))
            {
                // broadphase was probably deleted during
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
            if (xform.MapID == MapId.Nullspace)
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
        internal Box2 GetAABB(EntityUid uid, Vector2 position, Angle angle, TransformComponent xform, EntityQuery<TransformComponent> xformQuery)
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
        private Box2 GetAABBNoContainer(EntityUid uid, Vector2 position, Angle angle)
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
            var (worldPos, worldRot) = xform.GetWorldPositionRotation();

            return GetAABB(uid, worldPos, worldRot, xform, xformQuery);
        }

        #endregion
    }
}
