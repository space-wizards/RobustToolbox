using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Client.Interfaces.GameObjects
{
    public interface IClientEntityManager : IEntityManager
    {
        IEnumerable<IEntity> GetEntitiesInRange(GridCoordinates position, float Range);
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position);
        IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position);
        bool AnyEntitiesIntersecting(MapId mapId, Box2 box);
        void ApplyEntityStates(IEnumerable<EntityState> entityStates, IEnumerable<EntityUid> deletions, float serverTime);
    }
}
