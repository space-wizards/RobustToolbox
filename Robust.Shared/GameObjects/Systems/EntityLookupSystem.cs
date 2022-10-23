using System;
using System.Collections.Generic;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.BroadPhase;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Events;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

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
        /// Also return entities from an anchoring query.
        /// </summary>
        Anchored = 1 << 1,

        /// <summary>
        /// Include entities that are currently in containers.
        /// </summary>
        Contained = 1 << 2,

        // TODO: Need Dynamic, Static, and Sundries
        // Anchored needs killing
    }

    public sealed partial class EntityLookupSystem : EntitySystem
    {
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly SharedContainerSystem _container = default!;
        [Dependency] private readonly SharedPhysicsSystem _physics = default!;
        [Dependency] private readonly SharedTransformSystem _transform = default!;

        /// <summary>
        /// Returns all non-grid entities. Consider using your own flags if you wish for a faster query.
        /// </summary>
        public const LookupFlags DefaultFlags = LookupFlags.Contained | LookupFlags.Anchored;

        private const int GrowthRate = 256;

        private const float PointEnlargeRange = .00001f / 2;

        /// <summary>
        /// Like RenderTree we need to enlarge our lookup range for EntityLookupComponent as an entity is only ever on
        /// 1 EntityLookupComponent at a time (hence it may overlap without another lookup).
        /// </summary>
        private float _lookupEnlargementRange;

        public override void Initialize()
        {
            base.Initialize();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.LookupEnlargementRange, value => _lookupEnlargementRange = value, true);

            SubscribeLocalEvent<BroadphaseComponent, ComponentAdd>(OnBroadphaseAdd);
            SubscribeLocalEvent<GridAddEvent>(OnGridAdd);
            SubscribeLocalEvent<MapChangedEvent>(OnMapChange);

            SubscribeLocalEvent<MoveEvent>(OnMove);
            SubscribeLocalEvent<EntParentChangedMessage>(OnParentChange);
            SubscribeLocalEvent<EntInsertedIntoContainerMessage>(OnContainerInsert);
            SubscribeLocalEvent<EntRemovedFromContainerMessage>(OnContainerRemove);

            SubscribeLocalEvent<PhysicsComponent, PhysicsBodyTypeChangedEvent>(OnBodyTypeChange);
            SubscribeLocalEvent<CollisionChangeEvent>(OnPhysicsUpdate);

            EntityManager.EntityInitialized += OnEntityInit;
        }

        public override void Shutdown()
        {
            base.Shutdown();
            EntityManager.EntityInitialized -= OnEntityInit;
        }

        /// <summary>
        /// Updates the entity's AABB. Uses <see cref="ILookupWorldBox2Component"/>
        /// </summary>
        [UsedImplicitly]
        public void UpdateBounds(EntityUid uid, TransformComponent? xform = null, MetaDataComponent? meta = null)
        {
            if (_container.IsEntityInContainer(uid, meta))
                return;

            var xformQuery = GetEntityQuery<TransformComponent>();

            if (!xformQuery.Resolve(uid, ref xform) || xform.Anchored)
                return;

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var lookup = GetBroadphase(uid, xform, broadQuery, xformQuery);

            if (lookup == null) return;

            var coordinates = _transform.GetMoverCoordinates(xform.Coordinates, xformQuery);
            var lookupRotation = _transform.GetWorldRotation(lookup.Owner, xformQuery);
            // If we're contained then LocalRotation should be 0 anyway.
            var aabb = GetAABB(xform.Owner, coordinates.Position, _transform.GetWorldRotation(xform, xformQuery) - lookupRotation, xform, xformQuery);

            // TODO: Only container children need updating so could manually do this slightly better.
            AddToEntityTree(lookup, xform, aabb, xformQuery, lookupRotation);
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
            var xformQuery = GetEntityQuery<TransformComponent>();
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xform = xformQuery.GetComponent(fixture.Body.Owner);
            var broadphase = GetBroadphase(fixture.Body.Owner, xformQuery.GetComponent(fixture.Body.Owner), broadQuery, xformQuery);

            if (broadphase == null || xform.MapUid == null)
            {
                throw new InvalidOperationException();
            }

            var mapTransform = _physics.GetPhysicsTransform(fixture.Body.Owner, xform, xformQuery);
            var (_, broadWorldRot, _, broadInvMatrix) = xformQuery.GetComponent(broadphase.Owner).GetWorldPositionRotationMatrixWithInv();
            var broadphaseTransform = new Transform(broadInvMatrix.Transform(mapTransform.Position), mapTransform.Quaternion2D.Angle - broadWorldRot);
            var moveBuffer = Comp<SharedPhysicsMapComponent>(xform.MapUid.Value).MoveBuffer;
            var tree = fixture.Body.BodyType == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;

            AddProxies(fixture, tree, broadphaseTransform, mapTransform, moveBuffer);
        }

        internal void DestroyProxies(Fixture fixture, TransformComponent xform)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var broadphase = GetBroadphase(fixture.Body.Owner, xformQuery.GetComponent(fixture.Body.Owner), broadQuery, xformQuery);

            if (broadphase == null)
            {
                throw new InvalidOperationException();
            }

            var tree = fixture.Body.BodyType == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;
            DestroyProxies(fixture, tree);
        }

        #endregion

        #region Entity events

        private void OnPhysicsUpdate(ref CollisionChangeEvent ev)
        {
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var broadphase = GetBroadphase(ev.Body.Owner, xformQuery.GetComponent(ev.Body.Owner), broadQuery, xformQuery);

            if (broadphase == null)
                return;

            if (ev.CanCollide)
            {
                RemoveSundriesTree(ev.Body.Owner, broadphase);
                AddBroadTree(ev.Body, broadphase, ev.Body.BodyType);
            }
            else
            {
                RemoveBroadTree(ev.Body, broadphase, ev.Body.BodyType);
                AddSundriesTree(ev.Body.Owner, broadphase);
            }
        }

        private void OnBodyTypeChange(EntityUid uid, PhysicsComponent component, ref PhysicsBodyTypeChangedEvent args)
        {
            if (!component.CanCollide || HasComp<IMapGridComponent>(uid))
                return;

            var broadphase = GetBroadphase(Transform(uid));

            if (broadphase == null)
                return;

            if (args.Old != BodyType.Static && args.New != BodyType.Static)
                return;

            RemoveBroadTree(component, broadphase, args.Old);
            AddBroadTree(component, broadphase, component.BodyType);
        }

        private void RemoveBroadTree(PhysicsComponent body, BroadphaseComponent lookup, BodyType bodyType, FixturesComponent? manager = null)
        {
            if (!Resolve(body.Owner, ref manager))
                return;

            var tree = bodyType == BodyType.Static ? lookup.StaticTree : lookup.DynamicTree;

            foreach (var (_, fixture) in manager.Fixtures)
            {
                DestroyProxies(fixture, tree);
            }
        }

        private void DestroyProxies(Fixture fixture, IBroadPhase tree)
        {
            for (var i = 0; i < fixture.ProxyCount; i++)
            {
                tree.RemoveProxy(fixture.Proxies[i].ProxyId);
            }

            fixture.ProxyCount = 0;
            fixture.Proxies = Array.Empty<FixtureProxy>();
        }

        private void AddBroadTree(PhysicsComponent body, BroadphaseComponent lookup, BodyType bodyType, FixturesComponent? manager = null)
        {
            if (!Resolve(body.Owner, ref manager))
                return;

            var tree = bodyType == BodyType.Static ? lookup.StaticTree : lookup.DynamicTree;
            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(body.Owner);

            if (xform.MapUid == null)
            {
                throw new InvalidOperationException();
            }

            var mapTransform = _physics.GetPhysicsTransform(body.Owner, xform, xformQuery);
            var (_, broadWorldRot, _, broadInvMatrix) = xformQuery.GetComponent(lookup.Owner).GetWorldPositionRotationMatrixWithInv();
            var broadphaseTransform = new Transform(broadInvMatrix.Transform(mapTransform.Position), mapTransform.Quaternion2D.Angle - broadWorldRot);
            var moveBuffer = Comp<SharedPhysicsMapComponent>(xform.MapUid.Value).MoveBuffer;

            foreach (var (_, fixture) in manager.Fixtures)
            {
                AddProxies(fixture, tree, broadphaseTransform, mapTransform, moveBuffer);
            }
        }

        private void AddProxies(
            Fixture fixture,
            IBroadPhase tree,
            Transform broadphaseTransform,
            Transform mapTransform,
            Dictionary<FixtureProxy, Box2> moveBuffer)
        {
            var count = fixture.Shape.ChildCount;
            var proxies = fixture.Proxies;
            Array.Resize(ref proxies, count);

            for (var i = 0; i < count; i++)
            {
                var bounds = fixture.Shape.ComputeAABB(broadphaseTransform, i);
                var proxy = new FixtureProxy(bounds, fixture, i);
                proxy.ProxyId = tree.AddProxy(ref proxy);
                proxies[i] = proxy;
                moveBuffer[proxy] = fixture.Shape.ComputeAABB(mapTransform, i);
            }

            fixture.ProxyCount = count;
        }

        private void AddSundriesTree(EntityUid uid, BroadphaseComponent lookup)
        {
            var tree = lookup.SundriesTree;
            tree.Add(uid);
        }

        private void RemoveSundriesTree(EntityUid uid, BroadphaseComponent lookup)
        {
            var tree = lookup.SundriesTree;
            tree.Remove(uid);
        }

        private void OnEntityInit(EntityUid uid)
        {
            if (_container.IsEntityInContainer(uid))
                return;

            var xformQuery = GetEntityQuery<TransformComponent>();

            if (!xformQuery.TryGetComponent(uid, out var xform))
            {
                return;
            }

            if (_mapManager.IsMap(uid) ||
                _mapManager.IsGrid(uid))
            {
                return;
            }

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var lookup = GetBroadphase(uid, xform, broadQuery, xformQuery);

            // If nullspace or the likes.
            if (lookup == null) return;

            var coordinates = _transform.GetMoverCoordinates(xform.Coordinates, xformQuery);
            DebugTools.Assert(coordinates.EntityId == lookup.Owner);
            var lookupRotation = _transform.GetWorldRotation(lookup.Owner, xformQuery);

            // If we're contained then LocalRotation should be 0 anyway.
            var aabb = GetAABB(uid, coordinates.Position, _transform.GetWorldRotation(xform, xformQuery) - lookupRotation, xform, xformQuery);

            // Any child entities should be handled by their own OnEntityInit
            AddToEntityTree(lookup, xform, aabb, xformQuery, lookupRotation, false);
        }

        private void OnMove(ref MoveEvent args)
        {
            UpdatePosition(args.Sender, args.Component);
        }

        private void UpdatePosition(EntityUid uid, TransformComponent xform)
        {
            // Even if the entity is contained it may have children that aren't so we still need to update.
            if (!CanMoveUpdate(uid)) return;

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var lookup = GetBroadphase(uid, xform, broadQuery, xformQuery);

            if (lookup == null) return;

            var coordinates = _transform.GetMoverCoordinates(xform.Coordinates, xformQuery);
            var lookupRotation = _transform.GetWorldRotation(lookup.Owner, xformQuery);
            var aabb = GetAABB(uid, coordinates.Position, _transform.GetWorldRotation(xform) - lookupRotation, xform, xformQuery);
            AddToEntityTree(lookup, xform, aabb, xformQuery, lookupRotation);
        }

        private bool CanMoveUpdate(EntityUid uid)
        {
            return !_mapManager.IsMap(uid) &&
                     !_mapManager.IsGrid(uid) &&
                     !_container.IsEntityInContainer(uid);
        }

        private void OnParentChange(ref EntParentChangedMessage args)
        {
            var meta = MetaData(args.Entity);

            // If our parent is changing due to a container-insert, we let the container insert event handle that. Note
            // that the in-container flag gets set BEFORE insert parent change, and gets unset before the container
            // removal parent-change. So if it is set here, this must mean we are getting inserted.
            //
            // However, this means that this method will still get run in full on container removal. Additionally,
            // because not all container removals are guaranteed to result in a parent change, container removal events
            // also need to add the entity to a tree. So this generally results in:
            // add-to-tree -> remove-from-tree -> add-to-tree.
            // Though usually, `oldLookup == newLookup` for the last step. Its still shit though.
            //
            // TODO IMPROVE CONTAINER REMOVAL HANDLING

            if (_container.IsEntityInContainer(args.Entity, meta))
                return;

            if (meta.EntityLifeStage < EntityLifeStage.Initialized ||
                _mapManager.IsGrid(args.Entity) ||
                _mapManager.IsMap(args.Entity))
            {
                return;
            }

            var xformQuery = GetEntityQuery<TransformComponent>();
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xform = args.Transform;
            BroadphaseComponent? oldLookup = null;

            if (args.OldMapId != MapId.Nullspace && xformQuery.TryGetComponent(args.OldParent, out var parentXform))
            {
                oldLookup = GetBroadphase(args.OldParent.Value, parentXform, broadQuery, xformQuery);
            }

            var newLookup = GetBroadphase(args.Entity, xform, broadQuery, xformQuery);

            // If parent is the same then no need to do anything as position should stay the same.
            if (oldLookup == newLookup) return;

            RemoveFromEntityTree(oldLookup, xform, xformQuery);

            if (newLookup != null)
                AddToEntityTree(newLookup, xform, xformQuery, _transform.GetWorldRotation(newLookup.Owner, xformQuery));
        }

        private void OnContainerRemove(EntRemovedFromContainerMessage ev)
        {
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(ev.Entity);
            var lookup = GetBroadphase(ev.Entity, xform, broadQuery, xformQuery);

            if (lookup == null) return;

            AddToEntityTree(lookup, xform, xformQuery, _transform.GetWorldRotation(lookup.Owner, xformQuery));
        }

        private void OnContainerInsert(EntInsertedIntoContainerMessage ev)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();

            if (ev.OldParent == EntityUid.Invalid || !xformQuery.TryGetComponent(ev.OldParent, out var oldXform))
                return;

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var lookup = GetBroadphase(ev.OldParent, oldXform, broadQuery, xformQuery);

            RemoveFromEntityTree(lookup, xformQuery.GetComponent(ev.Entity), xformQuery);
        }

        private void AddToEntityTree(
            BroadphaseComponent lookup,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            Angle lookupRotation,
            bool recursive = true)
        {
            var coordinates = _transform.GetMoverCoordinates(xform.Coordinates, xformQuery);
            // If we're contained then LocalRotation should be 0 anyway.
            var aabb = GetAABB(xform.Owner, coordinates.Position, _transform.GetWorldRotation(xform, xformQuery) - lookupRotation, xform, xformQuery);
            AddToEntityTree(lookup, xform, aabb, xformQuery, lookupRotation, recursive);
        }

        private void AddToEntityTree(
            BroadphaseComponent? lookup,
            TransformComponent xform,
            Box2 aabb,
            EntityQuery<TransformComponent> xformQuery,
            Angle lookupRotation,
            bool recursive = true)
        {
            // If entity is in nullspace then no point keeping track of data structure.
            if (lookup == null) return;

            AddTree(xform.Owner, lookup, aabb);

            var childEnumerator = xform.ChildEnumerator;

            if (xform.ChildCount == 0 || !recursive) return;

            // If they're in a container then don't add to entitylookup due to the additional cost.
            // It's cheaper to just query these components at runtime given PVS no longer uses EntityLookupSystem.
            if (EntityManager.TryGetComponent<ContainerManagerComponent>(xform.Owner, out var conManager))
            {
                while (childEnumerator.MoveNext(out var child))
                {
                    if (conManager.ContainsEntity(child.Value)) continue;

                    var childXform = xformQuery.GetComponent(child.Value);
                    var coordinates = _transform.GetMoverCoordinates(childXform.Coordinates, xformQuery);
                    // TODO: If we have 0 position and not contained can optimise these further, but future problem.
                    var childAABB = GetAABBNoContainer(child.Value, coordinates.Position, childXform.WorldRotation - lookupRotation);
                    AddToEntityTree(lookup, childXform, childAABB, xformQuery, lookupRotation);
                }
            }
            else
            {
                while (childEnumerator.MoveNext(out var child))
                {
                    var childXform = xformQuery.GetComponent(child.Value);
                    var coordinates = _transform.GetMoverCoordinates(childXform.Coordinates, xformQuery);
                    // TODO: If we have 0 position and not contained can optimise these further, but future problem.
                    var childAABB = GetAABBNoContainer(child.Value, coordinates.Position, childXform.WorldRotation - lookupRotation);
                    AddToEntityTree(lookup, childXform, childAABB, xformQuery, lookupRotation);
                }
            }
        }

        private void AddTree(EntityUid uid, BroadphaseComponent broadphase, Box2 aabb, PhysicsComponent? body = null)
        {
            if (!Resolve(uid, ref body, false) || !body.CanCollide)
            {
                broadphase.SundriesTree.AddOrUpdate(uid, aabb);
                return;
            }

            AddBroadTree(body, broadphase, body.BodyType);
        }

        private void RemoveTree(EntityUid uid, BroadphaseComponent broadphase, PhysicsComponent? body = null)
        {
            if (!Resolve(uid, ref body, false) || !body.CanCollide)
            {
                broadphase.SundriesTree.Remove(uid);
                return;
            }

            RemoveBroadTree(body, broadphase, body.BodyType);
        }

        private void RemoveFromEntityTree(EntityUid uid, bool recursive = true)
        {
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(uid);
            var lookup = GetBroadphase(uid, xform, broadQuery, xformQuery);
            RemoveFromEntityTree(lookup, xform, xformQuery, recursive);
        }

        /// <summary>
        /// Recursively iterates through this entity's children and removes them from the entitylookupcomponent.
        /// </summary>
        private void RemoveFromEntityTree(BroadphaseComponent? lookup, TransformComponent xform, EntityQuery<TransformComponent> xformQuery, bool recursive = true)
        {
            // TODO: Move this out of the loop
            if (lookup == null) return;

            RemoveTree(xform.Owner, lookup);

            if (!recursive) return;

            var childEnumerator = xform.ChildEnumerator;

            while (childEnumerator.MoveNext(out var child))
            {
                RemoveFromEntityTree(lookup, xformQuery.GetComponent(child.Value), xformQuery);
            }
        }

        /// <summary>
        /// Attempt to get the relevant broadphase for this entity.
        /// Can return null if it's the map entity.
        /// </summary>
        private BroadphaseComponent? GetBroadphase(TransformComponent xform)
        {
            if (xform.MapID == MapId.Nullspace) return null;

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            return GetBroadphase(xform.Owner, xform, broadQuery, xformQuery);
        }

        public BroadphaseComponent? GetBroadphase(EntityUid uid, TransformComponent xform, EntityQuery<BroadphaseComponent> broadQuery, EntityQuery<TransformComponent> xformQuery)
        {
            if (xform.MapID == MapId.Nullspace) return null;

            var parent = xform.ParentUid;

            // if it's map (or in null-space) return null. Grids should return the map's broadphase.

            while (parent.IsValid())
            {
                if (broadQuery.TryGetComponent(parent, out var comp)) return comp;
                parent = xformQuery.GetComponent(parent).ParentUid;
            }

            return null;
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
