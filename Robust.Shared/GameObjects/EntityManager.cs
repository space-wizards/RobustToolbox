using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Prometheus;
using Robust.Shared.EntityLookup;
using Robust.Shared.GameObjects.EntitySystemMessages;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    public abstract class EntityManager : IEntityManager
    {
        #region Dependencies

        [Dependency] private readonly IEntityNetworkManager _entityNetworkManager = default!;
        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] protected readonly IEntitySystemManager EntitySystemManager = default!;
        [Dependency] private readonly IComponentFactory _componentFactory = default!;
        [Dependency] private readonly IComponentManager _componentManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        #endregion Dependencies

        /// <inheritdoc />
        public GameTick CurrentTick => _gameTiming.CurTick;

        /// <inheritdoc />
        public IComponentManager ComponentManager => _componentManager;

        /// <inheritdoc />
        public IEntityNetworkManager EntityNetManager => _entityNetworkManager;

        /// <inheritdoc />
        public IEntitySystemManager EntitySysManager => EntitySystemManager;

        /// <summary>
        ///     All entities currently stored in the manager.
        /// </summary>
        protected readonly Dictionary<EntityUid, Entity> Entities = new();

        protected readonly List<Entity> AllEntities = new();

        private readonly EntityEventBus _eventBus = new();

        /// <inheritdoc />
        public IEventBus EventBus => _eventBus;

        public bool Started { get; protected set; }

        public virtual void Initialize()
        {
            _entityNetworkManager.SetupNetworking();
            _entityNetworkManager.ReceivedComponentMessage += (sender, compMsg) => DispatchComponentMessage(compMsg);
            _entityNetworkManager.ReceivedSystemMessage += (sender, systemMsg) => EventBus.RaiseEvent(EventSource.Network, systemMsg);

            ComponentManager.Initialize();
            _componentManager.ComponentRemoved += (sender, args) => _eventBus.UnsubscribeEvents(args.Component);
        }

        public virtual void Startup()
        {
        }

        public virtual void Shutdown()
        {
            FlushEntities();
            EntitySystemManager.Shutdown();
            Started = false;
            _componentManager.Clear();
        }

        public virtual void Update(float frameTime, Histogram? histogram)
        {
            using (histogram?.WithLabels("EntityNet").NewTimer())
            {
                _entityNetworkManager.Update();
            }

            using (histogram?.WithLabels("EntitySystems").NewTimer())
            {
                EntitySystemManager.Update(frameTime);
            }

            using (histogram?.WithLabels("EntityEventBus").NewTimer())
            {
                _eventBus.ProcessEventQueue();
            }

            using (histogram?.WithLabels("EntityCull").NewTimer())
            {
                CullDeletedEntities();
            }
        }

        public virtual void FrameUpdate(float frameTime)
        {
            EntitySystemManager.FrameUpdate(frameTime);
        }

        #region Entity Management

        /// <inheritdoc />
        public abstract IEntity CreateEntityUninitialized(string? prototypeName);

        /// <inheritdoc />
        public abstract IEntity CreateEntityUninitialized(string? prototypeName, EntityCoordinates coordinates);

        /// <inheritdoc />
        public abstract IEntity CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates);

        /// <inheritdoc />
        public abstract IEntity SpawnEntity(string? protoName, EntityCoordinates coordinates);

        /// <inheritdoc />
        public abstract IEntity SpawnEntity(string? protoName, MapCoordinates coordinates);

        /// <inheritdoc />
        public abstract IEntity SpawnEntityNoMapInit(string? protoName, EntityCoordinates coordinates);

        /// <summary>
        /// Returns an entity by id
        /// </summary>
        /// <param name="uid"></param>
        /// <returns>Entity or throws if the entity doesn't exist</returns>
        public IEntity GetEntity(EntityUid uid)
        {
            return Entities[uid];
        }

        /// <summary>
        /// Attempt to get an entity, returning whether or not an entity was gotten.
        /// </summary>
        /// <param name="uid"></param>
        /// <param name="entity">The requested entity or null if the entity couldn't be found.</param>
        /// <returns>True if a value was returned, false otherwise.</returns>
        public bool TryGetEntity(EntityUid uid, [NotNullWhen(true)] out IEntity? entity)
        {
            if (Entities.TryGetValue(uid, out var cEntity) && !cEntity.Deleted)
            {
                entity = cEntity;
                return true;
            }

            // entity might get assigned if it's deleted but still found,
            // prevent somebody from being "smart".
            entity = null;
            return false;
        }

        public IEnumerable<IEntity> GetEntities(IEntityQuery query)
        {
            return query.Match(this);
        }

        public IEnumerable<IEntity> GetEntitiesInMap(MapId mapId)
        {
            foreach (var entity in EntitySystem.Get<SharedEntityLookupSystem>().GetEntitiesInMap(mapId))
            {
                yield return entity;
            }
        }

        /// <inheritdoc />
        public IEnumerable<IEntity> GetEntitiesAt(MapId mapId, Vector2 position, bool approximate = false)
        {
            if (mapId == MapId.Nullspace)
                yield break;

            foreach (var entity in GetEntitiesIntersecting(mapId, position))
            {
                var transform = entity.Transform;
                if (MathHelper.CloseTo(transform.Coordinates.X, position.X) &&
                    MathHelper.CloseTo(transform.Coordinates.Y, position.Y))
                {
                    yield return entity;
                }
            }
        }

        public IEnumerable<IEntity> GetEntities()
        {
            // Need to do an iterator loop to avoid issues with concurrent access.
            // ReSharper disable once ForCanBeConvertedToForeach
            for (var i = 0; i < AllEntities.Count; i++)
            {
                var entity = AllEntities[i];
                if (entity.Deleted)
                {
                    continue;
                }

                yield return entity;
            }
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        public virtual void DeleteEntity(IEntity e)
        {
            EventBus.QueueEvent(EventSource.Local, new EntityDeletedMessage(e));
            e.Shutdown();
        }

        public void DeleteEntity(EntityUid uid)
        {
            if (TryGetEntity(uid, out var entity))
            {
                DeleteEntity(entity);
                // TODO: Broadcast to EntityLookupSystem?
            }
        }

        public bool EntityExists(EntityUid uid)
        {
            return TryGetEntity(uid, out var _);
        }

        /// <summary>
        /// Disposes all entities and clears all lists.
        /// </summary>
        public void FlushEntities()
        {
            foreach (var e in GetEntities())
            {
                e.Shutdown();
            }

            CullDeletedEntities();
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected Entity AllocEntity(string? prototypeName, EntityUid? uid = null)
        {
            EntityPrototype? prototype = null;
            if (!string.IsNullOrWhiteSpace(prototypeName))
            {
                // If the prototype doesn't exist then we throw BEFORE we allocate the entity.
                prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);
            }

            var entity = AllocEntity(uid);

            entity.Prototype = prototype;

            return entity;
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected Entity AllocEntity(EntityUid? uid = null)
        {
            if (uid == null)
            {
                uid = GenerateEntityUid();
            }

            if (EntityExists(uid.Value))
            {
                throw new InvalidOperationException($"UID already taken: {uid}");
            }

            var entity = new Entity(this, uid.Value);

            // allocate the required MetaDataComponent
            _componentManager.AddComponent<MetaDataComponent>(entity);

            // allocate the required TransformComponent
            _componentManager.AddComponent<TransformComponent>(entity);

            Entities[entity.Uid] = entity;
            AllEntities.Add(entity);

            return entity;
        }

        /// <summary>
        ///     Allocates an entity and loads components but does not do initialization.
        /// </summary>
        private protected Entity CreateEntity(string? prototypeName, EntityUid? uid = null)
        {
            if (prototypeName == null)
                return AllocEntity(uid);

            var entity = AllocEntity(prototypeName, uid);
            try
            {
                EntityPrototype.LoadEntity(entity.Prototype, entity, _componentFactory, null);
                return entity;
            }
            catch (Exception e)
            {
                // Exception during entity loading.
                // Need to delete the entity to avoid corrupt state causing crashes later.
                DeleteEntity(entity);
                throw new EntityCreationException($"Exception inside CreateEntity with prototype {prototypeName}", e);
            }
        }

        private protected void LoadEntity(Entity entity, IEntityLoadContext? context)
        {
            EntityPrototype.LoadEntity(entity.Prototype, entity, _componentFactory, context);
        }

        private protected void InitializeAndStartEntity(Entity entity)
        {
            try
            {
                InitializeEntity(entity);
                StartEntity(entity);
            }
            catch (Exception e)
            {
                DeleteEntity(entity);
                throw new EntityCreationException("Exception inside InitializeAndStartEntity", e);
            }
        }

        private protected static void InitializeEntity(Entity entity)
        {
            entity.InitializeComponents();
        }

        private protected static void StartEntity(Entity entity)
        {
            entity.StartAllComponents();
        }

        private void CullDeletedEntities()
        {
            // Culling happens in updates.
            // It doesn't matter because to-be culled entities can't be accessed.
            // This should prevent most cases of "somebody is iterating while we're removing things"
            for (var i = 0; i < AllEntities.Count; i++)
            {
                var entity = AllEntities[i];
                if (!entity.Deleted)
                {
                    continue;
                }

                AllEntities.RemoveSwap(i);
                Entities.Remove(entity.Uid);

                // Process the one we just swapped next.
                i--;
            }
        }

        #endregion Entity Management

        private void DispatchComponentMessage(NetworkComponentMessage netMsg)
        {
            var compMsg = netMsg.Message;
            var compChannel = netMsg.Channel;
            var session = netMsg.Session;
            compMsg.Remote = true;

            var uid = netMsg.EntityUid;
            if (compMsg.Directed)
            {
                if (_componentManager.TryGetComponent(uid, netMsg.NetId, out var component))
                    component.HandleNetworkMessage(compMsg, compChannel, session);
            }
            else
            {
                foreach (var component in _componentManager.GetComponents(uid))
                {
                    component.HandleNetworkMessage(compMsg, compChannel, session);
                }
            }
        }

        protected abstract EntityUid GenerateEntityUid();

        #region Spatial Queries

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public bool AnyEntitiesIntersecting(MapId mapId, Box2 box, bool approximate = false)
        {
            if (mapId == MapId.Nullspace)
                return false;

            foreach (var _ in EntitySystem.Get<SharedEntityLookupSystem>().GetEntitiesIntersecting(mapId, box))
            {
                return true;
            }

            return false;
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Box2 position, bool approximate = false)
        {
            if (mapId == MapId.Nullspace)
                yield break;

            foreach (var entity in EntitySystem.Get<SharedEntityLookupSystem>().GetEntitiesIntersecting(mapId, position, approximate: approximate))
            {
                yield return entity;
            }
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapId mapId, Vector2 position, bool approximate = false)
        {
            if (mapId == MapId.Nullspace)
                yield break;

            foreach (var entity in EntitySystem.Get<SharedEntityLookupSystem>().GetEntitiesIntersecting(mapId, position))
            {
                if (!Intersecting(entity, position)) continue;
                yield return entity;
            }
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public IEnumerable<IEntity> GetEntitiesIntersecting(MapCoordinates position, bool approximate = false)
        {
            return GetEntitiesIntersecting(position.MapId, position.Position, approximate);
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public IEnumerable<IEntity> GetEntitiesIntersecting(EntityCoordinates position, bool approximate = false)
        {
            var mapPos = position.ToMap(this);
            return GetEntitiesIntersecting(mapPos.MapId, mapPos.Position, approximate);
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public IEnumerable<IEntity> GetEntitiesIntersecting(IEntity entity, bool approximate = false)
        {
            if (entity.TryGetComponent<IPhysBody>(out var component))
            {
                return GetEntitiesIntersecting(entity.Transform.MapID, component.GetWorldAABB(), approximate);
            }

            return GetEntitiesIntersecting(entity.Transform.Coordinates, approximate);
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public bool IsIntersecting(IEntity entityOne, IEntity entityTwo)
        {
            var position = entityOne.Transform.MapPosition.Position;
            return Intersecting(entityTwo, position);
        }

        [Obsolete("Use SharedEntityLookupSystem")]
        private static bool Intersecting(IEntity entity, Vector2 mapPosition)
        {
            if (entity.TryGetComponent(out IPhysBody? component))
            {
                if (component.GetWorldAABB().Contains(mapPosition))
                    return true;
            }
            else
            {
                var transform = entity.Transform;
                var entPos = transform.WorldPosition;
                if (MathHelper.CloseTo(entPos.X, mapPosition.X)
                    && MathHelper.CloseTo(entPos.Y, mapPosition.Y))
                {
                    return true;
                }
            }

            return false;
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public IEnumerable<IEntity> GetEntitiesInRange(EntityCoordinates position, float range, bool approximate = false)
        {
            var mapCoordinates = position.ToMap(this);
            var mapPosition = mapCoordinates.Position;
            var aabb = new Box2(mapPosition - new Vector2(range / 2, range / 2),
                mapPosition + new Vector2(range / 2, range / 2));
            return GetEntitiesIntersecting(mapCoordinates.MapId, aabb, approximate);
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public IEnumerable<IEntity> GetEntitiesInRange(MapId mapId, Box2 box, float range, bool approximate = false)
        {
            var aabb = box.Enlarged(range);
            return GetEntitiesIntersecting(mapId, aabb, approximate);
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public IEnumerable<IEntity> GetEntitiesInRange(MapId mapId, Vector2 point, float range, bool approximate = false)
        {
            var aabb = new Box2(point, point).Enlarged(range);
            return GetEntitiesIntersecting(mapId, aabb, approximate);
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public IEnumerable<IEntity> GetEntitiesInRange(IEntity entity, float range, bool approximate = false)
        {
            if (entity.TryGetComponent<IPhysBody>(out var component))
            {
                return GetEntitiesInRange(entity.Transform.MapID, component.GetWorldAABB(), range, approximate);
            }

            return GetEntitiesInRange(entity.Transform.Coordinates, range, approximate);
        }

        /// <inheritdoc />
        [Obsolete("Use SharedEntityLookupSystem")]
        public IEnumerable<IEntity> GetEntitiesInArc(EntityCoordinates coordinates, float range, Angle direction,
            float arcWidth, bool approximate = false)
        {
            var position = coordinates.ToMap(this).Position;

            foreach (var entity in GetEntitiesInRange(coordinates, range * 2, approximate))
            {
                var angle = new Angle(entity.Transform.WorldPosition - position);
                if (angle.Degrees < direction.Degrees + arcWidth / 2 &&
                    angle.Degrees > direction.Degrees - arcWidth / 2)
                    yield return entity;
            }
        }

        #endregion

        [Obsolete]
        public Box2 GetWorldAabbFromEntity(in IEntity ent)
        {
            if (ent.TryGetComponent(out IPhysBody? collider))
                return collider.GetWorldAABB();

            var pos = ent.Transform.WorldPosition;
            return new Box2(pos, pos);
        }

        // TODO: Do we need this?
        public virtual void Update()
        {
        }
    }

    public enum EntityMessageType : byte
    {
        Error = 0,
        ComponentMessage,
        SystemMessage
    }
}
