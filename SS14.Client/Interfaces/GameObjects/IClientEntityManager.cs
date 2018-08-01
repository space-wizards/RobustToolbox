using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;
using System.Collections.Generic;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface IClientEntityManager : IEntityManager
    {
        /// <summary>
        /// Creates an uninitialized entity.
        /// </summary>
        /// <param name="protoName">Prototype template to use. If this is null, the entity will only have an
        /// uninitialized TransformComponent inside.</param>
        /// <returns>Newly created entity.</returns>
        IEntity CreateEntity(string protoName);

        /// <summary>
        /// Spawns an initialized entity at the default location.
        /// </summary>
        /// <param name="protoName"></param>
        /// <returns></returns>
        Entity SpawnEntity(string protoName);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="coordinates"></param>
        /// <returns></returns>
        IEntity ForceSpawnEntityAt(string entityType, GridLocalCoordinates coordinates);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="entityType"></param>
        /// <param name="position"></param>
        /// <param name="argMap"></param>
        /// <returns></returns>
        IEntity ForceSpawnEntityAt(string entityType, Vector2 position, MapId argMap);

        IEnumerable<IEntity> GetEntitiesInRange(GridLocalCoordinates position, float Range);
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position);
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position);
        bool AnyEntitiesIntersecting(MapId mapId, Box2 box);
        void ApplyEntityStates(IEnumerable<EntityState> entityStates, IEnumerable<EntityUid> deletions, float serverTime);
    }
}
