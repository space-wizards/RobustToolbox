using Prometheus;
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
using System.Linq;
using System.Runtime.CompilerServices;
using Robust.Shared.Physics;
using Robust.Shared.Serialization.Markdown.Mapping;

namespace Robust.Shared.GameObjects
{
    public delegate void EntityUidQueryCallback(EntityUid uid);

    public delegate void ComponentQueryCallback<T>(EntityUid uid, T component) where T : Component;

    /// <inheritdoc />
    [Virtual]
    public partial class EntityManager : IEntityManager
    {
        #region Dependencies

        [IoC.Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [IoC.Dependency] protected readonly ILogManager LogManager = default!;
        [IoC.Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [IoC.Dependency] private readonly IGameTiming _gameTiming = default!;
        [IoC.Dependency] private readonly ISerializationManager _serManager = default!;
        [IoC.Dependency] private readonly ProfManager _prof = default!;

        // I feel like PJB might shed me for putting a system dependency here, but its required for setting entity
        // positions on spawn....
        private SharedTransformSystem _xforms = default!;

        private EntityQuery<MetaDataComponent> _metaQuery;
        private EntityQuery<TransformComponent> _xformQuery;

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

        private EntityDiffContext _context = new();

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
        public event Action<EntityUid>? EntityDeleted;

        /// <summary>
        /// Raised when an entity is queued for deletion. Not raised if an entity is deleted.
        /// </summary>
        public event Action<EntityUid>? EntityQueueDeleted;
        public event Action<EntityUid>? EntityDirtied; // only raised after initialization

        private string _xformName = string.Empty;

        private SharedMapSystem _mapSystem = default!;

        private ISawmill _sawmill = default!;
        private ISawmill _resolveSawmill = default!;

        public bool Started { get; protected set; }

        public bool ShuttingDown { get; protected set; }

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
            _xformName = _componentFactory.GetComponentName(typeof(TransformComponent));
            _sawmill = LogManager.GetSawmill("entity");
            _resolveSawmill = LogManager.GetSawmill("resolve");

            Initialized = true;
        }

        /// <summary>
        /// Returns true if the entity's data (apart from transform) is default.
        /// </summary>
        public bool IsDefault(EntityUid uid)
        {
            if (!_metaQuery.TryGetComponent(uid, out var metadata) || metadata.EntityPrototype == null)
                return false;

            var prototype = metadata.EntityPrototype;

            // Prototype may or may not have metadata / transforms
            var protoComps = prototype.Components.Keys.ToList();

            protoComps.Remove(_xformName);

            // Fast check if the component counts match.
            if (protoComps.Count != ComponentCount(uid) - 2)
                return false;

            // Check if entity name / description match
            if (metadata.EntityName != prototype.Name ||
                metadata.EntityDescription != prototype.Description)
            {
                return false;
            }

            // Get default prototype data
            Dictionary<string, MappingDataNode> protoData = new();
            try
            {
                _context.WritingReadingPrototypes = true;

                foreach (var compType in protoComps)
                {
                    if (compType == _xformName)
                        continue;

                    var comp = prototype.Components[compType];
                    protoData.Add(compType, _serManager.WriteValueAs<MappingDataNode>(comp.Component.GetType(), comp.Component, alwaysWrite: true, context: _context));
                }

                _context.WritingReadingPrototypes = false;
            }
            catch (Exception e)
            {
                _sawmill.Error($"Failed to convert prototype {prototype.ID} into yaml. Exception: {e.Message}");
                return false;
            }

            var comps = new HashSet<IComponent>(GetComponents(uid));
            var compNames = new HashSet<string>(protoComps.Count);
            foreach (var component in comps)
            {
                var compType = component.GetType();
                var compName = _componentFactory.GetComponentName(compType);

                if (compType == typeof(MetaDataComponent) || compType == typeof(TransformComponent))
                    continue;

                compNames.Add(compName);

                // If the component isn't on the prototype then it's custom.
                if (!protoComps.Contains(compName))
                    return false;

                MappingDataNode compMapping;
                try
                {
                    compMapping = _serManager.WriteValueAs<MappingDataNode>(compType, component, alwaysWrite: true, context: _context);
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Failed to serialize {compName} component of entity prototype {prototype.ID}. Exception: {e.Message}");
                    return false;
                }

                if (protoData.TryGetValue(compName, out var protoMapping))
                {
                    var diff = compMapping.Except(protoMapping);

                    if (diff != null && diff.Children.Count != 0)
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }

            // An entity may also remove components on init -> check no components are missing.
            foreach (var compType in protoComps)
            {
                if (!compNames.Contains(compType))
                    return false;
            }

            return true;
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
            _mapSystem = System<SharedMapSystem>();
            _xforms = System<SharedTransformSystem>();
            _metaQuery = GetEntityQuery<MetaDataComponent>();
            _xformQuery = GetEntityQuery<TransformComponent>();
        }

        public virtual void Shutdown()
        {
            ShuttingDown = true;
            FlushEntities();
            _eventBus.ClearEventTables();
            _entitySystemManager.Shutdown();
            ClearComponents();
            ShuttingDown = false;
            Started = false;
        }

        public virtual void Cleanup()
        {
            _componentFactory.ComponentAdded -= OnComponentAdded;
            _componentFactory.ComponentReferenceAdded -= OnComponentReferenceAdded;
            ShuttingDown = true;
            FlushEntities();
            _entitySystemManager.Clear();
            _eventBus.Dispose();
            _eventBus = null!;
            ClearComponents();

            ShuttingDown = false;
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

        public EntityUid CreateEntityUninitialized(string? prototypeName, EntityUid euid, ComponentRegistry? overrides = null)
        {
            return CreateEntity(prototypeName, euid, overrides);
        }

        /// <inheritdoc />
        public virtual EntityUid CreateEntityUninitialized(string? prototypeName, ComponentRegistry? overrides = null)
        {
            return CreateEntity(prototypeName, default, overrides);
        }

        /// <inheritdoc />
        public virtual EntityUid CreateEntityUninitialized(string? prototypeName, EntityCoordinates coordinates, ComponentRegistry? overrides = null)
        {
            var newEntity = CreateEntity(prototypeName, default, overrides);
            _xforms.SetCoordinates(newEntity, _xformQuery.GetComponent(newEntity), coordinates, unanchor: false);
            return newEntity;
        }

        /// <inheritdoc />
        public virtual EntityUid CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates, ComponentRegistry? overrides = null)
        {
            var newEntity = CreateEntity(prototypeName, default, overrides);
            var transform = _xformQuery.GetComponent(newEntity);

            if (coordinates.MapId == MapId.Nullspace)
            {
                DebugTools.Assert(_mapManager.GetMapEntityId(coordinates.MapId) == EntityUid.Invalid);
                transform._parent = EntityUid.Invalid;
                transform.Anchored = false;
                return newEntity;
            }

            var mapEnt = _mapManager.GetMapEntityId(coordinates.MapId);
            if (!TryGetComponent(mapEnt, out TransformComponent? mapXform))
                throw new ArgumentException($"Attempted to spawn entity on an invalid map. Coordinates: {coordinates}");

            EntityCoordinates coords;
            if (transform.Anchored && _mapManager.TryFindGridAt(coordinates, out var gridUid, out var grid))
            {
                coords = new EntityCoordinates(gridUid, _mapSystem.WorldToLocal(gridUid, grid, coordinates.Position));
                _xforms.SetCoordinates(newEntity, transform, coords, unanchor: false);
            }
            else
            {
                coords = new EntityCoordinates(mapEnt, coordinates.Position);
                _xforms.SetCoordinates(newEntity, transform, coords, null, newParent: mapXform);
            }

            return newEntity;
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
#pragma warning disable CS0618
                DebugTools.Assert(metadata.Owner == uid);
#pragma warning restore CS0618
            }

            if (metadata.EntityLastModifiedTick == _gameTiming.CurTick) return;

            metadata.EntityLastModifiedTick = _gameTiming.CurTick;

            if (metadata.EntityLifeStage > EntityLifeStage.Initializing)
            {
                EntityDirtied?.Invoke(uid);
            }
        }

        [Obsolete("use override with an EntityUid")]
        public void Dirty(Component component, MetaDataComponent? meta = null)
        {
            Dirty(component.Owner, component, meta);
        }

        public virtual void Dirty(EntityUid uid, Component component, MetaDataComponent? meta = null)
        {
            if (component.LifeStage >= ComponentLifeStage.Removing || !component.NetSyncEnabled)
                return;

            DirtyEntity(uid, meta);
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
            if (!metaQuery.TryGetComponent(e, out var meta) || meta.EntityDeleted)
                return;

            if (meta.EntityLifeStage == EntityLifeStage.Terminating)
            {
                var msg = $"Called Delete on an entity already being deleted. Entity: {ToPrettyString(e)}";
#if !EXCEPTION_TOLERANCE
                throw new InvalidOperationException(msg);
#else
                _sawmill.Error($"{msg}. Trace: {Environment.StackTrace}");
#endif
            }

            // Notify all entities they are being terminated prior to detaching & deleting
            RecursiveFlagEntityTermination(e, meta, metaQuery, xformQuery);

            // Then actually delete them
            RecursiveDeleteEntity(e, meta, metaQuery, xformQuery, xformSys);
        }

        private void RecursiveFlagEntityTermination(
            EntityUid uid,
            MetaDataComponent metadata,
            EntityQuery<MetaDataComponent> metaQuery,
            EntityQuery<TransformComponent> xformQuery)
        {
            var transform = xformQuery.GetComponent(uid);
            metadata.EntityLifeStage = EntityLifeStage.Terminating;

            try
            {
                var ev = new EntityTerminatingEvent(uid);
                EventBus.RaiseLocalEvent(uid, ref ev, true);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception while raising event {nameof(EntityTerminatingEvent)} on entity {ToPrettyString(uid, metadata)}\n{e}");
            }

            foreach (var child in transform._children)
            {
                if (!metaQuery.TryGetComponent(child, out var childMeta) || childMeta.EntityDeleted)
                {
                    _sawmill.Error($"A deleted entity was still the transform child of another entity. Parent: {ToPrettyString(uid, metadata)}.");
                    transform._children.Remove(child);
                    continue;
                }

                RecursiveFlagEntityTermination(child, childMeta, metaQuery, xformQuery);
            }
        }

        private void RecursiveDeleteEntity(
            EntityUid uid,
            MetaDataComponent metadata,
            EntityQuery<MetaDataComponent> metaQuery,
            EntityQuery<TransformComponent> xformQuery,
            SharedTransformSystem xformSys)
        {
            // Note about this method: #if EXCEPTION_TOLERANCE is not used here because we're gonna it in the future...

            var transform = xformQuery.GetComponent(uid);

            // Detach the base entity to null before iterating over children
            // This also ensures that the entity-lookup updates don't have to be re-run for every child (which recurses up the transform hierarchy).
            if (transform.ParentUid != EntityUid.Invalid)
            {
                try
                {
                    xformSys.DetachParentToNull(uid, transform);
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Caught exception while trying to detach parent of entity '{ToPrettyString(uid, metadata)}' to null.\n{e}");
                }
            }

            foreach (var child in transform._children)
            {
                try
                {
                    RecursiveDeleteEntity(child, metaQuery.GetComponent(child), metaQuery, xformQuery, xformSys);
                }
                catch(Exception e)
                {
                    _sawmill.Error($"Caught exception while trying to recursively delete child entity '{ToPrettyString(child)}' of '{ToPrettyString(uid, metadata)}'\n{e}");
                }
            }

            if (transform._children.Count != 0)
                _sawmill.Error($"Failed to delete all children of entity: {ToPrettyString(uid)}");

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
                        _sawmill.Error($"Caught exception while trying to call shutdown on component of entity '{ToPrettyString(uid, metadata)}'\n{e}");
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
                _sawmill.Error($"Caught exception while invoking event {nameof(EntityDeleted)} on '{ToPrettyString(uid, metadata)}'\n{e}");
            }

            _eventBus.OnEntityDeleted(uid);
            Entities.Remove(uid);
        }

