using SFML.System;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class ClientEntityManager : EntityManager, IClientEntityManager
    {
        public IEnumerable<IEntity> GetEntitiesInRange(Vector2f position, float Range)
        {
            Range *= Range; // Square it here to avoid Sqrt
            return from e in _entities.Values
                   where
                       (position -
                        e.GetComponent<TransformComponent>().Position).
                           LengthSquared() < Range
                   select e;
        }

        public override void InitializeEntities()
        {
            if (Initialized)
            {
                throw new InvalidOperationException("InitializeEntities() called multiple times");
            }
            base.InitializeEntities();
            EntitySystemManager.Initialize();
            Initialized = true;
        }
    }
}
