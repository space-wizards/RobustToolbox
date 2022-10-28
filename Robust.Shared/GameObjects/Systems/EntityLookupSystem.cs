using JetBrains.Annotations;
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

            if (!xformQuery.Resolve(uid, ref xform))
                return;

            // also ensure that no parent is in a container.
            DebugTools.Assert(!_container.IsEntityOrParentInContainer(uid, meta, xform, null, xformQuery));

            var broadQuery = GetEntityQuery<BroadphaseComponent>();

            // TODO combine mover-coordinate fetching with BroadphaseComponent fetching. They're kinda the same thing.
            var lookup = GetBroadphase(uid, xform, broadQuery, xformQuery);

            if (lookup == null) return;

            var lookupRotation = _transform.GetWorldRotation(lookup.Owner, xformQuery);
            var (coordinates, rotation) = _transform.GetMoverCoordinateRotation(xform, xformQuery);
            var relativeRotation = rotation - lookupRotation;
            var aabb = GetAABBNoContainer(xform.Owner, coordinates.Position, relativeRotation);

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
            var broadphase = GetBroadphase(fixture.Body.Owner, xformQuery.GetComponent(fixture.Body.Owner), broadQuery, xformQuery);

            if (broadphase == null || xform.MapUid == null)
            {
                throw new InvalidOperationException();
            }

            var mapTransform = new Transform(worldPos, worldRot);
            var (_, broadWorldRot, _, broadInvMatrix) = xformQuery.GetComponent(broadphase.Owner).GetWorldPositionRotationMatrixWithInv();
            var broadphaseTransform = new Transform(broadInvMatrix.Transform(mapTransform.Position), mapTransform.Quaternion2D.Angle - broadWorldRot);
            var moveBuffer = Comp<SharedPhysicsMapComponent>(xform.MapUid.Value).MoveBuffer;
            var tree = fixture.Body.BodyType == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;
            DebugTools.Assert(fixture.ProxyCount == 0);

            AddOrMoveProxies(fixture, tree, broadphaseTransform, mapTransform, moveBuffer);
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
            var broadphase = GetBroadphase(fixture.Body.Owner, xformQuery.GetComponent(fixture.Body.Owner), broadQuery, xformQuery);

            if (broadphase == null || xform.MapUid == null)
            {
                throw new InvalidOperationException();
            }

            var tree = fixture.Body.BodyType == BodyType.Static ? broadphase.StaticTree : broadphase.DynamicTree;
            var moveBuffer = Comp<SharedPhysicsMapComponent>(xform.MapUid.Value).MoveBuffer;
            DestroyProxies(fixture, tree, moveBuffer);
        }

        #endregion


        #region Entity Updating
        private void UpdatePosition(EntityUid uid, TransformComponent xform, BroadphaseComponent? lookup, EntityQuery<TransformComponent> xformQuery)
        {
            if (lookup == null)
                return;

            var lookupRotation = _transform.GetWorldRotation(lookup.Owner, xformQuery);
            var (coordinates, rotation) = _transform.GetMoverCoordinateRotation(xform, xformQuery);
            var relativeRotation = rotation - lookupRotation;
            var aabb = GetAABBNoContainer(uid, coordinates.Position, relativeRotation);
            AddToEntityTree(lookup, xform, aabb, xformQuery, lookupRotation);
        }

        private void UpdateParent(EntityUid uid,
            TransformComponent xform,
            BroadphaseComponent? lookup,
            EntityQuery<TransformComponent> xformQuery,
            EntityQuery<BroadphaseComponent> broadQuery,
            EntityUid oldParent)
        {
            BroadphaseComponent? oldLookup = null;
            if (oldParent.IsValid()
                && oldParent != uid // implies the entity was in null-space
                && xformQuery.TryGetComponent(oldParent, out var parentXform)
                && parentXform.MapID != MapId.Nullspace // see comment below
                && !broadQuery.TryGetComponent(oldParent, out oldLookup))
            {
                oldLookup = GetBroadphase(oldParent, parentXform, broadQuery, xformQuery);
            }

            // Note that the parentXform.MapID != MapId.Nullspace is required because currently grids are not allowed to
            // ever enter null-space. If they are in null-space, we assume that the grid is being deleted, as otherwise
            // RemoveFromEntityTree() will explode. This may eventually have to change if we stop universally sending
            // all grids to all players (i.e., out-of view grids will need to get sent to null-space)
            //
            // This also means the queries above can be reverted (check broadQuery, then xformQuery, as this will
            // generally save a component lookup.

            // If lookup remained unchanged we just update the position as normal
            if (oldLookup == lookup)
            {
                UpdatePosition(uid, xform, lookup, xformQuery);
                return;
            }

            RemoveFromEntityTree(oldLookup, xform, xformQuery);

            if (lookup != null)
                AddToEntityTree(lookup, xform, xformQuery, _transform.GetWorldRotation(lookup.Owner, xformQuery));
        }
        #endregion

        #region Entity events

        private void OnPhysicsUpdate(ref CollisionChangeEvent ev)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(ev.Body.Owner);

            if (xform.GridUid == ev.Body.Owner)
                return;
            DebugTools.Assert(!_mapManager.IsGrid(ev.Body.Owner));
            
            if (!ev.CanCollide && _container.IsEntityOrParentInContainer(ev.Body.Owner, null, xform, null, xformQuery))
            {
                // getting inserted, skip sundries insertion and just let container insertion handle tree removal.

                // TODO: for whatever fucking cursed reason, this is currently required.
                // FIX THIS, this is a hotfix
                var b = GetBroadphase(ev.Body.Owner, xform, GetEntityQuery<BroadphaseComponent>(), xformQuery);
                if (b != null)
                    RemoveBroadTree(ev.Body, b, ev.Body.BodyType);

                return;
            }

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var broadphase = GetBroadphase(ev.Body.Owner, xform, broadQuery, xformQuery);

            if (broadphase == null)
                return;

            if (ev.CanCollide)
            {
                RemoveSundriesTree(ev.Body.Owner, broadphase, ev.Body.BodyType);
                AddBroadTree(ev.Body, broadphase, ev.Body.BodyType, xform: xform);
            }
            else
            {
                RemoveBroadTree(ev.Body, broadphase, ev.Body.BodyType);
                AddSundriesTree(ev.Body.Owner, broadphase, ev.Body.BodyType);
            }
        }

        private void OnBodyTypeChange(EntityUid uid, PhysicsComponent component, ref PhysicsBodyTypeChangedEvent args)
        {
            // only matters if we swapped from static to non-static.
            if (args.Old != BodyType.Static && args.New != BodyType.Static)
                return;

            var xformQuery = GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(uid);

            if (xform.GridUid == uid)
                return;
            DebugTools.Assert(!_mapManager.IsGrid(uid));

            // fun fact: container insertion tries to update the fucking lookups like 3 or more times, each time iterating through all of its parents.
            if (_container.IsEntityOrParentInContainer(uid, null, xform, null, xformQuery))
                return;

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var broadphase = GetBroadphase(uid, xform, broadQuery, xformQuery);

            if (broadphase == null)
                return;

            if (component.CanCollide)
            {
                RemoveBroadTree(component, broadphase, args.Old);
                AddBroadTree(component, broadphase, component.BodyType);
            }
            else
            {
                RemoveSundriesTree(uid, broadphase, args.Old);
                AddSundriesTree(uid, broadphase, component.BodyType);
            }    
        }

        private void RemoveBroadTree(PhysicsComponent body, BroadphaseComponent lookup, BodyType bodyType, FixturesComponent? manager = null)
        {
            if (!Resolve(body.Owner, ref manager))
                return;

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

            var tree = bodyType == BodyType.Static ? lookup.StaticTree : lookup.DynamicTree;
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

        private void AddBroadTree(PhysicsComponent body, BroadphaseComponent lookup, BodyType bodyType, FixturesComponent? manager = null, TransformComponent? xform = null)
        {
            if (!Resolve(body.Owner, ref manager, ref xform))
                return;

            var tree = bodyType == BodyType.Static ? lookup.StaticTree : lookup.DynamicTree;
            var xformQuery = GetEntityQuery<TransformComponent>();

            DebugTools.Assert(!_container.IsEntityOrParentInContainer(body.Owner, null, xform, null, xformQuery));

            if (!TryComp<TransformComponent>(lookup.Owner, out var lookupXform) || lookupXform.MapUid == null)
            {
                throw new InvalidOperationException();
            }

            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform, xformQuery);
            var mapTransform = new Transform(worldPos, worldRot);
            var (_, broadWorldRot, _, broadInvMatrix) = xformQuery.GetComponent(lookup.Owner).GetWorldPositionRotationMatrixWithInv();
            var broadphaseTransform = new Transform(broadInvMatrix.Transform(mapTransform.Position), mapTransform.Quaternion2D.Angle - broadWorldRot);
            var moveBuffer = Comp<SharedPhysicsMapComponent>(lookupXform.MapUid.Value).MoveBuffer;

            foreach (var (_, fixture) in manager.Fixtures)
            {
                AddOrMoveProxies(fixture, tree, broadphaseTransform, mapTransform, moveBuffer);
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

        private void AddSundriesTree(EntityUid uid, BroadphaseComponent lookup, BodyType bodyType)
        {
            DebugTools.Assert(!_container.IsEntityOrParentInContainer(uid));
            var tree = bodyType == BodyType.Static ? lookup.StaticSundriesTree : lookup.SundriesTree;
            tree.Add(uid);
        }

        private void RemoveSundriesTree(EntityUid uid, BroadphaseComponent lookup, BodyType bodyType)
        {
            var tree = bodyType == BodyType.Static ? lookup.StaticSundriesTree : lookup.SundriesTree;
            tree.Remove(uid);
        }

        private void OnEntityInit(EntityUid uid)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();

            if (!xformQuery.TryGetComponent(uid, out var xform))
            {
                return;
            }

            if (_container.IsEntityOrParentInContainer(uid, null, xform, null, xformQuery))
                return;

            if (_mapManager.IsMap(uid) ||
                _mapManager.IsGrid(uid))
            {
                return;
            }

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var lookup = GetBroadphase(uid, xform, broadQuery, xformQuery);

            // If nullspace or the likes.
            if (lookup == null) return;

            var lookupRotation = _transform.GetWorldRotation(lookup.Owner, xformQuery);
            var (coordinates, rotation) = _transform.GetMoverCoordinateRotation(xform, xformQuery);
            var relativeRotation = rotation - lookupRotation;
            DebugTools.Assert(coordinates.EntityId == lookup.Owner);

            var aabb = GetAABBNoContainer(uid, coordinates.Position, relativeRotation);

            // Any child entities should be handled by their own OnEntityInit
            AddToEntityTree(lookup, xform, aabb, xformQuery, lookupRotation, false);
        }

        private void OnMove(ref MoveEvent args)
        {
            var xformQuery = GetEntityQuery<TransformComponent>();

            // TODO remove this check after #3368 gets merged
            if (args.Component.LifeStage < ComponentLifeStage.Initialized && args.Component.GridUid == null)
                _transform.SetGridId(args.Component, args.Component.FindGridEntityId(xformQuery));

            // Is this a grid?
            if (args.Component.GridUid == args.Sender)
                return;
            DebugTools.Assert(!_mapManager.IsGrid(args.Sender));

            var metaQuery = GetEntityQuery<MetaDataComponent>();
            var meta = metaQuery.GetComponent(args.Sender);

            if (meta.EntityLifeStage < EntityLifeStage.Initialized)
                return;

            if (_container.IsEntityOrParentInContainer(args.Sender, meta, args.Component, metaQuery, xformQuery))
            {
                // This move might be due to a parent change as a result of getting inserted into a container. In that
                // case, we will just let the container insert event handle that. Note that the in-container flag gets
                // set BEFORE insert parent change, and gets unset before the container removal parent-change. So if it
                // is set here, this must mean we are getting inserted.
                //
                // However, this means that this method will still get run in full on container removal. Additionally,
                // because not all container removals are guaranteed to result in a parent change, container removal
                // events also need to add the entity to a tree. So if an entity gets ejected/teleported to some other
                // grid this results in add-to-tree -> remove-from-tree -> add-to-tree.
                //
                // TODO IMPROVE CONTAINER REMOVAL HANDLING
                return;
            }

            if (args.Component.MapUid == args.Sender)
                return;
            DebugTools.Assert(!_mapManager.IsMap(args.Sender));

            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var lookup = GetBroadphase(args.Sender, args.Component, broadQuery, xformQuery);

            if (args.ParentChanged)
                UpdateParent(args.Sender, args.Component, lookup, xformQuery, broadQuery, args.OldPosition.EntityId);
            else
                UpdatePosition(args.Sender, args.Component, lookup, xformQuery);
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
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            BroadphaseComponent? lookup;

            if (ev.OldParent == EntityUid.Invalid)
                return;

            if (!broadQuery.TryGetComponent(ev.OldParent, out lookup))
            {
                if (!xformQuery.TryGetComponent(ev.OldParent, out var parentXform))
                    return;

                lookup = GetBroadphase(ev.OldParent, parentXform, broadQuery, xformQuery);
            }

            RemoveFromEntityTree(lookup, xformQuery.GetComponent(ev.Entity), xformQuery);
        }

        private void AddToEntityTree(
            BroadphaseComponent lookup,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            Angle lookupRotation,
            bool recursive = true)
        {
            // TODO combine mover-coordinate fetching with BroadphaseComponent fetching. They're kinda the same thing.
            var (coordinates, rotation) = _transform.GetMoverCoordinateRotation(xform, xformQuery);
            var relativeRotation = rotation - lookupRotation;
            var aabb = GetAABBNoContainer(xform.Owner, coordinates.Position, relativeRotation);
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

            AddTree(xform.Owner, lookup, aabb, xform: xform);

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
                    var (coordinates, rotation) = _transform.GetMoverCoordinateRotation(childXform, xformQuery);
                    var relativeRotation = rotation - lookupRotation;
                    // TODO: If we have 0 position and not contained can optimise these further, but future problem.
                    var childAABB = GetAABBNoContainer(child.Value, coordinates.Position, relativeRotation);
                    AddToEntityTree(lookup, childXform, childAABB, xformQuery, lookupRotation);
                }
            }
            else
            {
                while (childEnumerator.MoveNext(out var child))
                {
                    var childXform = xformQuery.GetComponent(child.Value);
                    var (coordinates, rotation) = _transform.GetMoverCoordinateRotation(childXform, xformQuery);
                    var relativeRotation = rotation - lookupRotation;
                    // TODO: If we have 0 position and not contained can optimise these further, but future problem.
                    var childAABB = GetAABBNoContainer(child.Value, coordinates.Position, relativeRotation);
                    AddToEntityTree(lookup, childXform, childAABB, xformQuery, lookupRotation);
                }
            }
        }

        private void AddTree(EntityUid uid, BroadphaseComponent broadphase, Box2 aabb, PhysicsComponent? body = null, TransformComponent? xform = null)
        {
            if (!Resolve(uid, ref body, false) || !body.CanCollide)
            {
                if (body?.BodyType == BodyType.Static)
                    broadphase.StaticSundriesTree.AddOrUpdate(uid, aabb);
                else
                    broadphase.SundriesTree.AddOrUpdate(uid, aabb);
                return;
            }

            AddBroadTree(body, broadphase, body.BodyType, xform: xform);
        }

        private void RemoveTree(EntityUid uid, BroadphaseComponent broadphase, PhysicsComponent? body = null)
        {
            if (!Resolve(uid, ref body, false) || !body.CanCollide)
            {
                if (body?.BodyType == BodyType.Static)
                    broadphase.StaticSundriesTree.Remove(uid);
                else
                    broadphase.SundriesTree.Remove(uid);
                return;
            }

            RemoveBroadTree(body, broadphase, body.BodyType);
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

        public BroadphaseComponent? GetBroadphase(EntityUid uid)
        {
            var broadQuery = GetEntityQuery<BroadphaseComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            return GetBroadphase(uid, xformQuery.GetComponent(uid), broadQuery, xformQuery);
        }

        public BroadphaseComponent? GetBroadphase(EntityUid uid, TransformComponent xform, EntityQuery<BroadphaseComponent> broadQuery, EntityQuery<TransformComponent> xformQuery)
        {
            if (xform.MapID == MapId.Nullspace) return null;

            var parent = xform.ParentUid;

            // if it's map (or in null-space) return null. Grids should return the map's broadphase.

            // TODO provide variant that also returns world rotation (and maybe position). Avoids having to iterate though parents twice.
            while (parent.IsValid())
            {
                if (broadQuery.TryGetComponent(parent, out var comp))
                    return comp;

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
