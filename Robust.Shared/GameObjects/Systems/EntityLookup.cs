using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;
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
        private readonly IEntityManager _entityManager;
        private readonly IMapManager _mapManager;

        private readonly Dictionary<MapId, DynamicTree<IEntity>> _entityTreesPerMap = new();

        // Using stacks so we always use latest data (given we only run it once per entity).
        private readonly Stack<MoveEvent> _moveQueue = new();
        private readonly Stack<RotateEvent> _rotateQueue = new();
        private readonly Queue<EntMapIdChangedMessage> _mapChangeQueue = new();

        /// <summary>
        /// Move and rotate events generate the same update so no point duplicating work in the same tick.
        /// </summary>
        private readonly HashSet<EntityUid> _handledThisTick = new();

        // TODO: Should combine all of the methods that check for IPhysBody and just use the one GetWorldAabbFromEntity method

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

            var eventBus = _entityManager.EventBus;
            eventBus.SubscribeEvent<MoveEvent>(EventSource.Local, this, ev => _moveQueue.Push(ev));
            eventBus.SubscribeEvent<RotateEvent>(EventSource.Local, this, ev => _rotateQueue.Push(ev));
            eventBus.SubscribeEvent<EntMapIdChangedMessage>(EventSource.Local, this, ev => _mapChangeQueue.Enqueue(ev));
            _entityManager.EntityDeleted += HandleEntityDeleted;
            _entityManager.EntityStarted += HandleEntityStarted;
            _mapManager.MapCreated += HandleMapCreated;
            _mapManager.MapDestroyed += HandleMapDestroyed;
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
            _mapChangeQueue.Clear();
            _entityTreesPerMap.Clear();

            _entityManager.EventBus.UnsubscribeEvents(this);
            _entityManager.EntityDeleted -= HandleEntityDeleted;
            _entityManager.EntityStarted -= HandleEntityStarted;
            _mapManager.MapCreated -= HandleMapCreated;
            _mapManager.MapDestroyed -= HandleMapDestroyed;
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
            _entityTreesPerMap[eventArgs.Map] = new DynamicTree<IEntity>(
                GetWorldAabbFromEntity,
                capacity: 16,
                growthFunc: x => x == 16 ? 3840 : x + 256
            );
        }

        private void HandleMapDestroyed(object? sender, MapEventArgs eventArgs)
        {
            _entityTreesPerMap.Remove(eventArgs.Map);
        }

        public void Update()
        {
            // Acruid said he'd deal with Update being called around IEntityManager later.
            _handledThisTick.Clear();

            while (_mapChangeQueue.TryDequeue(out var mapChangeEvent))
            {
                if (mapChangeEvent.Entity.Deleted) continue;
                RemoveFromEntityTree(mapChangeEvent.Entity, mapChangeEvent.OldMapId);
                UpdateEntityTree(mapChangeEvent.Entity, GetWorldAabbFromEntity(mapChangeEvent.Entity));
                _handledThisTick.Add(mapChangeEvent.Entity.Uid);
            }

            while (_moveQueue.TryPop(out var moveEvent))
            {
                if (moveEvent.Sender.Deleted || _handledThisTick.Contains(moveEvent.Sender.Uid)) continue;

                UpdateEntityTree(moveEvent.Sender, moveEvent.WorldAABB);
                _handledThisTick.Add(moveEvent.Sender.Uid);
            }

            while (_rotateQueue.TryPop(out var rotateEvent))
            {
                if (rotateEvent.Sender.Deleted || _handledThisTick.Contains(rotateEvent.Sender.Uid)) continue;

                UpdateEntityTree(rotateEvent.Sender, rotateEvent.WorldAABB);
            }
        }

        #region Spatial Queries

        /// <inheritdoc />
        public bool AnyEntitiesIntersecting(MapId mapId, Box2 box, bool approximate = false)
        {
            var found = false;
            _entityTreesPerMap[mapId].QueryAabb(ref found, (ref bool found, in IEntity ent) =>
            {
                if (!ent.Deleted)
                {
                    found = true;
                    return false;
                }

                return true;
            }, box, approximate);
            return found;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveOptimization)]
        public void FastEntitiesIntersecting(in MapId mapId, ref Box2 position, EntityQueryCallback callback)
        {
            if (mapId == MapId.Nullspace)
                return;

            _entityTreesPerMap[mapId]._b2Tree
                .FastQuery(ref position, (ref IEntity data) => callback(data));
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position, bool approximate = false)
        {
            if (!_entityTreesPerMap.TryGetValue(mapId, out var mapTree))
            {
                return Enumerable.Empty<IEntity>();
            }

            var list = new List<IEntity>();

            mapTree.QueryAabb(ref list, (ref List<IEntity> list, in IEntity ent) =>
            {
                if (!ent.Deleted)
                {
                    list.Add(ent);
                }
                return true;
            }, position, approximate);

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position, bool approximate = false)
        {
            const float range = .00001f / 2;
            var aabb = new Box2(position, position).Enlarged(range);

            if (mapId == MapId.Nullspace)
            {
                return Enumerable.Empty<IEntity>();
            }

            var list = new List<IEntity>();
            var state = (list, position);

            _entityTreesPerMap[mapId].QueryAabb(ref state, (ref (List<IEntity> list, Vector2 position) state, in IEntity ent) =>
            {
                if (Intersecting(ent, state.position))
                {
                    state.list.Add(ent);
                }
                return true;
            }, aabb, approximate);

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
                return GetEntitiesIntersecting(entity.Transform.MapID, component.GetWorldAABB(_mapManager), approximate);
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
                if (component.GetWorldAABB(_mapManager).Contains(mapPosition))
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
                return GetEntitiesInRange(entity.Transform.MapID, component.GetWorldAABB(_mapManager), range, approximate);
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
            if (!_entityTreesPerMap.TryGetValue(mapId, out var trees))
                yield break;

            foreach (var entity in trees)
            {
                if (!entity.Deleted)
                    yield return entity;
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesAt(MapId mapId, Vector2 position, bool approximate = false)
        {
            var list = new List<IEntity>();

            var state = (list, position);

            _entityTreesPerMap[mapId].QueryPoint(ref state, (ref (List<IEntity> list, Vector2 position) state, in IEntity ent) =>
            {
                var transform = ent.Transform;
                if (MathHelper.CloseTo(transform.Coordinates.X, state.position.X) &&
                    MathHelper.CloseTo(transform.Coordinates.Y, state.position.Y))
                {
                    state.list.Add(ent);
                }

                return true;
            }, position, approximate);

            return list;
        }

        #endregion

        #region Entity DynamicTree
        /// <inheritdoc />
        public virtual bool UpdateEntityTree(IEntity entity, Box2? worldAABB = null)
        {
            if (entity.Deleted)
            {
                RemoveFromEntityTrees(entity);
                return true;
            }

            if (!entity.Initialized || !_entityManager.EntityExists(entity.Uid))
            {
                return false;
            }

            var transform = entity.Transform;

            DebugTools.Assert(transform.Initialized);

            var mapId = transform.MapID;

            if (!_entityTreesPerMap.TryGetValue(mapId, out var entTree))
            {
                entTree = new DynamicTree<IEntity>(
                    GetWorldAabbFromEntity,
                    capacity: 16,
                    growthFunc: x => x == 16 ? 3840 : x + 256
                );
                _entityTreesPerMap.Add(mapId, entTree);
            }

            // for debugging
            var necessary = 0;

            if (entTree.AddOrUpdate(entity, worldAABB))
            {
                ++necessary;
            }

            foreach (var childTx in entity.Transform.ChildEntityUids)
            {
                if (UpdateEntityTree(_entityManager.GetEntity(childTx)))
                {
                    ++necessary;
                }
            }

            return necessary > 0;
        }

        public bool RemoveFromEntityTree(IEntity entity, MapId mapId)
        {
            if (_entityTreesPerMap.TryGetValue(mapId, out var tree))
            {
                return tree.Remove(entity);
            }

            return false;
        }

        /// <inheritdoc />
        public void RemoveFromEntityTrees(IEntity entity)
        {
            foreach (var mapId in _mapManager.GetAllMapIds())
            {
                if (_entityTreesPerMap.TryGetValue(mapId, out var entTree))
                {
                    entTree.Remove(entity);
                }
            }
        }

        public Box2 GetWorldAabbFromEntity(in IEntity ent)
        {
            if (ent.Deleted)
                return new Box2(0, 0, 0, 0);

            if (ent.TryGetComponent(out IPhysBody? collider))
                return collider.GetWorldAABB(_mapManager);

            var pos = ent.Transform.WorldPosition;
            return new Box2(pos, pos);
        }

        #endregion
    }
}
