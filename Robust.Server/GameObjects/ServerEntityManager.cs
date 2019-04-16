using System.Collections.Generic;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Robust.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class ServerEntityManager : EntityManager, IServerEntityManagerInternal
    {
        #region IEntityManager Members

        [Dependency]
        private readonly IPrototypeManager _protoManager;

        [Dependency]
        private readonly IMapManager _mapManager;

        private readonly List<(GameTick tick, EntityUid uid)> DeletionHistory = new List<(GameTick, EntityUid)>();

        public override IEntity SpawnEntity(string protoName)
        {
            var newEnt = CreateEntity(protoName);
            InitializeAndStartEntity(newEnt);
            return newEnt;
        }

        /// <inheritdoc />
        public override bool TrySpawnEntityAt(string entityType, GridCoordinates coordinates, out IEntity entity)
        {
            var prototype = _protoManager.Index<EntityPrototype>(entityType);
            if (prototype.CanSpawnAt(coordinates.Grid, coordinates.Position))
            {
                var result = CreateEntity(entityType);
                result.Transform.GridPosition = coordinates;
                InitializeAndStartEntity(result);
                entity = result;
                return true;
            }

            entity = null;
            return false;
        }

        /// <inheritdoc />
        public override bool TrySpawnEntityAt(string entityType, Vector2 position, MapId argMap, out IEntity entity)
        {
            var coordinates = new GridCoordinates(position, _mapManager.GetMap(argMap).FindGridAt(position));
            return TrySpawnEntityAt(entityType, coordinates, out entity);
        }

        /// <inheritdoc />
        public override IEntity ForceSpawnEntityAt(string entityType, GridCoordinates coordinates)
        {
            var entity = CreateEntity(entityType);
            entity.Transform.GridPosition = coordinates;
            InitializeAndStartEntity(entity);
            return entity;
        }

        /// <inheritdoc />
        public override IEntity ForceSpawnEntityAt(string entityType, Vector2 position, MapId argMap)
        {
            if (!_mapManager.TryGetMap(argMap, out var map))
            {
                map = _mapManager.DefaultMap;
            }

            return ForceSpawnEntityAt(entityType, new GridCoordinates(position, map.FindGridAt(position)));
        }

        /// <inheritdoc />
        public List<EntityState> GetEntityStates(GameTick fromTick)
        {
            var stateEntities = new List<EntityState>();
            foreach (IEntity entity in GetEntities())
            {
                if (entity.LastModifiedTick < fromTick)
                {
                    continue;
                }

                EntityState entityState = entity.GetEntityState(fromTick);
                stateEntities.Add(entityState);
            }

            return stateEntities;
        }

        public override void DeleteEntity(IEntity e)
        {
            base.DeleteEntity(e);

            DeletionHistory.Add((CurrentTick, e.Uid));
        }

        public List<EntityUid> GetDeletedEntities(GameTick fromTick)
        {
            List<EntityUid> list = new List<EntityUid>();
            foreach ((var tick, var id) in DeletionHistory)
            {
                if (tick >= fromTick)
                {
                    list.Add(id);
                }
            }

            return list;
        }

        public void CullDeletionHistory(GameTick toTick)
        {
            DeletionHistory.RemoveAll(hist => hist.tick <= toTick);
        }

        #endregion IEntityManager Members

        #region EntityGetters

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position)
        {
            foreach (var entity in GetEntities())
            {
                var transform = entity.Transform;
                if (transform.MapID != mapId)
                    continue;

                if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
                {
                    if (position.Intersects(component.WorldAABB))
                        yield return entity;
                }
                else
                {
                    if (position.Contains(transform.WorldPosition))
                    {
                        yield return entity;
                    }
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position)
        {
            foreach (var entity in GetEntities())
            {
                var transform = entity.Transform;
                if (transform.MapID != mapId)
                    continue;

                if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
                {
                    if (component.WorldAABB.Contains(position))
                        yield return entity;
                }
                else
                {
                    if (FloatMath.CloseTo(transform.GridPosition.X, position.X) && FloatMath.CloseTo(transform.GridPosition.Y, position.Y))
                    {
                        yield return entity;
                    }
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(GridCoordinates position)
        {
            return GetEntitiesIntersecting(position.MapID, position.ToWorld().Position);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity)
        {
            if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
            {
                return GetEntitiesIntersecting(entity.Transform.MapID, component.WorldAABB);
            }

            return GetEntitiesIntersecting(entity.Transform.GridPosition);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(GridCoordinates position, float range)
        {
            var aabb = new Box2(position.Position - new Vector2(range / 2, range / 2), position.Position + new Vector2(range / 2, range / 2));
            return GetEntitiesIntersecting(position.MapID, aabb);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(MapId mapID, Box2 box, float range)
        {
            var aabb = new Box2(box.Left - range, box.Top - range, box.Right + range, box.Bottom + range);
            return GetEntitiesIntersecting(mapID, aabb);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range)
        {
            if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
            {
                return GetEntitiesInRange(entity.Transform.MapID, component.WorldAABB, range);
            }
            else
            {
                GridCoordinates coords = entity.Transform.GridPosition;
                return GetEntitiesInRange(coords, range);
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInArc(GridCoordinates coordinates, float range, Angle direction, float arcwidth)
        {
            var entities = GetEntitiesInRange(coordinates, range);

            foreach (var entity in entities)
            {
                var angle = new Angle(entity.Transform.WorldPosition - coordinates.ToWorld().Position);
                if (angle.Degrees < direction.Degrees + arcwidth / 2 && angle.Degrees > direction.Degrees - arcwidth / 2)
                    yield return entity;
            }
        }

        #endregion EntityGetters

        IEntity IServerEntityManagerInternal.AllocEntity(string prototypeName, EntityUid? uid)
        {
            return AllocEntity(prototypeName, uid);
        }

        void IServerEntityManagerInternal.FinishEntityLoad(IEntity entity, IEntityLoadContext context)
        {
            LoadEntity((Entity) entity, context);
        }

        void IServerEntityManagerInternal.FinishEntityInitialization(IEntity entity)
        {
            InitializeEntity((Entity)entity);
        }

        void IServerEntityManagerInternal.FinishEntityStartup(IEntity entity)
        {
            StartEntity((Entity)entity);
        }

        /// <inheritdoc />
        public override void Startup()
        {
            base.Startup();
            EntitySystemManager.Initialize();
            Started = true;
        }
    }
}
