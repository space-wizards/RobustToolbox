using OpenTK;
using SFML.System;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Utility;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class ClientEntityManager : EntityManager, IClientEntityManager
    {
        public IEnumerable<IEntity> GetEntitiesInRange(Vector2 position, float Range)
        {
            Range *= Range; // Square it here to avoid Sqrt

            foreach (var entity in _entities.Values)
            {
                var transform = entity.GetComponent<ITransformComponent>();
                var relativePosition = position - transform.Position;
                if (relativePosition.LengthSquared <= Range)
                {
                    yield return entity;
                }
            }
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
