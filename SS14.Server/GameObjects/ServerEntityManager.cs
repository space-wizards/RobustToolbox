using System.Collections.Generic;
using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.GameObjects.Components.BoundingBox;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using SS14.Shared.Prototypes;
using SS14.Shared.Serialization;

namespace SS14.Server.GameObjects
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

        private readonly List<(uint tick, EntityUid uid)> DeletionHistory = new List<(uint, EntityUid)>();

        /// <inheritdoc />
        public override IEntity CreateEntity(string protoName)
        {
            return InternalCreateEntity(protoName, null);
        }

        /// <inheritdoc />
        public override Entity SpawnEntity(string protoName)
        {
            var newEnt = (Entity)CreateEntity(protoName);
            InitializeEntity(newEnt);
            return newEnt;
        }

        /// <inheritdoc />
        public override bool TrySpawnEntityAt(string entityType, GridLocalCoordinates coordinates, out IEntity entity)
        {
            var prototype = _protoManager.Index<EntityPrototype>(entityType);
            if (prototype.CanSpawnAt(coordinates.Grid, coordinates.Position))
            {
                Entity result = SpawnEntity(entityType);
                result.GetComponent<ITransformComponent>().LocalPosition = coordinates;
                if (Started)
                {
                    InitializeEntity(result);
                }

                entity = result;
                return true;
            }

            entity = null;
            return false;
        }

        /// <inheritdoc />
        public override bool TrySpawnEntityAt(string entityType, Vector2 position, MapId argMap, out IEntity entity)
        {
            var coordinates = new GridLocalCoordinates(position, _mapManager.GetMap(argMap).FindGridAt(position));
            return TrySpawnEntityAt(entityType, coordinates, out entity);
        }

        /// <inheritdoc />
        public override IEntity ForceSpawnEntityAt(string entityType, GridLocalCoordinates coordinates)
        {
            Entity entity = SpawnEntity(entityType);
            entity.GetComponent<ITransformComponent>().LocalPosition = coordinates;
            if (Started)
            {
                InitializeEntity(entity);
            }

            return entity;
        }

        /// <inheritdoc />
        public override IEntity ForceSpawnEntityAt(string entityType, Vector2 position, MapId argMap)
        {
            if (!_mapManager.TryGetMap(argMap, out var map))
            {
                map = _mapManager.DefaultMap;
            }

            return ForceSpawnEntityAt(entityType, new GridLocalCoordinates(position, map.FindGridAt(position)));
        }

        /// <inheritdoc />
        public List<EntityState> GetEntityStates(uint fromTick)
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

        public List<EntityUid> GetDeletedEntities(uint fromTick)
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

        public void CullDeletionHistory(uint toTick)
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
                var transform = entity.GetComponent<ITransformComponent>();
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
                var transform = entity.GetComponent<ITransformComponent>();
                if (transform.MapID != mapId)
                    continue;

                if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
                {
                    if (component.WorldAABB.Contains(position))
                        yield return entity;
                }
                else
                {
                    if (FloatMath.CloseTo(transform.LocalPosition.X, position.X) && FloatMath.CloseTo(transform.LocalPosition.Y, position.Y))
                    {
                        yield return entity;
                    }
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(GridLocalCoordinates position)
        {
            return GetEntitiesIntersecting(position.MapID, position.ToWorld().Position);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity)
        {
            if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
            {
                return GetEntitiesIntersecting(entity.GetComponent<ITransformComponent>().MapID, component.WorldAABB);
            }

            return GetEntitiesIntersecting(entity.GetComponent<ITransformComponent>().LocalPosition);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(GridLocalCoordinates position, float range)
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
                return GetEntitiesInRange(entity.GetComponent<ITransformComponent>().MapID, component.WorldAABB, range);
            }
            else
            {
                GridLocalCoordinates coords = entity.GetComponent<ITransformComponent>().LocalPosition;
                return GetEntitiesInRange(coords, range);
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInArc(GridLocalCoordinates coordinates, float range, Angle direction, float arcwidth)
        {
            var entities = GetEntitiesInRange(coordinates, range);

            foreach (var entity in entities)
            {
                var angle = new Angle(entity.GetComponent<ITransformComponent>().WorldPosition - coordinates.ToWorld().Position);
                if (angle.Degrees < direction.Degrees + arcwidth / 2 && angle.Degrees > direction.Degrees - arcwidth / 2)
                    yield return entity;
            }
        }

        #endregion EntityGetters

        IEntity IServerEntityManagerInternal.AllocEntity(string prototypeName, EntityUid? uid)
        {
            return AllocEntity(prototypeName, uid);
        }

        void IServerEntityManagerInternal.FinishEntity(IEntity entity, IEntityFinishContext context)
        {
            FinishEntity(entity, context);
        }

        /// <inheritdoc />
        public override void Startup()
        {
            base.Startup();
            EntitySystemManager.Initialize();
            Started = true;
            InitializeEntities();
        }
    }
}
