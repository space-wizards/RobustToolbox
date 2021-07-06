using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public interface IEntityLookup
    {
        // Not an EntitySystem given EntityManager has a dependency on it which means it's just easier to IoC it for tests.

        void Startup();

        void Shutdown();

        void Update();
        bool AnyEntitiesIntersecting(MapId mapId, Box2 box, bool approximate = false);

        IEnumerable<IEntity> GetEntitiesInMap(MapId mapId);

        IEnumerable<IEntity> GetEntitiesAt(MapId mapId, Vector2 position, bool approximate = false);

        IEnumerable<IEntity> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
            float arcWidth, bool approximate = false);

        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position, bool approximate = false);

        IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity, bool approximate = false);

        IEnumerable<IEntity> GetEntitiesIntersecting(MapCoordinates position, bool approximate = false);

        IEnumerable<IEntity> GetEntitiesIntersecting(EntityCoordinates position, bool approximate = false);

        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position, bool approximate = false);

        void FastEntitiesIntersecting(in MapId mapId, ref Box2 position, EntityQueryCallback callback);

        IEnumerable<IEntity> GetEntitiesInRange(EntityCoordinates position, float range, bool approximate = false);

        IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range, bool approximate = false);

        IEnumerable<IEntity> GetEntitiesInRange(MapId mapId, Vector2 point, float range, bool approximate = false);

        IEnumerable<IEntity> GetEntitiesInRange(MapId mapId, Box2 box, float range, bool approximate = false);

        bool IsIntersecting(IEntity entityOne, IEntity entityTwo);

        /// <summary>
        /// Updates the lookup for this entity.
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="worldAABB">Pass in to avoid it being re-calculated</param>
        /// <returns></returns>
        bool UpdateEntityTree(IEntity entity, Box2? worldAABB = null);

        void RemoveFromEntityTrees(IEntity entity);

        Box2 GetWorldAabbFromEntity(in IEntity ent);
    }

    [UsedImplicitly]
    public class EntityLookup : IEntityLookup, IEntityEventSubscriber
    {
        private readonly IComponentManager _compManager;
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

        public EntityLookup(IComponentManager compManager, IEntityManager entityManager, IMapManager mapManager)
        {
            _compManager = compManager;
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
            eventBus.SubscribeEvent<MoveEvent>(EventSource.Local, this, ev => _moveQueue.Push(ev));
            eventBus.SubscribeEvent<RotateEvent>(EventSource.Local, this, ev => _rotateQueue.Push(ev));
            eventBus.SubscribeEvent<EntParentChangedMessage>(EventSource.Local, this, ev => _parentChangeQueue.Enqueue(ev));

            eventBus.SubscribeLocalEvent<EntityLookupComponent, ComponentInit>(HandleLookupInit);
            eventBus.SubscribeLocalEvent<EntityLookupComponent, ComponentShutdown>(HandleLookupShutdown);
            eventBus.SubscribeEvent<GridInitializeEvent>(EventSource.Local, this, HandleGridInit);

            _entityManager.EntityDeleted += HandleEntityDeleted;
            _entityManager.EntityStarted += HandleEntityStarted;
            _mapManager.MapCreated += HandleMapCreated;
            Started = true;
        }

        private void HandleLookupShutdown(EntityUid uid, EntityLookupComponent component, ComponentShutdown args)
        {
            component.Tree.Clear();
        }

        private void HandleGridInit(GridInitializeEvent ev)
        {
            _entityManager.GetEntity(ev.EntityUid).EnsureComponent<EntityLookupComponent>();
        }

        private void HandleLookupInit(EntityUid uid, EntityLookupComponent component, ComponentInit args)
        {
            var capacity = (int) Math.Min(256, Math.Ceiling(component.Owner.Transform.ChildCount / (float) GrowthRate) * GrowthRate);

            component.Tree = new DynamicTree<IEntity>(
                GetWorldAabbFromEntity,
                capacity: capacity,
                growthFunc: x => x == GrowthRate ? GrowthRate * 8 : x + GrowthRate
            );
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

            _entityManager.EventBus.UnsubscribeEvents(this);
            _entityManager.EntityDeleted -= HandleEntityDeleted;
            _entityManager.EntityStarted -= HandleEntityStarted;
            _mapManager.MapCreated -= HandleMapCreated;
            Started = false;
        }

        private void HandleEntityDeleted(object? sender, EntityUid uid)
        {
            RemoveFromEntityTrees(_entityManager.GetEntity(uid));
        }

        private void HandleEntityStarted(object? sender, EntityUid uid)
        {
            UpdateEntityTree(_entityManager.GetEntity(uid));
        }

        private void HandleMapCreated(object? sender, MapEventArgs eventArgs)
        {
            if (eventArgs.Map == MapId.Nullspace) return;

            _mapManager.GetMapEntity(eventArgs.Map).EnsureComponent<EntityLookupComponent>();
        }

        public void Update()
        {
            // Acruid said he'd deal with Update being called around IEntityManager later.

            // Could be more efficient but essentially nuke their old lookup and add to new lookup if applicable.
            while (_parentChangeQueue.TryDequeue(out var mapChangeEvent))
            {
                _handledThisTick.Add(mapChangeEvent.Entity.Uid);
                RemoveFromEntityTrees(mapChangeEvent.Entity);

                if (mapChangeEvent.Entity.Deleted) continue;
                UpdateEntityTree(mapChangeEvent.Entity, GetWorldAabbFromEntity(mapChangeEvent.Entity));
            }

            while (_moveQueue.TryPop(out var moveEvent))
            {
                if (moveEvent.Sender.Deleted || !_handledThisTick.Add(moveEvent.Sender.Uid)) continue;

                UpdateEntityTree(moveEvent.Sender, moveEvent.WorldAABB);
            }

            while (_rotateQueue.TryPop(out var rotateEvent))
            {
                if (rotateEvent.Sender.Deleted || !_handledThisTick.Add(rotateEvent.Sender.Uid)) continue;

                UpdateEntityTree(rotateEvent.Sender, rotateEvent.WorldAABB);
            }

            _handledThisTick.Clear();
        }

        #region Spatial Queries

        private IEnumerable<EntityLookupComponent> GetLookupsIntersecting(MapId mapId, Box2 worldAABB)
        {
            if (mapId == MapId.Nullspace) yield break;

            var canBeEnclosed = true;

            // TODO: Recursive and all that.
            foreach (var grid in _mapManager.FindGridsIntersecting(mapId, worldAABB.Enlarged(_lookupEnlargementRange)))
            {
                yield return _entityManager.GetEntity(grid.GridEntityId).GetComponent<EntityLookupComponent>();

                // If wholly enclosed no point checking others.
                if (canBeEnclosed && grid.WorldBounds.Encloses(worldAABB))
                {
                    yield break;
                }

                canBeEnclosed = false;
            }

            yield return _mapManager.GetMapEntity(mapId).GetComponent<EntityLookupComponent>();
        }

        /// <inheritdoc />
        public bool AnyEntitiesIntersecting(MapId mapId, Box2 box, bool approximate = false)
        {
            var found = false;

            foreach (var lookup in GetLookupsIntersecting(mapId, box))
            {
                var offsetBox = box.Translated(-lookup.Owner.Transform.WorldPosition);

                lookup.Tree.QueryAabb(ref found, (ref bool found, in IEntity ent) =>
                {
                    if (!ent.Deleted)
                    {
                        found = true;
                        return false;
                    }

                    return true;
                }, offsetBox, approximate);
            }

            return found;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void FastEntitiesIntersecting(in MapId mapId, ref Box2 position, EntityQueryCallback callback)
        {
            foreach (var lookup in GetLookupsIntersecting(mapId, position))
            {
                var offsetBox = position.Translated(-lookup.Owner.Transform.WorldPosition);

                lookup.Tree._b2Tree.FastQuery(ref offsetBox, (ref IEntity data) => callback(data));
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position, bool approximate = false)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<IEntity>();

            var list = new List<IEntity>();

            foreach (var lookup in GetLookupsIntersecting(mapId, position))
            {
                var offsetBox = position.Translated(-lookup.Owner.Transform.WorldPosition);

                lookup.Tree.QueryAabb(ref list, (ref List<IEntity> list, in IEntity ent) =>
                {
                    if (!ent.Deleted)
                    {
                        list.Add(ent);
                    }
                    return true;
                }, offsetBox, approximate);
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position, bool approximate = false)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<IEntity>();

            var aabb = new Box2(position, position).Enlarged(PointEnlargeRange);
            var list = new List<IEntity>();
            var state = (list, position);

            foreach (var lookup in GetLookupsIntersecting(mapId, aabb))
            {
                var offsetBox = aabb.Translated(-lookup.Owner.Transform.WorldPosition);

                lookup.Tree.QueryAabb(ref state, (ref (List<IEntity> list, Vector2 position) state, in IEntity ent) =>
                {
                    if (Intersecting(ent, state.position))
                    {
                        state.list.Add(ent);
                    }
                    return true;
                }, offsetBox, approximate);
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapCoordinates position, bool approximate = false)
        {
            return GetEntitiesIntersecting(position.MapId, position.Position, approximate);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(EntityCoordinates position, bool approximate = false)
        {
            var mapPos = position.ToMap(_entityManager);
            return GetEntitiesIntersecting(mapPos.MapId, mapPos.Position, approximate);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity, bool approximate = false)
        {
            if (entity.TryGetComponent<IPhysBody>(out var component))
            {
                return GetEntitiesIntersecting(entity.Transform.MapID, component.GetWorldAABB(), approximate);
            }

            return GetEntitiesIntersecting(entity.Transform.Coordinates, approximate);
        }

        /// <inheritdoc />
        public bool IsIntersecting(IEntity entityOne, IEntity entityTwo)
        {
            var position = entityOne.Transform.MapPosition.Position;
            return Intersecting(entityTwo, position);
        }

        private bool Intersecting(IEntity entity, Vector2 mapPosition)
        {
            if (entity.TryGetComponent(out IPhysBody? component))
            {
                if (component.GetWorldAABB().Contains(mapPosition))
                    return true;
            }
            else
            {
                var transform = entity.Transform;
                var entPos = transform.WorldPosition;
                if (MathHelper.CloseTo(entPos.X, mapPosition.X)
                    && MathHelper.CloseTo(entPos.Y, mapPosition.Y))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(EntityCoordinates position, float range, bool approximate = false)
        {
            var mapCoordinates = position.ToMap(_entityManager);
            var mapPosition = mapCoordinates.Position;
            var aabb = new Box2(mapPosition - new Vector2(range / 2, range / 2),
                mapPosition + new Vector2(range / 2, range / 2));
            return GetEntitiesIntersecting(mapCoordinates.MapId, aabb, approximate);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(MapId mapId, Box2 box, float range, bool approximate = false)
        {
            var aabb = box.Enlarged(range);
            return GetEntitiesIntersecting(mapId, aabb, approximate);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(MapId mapId, Vector2 point, float range, bool approximate = false)
        {
            var aabb = new Box2(point, point).Enlarged(range);
            return GetEntitiesIntersecting(mapId, aabb, approximate);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range, bool approximate = false)
        {
            if (entity.TryGetComponent<IPhysBody>(out var component))
            {
                return GetEntitiesInRange(entity.Transform.MapID, component.GetWorldAABB(), range, approximate);
            }

            return GetEntitiesInRange(entity.Transform.Coordinates, range, approximate);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
            float arcWidth, bool approximate = false)
        {
            var position = coordinates.ToMap(_entityManager).Position;

            foreach (var entity in GetEntitiesInRange(coordinates, range * 2, approximate))
            {
                var angle = new Angle(entity.Transform.WorldPosition - position);
                if (angle.Degrees < direction.Degrees + arcWidth / 2 &&
                    angle.Degrees > direction.Degrees - arcWidth / 2)
                    yield return entity;
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInMap(MapId mapId)
        {
            foreach (EntityLookupComponent comp in _compManager.EntityQuery<EntityLookupComponent>(true))
            {
                foreach (var entity in comp.Tree)
                {
                    if (entity.Deleted) continue;

                    yield return entity;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesAt(MapId mapId, Vector2 position, bool approximate = false)
        {
            if (mapId == MapId.Nullspace) return Enumerable.Empty<IEntity>();

            var list = new List<IEntity>();

            var state = (list, position);

            var aabb = new Box2(position, position).Enlarged(PointEnlargeRange);

            foreach (var lookup in GetLookupsIntersecting(mapId, aabb))
            {
                var offsetPos = position -lookup.Owner.Transform.WorldPosition;

                lookup.Tree.QueryPoint(ref state, (ref (List<IEntity> list, Vector2 position) state, in IEntity ent) =>
                {
                    var transform = ent.Transform;
                    if (MathHelper.CloseTo(transform.Coordinates.X, state.position.X) &&
                        MathHelper.CloseTo(transform.Coordinates.Y, state.position.Y))
                    {
                        state.list.Add(ent);
                    }

                    return true;
                }, offsetPos, approximate);
            }

            return list;
        }

        #endregion

        #region Entity DynamicTree

        private EntityLookupComponent? GetLookup(IEntity entity)
        {
            if (entity.Transform.MapID == MapId.Nullspace)
            {
                return null;
            }

            // if it's map return null. Grids should return the map's broadphase.
            if (entity.HasComponent<EntityLookupComponent>() &&
                entity.Transform.Parent == null)
            {
                return null;
            }

            var parent = entity.Transform.Parent?.Owner;

            while (true)
            {
                if (parent == null) break;

                if (parent.TryGetComponent(out EntityLookupComponent? comp)) return comp;
                parent = parent.Transform.Parent?.Owner;
            }

            return null;
        }

        /// <inheritdoc />
        public virtual bool UpdateEntityTree(IEntity entity, Box2? worldAABB = null)
        {
            // look there's JANK everywhere but I'm just bandaiding it for now for shuttles and we'll fix it later when
            // PVS is more stable and entity anchoring has been battle-tested.
            if (entity.Deleted)
            {
                RemoveFromEntityTrees(entity);
                return true;
            }

            if (!entity.Initialized)
            {
                return false;
            }

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

            var transform = entity.Transform;
            DebugTools.Assert(transform.Initialized);

            // for debugging
            var necessary = 0;

            if (lookup.Tree.AddOrUpdate(entity, worldAABB))
            {
                ++necessary;
            }

            if (!entity.HasComponent<EntityLookupComponent>())
            {
                foreach (var childTx in entity.Transform.ChildEntityUids)
                {
                    if (!_handledThisTick.Add(childTx)) continue;

                    if (UpdateEntityTree(_entityManager.GetEntity(childTx)))
                    {
                        ++necessary;
                    }
                }
            }

            return necessary > 0;
        }

        /// <inheritdoc />
        public void RemoveFromEntityTrees(IEntity entity)
        {
            foreach (var lookup in _compManager.EntityQuery<EntityLookupComponent>(true))
            {
                lookup.Tree.Remove(entity);
            }
        }

        public Box2 GetWorldAabbFromEntity(in IEntity ent)
        {
            var pos = ent.Transform.WorldPosition;

            if (ent.Deleted || !ent.TryGetComponent(out PhysicsComponent? physics))
                return new Box2(pos, pos);

            return physics.GetWorldAABB(pos);
        }

        #endregion
    }
}
