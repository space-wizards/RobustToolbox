using OpenTK;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using System.Collections.Generic;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IServerEntityManager : IEntityManager
    {
        void Initialize();
        void LoadEntities();
        void SaveEntities();
        IEntity SpawnEntity(string template, int? uid = null);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="EntityType"></param>
        /// <param name="grid"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        bool TrySpawnEntityAt(string EntityType, IMapGrid grid, Vector2 position, out IEntity entity);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="EntityType"></param>
        /// <param name="grid"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        IEntity ForceSpawnEntityAt(string EntityType, IMapGrid grid, Vector2 position);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="EntityType"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        bool TrySpawnEntityAt(string EntityType, Vector2 position, out IEntity entity);

        /// <summary>
        /// Spawns an entity at a specific position
        /// </summary>
        /// <param name="EntityType"></param>
        /// <param name="position"></param>
        /// <returns></returns>
        IEntity ForceSpawnEntityAt(string EntityType, Vector2 position);
        List<EntityState> GetEntityStates();
    }
}
