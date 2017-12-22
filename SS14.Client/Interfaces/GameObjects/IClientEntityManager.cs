using OpenTK;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Map;
using System.Collections.Generic;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface IClientEntityManager : IEntityManager
    {
        IEnumerable<IEntity> GetEntitiesInRange(LocalCoordinates position, float Range);
        IEnumerable<IEntity> GetEntitiesIntersecting(Box2 position);
        IEnumerable<IEntity> GetEntitiesIntersecting(Vector2 position);
        bool AnyEntitiesIntersecting(Box2 position);
        void ApplyEntityStates(IEnumerable<EntityState> entityStates, float serverTime);
        void SpawnDummy();
    }
}
