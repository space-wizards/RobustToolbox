using SS14.Client.Interfaces.Network;
using SS14.Shared.GameObjects;
using SS14.Shared.GO;
using SS14.Shared.Maths;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class EntityManager : SS14.Shared.GameObjects.EntityManager
    {
        public EntityManager(INetworkManager networkManager)
            : base(EngineType.Client, new EntityNetworkManager(networkManager))
        {
        }


        public Entity[] GetEntitiesInRange(Vector2 position, float Range)
        {
            IEnumerable<Entity> entities = from e in _entities.Values
                                           where
                                               (position -
                                                e.GetComponent<TransformComponent>(ComponentFamily.Transform).Position).
                                                   Length < Range
                                           select e;

            return entities.ToArray();
        }
    }
}