using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Prometheus;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Profiling;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public delegate void EntityUidQueryCallback(EntityUid uid);

    public delegate void ComponentQueryCallback<T>(EntityUid uid, T component) where T : IComponent;

    /// <inheritdoc />
    [Virtual]
    public abstract partial class EntityManager : IEntityManager
    {
        #region Dependencies

        [IoC.Dependency] protected readonly IPrototypeManager PrototypeManager = default!;
        [IoC.Dependency] protected readonly ILogManager LogManager = default!;
        [IoC.Dependency] private readonly IEntitySystemManager _entitySystemManager = default!;
        [IoC.Dependency] private readonly IMapManager _mapManager = default!;
        [IoC.Dependency] private readonly IGameTiming _gameTiming = default!;
        [IoC.Dependency] private readonly ISerializationManager _serManager = default!;
        [IoC.Dependency] private readonly ProfManager _prof = default!;
        [IoC.Dependency] private readonly INetManager _netMan = default!;
        [IoC.Dependency] private readonly IReflectionManager _reflection = default!;

        // I feel like PJB might shed me for putting a system dependency here, but its required for setting entity
        // positions on spawn....
        private SharedTransformSystem _xforms = default!;
        private SharedContainerSystem _containers = default!;

        public EntityQuery<MetaDataComponent> MetaQuery;
        public EntityQuery<TransformComponent> TransformQuery;
        private EntityQuery<PhysicsComponent> _physicsQuery;
        private EntityQuery<ActorComponent> _actorQuery;

        #endregion Dependencies

        /// <inheritdoc />
        public GameTick CurrentTick => _gameTiming.CurTick;

        public static readonly MapInitEvent MapInitEventInstance = new();

        IComponentFactory IEntityManager.ComponentFactory => ComponentFactory;

        /// <inheritdoc />
        public IEntitySystemManager EntitySysManager => _entitySystemManager;

        /// <inheritdoc />
        public abstract IEntityNetworkManager EntityNetManager { get; }

        protected readonly Queue<EntityUid> QueuedDeletions = new();
        protected readonly HashSet<EntityUid> QueuedDeletionsSet = new();

        private EntityDiffContext _context = new();

        /// <summary>
        ///     All entities currently stored in the manager.
        /// </summary>
        protected readonly HashSet<EntityUid> Entities = new();

        private EntityEventBus _eventBus = null!;

        protected int NextEntityUid = (int) EntityUid.FirstUid;

        protected int NextNetworkId = (int) NetEntity.First;

        /// <inheritdoc />
        public IEventBus EventBus => _eventBus;

        public event Action<Entity<MetaDataComponent>>? EntityAdded;
        public event Action<Entity<MetaDataComponent>>? EntityInitialized;
        public event Action<Entity<MetaDataComponent>>? EntityDeleted;
        public event Action? BeforeEntityFlush;
        public event Action? AfterEntityFlush;

        /// <summary>
        /// Raised when an entity is queued for deletion. Not raised if an entity is deleted.
        /// </summary>
        public event Action<EntityUid>? EntityQueueDeleted;
        public event Action<Entity<MetaDataComponent>>? EntityDirtied;

        private string _xformName = string.Empty;

        private ComponentRegistration _metaReg = default!;
        private ComponentRegistration _xformReg = default!;

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

            _eventBus = new EntityEventBus(this, _reflection);

            InitializeComponents();
            _metaReg = _componentFactory.GetRegistration(typeof(MetaDataComponent));
            _xformReg = _componentFactory.GetRegistration(typeof(TransformComponent));
            _xformName = _xformReg.Name;
            _sawmill = LogManager.GetSawmill("entity");
            _resolveSawmill = LogManager.GetSawmill("resolve");

            Initialized = true;
        }

        /// <summary>
        /// Returns true if the entity's data (apart from transform) is default.
        /// </summary>
        public bool IsDefault(EntityUid uid)
        {
            if (!MetaQuery.TryGetComponent(uid, out var metadata) || metadata.EntityPrototype == null)
                return false;

            var prototype = metadata.EntityPrototype;

            // Check if entity name / description match
            if (metadata.EntityName != prototype.Name ||
                metadata.EntityDescription != prototype.Description)
            {
                return false;
            }

            var protoData = PrototypeManager.GetPrototypeData(prototype);
            var comps = _entCompIndex[uid];

            // Fast check if the component counts match.
            // Note that transform and metadata are not included in the prototype data.
            if (protoData.Count + 2 != comps.Count)
                return false;

            foreach (var component in comps)
            {
                if (component.Deleted)
                    return false;

                var compType = component.GetType();
                var compName = _componentFactory.GetComponentName(compType);
                if (compName == _xformName || compName == _metaReg.Name)
                    continue;

                // If the component isn't on the prototype then it's custom.
                if (!protoData.TryGetValue(compName, out var protoMapping))
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

                var diff = compMapping.Except(protoMapping);

                if (diff != null && diff.Children.Count != 0)
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
            _eventBus.LockSubscriptions();
            _mapSystem = System<SharedMapSystem>();
            _xforms = System<SharedTransformSystem>();
            _containers = System<SharedContainerSystem>();
            MetaQuery = GetEntityQuery<MetaDataComponent>();
            TransformQuery = GetEntityQuery<TransformComponent>();
            _physicsQuery = GetEntityQuery<PhysicsComponent>();
            _actorQuery = GetEntityQuery<ActorComponent>();
        }

        public virtual void Shutdown()
        {
            ShuttingDown = true;
            FlushEntities();
            _eventBus.ClearSubscriptions();
            _entitySystemManager.Shutdown();
            ClearComponents();
            ShuttingDown = false;
            Started = false;
        }

        public virtual void Cleanup()
        {
            _componentFactory.ComponentsAdded -= OnComponentsAdded;
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

        /// <inheritdoc />
        public EntityUid CreateEntityUninitialized(string? prototypeName, EntityUid euid, ComponentRegistry? overrides = null)
        {
            return CreateEntity(prototypeName, out _, overrides);
        }

        /// <inheritdoc />
        public virtual EntityUid CreateEntityUninitialized(string? prototypeName, ComponentRegistry? overrides = null)
        {
            return CreateEntity(prototypeName, out _, overrides);
        }

        /// <inheritdoc />
        public virtual EntityUid CreateEntityUninitialized(string? prototypeName, EntityCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default)
        {
            var newEntity = CreateEntity(prototypeName, out _, overrides);

            var xformComp = TransformQuery.GetComponent(newEntity);
            _xforms.SetCoordinates(newEntity, xformComp, coordinates, rotation: rotation, unanchor: false);
            return newEntity;
        }

        /// <inheritdoc />
        public virtual EntityUid CreateEntityUninitialized(string? prototypeName, MapCoordinates coordinates, ComponentRegistry? overrides = null, Angle rotation = default!)
        {
            var newEntity = CreateEntity(prototypeName, out _, overrides);
            var transform = TransformQuery.GetComponent(newEntity);

            if (coordinates.MapId == MapId.Nullspace)
            {
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
                _xforms.SetCoordinates(newEntity, transform, coords, rotation, unanchor: false);
            }
            else
            {
                coords = new EntityCoordinates(mapEnt, coordinates.Position);
                _xforms.SetCoordinates(newEntity, transform, coords, rotation, newParent: mapXform);
            }

            return newEntity;
        }

        /// <inheritdoc />
        public int EntityCount => Entities.Count;

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetEntities() => Entities;

        /// <inheritdoc />
        public virtual void DirtyEntity(EntityUid uid, MetaDataComponent? metadata = null)
        {
            // We want to retrieve MetaDataComponent even if its Deleted flag is set.
            if (!MetaQuery.ResolveInternal(uid, ref metadata))
                return;

            if (metadata.EntityLastModifiedTick == _gameTiming.CurTick)
                return;

            metadata.EntityLastModifiedTick = _gameTiming.CurTick;

            if (metadata.EntityLifeStage > EntityLifeStage.Initializing)
            {
                EntityDirtied?.Invoke((uid, metadata));
            }
        }

        /// <inheritdoc />
        [Obsolete("use override with an EntityUid or Entity<T>")]
        public void Dirty(IComponent component, MetaDataComponent? meta = null)
        {
            Dirty(component.Owner, component, meta);
        }

        /// <inheritdoc />
        public virtual void Dirty(EntityUid uid, IComponent component, MetaDataComponent? meta = null)
        {
            DebugTools.Assert(component.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {component.GetType()}");
            DebugTools.AssertOwner(uid, component);

            if (component.LifeStage >= ComponentLifeStage.Removing || !component.NetSyncEnabled)
                return;

            if (component.LastModifiedTick == CurrentTick)
                return;

            DirtyEntity(uid, meta);
            component.LastModifiedTick = CurrentTick;
        }

        /// <inheritdoc />
        public virtual void Dirty<T>(Entity<T> ent, MetaDataComponent? meta = null) where T : IComponent
        {
            DebugTools.Assert(ent.Comp.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {ent.Comp.GetType()}");

            if (ent.Comp.LifeStage >= ComponentLifeStage.Removing || !ent.Comp.NetSyncEnabled)
                return;

            if (ent.Comp.LastModifiedTick == CurrentTick)
                return;

            DirtyEntity(ent, meta);
            ent.Comp.LastModifiedTick = CurrentTick;
        }

        /// <inheritdoc />
        public virtual void Dirty<T1, T2>(Entity<T1, T2> ent, MetaDataComponent? meta = null)
            where T1 : IComponent
            where T2 : IComponent
        {
            DebugTools.Assert(ent.Comp1.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {ent.Comp1.GetType()}");
            DebugTools.Assert(ent.Comp2.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {ent.Comp2.GetType()}");

            // We're not gonna bother checking ent.Comp.NetSyncEnabled
            // chances are at least one of these components didn't get net-sync disabled.
            DirtyEntity(ent, meta);
            ent.Comp1.LastModifiedTick = CurrentTick;
            ent.Comp2.LastModifiedTick = CurrentTick;
        }

        /// <inheritdoc />
        public virtual void Dirty<T1, T2, T3>(Entity<T1, T2, T3> ent, MetaDataComponent? meta = null)
            where T1 : IComponent
            where T2 : IComponent
            where T3 : IComponent
        {
            DebugTools.Assert(ent.Comp1.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {ent.Comp1.GetType()}");
            DebugTools.Assert(ent.Comp2.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {ent.Comp2.GetType()}");
            DebugTools.Assert(ent.Comp3.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {ent.Comp3.GetType()}");

            // We're not gonna bother checking ent.Comp.NetSyncEnabled
            // chances are at least one of these components didn't get net-sync disabled.
            DirtyEntity(ent, meta);
            ent.Comp1.LastModifiedTick = CurrentTick;
            ent.Comp2.LastModifiedTick = CurrentTick;
            ent.Comp3.LastModifiedTick = CurrentTick;
        }

        /// <inheritdoc />
        public virtual void Dirty<T1, T2, T3, T4>(Entity<T1, T2, T3, T4> ent, MetaDataComponent? meta = null)
            where T1 : IComponent
            where T2 : IComponent
            where T3 : IComponent
            where T4 : IComponent
        {
            DebugTools.Assert(ent.Comp1.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {ent.Comp1.GetType()}");
            DebugTools.Assert(ent.Comp2.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {ent.Comp2.GetType()}");
            DebugTools.Assert(ent.Comp3.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {ent.Comp3.GetType()}");
            DebugTools.Assert(ent.Comp4.GetType().HasCustomAttribute<NetworkedComponentAttribute>(),
                $"Attempted to dirty a non-networked component: {ent.Comp4.GetType()}");

            // We're not gonna bother checking ent.Comp.NetSyncEnabled
            // chances are at least one of these components didn't get net-sync disabled.
            DirtyEntity(ent, meta);
            ent.Comp1.LastModifiedTick = CurrentTick;
            ent.Comp2.LastModifiedTick = CurrentTick;
            ent.Comp3.LastModifiedTick = CurrentTick;
            ent.Comp4.LastModifiedTick = CurrentTick;
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        public virtual void DeleteEntity(EntityUid? uid)
        {
            if (uid == null)
                return;

            // Some UIs get disposed after entity-manager has shut down and already deleted all entities.
            if (!Started)
                return;

            // Networking blindly spams entities at this function, they can already be
            // deleted from being a child of a previously deleted entity
            // TODO: Why does networking need to send deletes for child entities?
            if (MetaQuery.TryGetComponent(uid.Value, out var meta))
                DeleteEntity(uid.Value, meta, TransformQuery.GetComponent(uid.Value));
        }

        /// <summary>
        /// Shuts-down and removes given Entity. This is also broadcast to all clients.
        /// </summary>
        public void DeleteEntity(EntityUid e, MetaDataComponent meta, TransformComponent xform)
        {
            // Some UIs get disposed after entity-manager has shut down and already deleted all entities.
            if (!Started)
                return;

            if (meta.EntityLifeStage >= EntityLifeStage.Deleted)
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
            RecursiveFlagEntityTermination(e, meta, xform);

            TransformComponent? parentXform = null;
            if (xform.ParentUid.IsValid())
                TransformQuery.Resolve(xform.ParentUid, ref parentXform);

            // Then actually delete them
            RecursiveDeleteEntity(e, meta, xform, parentXform);
        }

        private void RecursiveFlagEntityTermination(EntityUid uid,
            MetaDataComponent metadata,
            TransformComponent xform)
        {
            DebugTools.Assert(metadata.EntityLifeStage < EntityLifeStage.Terminating);
            SetLifeStage(metadata, EntityLifeStage.Terminating);

            try
            {
                var ev = new EntityTerminatingEvent((uid, metadata));
                EventBus.RaiseLocalEvent(uid, ref ev, true);
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception while raising event {nameof(EntityTerminatingEvent)} on entity {ToPrettyString(uid, metadata)}\n{e}");
            }

            foreach (var child in xform._children)
            {
                if (!MetaQuery.TryGetComponent(child, out var childMeta) || childMeta.EntityDeleted)
                {
                    _sawmill.Error($"A deleted entity was still the transform child of another entity. Parent: {ToPrettyString(uid, metadata)}.");
                    xform._children.Remove(child);
                    continue;
                }

                RecursiveFlagEntityTermination(child, childMeta, TransformQuery.GetComponent(child));
            }
        }

        private void RecursiveDeleteEntity(
            EntityUid uid,
            MetaDataComponent metadata,
            TransformComponent transform,
            TransformComponent? parentXform)
        {
            DebugTools.Assert(transform.ParentUid.IsValid() == (parentXform != null));
            DebugTools.Assert(parentXform == null || parentXform._children.Contains(uid));

            // Note about this method: #if EXCEPTION_TOLERANCE is not used here because we're gonna it in the future...

            // Detach the base entity to null before iterating over children
            // This also ensures that the entity-lookup updates don't have to be re-run for every child (which recurses up the transform hierarchy).
            _xforms.DetachEntity(uid, transform, metadata, parentXform, true);

            foreach (var child in transform._children)
            {
                try
                {
                    var childMeta = MetaQuery.GetComponent(child);
                    var childXform = TransformQuery.GetComponent(child);
                    DebugTools.AssertEqual(childXform.ParentUid, uid);
                    RecursiveDeleteEntity(child, childMeta, childXform, transform);
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
                        LifeShutdown(component);
                    }
                    catch (Exception e)
                    {
                        _sawmill.Error($"Caught exception while trying to call shutdown on component of entity '{ToPrettyString(uid, metadata)}'\n{e}");
                    }
                }
            }

            // Dispose all my components, in a safe order so transform is available
            DisposeComponents(uid, metadata);
            SetLifeStage(metadata, EntityLifeStage.Deleted);

            try
            {
                EntityDeleted?.Invoke((uid, metadata));
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception while invoking event {nameof(EntityDeleted)} on '{ToPrettyString(uid, metadata)}'\n{e}");
            }

            _eventBus.OnEntityDeleted(uid);
            Entities.Remove(uid);
            // Need to get the ID above before MetadataComponent shutdown but only remove it after everything else is done.
            NetEntityLookup.Remove(metadata.NetEntity);
        }

        public virtual void QueueDeleteEntity(EntityUid? uid)
        {
            if (uid == null)
                return;

            if (!QueuedDeletionsSet.Add(uid.Value))
                return;

            QueuedDeletions.Enqueue(uid.Value);
            EntityQueueDeleted?.Invoke(uid.Value);
        }

        public bool IsQueuedForDeletion(EntityUid uid) => QueuedDeletionsSet.Contains(uid);

        public bool EntityExists(EntityUid uid)
        {
            return MetaQuery.HasComponentInternal(uid);
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

            return MetaQuery.Resolve(uid.Value, ref metadata) && metadata.EntityPaused;
        }

        public bool Deleted(EntityUid uid)
        {
            return !MetaQuery.TryGetComponentInternal(uid, out var comp) || comp.EntityDeleted;
        }

        public bool Deleted([NotNullWhen(false)] EntityUid? uid)
        {
            return !uid.HasValue || !MetaQuery.TryGetComponentInternal(uid.Value, out var comp) || comp.EntityDeleted;
        }

        /// <summary>
        /// Disposes all entities and clears all lists.
        /// </summary>
        public virtual void FlushEntities()
        {
            BeforeEntityFlush?.Invoke();
            QueuedDeletions.Clear();
            QueuedDeletionsSet.Clear();

            // First, we directly delete all maps. This will delete most entities while reducing the number of component
            // lookups

            var maps = _entTraitDict[typeof(MapComponent)].Keys.ToArray();
            foreach (var map in maps)
            {
                try
                {
                    DeleteEntity(map);
                }
                catch (Exception e)
                {
                    _sawmill.Log(LogLevel.Error, e,
                        $"Caught exception while trying to delete map entity {ToPrettyString(map)}, this might corrupt the game state...");
#if !EXCEPTION_TOLERANCE
                    throw;
#endif
                }
            }

            // Then delete all other entities.
            var ents = _entTraitDict[typeof(MetaDataComponent)].ToArray();
            DebugTools.Assert(ents.Length == Entities.Count);
            foreach (var (uid, comp) in ents)
            {
                var meta = (MetaDataComponent) comp;
                if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
                    continue;

                try
                {
                    DeleteEntity(uid, meta, TransformQuery.GetComponent(uid));
                }
                catch (Exception e)
                {
                    _sawmill.Log(LogLevel.Error, e,
                        $"Caught exception while trying to delete entity {ToPrettyString(uid, meta)}, this might corrupt the game state...");
#if !EXCEPTION_TOLERANCE
                    throw;
#endif
                }
            }

            if (Entities.Count != 0)
                _sawmill.Error("Entities were spawned while flushing entities.");

            AfterEntityFlush?.Invoke();
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private protected EntityUid AllocEntity(
            EntityPrototype? prototype,
            out MetaDataComponent metadata)
        {
            var entity = AllocEntity(out metadata);
            metadata._entityPrototype = prototype;
            Dirty(entity, metadata, metadata);
            return entity;
        }

        /// <summary>
        ///     Allocates an entity and stores it but does not load components or do initialization.
        /// </summary>
        private EntityUid AllocEntity(out MetaDataComponent metadata)
        {
            var uid = GenerateEntityUid();

#if DEBUG
            if (EntityExists(uid))
            {
                throw new InvalidOperationException($"UID already taken: {uid}");
            }
#endif

            metadata = new MetaDataComponent
            {
#pragma warning disable CS0618
                Owner = uid,
#pragma warning restore CS0618
                EntityLastModifiedTick = _gameTiming.CurTick
            };

            var netEntity = GenerateNetEntity();
            SetNetEntity(uid, netEntity, metadata);

            // we want this called before adding components
            EntityAdded?.Invoke((uid, metadata));
            _eventBus.OnEntityAdded(uid);

            Entities.Add(uid);
            // add the required MetaDataComponent directly.
            AddComponentInternal(uid, metadata, _metaReg, false, true, metadata);

            // allocate the required TransformComponent
            var xformComp = Unsafe.As<TransformComponent>(_componentFactory.GetComponent(_xformReg));
#pragma warning disable CS0618 // Type or member is obsolete
            xformComp.Owner = uid;
#pragma warning restore CS0618 // Type or member is obsolete
            AddComponentInternal(uid, xformComp, false, true, metadata);

            return uid;
        }

        /// <summary>
        ///     Allocates an entity and loads components but does not do initialization.
        /// </summary>
        internal virtual EntityUid CreateEntity(string? prototypeName, out MetaDataComponent metadata, IEntityLoadContext? context = null)
        {
            if (prototypeName == null)
                return AllocEntity(out metadata);

            if (!PrototypeManager.TryIndex<EntityPrototype>(prototypeName, out var prototype))
                throw new EntityCreationException($"Attempted to spawn an entity with an invalid prototype: {prototypeName}");

            return CreateEntity(prototype, out metadata, context);
        }

        /// <summary>
        ///     Allocates an entity and loads components but does not do initialization.
        /// </summary>
        private protected EntityUid CreateEntity(EntityPrototype prototype, out MetaDataComponent metadata, IEntityLoadContext? context = null)
        {
            var entity = AllocEntity(prototype, out metadata);
            try
            {
                EntityPrototype.LoadEntity((entity, metadata), ComponentFactory, this, _serManager, context);
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
            EntityPrototype.LoadEntity((entity, MetaQuery.GetComponent(entity)), ComponentFactory, this, _serManager, context);
        }

        private protected void LoadEntity(EntityUid entity, IEntityLoadContext? context, EntityPrototype? prototype)
        {
            var meta = MetaQuery.GetComponent(entity);
            DebugTools.Assert(meta.EntityPrototype == prototype);
            EntityPrototype.LoadEntity((entity, meta), ComponentFactory, this, _serManager, context);
        }

        public void InitializeAndStartEntity(EntityUid entity, MapId? mapId = null)
        {
            var doMapInit = _mapManager.IsMapInitialized(mapId ?? TransformQuery.GetComponent(entity).MapID);
            InitializeAndStartEntity(entity, doMapInit);
        }

        public void InitializeAndStartEntity(Entity<MetaDataComponent?> entity, bool doMapInit)
        {
            if (!MetaQuery.Resolve(entity.Owner, ref entity.Comp))
                return;

            try
            {
                InitializeEntity(entity.Owner, entity.Comp);
                StartEntity(entity.Owner);

                if (doMapInit)
                    RunMapInit(entity.Owner, entity.Comp);
            }
            catch (Exception e)
            {
                DeleteEntity(entity);
                throw new EntityCreationException("Exception inside InitializeAndStartEntity", e);
            }
        }

        public void InitializeEntity(EntityUid entity, MetaDataComponent? meta = null)
        {
            DebugTools.AssertOwner(entity, meta);
            meta ??= GetComponent<MetaDataComponent>(entity);
            InitializeComponents(entity, meta);
            EntityInitialized?.Invoke((entity, meta));
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
            SetLifeStage(meta, EntityLifeStage.MapInitialized);

            EventBus.RaiseLocalEvent(entity, MapInitEventInstance);
        }

        /// <inheritdoc />
        [return: NotNullIfNotNull("uid")]
        public EntityStringRepresentation? ToPrettyString(EntityUid? uid, MetaDataComponent? metadata = null)
        {
            return uid == null ? null : ToPrettyString(uid.Value, metadata);
        }

        /// <inheritdoc />
        public EntityStringRepresentation ToPrettyString(EntityUid uid, MetaDataComponent? metadata)
            =>  ToPrettyString((uid, metadata));

        /// <inheritdoc />
        public EntityStringRepresentation ToPrettyString(Entity<MetaDataComponent?> entity)
        {
            if (entity.Comp == null && !MetaQuery.Resolve(entity.Owner, ref entity.Comp, false))
                return new EntityStringRepresentation(entity.Owner, default, true);

            return new EntityStringRepresentation(entity.Owner, entity.Comp, _actorQuery.CompOrNull(entity));
        }

        /// <inheritdoc />
        [return: NotNullIfNotNull("netEntity")]
        public EntityStringRepresentation? ToPrettyString(NetEntity? netEntity)
        {
            return netEntity == null ? null : ToPrettyString(netEntity.Value);
        }

        /// <inheritdoc />
        public EntityStringRepresentation ToPrettyString(NetEntity netEntity)
        {
            if (!TryGetEntityData(netEntity, out var uid, out var meta))
                return new EntityStringRepresentation(EntityUid.Invalid, netEntity, true);

            return ToPrettyString(uid.Value, meta);
        }

        #endregion Entity Management

        public virtual void RaisePredictiveEvent<T>(T msg) where T : EntityEventArgs
        {
            // Part of shared the EntityManager so that systems can have convenient proxy methods, but the
            // server should never be calling this.
            DebugTools.Assert("Why are you raising predictive events on the server?");
        }

        /// <summary>
        /// Raises an event locally on client or networked on server.
        /// </summary>
        public abstract void RaiseSharedEvent<T>(T message, EntityUid? user = null) where T : EntityEventArgs;

        /// <summary>
        /// Raises an event locally on client or networked on server.
        /// </summary>
        public abstract void RaiseSharedEvent<T>(T message, ICommonSession? user = null) where T : EntityEventArgs;

        /// <summary>
        ///     Factory for generating a new EntityUid for an entity currently being created.
        /// </summary>
        internal EntityUid GenerateEntityUid()
        {
            return new EntityUid(NextEntityUid++);
        }

        /// <summary>
        /// Generates a unique network id and increments <see cref="NextNetworkId"/>
        /// </summary>
        protected virtual NetEntity GenerateNetEntity() => new(NextNetworkId++);
    }

    public enum EntityMessageType : byte
    {
        Error = 0,
        SystemMessage
    }
}
