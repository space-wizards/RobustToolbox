using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.Network;
using GameObject;
using Lidgren.Network;
using GorgonLibrary;
using SS13_Shared;
using SS13_Shared.GO;

namespace CGO
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class EntityManager:GameObject.EntityManager
    {
        public EntityManager(INetworkManager networkManager)
            :base(EngineType.Client, new EntityNetworkManager(networkManager))
        {}

        
        public Entity[] GetEntitiesInRange(Vector2D position, float Range)
        {
            var entities = from e in _entities.Values
                           where (position - e.GetComponent<TransformComponent>(ComponentFamily.Transform).Position).Length < Range
                           select e;

            return entities.ToArray();
        }
    }
}
