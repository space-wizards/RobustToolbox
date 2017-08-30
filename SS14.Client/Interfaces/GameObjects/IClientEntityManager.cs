using OpenTK;
using SFML.System;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using System.Collections.Generic;

namespace SS14.Client.Interfaces.GameObjects
{
    public interface IClientEntityManager : IEntityManager
    {
        IEnumerable<IEntity> GetEntitiesInRange(WorldCoordinates position, float Range);
        IEnumerable<IEntity> GetEntitiesIntersecting(Box2 position);
        IEnumerable<IEntity> GetEntitiesIntersecting(Vector2 position);
        bool AnyEntitiesIntersecting(Box2 position);
        void ApplyEntityStates(IEnumerable<EntityState> entityStates, float serverTime);
    }
}