        public virtual void QueueDeleteEntity(EntityUid uid)
        {
            if (!QueuedDeletionsSet.Add(uid))
                return;

            QueuedDeletions.Enqueue(uid);
            EntityQueueDeleted?.Invoke(uid);
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

        /// <inheritdoc />
        public bool IsPaused(EntityUid? uid, MetaDataComponent? metadata = null)
        {
            if (uid == null)
                return false;

            return _metaQuery.Resolve(uid.Value, ref metadata) && metadata.EntityPaused;
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
        public virtual void FlushEntities()
        {
            QueuedDeletions.Clear();
            QueuedDeletionsSet.Clear();
            foreach (var e in GetEntities().ToArray())
            {
                DeleteEntity(e);
            }

            if (Entities.Count != 0)
                _sawmill.Error("Entities were spawned while flushing entities.");
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected EntityUid AllocEntity(
            EntityPrototype? prototype,
            out MetaDataComponent metadata,
            EntityUid uid = default)
        {
            var entity = AllocEntity(out metadata, uid);
            metadata._entityPrototype = prototype;
            Dirty(entity, metadata, metadata);
            return entity;
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private EntityUid AllocEntity(out MetaDataComponent metadata, EntityUid uid = default)
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

#pragma warning disable CS0618
            metadata = new MetaDataComponent { Owner = uid };
#pragma warning restore CS0618

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
        private protected virtual EntityUid CreateEntity(string? prototypeName, EntityUid uid = default, IEntityLoadContext? context = null)
        {
            if (prototypeName == null)
                return AllocEntity(out _, uid);

            if (!PrototypeManager.TryIndex<EntityPrototype>(prototypeName, out var prototype))
                throw new EntityCreationException($"Attempted to spawn an entity with an invalid prototype: {prototypeName}");

            return CreateEntity(prototype, uid, context);
        }

        /// <summary>
        ///     Allocates an entity and loads components but does not do initialization.
        /// </summary>
        private protected EntityUid CreateEntity(EntityPrototype prototype, EntityUid uid = default, IEntityLoadContext? context = null)
        {
            var entity = AllocEntity(prototype, out var metadata, uid);
            try
            {
                EntityPrototype.LoadEntity(metadata.EntityPrototype, entity, ComponentFactory, this, _serManager, context);
                return entity;
            }
            catch (Exception e)
            {
                // Exception during entity loading.
                // Need to delete the entity to avoid corrupt state causing crashes later.
                DeleteEntity(entity);
                throw new EntityCreationException($"Exception inside CreateEntity with prototype {prototype.ID}", e);
            }
        }

        private protected void LoadEntity(EntityUid entity, IEntityLoadContext? context)
        {
            EntityPrototype.LoadEntity(_metaQuery.GetComponent(entity).EntityPrototype, entity, ComponentFactory, this, _serManager, context);
        }

        private protected void LoadEntity(EntityUid entity, IEntityLoadContext? context, EntityPrototype? prototype)
        {
            EntityPrototype.LoadEntity(prototype, entity, ComponentFactory, this, _serManager, context);
        }

        public void InitializeAndStartEntity(EntityUid entity, MapId? mapId = null)
        {
            try
            {
                var meta = _metaQuery.GetComponent(entity);
                InitializeEntity(entity, meta);
                StartEntity(entity);

                // If the map we're initializing the entity on is initialized, run map init on it.
                if (_mapManager.IsMapInitialized(mapId ?? _xformQuery.GetComponent(entity).MapID))
                    RunMapInit(entity, meta);
            }
            catch (Exception e)
            {
                DeleteEntity(entity);
                throw new EntityCreationException("Exception inside InitializeAndStartEntity", e);
            }
        }

        public void InitializeEntity(EntityUid entity, MetaDataComponent? meta = null)
        {
            InitializeComponents(entity, meta);
            EntityInitialized?.Invoke(entity);
        }

        public void StartEntity(EntityUid entity)
        {
            StartComponents(entity);
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

        private sealed class EntityDiffContext : ISerializationContext
        {
            public SerializationManager.SerializerProvider SerializerProvider { get; }
            public bool WritingReadingPrototypes { get; set; }

            public EntityDiffContext()
            {
                SerializerProvider = new();
                SerializerProvider.RegisterSerializer(this);
            }
        }
    }

    public enum EntityMessageType : byte
    {
        Error = 0,
        SystemMessage
    }
}
