using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;
using System.Collections.Generic;
using SS14.Shared.Maths;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface IClientEntityManager : IEntityManager
    {
        IEnumerable<IEntity> GetEntitiesInRange(LocalCoordinates position, float Range);
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position);
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position);
        bool AnyEntitiesIntersecting(MapId mapId, Box2 box);
        void ApplyEntityStates(IEnumerable<EntityState> entityStates, float serverTime);
    }
}
