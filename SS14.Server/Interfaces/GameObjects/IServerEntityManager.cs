using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;
using System.Collections.Generic;
using SS14.Shared.Maths;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IServerEntityManager : IEntityManager
    {
        IEntity SpawnEntity(string template, EntityUid? uid = null);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="EntityType"></param>
        /// <param name="position"></param>
        /// <param name="argMap"></param>
        /// <param name="entity"></param>
        /// <returns></returns>
        bool TrySpawnEntityAt(string EntityType, Vector2 position, MapId argMap, out IEntity entity);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="EntityType"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        IEntity ForceSpawnEntityAt(string EntityType, LocalCoordinates coordinates);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="EntityType"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        bool TrySpawnEntityAt(string EntityType, LocalCoordinates coordinates, out IEntity entity);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="EntityType"></param>
        /// <param name="position"></param>
        /// <param name="argMap"></param>
        /// <returns></returns>
        IEntity ForceSpawnEntityAt(string EntityType, Vector2 position, MapId argMap);

        /// <summary>
        /// Gets entities with a bounding box that intersects this box
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="position"></param>
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position);

        /// <summary>
        /// Gets entities with a bounding box that intersects this point
        /// </summary>
        /// <param name="mapId"></param>
        /// <param name="position"></param>
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position);

        /// <summary>
        /// Gets entities with a bounding box that intersects this point in coordinate form
        /// </summary>
        /// <param name="position"></param>
        IEnumerable<IEntity> GetEntitiesIntersecting(LocalCoordinates position);

        /// <summary>
        /// Gets entities that intersect with this entity
        /// </summary>
        /// <param name="position"></param>
        IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity);

        /// <summary>
        /// Gets entities within a certain *square* range of this local coordinate
        /// </summary>
        /// <param name="position"></param>
        /// <param name="Range"></param>
        IEnumerable<IEntity> GetEntitiesInRange(LocalCoordinates position, float Range);

        /// <summary>
        /// Gets entities within a certain *square* range of this entity
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="Range"></param>
        IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float Range);

        /// <summary>
        /// Gets entities within a certain *square* range of this bounding box
        /// </summary>
        /// <param name="mapID"></param>
        /// <param name="box"></param>
        /// <param name="Range"></param>
        IEnumerable<IEntity> GetEntitiesInRange(MapId mapID, Box2 box, float Range);

        List<EntityState> GetEntityStates();
    }
}
