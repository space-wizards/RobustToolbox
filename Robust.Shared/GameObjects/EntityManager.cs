using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Prometheus;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public delegate void EntityQueryCallback(IEntity entity);

    /// <inheritdoc />
    public abstract class EntityManager : IEntityManager
    {
        #region Dependencies

        [IoC.Dependency] private readonly IEntityNetworkManager EntityNetworkManager = default!;
        [IoC.Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [IoC.Dependency] protected readonly IEntitySystemManager EntitySystemManager = default!;
        [IoC.Dependency] private readonly IComponentFactory ComponentFactory = default!;
        [IoC.Dependency] private readonly IComponentManager _componentManager = default!;
        [IoC.Dependency] private readonly IGameTiming _gameTiming = default!;

        private SharedEntityLookupSystem _lookupSystem = default!;

        #endregion Dependencies

        /// <inheritdoc />
        public GameTick CurrentTick => _gameTiming.CurTick;

        /// <inheritdoc />
        public IComponentManager ComponentManager => _componentManager;

        /// <inheritdoc />
        public IEntityNetworkManager EntityNetManager => EntityNetworkManager;

        /// <inheritdoc />
        public IEntitySystemManager EntitySysManager => EntitySystemManager;

        /// <summary>
        ///     All entities currently stored in the manager.
        /// </summary>
        protected readonly Dictionary<EntityUid, Entity> Entities = new();

        protected readonly List<Entity> AllEntities = new();

        private EntityEventBus _eventBus = null!;

        /// <inheritdoc />
        public IEventBus EventBus => _eventBus;

        public event EventHandler<EntityUid>? EntityAdded;
        public event EventHandler<EntityUid>? EntityInitialized;
        public event EventHandler<EntityUid>? EntityDeleted;

        public bool Started { get; protected set; }

        /// <summary>
        /// Constructs a new instance of <see cref="EntityManager"/>.
        /// </summary>
        public EntityManager()
        {
        }

        public virtual void Initialize()
        {
            _eventBus = new EntityEventBus(this);

            EntityNetworkManager.SetupNetworking();
            EntityNetworkManager.ReceivedComponentMessage += (sender, compMsg) => DispatchComponentMessage(compMsg);
            EntityNetworkManager.ReceivedSystemMessage += (sender, systemMsg) => EventBus.RaiseEvent(EventSource.Network, systemMsg);

            ComponentManager.Initialize();
            _componentManager.ComponentRemoved += (sender, args) => _eventBus.UnsubscribeEvents(args.Component);
        }

        public virtual void Startup()
        {
            _lookupSystem = EntitySystemManager.GetEntitySystem<SharedEntityLookupSystem>();
        }

        public virtual void Shutdown()
        {
            FlushEntities();
            _eventBus.ClearEventTables();
            EntitySystemManager.Shutdown();
            Started = false;
            _componentManager.Clear();
        }

        public virtual void TickUpdate(float frameTime, Histogram? histogram)
        {
            using (histogram?.WithLabels("EntityNet").NewTimer())
            {
                EntityNetworkManager.TickUpdate();
            }

            using (histogram?.WithLabels("EntitySystems").NewTimer())
            {
                EntitySystemManager.TickUpdate(frameTime);
            }

            using (histogram?.WithLabels("EntityEventBus").NewTimer())
            {
                _eventBus.ProcessEventQueue();
            }

            using (histogram?.WithLabels("ComponentCull").NewTimer())
            {
                _componentManager.CullRemovedComponents();
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
            // Networking blindly spams entities at this function, they can already be
            // deleted from being a child of a previously deleted entity
            // TODO: Why does networking need to send deletes for child entities?
            if (e.Deleted)
                return;

            if (e.LifeStage >= EntityLifeStage.Terminating)
#if !EXCEPTION_TOLERANCE
                throw new InvalidOperationException("Called Delete on an entity already being deleted.");
#else
                return;
#endif

            RecursiveDeleteEntity(e);
        }

        private void RecursiveDeleteEntity(IEntity entity)
        {
            if(entity.Deleted) //TODO: Why was this still a child if it was already deleted?
                return;

            var transform = entity.Transform;
            entity.LifeStage = EntityLifeStage.Terminating;

            EventBus.RaiseLocalEvent(entity.Uid, new EntityTerminatingEvent(), false);

            // DeleteEntity modifies our _children collection, we must cache the collection to iterate properly
            foreach (var childTransform in transform.Children.ToArray())
            {
                // Recursion Alert
                RecursiveDeleteEntity(childTransform.Owner);
            }

            // Dispose all my components, in a safe order so transform is available
            ComponentManager.DisposeComponents(entity.Uid);

            // map does not have a parent node, everything else needs to be detached
            if (transform.ParentUid != EntityUid.Invalid)
            {
                // Detach from my parent, if any
                transform.DetachParentToNull();
            }

            entity.LifeStage = EntityLifeStage.Deleted;
            EntityDeleted?.Invoke(this, entity.Uid);
            EventBus.RaiseEvent(EventSource.Local, new EntityDeletedMessage(entity));
        }

        public void DeleteEntity(EntityUid uid)
        {
            if (TryGetEntity(uid, out var entity))
            {
                DeleteEntity(entity);
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
                DeleteEntity(e);
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

            // we want this called before adding components
            EntityAdded?.Invoke(this, entity.Uid);

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
                EntityPrototype.LoadEntity(entity.Prototype, entity, ComponentFactory, null);
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
            EntityPrototype.LoadEntity(entity.Prototype, entity, ComponentFactory, context);
        }

        private protected void InitializeAndStartEntity(Entity entity)
        {
            try
            {
                InitializeEntity(entity);
                EntityInitialized?.Invoke(this, entity.Uid);
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
            EntitySystem.Get<SharedEntityLookupSystem>().UpdateEntityTree(entity);
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
                _lookupSystem.RemoveFromEntityTrees(entity);

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

        /// <summary>
        /// Factory for generating a new EntityUid for an entity currently being created.
        /// </summary>
        protected abstract EntityUid GenerateEntityUid();
    }

    public enum EntityMessageType : byte
    {
        Error = 0,
        ComponentMessage,
        SystemMessage
    }
}
