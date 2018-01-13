using OpenTK;
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
using SS14.Shared.Map;
using Vector2 = SS14.Shared.Maths.Vector2;
using SS14.Client.Interfaces.GameObjects.Components;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class ClientEntityManager : EntityManager, IClientEntityManager
    {
        public IEnumerable<IEntity> GetEntitiesInRange(LocalCoordinates worldPos, float Range)
        {
            Range *= Range; // Square it here to avoid Sqrt

            foreach (var entity in GetEntities())
            {
                var transform = entity.GetComponent<ITransformComponent>();
                var relativePosition = worldPos.Position - transform.WorldPosition;
                if (relativePosition.LengthSquared <= Range)
                {
                    yield return entity;
                }
            }
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(Box2 position)
        {
            foreach (var entity in GetEntities())
            {
                if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
                {
                    if (position.Intersects(component.WorldAABB))
                        yield return entity;
                }
                else
                {
                    var transform = entity.GetComponent<ITransformComponent>();
                    if (position.Contains(transform.WorldPosition))
                    {
                        yield return entity;
                    }
                }
            }
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(Vector2 position)
        {
            foreach (var entity in GetEntities())
            {
                if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
                {
                    if (component.WorldAABB.Contains(position))
                        yield return entity;
                }
                else
                {
                    var transform = entity.GetComponent<ITransformComponent>();
                    if (FloatMath.CloseTo(transform.LocalPosition.X, position.X) && FloatMath.CloseTo(transform.LocalPosition.Y, position.Y))
                    {
                        yield return entity;
                    }
                }
            }
        }

        public bool AnyEntitiesIntersecting(Box2 position)
        {
            foreach (var entity in GetEntities())
            {
                if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
                {
                    if (position.Intersects(component.WorldAABB))
                        return true;
                }
                else
                {
                    var transform = entity.GetComponent<ITransformComponent>();
                    if (position.Contains(transform.WorldPosition))
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public void Initialize()
        {
            if (Initialized)
            {
                throw new InvalidOperationException("InitializeEntities() called multiple times");
            }
            InitializeEntities();
            EntitySystemManager.Initialize();
            Initialized = true;
        }

        public void ApplyEntityStates(IEnumerable<EntityState> entityStates, float serverTime)
        {
            var entityKeys = new HashSet<int>();
            foreach (EntityState es in entityStates)
            {
                //Todo defer component state result processing until all entities are loaded and initialized...
                es.ReceivedTime = serverTime;
                entityKeys.Add(es.StateData.Uid);
                //Known entities
                if (_entities.TryGetValue(es.StateData.Uid, out var entity))
                {
                    entity.HandleEntityState(es);
                }
                else //Unknown entities
                {
                    IEntity newEntity = SpawnEntity(es.StateData.TemplateName, es.StateData.Uid);
                    newEntity.Name = es.StateData.Name;
                    newEntity.HandleEntityState(es);
                }
            }

            //Delete entities that exist here but don't exist in the entity states
            int[] toDelete = _entities.Keys.Where(k => !entityKeys.Contains(k)).ToArray();
            foreach (int k in toDelete)
            {
                DeleteEntity(k);
            }

            // After the first set of states comes in we do the initialization.
            if (!Initialized && MapsInitialized)
            {
                Initialize();
            }
        }
    }
}
