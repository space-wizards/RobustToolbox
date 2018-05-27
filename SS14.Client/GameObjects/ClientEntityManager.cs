using System;
using System.Collections.Generic;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public sealed class ClientEntityManager : EntityManager, IClientEntityManager, IDisposable
    {
        public IEnumerable<IEntity> GetEntitiesInRange(GridLocalCoordinates position, float Range)
        {
            var AABB = new Box2(position.Position - new Vector2(Range / 2, Range / 2), position.Position + new Vector2(Range / 2, Range / 2));
            return GetEntitiesIntersecting(position.MapID, AABB);
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position)
        {
            foreach (var entity in GetEntities())
            {
                var transform = entity.GetComponent<ITransformComponent>();
                if (transform.MapID != mapId)
                    continue;

                if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
                {
                    if (position.Intersects(component.WorldAABB))
                        yield return entity;
                }
                else
                {
                    if (position.Contains(transform.WorldPosition))
                    {
                        yield return entity;
                    }
                }
            }
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position)
        {
            foreach (var entity in GetEntities())
            {
                var transform = entity.GetComponent<ITransformComponent>();
                if (transform.MapID != mapId)
                    continue;

                if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
                {
                    if (component.WorldAABB.Contains(position))
                        yield return entity;
                }
                else
                {
                    if (FloatMath.CloseTo(transform.LocalPosition.X, position.X) && FloatMath.CloseTo(transform.LocalPosition.Y, position.Y))
                    {
                        yield return entity;
                    }
                }
            }
        }

        public bool AnyEntitiesIntersecting(MapId mapId, Box2 box)
        {
            foreach (var entity in GetEntities())
            {
                var transform = entity.GetComponent<ITransformComponent>();
                if (transform.MapID != mapId)
                    continue;

                if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
                {
                    if (box.Intersects(component.WorldAABB))
                        return true;
                }
                else
                {
                    if (box.Contains(transform.WorldPosition))
                    {
                        return true;
                    }
                }
            }

            return false;
        }

        public override void Startup()
        {
            base.Startup();

            if (Started)
            {
                throw new InvalidOperationException("InitializeEntities() called multiple times");
            }

            InitializeEntities();
            EntitySystemManager.Initialize();
            Started = true;
        }

        public void ApplyEntityStates(IEnumerable<EntityState> entityStates, IEnumerable<EntityUid> deletions, float serverTime)
        {
            foreach (EntityState es in entityStates)
            {
                //Todo defer component state result processing until all entities are loaded and initialized...
                es.ReceivedTime = serverTime;
                //Known entities
                if (Entities.TryGetValue(es.StateData.Uid, out var entity))
                {
                    entity.HandleEntityState(es);
                }
                else //Unknown entities
                {
                    Entity newEntity = SpawnEntity(es.StateData.TemplateName, es.StateData.Uid);
                    if (Started)
                    {
                        InitializeEntity(newEntity);
                    }

                    newEntity.Name = es.StateData.Name;
                    newEntity.HandleEntityState(es);
                }
            }

            foreach (var id in deletions)
            {
                DeleteEntity(id);
            }

            // After the first set of states comes in we do the startup.
            if (!Started && MapsInitialized)
            {
                Startup();
            }
        }

        public void Dispose()
        {
            Shutdown();
        }
    }
}
