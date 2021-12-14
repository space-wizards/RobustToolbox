using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
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

    public interface IEntityLookup
    {
        // Not an EntitySystem given _entityManager has a dependency on it which means it's just easier to IoC it for tests.

        void Startup();

        void Shutdown();

        void Update();
        bool AnyEntitiesIntersecting(MapId mapId, Box2 box, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInMap(MapId mapId, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesAt(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored);

        IEnumerable<EntityUid> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
            float arcWidth, LookupFlags flags = LookupFlags.IncludeAnchored);

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

        /// <summary>
        /// Updates the lookup for this entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="worldAABB">Pass in to avoid it being re-calculated</param>
        /// <returns></returns>
        bool UpdateEntityTree(EntityUid entity, Box2? worldAABB = null);

        void RemoveFromEntityTrees(EntityUid entity);

        Box2 GetWorldAabbFromEntity(in EntityUid ent);
    }

    [UsedImplicitly]
    public class EntityLookup : IEntityLookup, IEntityEventSubscriber
    {
        private readonly IEntityManager _entityManager;
        private readonly IMapManager _mapManager;

        private const int GrowthRate = 256;

        private const float PointEnlargeRange = .00001f / 2;

        // Using stacks so we always use latest data (given we only run it once per entity).
        private readonly Stack<MoveEvent> _moveQueue = new();
        private readonly Stack<RotateEvent> _rotateQueue = new();
        private readonly Queue<EntParentChangedMessage> _parentChangeQueue = new();

        /// <summary>
        /// Like RenderTree we need to enlarge our lookup range for EntityLookupComponent as an entity is only ever on
        /// 1 EntityLookupComponent at a time (hence it may overlap without another lookup).
        /// </summary>
        private float _lookupEnlargementRange;

        /// <summary>
        /// Move and rotate events generate the same update so no point duplicating work in the same tick.
        /// </summary>
        private readonly HashSet<EntityUid> _handledThisTick = new();

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

            var configManager = IoCManager.Resolve<IConfigurationManager>();
            configManager.OnValueChanged(CVars.LookupEnlargementRange, value => _lookupEnlargementRange = value, true);

            var eventBus = _entityManager.EventBus;
            eventBus.SubscribeEvent(EventSource.Local, this, (ref MoveEvent ev) => _moveQueue.Push(ev));
            eventBus.SubscribeEvent(EventSource.Local, this, (ref RotateEvent ev) => _rotateQueue.Push(ev));
            eventBus.SubscribeEvent(EventSource.Local, this, (ref EntParentChangedMessage ev) => _parentChangeQueue.Enqueue(ev));
            eventBus.SubscribeEvent<AnchorStateChangedEvent>(EventSource.Local, this, HandleAnchored);

            eventBus.SubscribeLocalEvent<EntityLookupComponent, ComponentInit>(HandleLookupInit);
            eventBus.SubscribeLocalEvent<EntityLookupComponent, ComponentShutdown>(HandleLookupShutdown);
            eventBus.SubscribeEvent<GridInitializeEvent>(EventSource.Local, this, HandleGridInit);

            _entityManager.EntityDeleted += HandleEntityDeleted;
            _entityManager.EntityStarted += HandleEntityStarted;
            _mapManager.MapCreated += HandleMapCreated;
            Started = true;
        }

        public void Shutdown()
        {
            // If we haven't even started up, there's nothing to clean up then.
            if (!Started)
                return;

            _moveQueue.Clear();
            _rotateQueue.Clear();
            _handledThisTick.Clear();
            _parentChangeQueue.Clear();

            _entityManager.EntityDeleted -= HandleEntityDeleted;
            _entityManager.EntityStarted -= HandleEntityStarted;
            _mapManager.MapCreated -= HandleMapCreated;
            Started = false;
        }

        private void HandleAnchored(ref AnchorStateChangedEvent @event)
        {
            // This event needs to be handled immediately as anchoring is handled immediately
            // and any callers may potentially get duplicate entities that just changed state.
            if (_entityManager.GetComponent<TransformComponent>(@event.Entity).Anchored)
            {
                RemoveFromEntityTrees(@event.Entity);
            }
            else
            {
                UpdateEntityTree(@event.Entity);
            }
        }

        private void HandleLookupShutdown(EntityUid uid, EntityLookupComponent component, ComponentShutdown args)
        {
            component.Tree.Clear();
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            _entityManager.EnsureComponent<EntityLookupComponent>(ev.EntityUid);
        }

        private void HandleLookupInit(EntityUid uid, EntityLookupComponent component, ComponentInit args)
        {
            var capacity = (int) Math.Min(256, Math.Ceiling(_entityManager.GetComponent<TransformComponent>(component.Owner).ChildCount / (float) GrowthRate) * GrowthRate);

            component.Tree = new DynamicTree<EntityUid>(
                GetRelativeAABBFromEntity,
                capacity: capacity,
                growthFunc: x => x == GrowthRate ? GrowthRate * 8 : x * 2
            );
        }

        private Box2 GetRelativeAABBFromEntity(in EntityUid entity)
        {
            // TODO: Should feed in AABB to lookup so it's not enlarged unnecessarily

            var aabb = GetWorldAABB(entity);
            var tree = GetLookup(entity);

            if (tree == null)
                return aabb;

            return _entityManager.GetComponent<TransformComponent>(tree.Owner).InvWorldMatrix.TransformBox(aabb);
        }

        private void HandleEntityDeleted(object? sender, EntityUid uid)
        {
            RemoveFromEntityTrees(uid);
        }

        private void HandleEntityStarted(object? sender, EntityUid uid)
        {
            if (_entityManager.GetComponent<TransformComponent>(uid).Anchored) return;
            UpdateEntityTree(uid);
        }

        private void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;

            _mapManager.GetMapEntityId(eventArgs.Map).EnsureComponent<EntityLookupComponent>();
        }

        public void Update()
        {
            // Acruid said he'd deal with Update being called around I_entityManager later.

            // Could be more efficient but essentially nuke their old lookup and add to new lookup if applicable.
            while (_parentChangeQueue.TryDequeue(out var mapChangeEvent))
            {
                _handledThisTick.Add(mapChangeEvent.Entity);
                RemoveFromEntityTrees(mapChangeEvent.Entity);

                if (_entityManager.Deleted(mapChangeEvent.Entity) || _entityManager.GetComponent<TransformComponent>(mapChangeEvent.Entity).Anchored) continue;
                UpdateEntityTree(mapChangeEvent.Entity, GetWorldAabbFromEntity(mapChangeEvent.Entity));
            }

            while (_moveQueue.TryPop(out var moveEvent))
            {
                if (!_handledThisTick.Add(moveEvent.Sender) || _entityManager.Deleted(moveEvent.Sender) ||
                    _entityManager.GetComponent<TransformComponent>(moveEvent.Sender).Anchored) continue;

                DebugTools.Assert(!_entityManager.GetComponent<TransformComponent>(moveEvent.Sender).Anchored);
                UpdateEntityTree(moveEvent.Sender, moveEvent.WorldAABB);
            }

            while (_rotateQueue.TryPop(out var rotateEvent))
            {
                if (!_handledThisTick.Add(rotateEvent.Sender) ||
                    _entityManager.Deleted(rotateEvent.Sender) ||
                    _entityManager.GetComponent<TransformComponent>(rotateEvent.Sender).Anchored) continue;

                DebugTools.Assert(!_entityManager.GetComponent<TransformComponent>(rotateEvent.Sender).Anchored);
                UpdateEntityTree(rotateEvent.Sender, rotateEvent.WorldAABB);
            }

            _handledThisTick.Clear();
        }

        #region Spatial Queries

        private LookupsEnumerator GetLookupsIntersecting(MapId mapId, Box2 worldAABB)
        {
            _mapManager.FindGridsIntersectingEnumerator(mapId, worldAABB, out var gridEnumerator, true);

            return new LookupsEnumerator(_entityManager, _mapManager, mapId, gridEnumerator);
        }

        private struct LookupsEnumerator
        {
            private IEntityManager _entityManager;
            private IMapManager _mapManager;

            private MapId _mapId;
            private FindGridsEnumerator _enumerator;

            private bool _final;

            public LookupsEnumerator(IEntityManager entityManager, IMapManager mapManager, MapId mapId, FindGridsEnumerator enumerator)
            {
                _entityManager = entityManager;
                _mapManager = mapManager;

                _mapId = mapId;
                _enumerator = enumerator;
                _final = false;
            }

            public bool MoveNext([NotNullWhen(true)] out EntityLookupComponent? component)
            {
                if (!_enumerator.MoveNext(out var grid))
                {
                    if (_final || _mapId == MapId.Nullspace)
                    {
                        component = null;
                        return false;
                    }

                    _final = true;
                    EntityUid mapUid = _mapManager.GetMapEntityIdOrThrow(_mapId);
                    component = _entityManager.GetComponent<EntityLookupComponent>(mapUid);
                    return true;
                }

                // TODO: Recursive and all that.
                component = _entityManager.GetComponent<EntityLookupComponent>(grid.GridEntityId);
                return true;
            }
        }

        private IEnumerable<EntityUid> GetAnchored(MapId mapId, Box2 worldAABB, LookupFlags flags)
        {
            if ((flags & LookupFlags.IncludeAnchored) == 0x0) yield break;
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
            {
                foreach (var uid in grid.GetAnchoredEntities(worldAABB))
                {
                    if (!_entityManager.EntityExists(uid)) continue;
                    yield return uid;
                }
            }
        }

        private IEnumerable<EntityUid> GetAnchored(MapId mapId, Box2Rotated worldBounds, LookupFlags flags)
        {
            if ((flags & LookupFlags.IncludeAnchored) == 0x0) yield break;
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldBounds))
            {
                foreach (var uid in grid.GetAnchoredEntities(worldBounds))
                {
                    if (!_entityManager.EntityExists(uid)) continue;
                    yield return uid;
                }
            }
        }

        /// <inheritdoc />
        public bool AnyEntitiesIntersecting(MapId mapId, Box2 box, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var found = false;
            var enumerator = GetLookupsIntersecting(mapId, box);

            while (enumerator.MoveNext(out var lookup))
            {
                var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(box);

                lookup.Tree.QueryAabb(ref found, (ref bool found, in EntityUid ent) =>
                {
                    if (_entityManager.Deleted(ent))
                        return true;

                    found = true;
                    return false;

                }, offsetBox, (flags & LookupFlags.Approximate) != 0x0);
            }

            if (!found)
            {
                foreach (var _ in GetAnchored(mapId, box, flags))
                {
                    return true;
                }
            }

            return found;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void FastEntitiesIntersecting(in MapId mapId, ref Box2 worldAABB, EntityUidQueryCallback callback, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var enumerator = GetLookupsIntersecting(mapId, worldAABB);
            while (enumerator.MoveNext(out var lookup))
            {
                var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(worldAABB);

                lookup.Tree._b2Tree.FastQuery(ref offsetBox, (ref EntityUid data) => callback(data));
            }

            if ((flags & LookupFlags.IncludeAnchored) != 0x0)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB))
                {
                    foreach (var uid in grid.GetAnchoredEntities(worldAABB))
                    {
                        if (!_entityManager.EntityExists(uid)) continue;
                        callback(uid);
                    }
                }
            }
        }

        /// <inheritdoc />
        public void FastEntitiesIntersecting(EntityLookupComponent lookup, ref Box2 localAABB, EntityUidQueryCallback callback)
        {
            lookup.Tree._b2Tree.FastQuery(ref localAABB, (ref EntityUid data) => callback(data));
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2 worldAABB, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

            var list = new List<EntityUid>();
            var enumerator = GetLookupsIntersecting(mapId, worldAABB);

            while (enumerator.MoveNext(out var lookup))
            {
                var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(worldAABB);

                lookup.Tree.QueryAabb(ref list, (ref List<EntityUid> list, in EntityUid ent) =>
                {
                    if (!_entityManager.Deleted(ent))
                    {
                        list.Add(ent);
                    }
                    return true;
                }, offsetBox, (flags & LookupFlags.Approximate) != 0x0);
            }

            foreach (var ent in GetAnchored(mapId, worldAABB, flags))
            {
                list.Add(ent);
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Box2Rotated worldBounds, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

            var list = new List<EntityUid>();
            var worldAABB = worldBounds.CalcBoundingBox();
            var enumerator = GetLookupsIntersecting(mapId, worldAABB);

            while (enumerator.MoveNext(out var lookup))
            {
                var offsetBox = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(worldBounds);

                lookup.Tree.QueryAabb(ref list, (ref List<EntityUid> list, in EntityUid ent) =>
                {
                    if (!_entityManager.Deleted(ent))
                    {
                        list.Add(ent);
                    }
                    return true;
                }, offsetBox, (flags & LookupFlags.Approximate) != 0x0);
            }

            foreach (var ent in GetAnchored(mapId, worldBounds, flags))
            {
                list.Add(ent);
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

            var aabb = new Box2(position, position).Enlarged(PointEnlargeRange);
            var list = new List<EntityUid>();
            var state = (list, position);

            var enumerator = GetLookupsIntersecting(mapId, aabb);

            while (enumerator.MoveNext(out var lookup))
            {
                var localPoint = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.Transform(position);

                lookup.Tree.QueryPoint(ref state, (ref (List<EntityUid> list, Vector2 position) state, in EntityUid ent) =>
                {
                    if (Intersecting(ent, state.position))
                    {
                        state.list.Add(ent);
                    }
                    return true;
                }, localPoint, (flags & LookupFlags.Approximate) != 0x0);
            }

            if ((flags & LookupFlags.IncludeAnchored) != 0x0 &&
                _mapManager.TryFindGridAt(mapId, position, out var grid) &&
                grid.TryGetTileRef(position, out var tile))
            {
                foreach (var uid in grid.GetAnchoredEntities(tile.GridIndices))
                {
                    if (!_entityManager.EntityExists(uid)) continue;
                    state.list.Add(uid);
                }
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(MapCoordinates position, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            return GetEntitiesIntersecting(position.MapId, position.Position, flags);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(EntityCoordinates position, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var mapPos = position.ToMap(_entityManager);
            return GetEntitiesIntersecting(mapPos.MapId, mapPos.Position, flags);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesIntersecting(EntityUid entity, float enlarged = 0f, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var worldAABB = GetWorldAabbFromEntity(entity);
            var xform = _entityManager.GetComponent<TransformComponent>(entity);

            var (worldPos, worldRot) = xform.GetWorldPositionRotation();

            var enumerator = GetLookupsIntersecting(xform.MapID, worldAABB);
            var list = new List<EntityUid>();

            while (enumerator.MoveNext(out var lookup))
            {
                // To get the tightest bounds possible we'll re-calculate it for each lookup.
                var localBounds = GetLookupBounds(entity, lookup, worldPos, worldRot, enlarged);

                lookup.Tree.QueryAabb(ref list, (ref List<EntityUid> list, in EntityUid ent) =>
                {
                    if (!_entityManager.Deleted(ent))
                    {
                        list.Add(ent);
                    }
                    return true;
                }, localBounds, (flags & LookupFlags.Approximate) != 0x0);
            }

            foreach (var ent in GetAnchored(xform.MapID, worldAABB, flags))
            {
                list.Add(ent);
            }

            return list;
        }

        private Box2 GetLookupBounds(EntityUid uid, EntityLookupComponent lookup, Vector2 worldPos, Angle worldRot, float enlarged)
        {
            var (_, lookupRot, lookupInvWorldMatrix) = _entityManager.GetComponent<TransformComponent>(lookup.Owner).GetWorldPositionRotationInvMatrix();

            var localPos = lookupInvWorldMatrix.Transform(worldPos);
            var localRot = worldRot - lookupRot;

            if (_entityManager.TryGetComponent(uid, out FixturesComponent? manager))
            {
                var transform = new Transform(localPos, localRot);
                Box2? aabb = null;

                foreach (var (_, fixture) in manager.Fixtures)
                {
                    if (!fixture.Hard) continue;
                    for (var i = 0; i < fixture.Shape.ChildCount; i++)
                    {
                        aabb = aabb?.Union(fixture.Shape.ComputeAABB(transform, i)) ?? fixture.Shape.ComputeAABB(transform, i);
                    }
                }

                if (aabb != null)
                {
                    return aabb.Value.Enlarged(enlarged);
                }
            }

            // So IsEmpty checks don't get triggered
            return new Box2(localPos - float.Epsilon, localPos + float.Epsilon);
        }

        /// <inheritdoc />
        public bool IsIntersecting(EntityUid entityOne, EntityUid entityTwo)
        {
            var position = _entityManager.GetComponent<TransformComponent>(entityOne).MapPosition.Position;
            return Intersecting(entityTwo, position);
        }

        private bool Intersecting(EntityUid entity, Vector2 mapPosition)
        {
            if (_entityManager.TryGetComponent(entity, out IPhysBody? component))
            {
                if (component.GetWorldAABB().Contains(mapPosition))
                    return true;
            }
            else
            {
                var transform = _entityManager.GetComponent<TransformComponent>(entity);
                var entPos = transform.WorldPosition;
                if (MathHelper.CloseToPercent(entPos.X, mapPosition.X)
                    && MathHelper.CloseToPercent(entPos.Y, mapPosition.Y))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInRange(EntityCoordinates position, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var mapCoordinates = position.ToMap(_entityManager);
            var mapPosition = mapCoordinates.Position;
            var aabb = new Box2(mapPosition - new Vector2(range, range),
                mapPosition + new Vector2(range, range));
            return GetEntitiesIntersecting(mapCoordinates.MapId, aabb, flags);
            // TODO: Use a circle shape here mate
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInRange(MapId mapId, Box2 box, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var aabb = box.Enlarged(range);
            return GetEntitiesIntersecting(mapId, aabb, flags);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInRange(MapId mapId, Vector2 point, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var aabb = new Box2(point, point).Enlarged(range);
            return GetEntitiesIntersecting(mapId, aabb, flags);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInRange(EntityUid entity, float range, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var worldAABB = GetWorldAabbFromEntity(entity);
            return GetEntitiesInRange(_entityManager.GetComponent<TransformComponent>(entity).MapID, worldAABB, range, flags);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
            float arcWidth, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            var position = coordinates.ToMap(_entityManager).Position;

            foreach (var entity in GetEntitiesInRange(coordinates, range * 2, flags))
            {
                var angle = new Angle(_entityManager.GetComponent<TransformComponent>(entity).WorldPosition - position);
                if (angle.Degrees < direction.Degrees + arcWidth / 2 &&
                    angle.Degrees > direction.Degrees - arcWidth / 2)
                    yield return entity;
            }
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesInMap(MapId mapId, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            DebugTools.Assert((flags & LookupFlags.Approximate) == 0x0);

            foreach (EntityLookupComponent comp in _entityManager.EntityQuery<EntityLookupComponent>(true))
            {
                if (_entityManager.GetComponent<TransformComponent>(comp.Owner).MapID != mapId) continue;

                foreach (var entity in comp.Tree)
                {
                    if (_entityManager.Deleted(entity)) continue;

                    yield return entity;
                }
            }

            if ((flags & LookupFlags.IncludeAnchored) == 0x0) yield break;

            foreach (var grid in _mapManager.GetAllMapGrids(mapId))
            {
                foreach (var tile in grid.GetAllTiles())
                {
                    foreach (var uid in grid.GetAnchoredEntities(tile.GridIndices))
                    {
                        if (!_entityManager.EntityExists(uid)) continue;
                        yield return uid;
                    }
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntitiesAt(MapId mapId, Vector2 position, LookupFlags flags = LookupFlags.IncludeAnchored)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<EntityUid>();

            var list = new List<EntityUid>();

            var state = (list, position);

            var aabb = new Box2(position, position).Enlarged(PointEnlargeRange);
            var enumerator = GetLookupsIntersecting(mapId, aabb);

            while (enumerator.MoveNext(out var lookup))
            {
                var offsetPos = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.Transform(position);

                lookup.Tree.QueryPoint(ref state, (ref (List<EntityUid> list, Vector2 position) state, in EntityUid ent) =>
                {
                    state.list.Add(ent);
                    return true;
                }, offsetPos, (flags & LookupFlags.Approximate) != 0x0);
            }

            if ((flags & LookupFlags.IncludeAnchored) != 0x0)
            {
                foreach (var grid in _mapManager.FindGridsIntersecting(mapId, aabb))
                {
                    foreach (var uid in grid.GetAnchoredEntities(aabb))
                    {
                        if (!_entityManager.EntityExists(uid)) continue;
                        list.Add(uid);
                    }
                }
            }

            return list;
        }

        #endregion

        #region Entity DynamicTree

        private EntityLookupComponent? GetLookup(EntityUid entity)
        {
            if (_entityManager.GetComponent<TransformComponent>(entity).MapID == MapId.Nullspace)
            {
                return null;
            }

            // if it's map return null. Grids should return the map's broadphase.
            if (_entityManager.HasComponent<EntityLookupComponent>(entity) &&
                _entityManager.GetComponent<TransformComponent>(entity).Parent == null)
            {
                return null;
            }

            var parent = _entityManager.GetComponent<TransformComponent>(entity).Parent;

            while (true)
            {
                if (parent == null) break;

                if (_entityManager.TryGetComponent(parent.Owner, out EntityLookupComponent? comp)) return comp;
                parent = parent.Parent;
            }

            return null;
        }

        /// <inheritdoc />
        public virtual bool UpdateEntityTree(EntityUid entity, Box2? worldAABB = null)
        {
            // look there's JANK everywhere but I'm just bandaiding it for now for shuttles and we'll fix it later when
            // PVS is more stable and entity anchoring has been battle-tested.
            if (_entityManager.Deleted(entity))
            {
                RemoveFromEntityTrees(entity);
                return true;
            }

            DebugTools.Assert(_entityManager.GetComponent<MetaDataComponent>(entity).EntityInitialized);
            DebugTools.Assert(!_entityManager.GetComponent<TransformComponent>(entity).Anchored);

            var lookup = GetLookup(entity);

            if (lookup == null)
            {
                RemoveFromEntityTrees(entity);
                return true;
            }

            // Temp PVS guard for when we clear dynamictree for now.
            worldAABB ??= GetWorldAabbFromEntity(entity);
            var center = worldAABB.Value.Center;

            if (float.IsNaN(center.X) || float.IsNaN(center.Y))
            {
                RemoveFromEntityTrees(entity);
                return true;
            }

            var transform = _entityManager.GetComponent<TransformComponent>(entity);
            DebugTools.Assert(transform.Initialized);

            var aabb = _entityManager.GetComponent<TransformComponent>(lookup.Owner).InvWorldMatrix.TransformBox(worldAABB.Value);

            // for debugging
            var necessary = 0;

            if (lookup.Tree.AddOrUpdate(entity, aabb))
            {
                ++necessary;
            }

            if (!_entityManager.HasComponent<EntityLookupComponent>(entity))
            {
                foreach (var childTx in _entityManager.GetComponent<TransformComponent>(entity).ChildEntities)
                {
                    if (!_handledThisTick.Add(childTx)) continue;

                    if (UpdateEntityTree(childTx))
                    {
                        ++necessary;
                    }
                }
            }

            return necessary > 0;
        }

        /// <inheritdoc />
        public void RemoveFromEntityTrees(EntityUid entity)
        {
            // TODO: Need to fix ordering issues and then we can just directly remove it from the tree
            // rather than this O(n) legacy garbage.
            // Also we can't early returns because somehow it gets added to multiple trees!!!
            foreach (var lookup in _entityManager.EntityQuery<EntityLookupComponent>(true))
            {
                lookup.Tree.Remove(entity);
            }
        }

        public Box2 GetWorldAabbFromEntity(in EntityUid ent)
        {
            return GetWorldAABB(ent);
        }

        private Box2 GetWorldAABB(in EntityUid ent)
        {
            Vector2 pos;
            var transform = _entityManager.GetComponent<TransformComponent>(ent);

            if ((!_entityManager.EntityExists(ent) ? EntityLifeStage.Deleted : _entityManager.GetComponent<MetaDataComponent>(ent).EntityLifeStage) >= EntityLifeStage.Deleted)
            {
                pos = transform.WorldPosition;
                return new Box2(pos, pos);
            }

            if (ent.TryGetContainerMan(out var manager))
            {
                return GetWorldAABB(manager.Owner);
            }

            pos = transform.WorldPosition;

            return _entityManager.TryGetComponent(ent, out ILookupWorldBox2Component? lookup) ?
                lookup.GetWorldAABB(pos) :
                new Box2(pos, pos);
        }

        #endregion
    }
}
