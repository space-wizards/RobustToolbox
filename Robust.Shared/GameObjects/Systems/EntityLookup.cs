using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    [Flags]
    public enum LookupFlags : byte
    {
        None = 0,
        Approximate = 1 << 0,
        IncludeAnchored = 1 << 1,
        // IncludeGrids = 1 << 2,
    }

    // TODO: Nuke IEntityLookup and just make a system
    public interface IEntityLookup
    {
        // Not an EntitySystem given _entityManager has a dependency on it which means it's just easier to IoC it for tests.

        void Startup();

        void Shutdown();

        bool AnyEntitiesIntersecting(MapId mapId, Box2 box, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInMap(MapId mapId, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesAt(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
            float arcWidth, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(GridId gridId, IEnumerable<Vector2i> gridIndices);

        IEnumerable<EntityUid> GetEntitiesIntersecting(GridId gridId, Vector2i gridIndices);

        IEnumerable<EntityUid> GetEntitiesIntersecting(TileRef tileRef);

        IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldAABB, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(EntityUid entity, float enlarged = 0f, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(MapCoordinates position, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(EntityCoordinates position, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored);

        void FastEntitiesIntersecting(in MapId mapId, ref Box2 worldAABB, EntityUidQueryCallback callback, LookupFlags flags = LookupFlags.IncludeAnchored);

        void FastEntitiesIntersecting(EntityLookupComponent lookup, ref Box2 localAABB, EntityUidQueryCallback callback);

        IEnumerable<EntityUid> GetEntitiesInRange(EntityCoordinates position, float range, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInRange(EntityUid entity, float range, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInRange(MapId mapId, Vector2 point, float range, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInRange(MapId mapId, Box2 box, float range, LookupFlags flags = LookupFlags.IncludeAnchored);

        bool IsIntersecting(EntityUid entityOne, EntityUid entityTwo);

        Box2 GetWorldAABB(EntityUid ent, TransformComponent? xform = null);

        Box2 GetLocalBounds(TileRef tileRef, ushort tileSize);

        Box2 GetLocalBounds(Vector2i gridIndices, ushort tileSize);

        Box2Rotated GetWorldBounds(TileRef tileRef, Matrix3? worldMatrix = null, Angle? angle = null);
    }

    [UsedImplicitly]
    public sealed partial class EntityLookup : IEntityLookup, IEntityEventSubscriber
    {
        private readonly IEntityManager _entityManager;
        private readonly IMapManager _mapManager;
        private SharedContainerSystem _container = default!;
        private SharedTransformSystem _transform = default!;

        private const int GrowthRate = 256;

        private const float PointEnlargeRange = .00001f / 2;

        /// <summary>
        /// Like RenderTree we need to enlarge our lookup range for EntityLookupComponent as an entity is only ever on
        /// 1 EntityLookupComponent at a time (hence it may overlap without another lookup).
        /// </summary>
        private float _lookupEnlargementRange;

        // TODO: Should combine all of the methods that check for IPhysBody and just use the one GetWorldAabbFromEntity method

        // TODO: Combine GridTileLookupSystem and entity anchoring together someday.
        // Queries are a bit of spaghet rn but ideally you'd just have:
        // A) The fast tile-based one
        // B) The physics-only one (given physics needs it to be fast af)
        // C) A generic use one that covers anything not caught in the above.

        public bool Started { get; private set; } = false;

        public EntityLookup(IEntityManager entityManager, IMapManager mapManager)
        {
            _entityManager = entityManager;
            _mapManager = mapManager;
        }

        public void Startup()
        {
            if (Started)
            {
                throw new InvalidOperationException("Startup() called multiple times.");
            }

            _container = _entityManager.EntitySysManager.GetEntitySystem<SharedContainerSystem>();
            _transform = _entityManager.EntitySysManager.GetEntitySystem<SharedTransformSystem>();
            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.LookupEnlargementRange, value => _lookupEnlargementRange = value, true);

            var eventBus = _entityManager.EventBus;
            eventBus.SubscribeEvent<MoveEvent>(EventSource.Local, this, OnMove);
            eventBus.SubscribeEvent<RotateEvent>(EventSource.Local, this, OnRotate);
            eventBus.SubscribeEvent<EntParentChangedMessage>(EventSource.Local, this, OnParentChange);
            eventBus.SubscribeEvent<AnchorStateChangedEvent>(EventSource.Local, this, OnAnchored);
            eventBus.SubscribeEvent<UpdateLookupBoundsEvent>(EventSource.Local, this, OnBoundsUpdate);

            eventBus.SubscribeLocalEvent<EntityLookupComponent, ComponentAdd>(OnLookupAdd);
            eventBus.SubscribeLocalEvent<EntityLookupComponent, ComponentShutdown>(OnLookupShutdown);
            eventBus.SubscribeEvent<GridInitializeEvent>(EventSource.Local, this, OnGridInit);

            eventBus.SubscribeEvent<EntityTerminatingEvent>(EventSource.Local, this, OnTerminate);

            _entityManager.EntityInitialized += OnEntityInit;
            _mapManager.MapCreated += OnMapCreated;
            Started = true;
        }

        public void Shutdown()
        {
            // If we haven't even started up, there's nothing to clean up then.
            if (!Started)
                return;

            _entityManager.EntityInitialized -= OnEntityInit;
            _mapManager.MapCreated -= OnMapCreated;
            Started = false;
        }

        private void OnAnchored(ref AnchorStateChangedEvent args)
        {
            // This event needs to be handled immediately as anchoring is handled immediately
            // and any callers may potentially get duplicate entities that just changed state.
            if (args.Anchored)
            {
                RemoveFromEntityTree(args.Entity);
            }
            else if (_entityManager.TryGetComponent(args.Entity, out MetaDataComponent? meta) && meta.EntityLifeStage < EntityLifeStage.Terminating)
            {
                var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
                var xform = xformQuery.GetComponent(args.Entity);
                var lookup = GetLookup(args.Entity, xform, xformQuery);

                if (lookup == null)
                    throw new InvalidOperationException();

                var lookupXform = xformQuery.GetComponent(lookup.Owner);
                var coordinates = _transform.GetMoverCoordinates(xform.Coordinates, xformQuery);
                DebugTools.Assert(coordinates.EntityId == lookup.Owner);

                // If we're contained then LocalRotation should be 0 anyway.
                var aabb = GetAABB(args.Entity, coordinates.Position, _transform.GetWorldRotation(xform) - _transform.GetWorldRotation(lookupXform), xform, xformQuery);
                AddToEntityTree(lookup, xform, aabb, xformQuery);
            }
            // else -> the entity is terminating. We can ignore this un-anchor event, as this entity will be removed by the tree via OnEntityDeleted.
        }

        #region DynamicTree

        private void OnLookupShutdown(EntityUid uid, EntityLookupComponent component, ComponentShutdown args)
        {
            component.Tree.Clear();
        }

        private void OnGridInit(GridInitializeEvent ev)
        {
            _entityManager.EnsureComponent<EntityLookupComponent>(ev.EntityUid);
        }

        private void OnLookupAdd(EntityUid uid, EntityLookupComponent component, ComponentAdd args)
        {
            int capacity;

            if (_entityManager.TryGetComponent(uid, out TransformComponent? xform))
            {
                capacity = (int) Math.Min(256, Math.Ceiling(xform.ChildCount / (float) GrowthRate) * GrowthRate);
            }
            else
            {
                capacity = 256;
            }

            component.Tree = new DynamicTree<EntityUid>(
                GetTreeAABB,
                capacity: capacity,
                growthFunc: x => x == GrowthRate ? GrowthRate * 8 : x * 2
            );
        }

        private void OnMapCreated(object? sender, MapEventArgs eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;

            _entityManager.EnsureComponent<EntityLookupComponent>(_mapManager.GetMapEntityId(eventArgs.Map));
        }

        private Box2 GetTreeAABB(in EntityUid entity)
        {
            // TODO: Should feed in AABB to lookup so it's not enlarged unnecessarily
            var aabb = GetWorldAABB(entity);
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
            var tree = GetLookup(entity, xformQuery);

            if (tree == null)
                return aabb;

            return xformQuery.GetComponent(tree.Owner).InvWorldMatrix.TransformBox(aabb);
        }

        #endregion

        #region Entity events

        private void OnTerminate(ref EntityTerminatingEvent args)
        {
            RemoveFromEntityTree(args.Owner, false);
        }

        private void OnEntityInit(object? sender, EntityUid uid)
        {
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();

            if (!xformQuery.TryGetComponent(uid, out var xform) ||
                xform.Anchored ||
                _mapManager.IsMap(uid) ||
                _mapManager.IsGrid(uid)) return;

            var lookup = GetLookup(uid, xform, xformQuery);

            // If nullspace or the likes.
            if (lookup == null) return;

            var lookupXform = xformQuery.GetComponent(lookup.Owner);
            var coordinates = _transform.GetMoverCoordinates(xform.Coordinates, xformQuery);
            DebugTools.Assert(coordinates.EntityId == lookup.Owner);

            // If we're contained then LocalRotation should be 0 anyway.
            var aabb = GetAABB(uid, coordinates.Position, _transform.GetWorldRotation(xform) - _transform.GetWorldRotation(lookupXform), xform, xformQuery);

            // Any child entities should be handled by their own OnEntityInit
            AddToEntityTree(lookup, xform, aabb, xformQuery, false);
        }

        private void OnMove(ref MoveEvent args)
        {
            UpdatePosition(args.Sender, args.Component);
        }

        private void OnRotate(ref RotateEvent args)
        {
            UpdatePosition(args.Sender, args.Component);
        }

        private void UpdatePosition(EntityUid uid, TransformComponent xform)
        {
            // Even if the entity is contained it may have children that aren't so we still need to update.
            if (!CanMoveUpdate(uid, xform)) return;

            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
            var lookup = GetLookup(uid, xform, xformQuery);

            if (lookup == null) return;

            var lookupXform = xformQuery.GetComponent(lookup.Owner);
            var coordinates = _transform.GetMoverCoordinates(xform.Coordinates, xformQuery);
            var aabb = GetAABB(uid, coordinates.Position, _transform.GetWorldRotation(xform) - _transform.GetWorldRotation(lookupXform), xformQuery.GetComponent(uid), xformQuery);
            AddToEntityTree(lookup, xform, aabb, xformQuery);
        }

        private bool CanMoveUpdate(EntityUid uid, TransformComponent xform)
        {
            return !_mapManager.IsMap(uid) &&
                     !_mapManager.IsGrid(uid) &&
                     !_container.IsEntityInContainer(uid, xform);
        }

        private void OnParentChange(ref EntParentChangedMessage args)
        {
            if (_mapManager.IsMap(args.Entity) ||
                _mapManager.IsGrid(args.Entity) ||
                _entityManager.GetComponent<MetaDataComponent>(args.Entity).EntityLifeStage < EntityLifeStage.Initialized) return;

            EntityLookupComponent? oldLookup = null;
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(args.Entity);

            if (args.OldParent != null)
            {
                oldLookup = GetLookup(args.OldParent.Value, xformQuery);
            }

            var newLookup = GetLookup(args.Entity, xform, xformQuery);

            // If parent is the same then no need to do anything as position should stay the same.
            if (oldLookup == newLookup) return;

            RemoveFromEntityTree(oldLookup, xform, xformQuery);

            if (newLookup != null)
                AddToEntityTree(newLookup, xform, xformQuery);
        }

        private void OnBoundsUpdate(ref UpdateLookupBoundsEvent ev)
        {
            // TODO: Just nuke the old one and add to new one.
            throw new NotImplementedException();
        }

        private void AddToEntityTree(
            EntityLookupComponent lookup,
            TransformComponent xform,
            EntityQuery<TransformComponent> xformQuery,
            bool recursive = true,
            bool contained = false)
        {
            var lookupXform = xformQuery.GetComponent(lookup.Owner);
            var coordinates = _transform.GetMoverCoordinates(xform.Coordinates, xformQuery);
            // If we're contained then LocalRotation should be 0 anyway.
            var aabb = GetAABB(xform.Owner, coordinates.Position, _transform.GetWorldRotation(xform) - _transform.GetWorldRotation(lookupXform), xform, xformQuery);
            AddToEntityTree(lookup, xform, aabb, xformQuery, recursive, contained);
        }

        private void AddToEntityTree(
            EntityLookupComponent? lookup,
            TransformComponent xform,
            Box2 aabb,
            EntityQuery<TransformComponent> xformQuery,
            bool recursive = true,
            bool contained = false)
        {
            // If entity is in nullspace then no point keeping track of data structure.
            if (lookup == null) return;

            if (!xform.Anchored)
                lookup.Tree.AddOrUpdate(xform.Owner, aabb);

            var childEnumerator = xform.ChildEnumerator;

            if (xform.ChildCount == 0 || !recursive) return;

            // TODO: Pass this down instead son.
            var lookupXform = xformQuery.GetComponent(lookup.Owner);
            // TODO: Just don't store contained stuff, it's way too expensive for updates and makes the tree much bigger.

            // Recursively update children.
            if (contained)
            {
                // Just re-use the topmost AABB.
                while (childEnumerator.MoveNext(out var child))
                {
                    AddToEntityTree(lookup, xformQuery.GetComponent(child.Value), aabb, xformQuery, contained: true);
                }
            }
            // If they're in a container then it just uses the parent's AABB.
            else if (_entityManager.TryGetComponent<ContainerManagerComponent>(xform.Owner, out var conManager))
            {
                while (childEnumerator.MoveNext(out var child))
                {
                    if (conManager.ContainsEntity(child.Value))
                    {
                        AddToEntityTree(lookup, xformQuery.GetComponent(child.Value), aabb, xformQuery, contained: true);
                    }
                    else
                    {
                        var coordinates = _transform.GetMoverCoordinates(xform.Coordinates, xformQuery);
                        var childXform = xformQuery.GetComponent(child.Value);
                        // TODO: If we have 0 position and not contained can optimise these further, but future problem.
                        var childAABB = GetAABBNoContainer(child.Value, coordinates.Position, childXform.WorldRotation - lookupXform.WorldRotation);
                        AddToEntityTree(lookup, childXform, childAABB, xformQuery);
                    }
                }
            }
            else
            {
                while (childEnumerator.MoveNext(out var child))
                {
                    var coordinates = _transform.GetMoverCoordinates(xform.Coordinates, xformQuery);
                    var childXform = xformQuery.GetComponent(child.Value);
                    // TODO: If we have 0 position and not contained can optimise these further, but future problem.
                    var childAABB = GetAABBNoContainer(child.Value, coordinates.Position, childXform.WorldRotation - lookupXform.WorldRotation);
                    AddToEntityTree(lookup, childXform, childAABB, xformQuery);
                }
            }
        }

        private void RemoveFromEntityTree(EntityUid uid, bool recursive = true)
        {
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(uid);
            var lookup = GetLookup(uid, xform, xformQuery);
            RemoveFromEntityTree(lookup, xform, xformQuery, recursive);
        }

        /// <summary>
        /// Recursively iterates through this entity's children and removes them from the entitylookupcomponent.
        /// </summary>
        private void RemoveFromEntityTree(EntityLookupComponent? lookup, TransformComponent xform, EntityQuery<TransformComponent> xformQuery, bool recursive = true)
        {
            // TODO: Move this out of the loop
            if (lookup == null) return;

            lookup.Tree.Remove(xform.Owner);

            if (!recursive) return;

            var childEnumerator = xform.ChildEnumerator;

            while (childEnumerator.MoveNext(out var child))
            {
                RemoveFromEntityTree(lookup, xformQuery.GetComponent(child.Value), xformQuery);
            }
        }

        #endregion

        private EntityLookupComponent? GetLookup(EntityUid entity, EntityQuery<TransformComponent> xformQuery)
        {
            var xform = xformQuery.GetComponent(entity);
            return GetLookup(entity, xform, xformQuery);
        }

        private EntityLookupComponent? GetLookup(EntityUid uid, TransformComponent xform, EntityQuery<TransformComponent> xformQuery)
        {
            if (xform.MapID == MapId.Nullspace)
                return null;

            var parent = xform.ParentUid;
            var lookupQuery = _entityManager.GetEntityQuery<EntityLookupComponent>();

            // If we're querying a map / grid just return it directly.
            if (lookupQuery.TryGetComponent(uid, out var lookup))
            {
                return lookup;
            }

            while (parent.IsValid())
            {
                if (lookupQuery.TryGetComponent(parent, out var comp)) return comp;
                parent = xformQuery.GetComponent(parent).ParentUid;
            }

            return null;
        }

        #region Bounds

        /// <summary>
        /// Get the AABB of an entity with the supplied position and angle. Tries to consider if the entity is in a container.
        /// </summary>
        private Box2 GetAABB(EntityUid uid, Vector2 position, Angle angle, TransformComponent xform, EntityQuery<TransformComponent> xformQuery)
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
            // DebugTools.Assert(!_container.IsEntityInContainer(uid, xform));
            Box2 localAABB;
            var transform = new Transform(position, angle);

            if (_entityManager.TryGetComponent<ILookupWorldBox2Component>(uid, out var worldLookup))
            {
                localAABB = worldLookup.GetAABB(transform);
            }
            else
            {
                localAABB = new Box2Rotated(new Box2(transform.Position, transform.Position), transform.Quaternion2D.Angle, transform.Position).CalcBoundingBox();
            }

            return localAABB;
        }

        public Box2 GetWorldAABB(EntityUid uid, TransformComponent? xform = null)
        {
            var xformQuery = _entityManager.GetEntityQuery<TransformComponent>();
            xform ??= xformQuery.GetComponent(uid);
            var (worldPos, worldRot) = xform.GetWorldPositionRotation();

            return GetAABB(uid, worldPos, worldRot, xform, xformQuery);
        }

        #endregion
    }
}

/// <summary>
/// Flags this entity for an update to their lookup bounds.
/// </summary>
[ByRefEvent]
public readonly struct UpdateLookupBoundsEvent
{
    public readonly EntityUid Uid;

    public UpdateLookupBoundsEvent(EntityUid uid)
    {
        Uid = uid;
    }
}
