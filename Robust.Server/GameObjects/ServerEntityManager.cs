using System.Collections.Generic;
using Robust.Server.Interfaces.GameObjects;
using Robust.Server.Interfaces.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameObjects
{
    /// <summary>
    /// Manager for entities -- controls things like template loading and instantiation
    /// </summary>
    public class ServerEntityManager : EntityManager, IServerEntityManagerInternal
    {
        #region IEntityManager Members

#pragma warning disable 649
        [Dependency] private readonly IMapManager _mapManager;
        [Dependency] private readonly IPauseManager _pauseManager;
#pragma warning restore 649

        private readonly List<(GameTick tick, EntityUid uid)> DeletionHistory = new List<(GameTick, EntityUid)>();

        public override IEntity CreateEntityUninitialized(string prototypeName)
        {
            return CreateEntity(prototypeName);
        }

        public override IEntity CreateEntityUninitialized(string prototypeName, GridCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName);
            if(coordinates.GridID != GridId.Nullspace)
            {
                var gridEntityId = _mapManager.GetGrid(coordinates.GridID).GridEntity;
                newEntity.Transform.AttachParent(GetEntity(gridEntityId));
                newEntity.Transform.LocalPosition = coordinates.Position;
            }
            return newEntity;
        }

        public override IEntity CreateEntityUninitialized(string prototypeName, MapCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName);
            newEntity.Transform.AttachParent(_mapManager.GetMapEntity(coordinates.MapId));
            newEntity.Transform.WorldPosition = coordinates.Position;
            return newEntity;
        }

        public override IEntity SpawnEntity(string protoName, GridCoordinates coordinates)
        {
            var entity = SpawnEntityNoMapInit(protoName, coordinates);
            entity.RunMapInit();
            return entity;
        }

        public override IEntity SpawnEntityNoMapInit(string protoName, GridCoordinates coordinates)
        {
            var newEnt = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity((Entity)newEnt);
            return newEnt;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntityAt(string entityType, GridCoordinates coordinates)
        {
            var entity = CreateEntityUninitialized(entityType, coordinates);
            InitializeAndStartEntity((Entity)entity);
            var grid = _mapManager.GetGrid(coordinates.GridID);
            if (_pauseManager.IsMapInitialized(grid.ParentMapId))
            {
                entity.RunMapInit();
            }
            return entity;
        }

        /// <inheritdoc />
        public override IEntity SpawnEntityAt(string entityType, MapCoordinates coordinates)
        {
            var entity = CreateEntityUninitialized(entityType, coordinates);
            InitializeAndStartEntity((Entity)entity);
            return entity;
        }

        /// <inheritdoc />
        public List<EntityState> GetEntityStates(GameTick fromTick)
        {
            var stateEntities = new List<EntityState>();
            foreach (IEntity entity in GetEntities())
            {
                DebugTools.Assert(entity.Initialized && !entity.Deleted);

                if (entity.LastModifiedTick <= fromTick)
                    continue;

                stateEntities.Add(GetEntityState(ComponentManager, entity.Uid, fromTick));
            }

            // no point sending an empty collection
            return stateEntities.Count == 0 ? default : stateEntities;
        }

        public override void DeleteEntity(IEntity e)
        {
            base.DeleteEntity(e);

            DeletionHistory.Add((CurrentTick, e.Uid));
        }

        public List<EntityUid> GetDeletedEntities(GameTick fromTick)
        {
            var list = new List<EntityUid>();
            foreach (var (tick, id) in DeletionHistory)
            {
                if (tick >= fromTick)
                {
                    list.Add(id);
                }
            }

            // no point sending an empty collection
            return list.Count == 0 ? default : list;
        }

        public void CullDeletionHistory(GameTick toTick)
        {
            DeletionHistory.RemoveAll(hist => hist.tick <= toTick);
        }

        #endregion IEntityManager Members

        #region EntityGetters

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position)
        {
            foreach (var entity in GetEntities())
            {
                var transform = entity.Transform;
                if (transform.MapID != mapId)
                    continue;

                if (entity.TryGetComponent<ICollidableComponent>(out var component))
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

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position)
        {
            foreach (var entity in GetEntities())
            {
                var transform = entity.Transform;
                if (transform.MapID != mapId)
                    continue;

                if (entity.TryGetComponent<ICollidableComponent>(out var component))
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

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(GridCoordinates position)
        {
            return GetEntitiesIntersecting(_mapManager.GetGrid(position.GridID).ParentMapId, position.ToWorld(_mapManager).Position);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity)
        {
            if (entity.TryGetComponent<ICollidableComponent>(out var component))
            {
                return GetEntitiesIntersecting(entity.Transform.MapID, component.WorldAABB);
            }

            return GetEntitiesIntersecting(entity.Transform.GridPosition);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(GridCoordinates position, float range)
        {
            var aabb = new Box2(position.Position - new Vector2(range / 2, range / 2), position.Position + new Vector2(range / 2, range / 2));
            return GetEntitiesIntersecting(_mapManager.GetGrid(position.GridID).ParentMapId, aabb);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(MapId mapId, Box2 box, float range)
        {
            var aabb = new Box2(box.Left - range, box.Top - range, box.Right + range, box.Bottom + range);
            return GetEntitiesIntersecting(mapId, aabb);
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range)
        {
            if (entity.TryGetComponent<ICollidableComponent>(out var component))
            {
                return GetEntitiesInRange(entity.Transform.MapID, component.WorldAABB, range);
            }
            else
            {
                GridCoordinates coords = entity.Transform.GridPosition;
                return GetEntitiesInRange(coords, range);
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesInArc(GridCoordinates coordinates, float range, Angle direction, float arcWidth)
        {
            var entities = GetEntitiesInRange(coordinates, range*2);

            foreach (var entity in entities)
            {
                var angle = new Angle(entity.Transform.WorldPosition - coordinates.ToWorld(_mapManager).Position);
                if (angle.Degrees < direction.Degrees + arcWidth / 2 && angle.Degrees > direction.Degrees - arcWidth / 2)
                    yield return entity;
            }
        }

        #endregion EntityGetters

        IEntity IServerEntityManagerInternal.AllocEntity(string prototypeName, EntityUid? uid)
        {
            return AllocEntity(prototypeName, uid);
        }

        void IServerEntityManagerInternal.FinishEntityLoad(IEntity entity, IEntityLoadContext context)
        {
            LoadEntity((Entity) entity, context);
        }

        void IServerEntityManagerInternal.FinishEntityInitialization(IEntity entity)
        {
            InitializeEntity((Entity)entity);
        }

        void IServerEntityManagerInternal.FinishEntityStartup(IEntity entity)
        {
            StartEntity((Entity)entity);
        }

        /// <inheritdoc />
        public override void Startup()
        {
            base.Startup();
            EntitySystemManager.Initialize();
            Started = true;
        }

        /// <summary>
        /// Generates a network entity state for the given entity.
        /// </summary>
        /// <param name="compMan">ComponentManager that contains the components for the entity.</param>
        /// <param name="entityUid">Uid of the entity to generate the state from.</param>
        /// <param name="fromTick">Only provide delta changes from this tick.</param>
        /// <returns>New entity State for the given entity.</returns>
        private static EntityState GetEntityState(IComponentManager compMan, EntityUid entityUid, GameTick fromTick)
        {
            var compStates = new List<ComponentState>();
            var changed = new List<ComponentChanged>();

            foreach (var comp in compMan.GetNetComponents(entityUid))
            {
                DebugTools.Assert(comp.Initialized);

                //Ticks start at 1
                DebugTools.Assert(comp.CreationTick != GameTick.Zero && comp.LastModifiedTick != GameTick.Zero);

                if (comp.NetSyncEnabled && comp.LastModifiedTick >= fromTick)
                    compStates.Add(comp.GetComponentState());

                if (comp.CreationTick >= fromTick && !comp.Deleted)
                {
                    // Can't be null since it's returned by GetNetComponents
                    // ReSharper disable once PossibleInvalidOperationException
                    changed.Add(ComponentChanged.Added(comp.NetID.Value, comp.Name));
                }
                else if (comp.Deleted && comp.LastModifiedTick >= fromTick)
                {
                    // Can't be null since it's returned by GetNetComponents
                    // ReSharper disable once PossibleInvalidOperationException
                    changed.Add(ComponentChanged.Removed(comp.NetID.Value));
                }
            }

            return new EntityState(entityUid, changed, compStates);
        }
    }
}
