using System;
using System.Collections.Generic;
using SS14.Client.Interfaces.GameObjects;
using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.GameObjects.Components;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;
using SS14.Shared.Maths;

namespace SS14.Client.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public sealed class ClientEntityManager : EntityManager, IClientEntityManager, IDisposable
    {
        [Dependency]
        readonly IMapManager _mapManager;

        private int NextClientEntityUid = EntityUid.ClientUid + 1;

        public IEnumerable<IEntity> GetEntitiesInRange(GridCoordinates position, float Range)
        {
            var AABB = new Box2(position.Position - new Vector2(Range / 2, Range / 2), position.Position + new Vector2(Range / 2, Range / 2));
            return GetEntitiesIntersecting(position.MapID, AABB);
        }

        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position)
        {
            foreach (var entity in GetEntities())
            {
                var transform = entity.Transform;
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
                var transform = entity.Transform;
                if (transform.MapID != mapId)
                    continue;

                if (entity.TryGetComponent<BoundingBoxComponent>(out var component))
                {
                    if (component.WorldAABB.Contains(position))
                        yield return entity;
                }
                else
                {
                    if (FloatMath.CloseTo(transform.GridPosition.X, position.X) && FloatMath.CloseTo(transform.GridPosition.Y, position.Y))
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
                var transform = entity.Transform;
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
                throw new InvalidOperationException("Startup() called multiple times");
            }

            EntitySystemManager.Initialize();
            Started = true;
        }

        public void ApplyEntityStates(IEnumerable<EntityState> curEntStates, IEnumerable<EntityUid> deletions, List<EntityState> nextEntStates)
        {
            var toApply = new List<(Entity, EntityState, EntityState)>();
            var toInitialize = new List<Entity>();
            foreach (var es in curEntStates)
            {
                var nextState = nextEntStates?.Find(s => s.StateData.Uid == es.StateData.Uid);
                //Todo defer component state result processing until all entities are loaded and initialized...
                //Known entities
                if (Entities.TryGetValue(es.StateData.Uid, out var entity))
                {
                    toApply.Add(((Entity)entity, es, nextState));
                }
                else //Unknown entities
                {
                    var newEntity = CreateEntity(es.StateData.TemplateName, es.StateData.Uid);
                    newEntity.Name = es.StateData.Name;
                    toApply.Add((newEntity, es, nextState));
                    toInitialize.Add(newEntity);
                }
            }

            // Make sure this is done after all entities have been instantiated.
            foreach (var (entity, es, nextState) in toApply)
            {
                entity.HandleEntityState(es, nextState);
            }

            foreach (var id in deletions)
            {
                DeleteEntity(id);
            }

            foreach (var entity in toInitialize)
            {
                InitializeEntity(entity);
            }

            foreach (var entity in toInitialize)
            {
                StartEntity(entity);
            }
        }

        public void Dispose()
        {
            Shutdown();
        }

        public override IEntity SpawnEntity(string protoName)
        {
            var ent = CreateEntity(protoName, NewClientEntityUid());
            InitializeAndStartEntity(ent);
            return ent;
        }

        public override IEntity ForceSpawnEntityAt(string entityType, GridCoordinates coordinates)
        {
            var entity = CreateEntity(entityType, NewClientEntityUid());
            entity.Transform.GridPosition = coordinates;
            InitializeAndStartEntity(entity);

            return entity;
        }

        public override IEntity ForceSpawnEntityAt(string entityType, Vector2 position, MapId argMap)
        {
            if (!_mapManager.TryGetMap(argMap, out var map))
            {
                map = _mapManager.DefaultMap;
            }

            return ForceSpawnEntityAt(entityType, new GridCoordinates(position, map.FindGridAt(position)));

        }

        public override bool TrySpawnEntityAt(string entityType, Vector2 position, MapId argMap, out IEntity entity)
        {
            // TODO: check collisions here?
            entity = ForceSpawnEntityAt(entityType, position, argMap);
            return true;
        }

        public override bool TrySpawnEntityAt(string entityType, GridCoordinates coordinates, out IEntity entity)
        {
            // TODO: check collisions here?
            entity = ForceSpawnEntityAt(entityType, coordinates);
            return true;
        }

        EntityUid NewClientEntityUid()
        {
            return new EntityUid(NextClientEntityUid++);
        }

    }
}
