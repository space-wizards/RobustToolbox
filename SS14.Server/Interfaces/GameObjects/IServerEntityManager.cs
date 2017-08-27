using OpenTK;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using System.Collections.Generic;

namespace SS14.Server.Interfaces.GameObjects
{
    public interface IServerEntityManager : IEntityManager
    {
        void Initialize();
        void LoadEntities();
        void SaveEntities();
        IEntity SpawnEntity(string template, int? uid = null);
        bool TrySpawnEntityAt(string entityTemplateName, IMapGrid grid, Vector2 vector2, out IEntity entity);
        IEntity ForceSpawnEntityAt(string entityTemplateName, IMapGrid grid, Vector2 vector2);
        bool TrySpawnEntityAt(string entityTemplateName, Vector2 vector2, out IEntity entity);
        IEntity ForceSpawnEntityAt(string entityTemplateName, Vector2 vector2);
        List<EntityState> GetEntityStates();
    }
}
