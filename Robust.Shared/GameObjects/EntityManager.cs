using Prometheus;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.GameObjects
{
    public delegate void EntityUidQueryCallback(EntityUid uid);

    /// <inheritdoc />
    [Virtual]
    public partial class EntityManager : IEntityManager
    {
        #region Dependencies

        [Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [Dependency] private readonly IMapManager _mapManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ISerializationManager _serManager = default!;
        [Dependency] private readonly ProfManager _prof = default!;

        // I feel like PJB might shed me for putting a system dependency here, but its required for setting entity
        // positions on spawn....
        private SharedTransformSystem _xforms = default!;

        #endregion Dependencies

        /// <inheritdoc />
        public GameTick CurrentTick => _gameTiming.CurTick;

        public static readonly MapInitEvent MapInitEventInstance = new();

        IComponentFactory IEntityManager.ComponentFactory => ComponentFactory;

        /// <inheritdoc />
        public IEntitySystemManager EntitySysManager => _entitySystemManager;

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

        public event Action<EntityUid>? EntityAdded;
        public event Action<EntityUid>? EntityInitialized;
        public event Action<EntityUid>? EntityStarted;
        public event Action<EntityUid>? EntityDeleted;
        public event Action<EntityUid>? EntityDirtied; // only raised after initialization

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
            if(!Initialized)
                throw new InvalidOperationException("Startup() called without Initialized");
            if (Started)
                throw new InvalidOperationException("Startup() called multiple times");

            // TODO: Probably better to call this on its own given it's so infrequent.
            _entitySystemManager.Initialize();
            Started = true;
            _eventBus.CalcOrdering();
            _xforms = _entitySystemManager.GetEntitySystem<SharedTransformSystem>();
        }

        public virtual void Shutdown()
        {
            FlushEntities();
            _eventBus.ClearEventTables();
            _entitySystemManager.Shutdown();
            ClearComponents();
            Started = false;
        }

        public virtual void Cleanup()
        {
            _componentFactory.ComponentAdded -= OnComponentAdded;
            _componentFactory.ComponentReferenceAdded -= OnComponentReferenceAdded;
            FlushEntities();
            _entitySystemManager.Clear();
            _eventBus.Dispose();
            _eventBus = null!;
            ClearComponents();

            Initialized = false;
            Started = false;
        }

        public virtual void TickUpdate(float frameTime, bool noPredictions, Histogram? histogram)
        {
            using (histogram?.WithLabels("EntitySystems").NewTimer())
            using (_prof.Group("Systems"))
            {
                _entitySystemManager.TickUpdate(frameTime, noPredictions);
            }

            using (histogram?.WithLabels("EntityEventBus").NewTimer())
            using (_prof.Group("Events"))
            {
                _eventBus.ProcessEventQueue();
            }

            using (histogram?.WithLabels("QueuedDeletion").NewTimer())
            using (_prof.Group("QueueDel"))
            {
                while (QueuedDeletions.TryDequeue(out var uid))
                {
                    DeleteEntity(uid);
                }

                QueuedDeletionsSet.Clear();
            }

            using (histogram?.WithLabels("ComponentCull").NewTimer())
            using (_prof.Group("ComponentCull"))
            {
                CullRemovedComponents();
            }
        }

        public virtual void FrameUpdate(float frameTime)
        {
            _entitySystemManager.FrameUpdate(frameTime);
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
                _xforms.SetCoordinates(GetComponent<TransformComponent>(newEntity), coordinates, unanchor: false);
            }

            return newEntity;
        }

        /// <inheritdoc />
        public virtual EntityUid CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates)
        {
            var newEntity = CreateEntity(prototypeName);
            var transform = GetComponent<TransformComponent>(newEntity);

            var mapEnt = _mapManager.GetMapEntityId(coordinates.MapId);
            TryGetComponent(mapEnt, out TransformComponent? mapXform);

            // If the entity is being spawned in null-space, we will parent the entity to the null-map, IF it exists.
            // For whatever reason, tests create and expect null-space to have a map entity, and it does on the client, but it
            // intentionally doesn't on the server??
            if (coordinates.MapId == MapId.Nullspace &&
                mapXform == null)
            {
                transform._parent = EntityUid.Invalid;
                transform.Anchored = false;
                return newEntity;
            }

            if (mapXform == null)
                throw new ArgumentException($"Attempted to spawn entity on an invalid map. Coordinates: {coordinates}");

            EntityCoordinates coords;
            if (transform.Anchored && _mapManager.TryFindGridAt(coordinates, out var grid))
            {
                coords = new(grid.GridEntityId, grid.WorldToLocal(coordinates.Position));
                _xforms.SetCoordinates(transform, coords, unanchor: false);
            }
            else
            {
                coords = new EntityCoordinates(mapEnt, coordinates.Position);
                _xforms.SetCoordinates(transform, coords, null, newParent: mapXform);
            }

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
        public virtual void DirtyEntity(EntityUid uid, MetaDataComponent? metadata = null)
        {
            // We want to retrieve MetaDataComponent even if its Deleted flag is set.
            if (metadata == null)
            {
                if (!_entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()].TryGetValue(uid, out var component))
                    throw new KeyNotFoundException($"Entity {uid} does not exist, cannot dirty it.");
                metadata = (MetaDataComponent)component;
            }
            else
            {
                DebugTools.Assert(metadata.Owner == uid);
            }

            if (metadata.EntityLastModifiedTick == _gameTiming.CurTick) return;

            metadata.EntityLastModifiedTick = _gameTiming.CurTick;

            if (metadata.EntityLifeStage > EntityLifeStage.Initializing)
            {
                EntityDirtied?.Invoke(uid);
            }
        }

        public virtual void Dirty(Component component, MetaDataComponent? meta = null)
        {
            var owner = component.Owner;

            // Deserialization will cause this to be true.
            // ReSharper disable once ConditionIsAlwaysTrueOrFalse
            if (!owner.IsValid() || component.LifeStage >= ComponentLifeStage.Removing)
                return;

            if (!component.NetSyncEnabled)
                return;

            DirtyEntity(owner, meta);
            component.LastModifiedTick = CurrentTick;
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        /// <param name="e">Entity to remove</param>
        public virtual void DeleteEntity(EntityUid e)
        {
            // Some UIs get disposed after entity-manager has shut down and already deleted all entities.
            if (!Started)
                return;

            var metaQuery = GetEntityQuery<MetaDataComponent>();
            var xformQuery = GetEntityQuery<TransformComponent>();
            var xformSys = EntitySysManager.GetEntitySystem<SharedTransformSystem>();

            // Networking blindly spams entities at this function, they can already be
            // deleted from being a child of a previously deleted entity
            // TODO: Why does networking need to send deletes for child entities?
            if (!metaQuery.TryGetComponent(e, out var comp)
                || comp is not MetaDataComponent meta || meta.EntityDeleted)
                return;

            if (meta.EntityLifeStage == EntityLifeStage.Terminating)
            {
                var msg = $"Called Delete on an entity already being deleted. Entity: {ToPrettyString(meta.Owner)}";
#if !EXCEPTION_TOLERANCE
                throw new InvalidOperationException(msg);
#else
                Logger.Error($"{msg}. Trace: {Environment.StackTrace}");
#endif
            }

            // Notify all entities they are being terminated prior to detaching & deleting
            RecursiveFlagEntityTermination(meta, metaQuery, xformQuery, xformSys);

            // Then actually delete them
            RecursiveDeleteEntity(meta, metaQuery, xformQuery, xformSys);
        }

        private void RecursiveFlagEntityTermination(MetaDataComponent metadata, EntityQuery<MetaDataComponent> metaQuery, EntityQuery<TransformComponent> xformQuery, SharedTransformSystem xformSys)
        {
            var uid = metadata.Owner;
            var transform = xformQuery.GetComponent(uid);
            metadata.EntityLifeStage = EntityLifeStage.Terminating;

            try
            {
                var ev = new EntityTerminatingEvent(uid);
                EventBus.RaiseLocalEvent(uid, ref ev, true);
            }
            catch (Exception e)
            {
                Logger.Error($"Caught exception while raising event {nameof(EntityTerminatingEvent)} on entity {ToPrettyString(uid, metadata)}\n{e}");
            }

            foreach (var child in transform._children)
            {
                if (!metaQuery.TryGetComponent(child, out var childMeta) || childMeta.EntityDeleted)
                {
                    Logger.Error($"A deleted entity was still the transform child of another entity. Parent: {ToPrettyString(uid, metadata)}.");
                    transform._children.Remove(child);
                    continue;
                }

                RecursiveFlagEntityTermination(childMeta, metaQuery, xformQuery, xformSys);
            }
        }

        private void RecursiveDeleteEntity(MetaDataComponent metadata, EntityQuery<MetaDataComponent> metaQuery, EntityQuery<TransformComponent> xformQuery, SharedTransformSystem xformSys)
        {
            // Note about this method: #if EXCEPTION_TOLERANCE is not used here because we're gonna it in the future...

            var uid = metadata.Owner;
            var transform = xformQuery.GetComponent(uid);

            // Detach the base entity to null before iterating over children
            // This also ensures that the entity-lookup updates don't have to be re-run for every child (which recurses up the transform hierarchy).
            if (transform.ParentUid != EntityUid.Invalid)
            {
                try
                {
                    xformSys.DetachParentToNull(transform, xformQuery, metaQuery);
                }
                catch (Exception e)
                {
                    Logger.Error($"Caught exception while trying to detach parent of entity '{ToPrettyString(uid, metadata)}' to null.\n{e}");
                }
            }

            foreach (var child in transform._children)
            {
                try
                {
                    RecursiveDeleteEntity(metaQuery.GetComponent(child), metaQuery, xformQuery, xformSys);
                }
                catch(Exception e)
                {
                    Logger.Error($"Caught exception while trying to recursively delete child entity '{ToPrettyString(child)}' of '{ToPrettyString(uid, metadata)}'\n{e}");
                }
            }

            if (transform._children.Count != 0)
                Logger.Error($"Failed to delete all children of entity: {ToPrettyString(uid)}");

            // Shut down all components.
            foreach (var component in InSafeOrder(_entCompIndex[uid]))
            {
                if (component.Running)
                {
                    try
                    {
                        component.LifeShutdown(this);
                    }
                    catch (Exception e)
                    {
                        Logger.Error($"Caught exception while trying to call shutdown on component of entity '{ToPrettyString(uid, metadata)}'\n{e}");
                    }
                }
            }

            // Dispose all my components, in a safe order so transform is available
            DisposeComponents(uid);
            metadata.EntityLifeStage = EntityLifeStage.Deleted;

            try
            {
                EntityDeleted?.Invoke(uid);
            }
            catch (Exception e)
            {
                Logger.Error($"Caught exception while invoking event {nameof(EntityDeleted)} on '{ToPrettyString(uid, metadata)}'\n{e}");
            }

            _eventBus.OnEntityDeleted(uid);

            // Another try-catch, so quickly after the other one?!
            // Yes. Both of these are try-catch blocks for *events*, which take our precious execution flow away from
            // us and into whatever spooky code subscribed to this. We don't want an exception in user code suddenly
            // fucking up entity deletion and leaving us with a frankesteintity, now do we?
            try
            {
                EventBus.RaiseEvent(EventSource.Local, new EntityDeletedMessage(uid));
            }
            catch (Exception e)
            {
                Logger.Error($"Caught exception while raising {nameof(EntityDeletedMessage)} on '{ToPrettyString(uid, metadata)}'\n{e}");
            }

            Entities.Remove(uid);
        }

        public void QueueDeleteEntity(EntityUid uid)
        {
            if(QueuedDeletionsSet.Add(uid))
                QueuedDeletions.Enqueue(uid);
        }

        public bool IsQueuedForDeletion(EntityUid uid) => QueuedDeletionsSet.Contains(uid);

        public bool EntityExists(EntityUid uid)
        {
            return _entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()].ContainsKey(uid);
        }

        public bool EntityExists(EntityUid? uid)
        {
            return uid.HasValue && EntityExists(uid.Value);
        }

        public bool Deleted(EntityUid uid)
        {
            return !_entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()].TryGetValue(uid, out var comp) || ((MetaDataComponent) comp).EntityDeleted;
        }

        public bool Deleted([NotNullWhen(false)] EntityUid? uid)
        {
            return !uid.HasValue || !_entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()].TryGetValue(uid.Value, out var comp) || ((MetaDataComponent) comp).EntityDeleted;
        }

        /// <summary>
        /// Disposes all entities and clears all lists.
        /// </summary>
        public void FlushEntities()
        {
            QueuedDeletions.Clear();
            QueuedDeletionsSet.Clear();
            foreach (var e in GetEntities())
            {
                DeleteEntity(e);
            }
            DebugTools.Assert(Entities.Count == 0);
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected EntityUid AllocEntity(
            string? prototypeName,
            out MetaDataComponent metadata,
            EntityUid uid = default)
        {
            EntityPrototype? prototype = null;
            if (!string.IsNullOrWhiteSpace(prototypeName))
            {
                // If the prototype doesn't exist then we throw BEFORE we allocate the entity.
                prototype = PrototypeManager.Index<EntityPrototype>(prototypeName);
            }

            var entity = AllocEntity(out metadata, uid);

            metadata._entityPrototype = prototype;
            Dirty(metadata, metadata);

            return entity;
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected EntityUid AllocEntity(out MetaDataComponent metadata, EntityUid uid = default)
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
            EntityAdded?.Invoke(uid);
            _eventBus.OnEntityAdded(uid);

            metadata = new MetaDataComponent { Owner = uid };

            Entities.Add(uid);
            // add the required MetaDataComponent directly.
            AddComponentInternal(uid, metadata, false, false);

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
                return AllocEntity(out _, uid);

            var entity = AllocEntity(prototypeName, out var metadata, uid);
            try
            {
                EntityPrototype.LoadEntity(metadata.EntityPrototype, entity, ComponentFactory, this, _serManager, null);
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
            EntityPrototype.LoadEntity(GetComponent<MetaDataComponent>(entity).EntityPrototype, entity, ComponentFactory, this, _serManager, context);
        }

        private protected void LoadEntity(EntityUid entity, IEntityLoadContext? context, EntityPrototype? prototype)
        {
            EntityPrototype.LoadEntity(prototype, entity, ComponentFactory, this, _serManager, context);
        }

        public void InitializeAndStartEntity(EntityUid entity, MapId? mapId = null)
        {
            try
            {
                var meta = GetComponent<MetaDataComponent>(entity);
                InitializeEntity(entity, meta);
                StartEntity(entity);

                // If the map we're initializing the entity on is initialized, run map init on it.
                if (_mapManager.IsMapInitialized(mapId ?? GetComponent<TransformComponent>(entity).MapID))
                    RunMapInit(entity, meta);
            }
            catch (Exception e)
            {
                DeleteEntity(entity);
                throw new EntityCreationException("Exception inside InitializeAndStartEntity", e);
            }
        }

        protected void InitializeEntity(EntityUid entity, MetaDataComponent? meta = null)
        {
            InitializeComponents(entity, meta);
            EntityInitialized?.Invoke(entity);
        }

        protected void StartEntity(EntityUid entity)
        {
            StartComponents(entity);
            EntityStarted?.Invoke(entity);
        }

        public void RunMapInit(EntityUid entity, MetaDataComponent meta)
        {
            if (meta.EntityLifeStage == EntityLifeStage.MapInitialized)
                return; // Already map initialized, do nothing.

            DebugTools.Assert(meta.EntityLifeStage == EntityLifeStage.Initialized, $"Expected entity {ToPrettyString(entity)} to be initialized, was {meta.EntityLifeStage}");
            meta.EntityLifeStage = EntityLifeStage.MapInitialized;

            EventBus.RaiseLocalEvent(entity, MapInitEventInstance, false);
        }

        /// <inheritdoc />
        public virtual EntityStringRepresentation ToPrettyString(EntityUid uid)
        {
            // We want to retrieve the MetaData component even if it is deleted.
            if (!_entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()].TryGetValue(uid, out var component))
                return new EntityStringRepresentation(uid, true);

            var metadata = (MetaDataComponent) component;

            return ToPrettyString(uid, metadata);
        }

        private EntityStringRepresentation ToPrettyString(EntityUid uid, MetaDataComponent metadata)
        {
            return new EntityStringRepresentation(uid, metadata.EntityDeleted, metadata.EntityName, metadata.EntityPrototype?.ID);
        }

        #endregion Entity Management

        public virtual void RaisePredictiveEvent<T>(T msg) where T : EntityEventArgs
        {
            // Part of shared the EntityManager so that systems can have convenient proxy methods, but the
            // server should never be calling this.
            DebugTools.Assert("Why are you raising predictive events on the server?");
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
        SystemMessage
    }
}
