using OpenTK;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using System.Collections.Generic;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IServerEntityManager : IEntityManager
    {
        void Initialize();

        IEntity SpawnEntity(string template, int? uid = null);

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

        List<EntityState> GetEntityStates();
    }
}
