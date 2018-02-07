using SS14.Server.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using System.Collections.Generic;
using SS14.Shared.Prototypes;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Map;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Maths;
using SS14.Shared.GameObjects.Serialization;

namespace SS14.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class ServerEntityManager : EntityManager, IServerEntityManager
    {
        #region IEntityManager Members

        [Dependency]
        private readonly IPrototypeManager _protoManager;

        /// <inheritdoc />
        public bool TrySpawnEntityAt(string entityType, LocalCoordinates coordinates, out IEntity entity)
        {
            var prototype = _protoManager.Index<EntityPrototype>(entityType);
            if (prototype.CanSpawnAt(coordinates.Grid, coordinates.Position))
            {
                Entity result = SpawnEntity(entityType);
                result.GetComponent<TransformComponent>().LocalPosition = coordinates;
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
        public bool TrySpawnEntityAt(string entityType, Vector2 position, MapId argMap, out IEntity entity)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            var coordinates = new LocalCoordinates(position, mapManager.GetMap(argMap).FindGridAt(position));
            return TrySpawnEntityAt(entityType, coordinates, out entity);
        }

        /// <inheritdoc />
        public IEntity ForceSpawnEntityAt(string entityType, LocalCoordinates coordinates)
        {
            Entity entity = SpawnEntity(entityType);
            entity.GetComponent<TransformComponent>().LocalPosition = coordinates;
            if (Started)
            {
                InitializeEntity(entity);
            }
            return entity;
        }

        /// <inheritdoc />
        public IEntity ForceSpawnEntityAt(string entityType, Vector2 position, MapId argMap)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            if (!mapManager.TryGetMap(argMap, out var map))
            {
                map = mapManager.DefaultMap;
            }

            return ForceSpawnEntityAt(entityType, new LocalCoordinates(position, map.FindGridAt(position)));
        }

        public List<EntityState> GetEntityStates()
        {
            var stateEntities = new List<EntityState>();
            foreach (IEntity entity in GetEntities())
            {
                EntityState entityState = entity.GetEntityState();
                stateEntities.Add(entityState);
            }
            return stateEntities;
        }

        public void SaveGridEntities(EntitySerializer serializer, GridId gridId)
        {
            // serialize all entities to disk
            foreach (var entity in _allEntities)
            {
                if (entity.TryGetComponent<ITransformComponent>(out var transform) && transform.GridID == gridId)
                {
                    entity.ExposeData(serializer);
                }
            }
        }

        #endregion IEntityManager Members

        #region EntityGetters

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

        public IEnumerable<IEntity> GetEntitiesIntersecting(LocalCoordinates position)
        {
            return GetEntitiesIntersecting(position.MapID, position.ToWorld().Position);
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity)
        {
            if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
            {
                return GetEntitiesIntersecting(entity.GetComponent<ITransformComponent>().MapID, component.WorldAABB);
            }
            else
            {
                return GetEntitiesIntersecting(entity.GetComponent<ITransformComponent>().LocalPosition);
            }
        }

        public IEnumerable<IEntity> GetEntitiesInRange(LocalCoordinates position, float range)
        {
            var aabb = new Box2(position.Position - new Vector2(range / 2, range / 2), position.Position + new Vector2(range / 2, range / 2));
            return GetEntitiesIntersecting(position.MapID, aabb);
        }

        public IEnumerable<IEntity> GetEntitiesInRange(MapId mapID, Box2 box, float range)
        {
            var aabb = new Box2(box.Left-range, box.Top+range, box.Right+range, box.Bottom-range);
            return GetEntitiesIntersecting(mapID, aabb);
        }

        public IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range)
        {
            if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
            {
                return GetEntitiesInRange(entity.GetComponent<ITransformComponent>().MapID, component.WorldAABB, range);
            }
            else
            {
                LocalCoordinates coords = entity.GetComponent<ITransformComponent>().LocalPosition;
                return GetEntitiesInRange(coords, range);
            }
        }

        #endregion LocationGetters

        public override void Startup()
        {
            base.Startup();
            EntitySystemManager.Initialize();
            Started = true;
            InitializeEntities();
        }
    }
}
