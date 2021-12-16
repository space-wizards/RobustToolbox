using System;
using System.Collections.Generic;
using System.Linq;
using Prometheus;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects
{
    public delegate void EntityUidQueryCallback(EntityUid uid);

    /// <inheritdoc />
    public partial class EntityManager : IEntityManager
    {
        #region Dependencies

        [IoC.Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [IoC.Dependency] protected readonly IEntitySystemManager EntitySystemManager = default!;
        [IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [IoC.Dependency] private readonly IGameTiming _gameTiming = default!;
        [IoC.Dependency] private readonly IPauseManager _pauseManager = default!;

        #endregion Dependencies

        /// <inheritdoc />
        public GameTick CurrentTick => _gameTiming.CurTick;

        IComponentFactory IEntityManager.ComponentFactory => ComponentFactory;

        /// <inheritdoc />
        public IEntitySystemManager EntitySysManager => EntitySystemManager;

        /// <inheritdoc />
        public virtual IEntityNetworkManager? EntityNetManager => null;

        protected readonly Queue<EntityUid> QueuedDeletions = new();
        protected readonly HashSet<EntityUid> QueuedDeletionsSet = new();

        /// <summary>
        ///     All entities currently stored in the manager.
        /// </summary>
        protected readonly HashSet<EntityUid> Entities = new();

        private EntityEventBus _eventBus = null!;

        protected virtual int NextEntityUid { get; set; } = (int) EntityUid.FirstUid;

        /// <inheritdoc />
        public IEventBus EventBus => _eventBus;

        public event EventHandler<EntityUid>? EntityAdded;
        public event EventHandler<EntityUid>? EntityInitialized;
        public event EventHandler<EntityUid>? EntityStarted;
        public event EventHandler<EntityUid>? EntityDeleted;

        public bool Started { get; protected set; }
        public bool Initialized { get; protected set; }

        /// <summary>
        /// Constructs a new instance of <see cref="EntityManager"/>.
        /// </summary>
        public EntityManager()
        {
        }

        public virtual void Initialize()
        {
            if (Initialized)
                throw new InvalidOperationException("Initialize() called multiple times");

            _eventBus = new EntityEventBus(this);

            InitializeComponents();

            Initialized = true;
        }

        public virtual void Startup()
        {
            if (Started)
                throw new InvalidOperationException("Startup() called multiple times");

            EntitySystemManager.Initialize();
            Started = true;
        }

        public virtual void Shutdown()
        {
            FlushEntities();
            _eventBus.ClearEventTables();
            EntitySystemManager.Shutdown();
            ClearComponents();
            Initialized = false;
            Started = false;
        }

        public void Cleanup()
        {
            QueuedDeletions.Clear();
            QueuedDeletionsSet.Clear();
            EntitySystemManager.Clear();
            Entities.Clear();
            _eventBus.Dispose();
            _eventBus = null!;
            ClearComponents();

            Initialized = false;
            Started = false;
        }

        public virtual void TickUpdate(float frameTime, Histogram? histogram)
        {
            using (histogram?.WithLabels("EntitySystems").NewTimer())
            {
                EntitySystemManager.TickUpdate(frameTime);
            }

            using (histogram?.WithLabels("EntityEventBus").NewTimer())
            {
                _eventBus.ProcessEventQueue();
            }

            using (histogram?.WithLabels("QueuedDeletion").NewTimer())
            {
                while (QueuedDeletions.TryDequeue(out var uid))
                {
                    DeleteEntity(uid);
                }

                QueuedDeletionsSet.Clear();
            }

            using (histogram?.WithLabels("ComponentCull").NewTimer())
            {
                CullRemovedComponents();
            }
        }

        public virtual void FrameUpdate(float frameTime)
        {
            EntitySystemManager.FrameUpdate(frameTime);
        }

        #region Entity Management

        public EntityUid CreateEntityUninitialized(string? prototypeName, EntityUid euid)
        {
            return CreateEntity(prototypeName, euid);
        }

        /// <inheritdoc />
        public virtual EntityUid CreateEntityUninitialized(string? prototypeName)
        {
            return CreateEntity(prototypeName);
        }

        /// <inheritdoc />
        public virtual EntityUid CreateEntityUninitialized(string? prototypeName, EntityCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName);

            if (coordinates.IsValid(this))
            {
                GetComponent<TransformComponent>(newEntity).Coordinates = coordinates;
            }

            return newEntity;
        }

        /// <inheritdoc />
        public virtual EntityUid CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName);
            var transform = GetComponent<TransformComponent>(newEntity);
            transform.AttachParent(_mapManager.GetMapEntityId(coordinates.MapId));

            // TODO: Look at this bullshit. Please code a way to force-move an entity regardless of anchoring.
            var oldAnchored = transform.Anchored;
            transform.Anchored = false;
            transform.WorldPosition = coordinates.Position;
            transform.Anchored = oldAnchored;
            return newEntity;
        }

        /// <inheritdoc />
        public virtual EntityUid SpawnEntity(string? protoName, EntityCoordinates coordinates)
        {
            if (!coordinates.IsValid(this))
                throw new InvalidOperationException($"Tried to spawn entity {protoName} on invalid coordinates {coordinates}.");

            var entity = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity(entity, coordinates.GetMapId(this));
            return entity;
        }

        /// <inheritdoc />
        public virtual EntityUid SpawnEntity(string? protoName, MapCoordinates coordinates)
        {
            var entity = CreateEntityUninitialized(protoName, coordinates);
            InitializeAndStartEntity(entity, coordinates.MapId);
            return entity;
        }

        /// <inheritdoc />
        public int EntityCount => Entities.Count;

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntities() => Entities;

        /// <summary>
        /// Marks this entity as dirty so that it will be updated over the network.
        /// </summary>
        /// <remarks>
        /// Calling Dirty on a component will call this directly.
        /// </remarks>
        public void DirtyEntity(EntityUid uid)
        {
            var currentTick = CurrentTick;

            // We want to retrieve MetaDataComponent even if its Deleted flag is set.
            if (!_entTraitDict[typeof(MetaDataComponent)].TryGetValue(uid, out var component))
                throw new KeyNotFoundException($"Entity {uid} does not exist, cannot dirty it.");

            var metadata = (MetaDataComponent)component;

            if (metadata.EntityLastModifiedTick == currentTick) return;

            metadata.EntityLastModifiedTick = currentTick;

            var dirtyEvent = new EntityDirtyEvent {Uid = uid};
            EventBus.RaiseLocalEvent(uid, ref dirtyEvent);
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        public virtual void DeleteEntity(EntityUid e)
        {
            // Networking blindly spams entities at this function, they can already be
            // deleted from being a child of a previously deleted entity
            // TODO: Why does networking need to send deletes for child entities?
            if (!_entTraitDict[typeof(MetaDataComponent)].TryGetValue(e, out var comp)
                || comp is not MetaDataComponent meta || meta.EntityDeleted)
                return;

            if (meta.EntityLifeStage == EntityLifeStage.Terminating)
#if !EXCEPTION_TOLERANCE
                throw new InvalidOperationException("Called Delete on an entity already being deleted.");
#else
                return;
#endif

            RecursiveDeleteEntity(e);
        }

        private void RecursiveDeleteEntity(EntityUid uid)
        {
            if(Deleted(uid)) //TODO: Why was this still a child if it was already deleted?
                return;

            var transform = GetComponent<TransformComponent>(uid);
            var metadata = GetComponent<MetaDataComponent>(uid);
            metadata.EntityLifeStage = EntityLifeStage.Terminating;
            EventBus.RaiseLocalEvent(uid, new EntityTerminatingEvent(), false);

            // DeleteEntity modifies our _children collection, we must cache the collection to iterate properly
            foreach (var childTransform in transform.Children.ToArray())
            {
                // Recursion Alert
                RecursiveDeleteEntity(childTransform.Owner);
            }

            // Shut down all components.
            foreach (var component in InSafeOrder(_entCompIndex[uid]))
            {
                if(component.Running)
                    component.LifeShutdown();
            }

            // map does not have a parent node, everything else needs to be detached
            if (transform.ParentUid != EntityUid.Invalid)
            {
                // Detach from my parent, if any
                transform.DetachParentToNull();
            }

            // Dispose all my components, in a safe order so transform is available
            DisposeComponents(uid);

            metadata.EntityLifeStage = EntityLifeStage.Deleted;
            EntityDeleted?.Invoke(this, uid);
            EventBus.RaiseEvent(EventSource.Local, new EntityDeletedMessage(uid));
            Entities.Remove(uid);
        }

        public void QueueDeleteEntity(EntityUid uid)
        {
            if(QueuedDeletionsSet.Add(uid))
                QueuedDeletions.Enqueue(uid);
        }

        public bool EntityExists(EntityUid uid)
        {
            return _entTraitDict[typeof(MetaDataComponent)].ContainsKey(uid);
        }

        public bool EntityExists(EntityUid? uid)
        {
            return uid.HasValue && EntityExists(uid.Value);
        }

        public bool Deleted(EntityUid uid)
        {
            return !_entTraitDict[typeof(MetaDataComponent)].TryGetValue(uid, out var comp) || ((MetaDataComponent) comp).EntityDeleted;
        }

        public bool Deleted(EntityUid? uid)
        {
            return !uid.HasValue || !_entTraitDict[typeof(MetaDataComponent)].TryGetValue(uid.Value, out var comp) || ((MetaDataComponent) comp).EntityDeleted;
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
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected EntityUid AllocEntity(string? prototypeName, EntityUid uid = default)
        {
            EntityPrototype? prototype = null;
            if (!string.IsNullOrWhiteSpace(prototypeName))
            {
                // If the prototype doesn't exist then we throw BEFORE we allocate the entity.
                prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);
            }

            var entity = AllocEntity(uid);

            GetComponent<MetaDataComponent>(entity).EntityPrototype = prototype;

            return entity;
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected EntityUid AllocEntity(EntityUid uid = default)
        {
            if (uid == default)
            {
                uid = GenerateEntityUid();
            }

            if (EntityExists(uid))
            {
                throw new InvalidOperationException($"UID already taken: {uid}");
            }

            // we want this called before adding components
            EntityAdded?.Invoke(this, uid);

            var metadata = new MetaDataComponent { Owner = uid };

            Entities.Add(uid);
            // add the required MetaDataComponent directly.
            AddComponentInternal(uid, metadata);

            // allocate the required TransformComponent
            AddComponent<TransformComponent>(uid);

            return uid;
        }

        /// <summary>
        ///     Allocates an entity and loads components but does not do initialization.
        /// </summary>
        private protected virtual EntityUid CreateEntity(string? prototypeName, EntityUid uid = default)
        {
            if (prototypeName == null)
                return AllocEntity(uid);

            var entity = AllocEntity(prototypeName, uid);
            try
            {
                EntityPrototype.LoadEntity(GetComponent<MetaDataComponent>(entity).EntityPrototype, entity, ComponentFactory, null);
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

        private protected void LoadEntity(EntityUid entity, IEntityLoadContext? context)
        {
            EntityPrototype.LoadEntity(GetComponent<MetaDataComponent>(entity).EntityPrototype, entity, ComponentFactory, context);
        }

        private void InitializeAndStartEntity(EntityUid entity, MapId mapId)
        {
            try
            {
                InitializeEntity(entity);
                StartEntity(entity);

                // If the map we're initializing the entity on is initialized, run map init on it.
                if (_pauseManager.IsMapInitialized(mapId))
                    entity.RunMapInit();
            }
            catch (Exception e)
            {
                DeleteEntity(entity);
                throw new EntityCreationException("Exception inside InitializeAndStartEntity", e);
            }
        }

        protected void InitializeEntity(EntityUid entity)
        {
            InitializeComponents(entity);
            EntityInitialized?.Invoke(this, entity);
        }

        protected void StartEntity(EntityUid entity)
        {
            StartComponents(entity);
            EntityStarted?.Invoke(this, entity);
        }

        /// <inheritdoc />
        public virtual EntityStringRepresentation ToPrettyString(EntityUid uid)
        {
            // We want to retrieve the MetaData component even if it is deleted.
            if (!_entTraitDict[typeof(MetaDataComponent)].TryGetValue(uid, out var component))
                return new EntityStringRepresentation(uid, true);

            var metadata = (MetaDataComponent) component;

            return new EntityStringRepresentation(uid, metadata.EntityDeleted, metadata.EntityName, metadata.EntityPrototype?.ID);
        }

#endregion Entity Management

        protected void DispatchComponentMessage(NetworkComponentMessage netMsg)
        {
            var compMsg = netMsg.Message;
            var compChannel = netMsg.Channel;
            var session = netMsg.Session;
            compMsg.Remote = true;

#pragma warning disable 618
            var uid = netMsg.EntityUid;
            if (compMsg.Directed)
            {
                if (TryGetComponent(uid, (ushort) netMsg.NetId, out var component))
                    component.HandleNetworkMessage(compMsg, compChannel, session);
            }
            else
            {
                foreach (var component in GetComponents(uid))
                {
                    component.HandleNetworkMessage(compMsg, compChannel, session);
                }
            }
#pragma warning restore 618
        }

        /// <summary>
        ///     Factory for generating a new EntityUid for an entity currently being created.
        /// </summary>
        /// <inheritdoc />
        protected virtual EntityUid GenerateEntityUid()
        {
            return new(NextEntityUid++);
        }
    }

    public enum EntityMessageType : byte
    {
        Error = 0,
        ComponentMessage,
        SystemMessage
    }
}
