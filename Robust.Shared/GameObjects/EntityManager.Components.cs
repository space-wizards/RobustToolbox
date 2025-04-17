using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    public partial class EntityManager
    {
        [IoC.Dependency] private readonly IComponentFactory _componentFactory = default!;

#if EXCEPTION_TOLERANCE
        [IoC.Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

        public IComponentFactory ComponentFactory => _componentFactory;

        private const int TypeCapacity = 32;
        private const int ComponentCollectionCapacity = 1024;
        private const int EntityCapacity = 1024;
        private const int NetComponentCapacity = 8;

        private FrozenDictionary<Type, Dictionary<EntityUid, IComponent>> _entTraitDict
            = FrozenDictionary<Type, Dictionary<EntityUid, IComponent>>.Empty;

        private Dictionary<EntityUid, IComponent>[] _entTraitArray
            = Array.Empty<Dictionary<EntityUid, IComponent>>();

        private readonly HashSet<IComponent> _deleteSet = new(TypeCapacity);

        private UniqueIndexHkm<EntityUid, IComponent> _entCompIndex =
            new(ComponentCollectionCapacity);

        /// <inheritdoc />
        public event Action<AddedComponentEventArgs>? ComponentAdded;

        /// <inheritdoc />
        public event Action<RemovedComponentEventArgs>? ComponentRemoved;

        public void InitializeComponents()
        {
            if (Initialized)
                throw new InvalidOperationException("Already initialized.");

            FillComponentDict();
            _componentFactory.ComponentsAdded += OnComponentsAdded;
        }

        /// <summary>
        ///     Instantly clears all components from the manager. This will NOT shut them down gracefully.
        ///     Any entities relying on existing components will be broken.
        /// </summary>
        public void ClearComponents()
        {
            _entCompIndex.Clear();
            _deleteSet.Clear();
            foreach (var dict in _entTraitDict.Values)
            {
                dict.Clear();
            }
        }

        private void RegisterComponents(IEnumerable<ComponentRegistration> components)
        {
            var traitDict = _entTraitDict.ToDictionary();
            foreach (var reg in components)
            {
                var dict = new Dictionary<EntityUid, IComponent>();
                traitDict.Add(reg.Type, dict);
                CompIdx.AssignArray(ref _entTraitArray, reg.Idx, dict);
            }
            _entTraitDict = traitDict.ToFrozenDictionary();
        }

        private void OnComponentsAdded(ComponentRegistration[] components)
        {
            RegisterComponents(components);
        }

        #region Component Management

        /// <inheritdoc />
        public int Count<T>() where T : IComponent
        {
            var dict = _entTraitDict[typeof(T)];
            return dict.Count;
        }

        /// <inheritdoc />
        public int Count(Type component)
        {
            DebugTools.Assert(component.IsAssignableTo(typeof(IComponent)));
            var dict = _entTraitDict[component];
            return dict.Count;
        }

        [Obsolete("Use InitializeEntity")]
        public void InitializeComponents(EntityUid uid, MetaDataComponent? metadata = null)
        {
            DebugTools.AssertOwner(uid, metadata);
            metadata ??= MetaQuery.GetComponent(uid);
            DebugTools.Assert(metadata.EntityLifeStage == EntityLifeStage.PreInit);
            SetLifeStage(metadata, EntityLifeStage.Initializing);

            // Initialize() can modify the collection of components. Copy them.
            FixedArray32<IComponent?> compsFixed = default;

            var comps = compsFixed.AsSpan;
            CopyComponentsInto(ref comps, uid);

            foreach (var comp in comps)
            {
                if (comp is {LifeStage: ComponentLifeStage.Added})
                    LifeInitialize(uid, comp, _componentFactory.GetIndex(comp.GetType()));
            }

#if DEBUG
            // Second integrity check in case of.
            foreach (var t in _entCompIndex[uid])
            {
                if (!t.Deleted && !t.Initialized)
                {
                    DebugTools.Assert(
                        $"Component {t.GetType()} was not initialized at the end of {nameof(InitializeComponents)}.");
                }
            }

#endif
            DebugTools.Assert(metadata.EntityLifeStage == EntityLifeStage.Initializing);
            SetLifeStage(metadata, EntityLifeStage.Initialized);
        }

        [Obsolete("Use StartEntity")]
        public void StartComponents(EntityUid uid)
        {
            // Startup() can modify _components
            // This code can only handle additions to the list. Is there a better way? Probably not.
            FixedArray32<IComponent?> compsFixed = default;

            var comps = compsFixed.AsSpan;
            CopyComponentsInto(ref comps, uid);

            // TODO: please for the love of god remove these initialization order hacks.

            // Init transform first, we always have it.
            var transform = TransformQuery.GetComponent(uid);
            if (transform.LifeStage == ComponentLifeStage.Initialized)
                LifeStartup(uid, transform, CompIdx.Index<TransformComponent>());

            // Init physics second if it exists.
            if (_physicsQuery.TryComp(uid, out var phys) && phys.LifeStage == ComponentLifeStage.Initialized)
            {
                LifeStartup(uid, phys, CompIdx.Index<PhysicsComponent>());
            }

            // Do rest of components.
            foreach (var comp in comps)
            {
                if (comp is { LifeStage: ComponentLifeStage.Initialized })
                    LifeStartup(uid, comp, _componentFactory.GetIndex(comp.GetType()));
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void AddComponents(EntityUid target, EntityPrototype prototype, bool removeExisting = true)
        {
            AddComponents(target, prototype.Components, removeExisting);
        }

        /// <inheritdoc />
        public void AddComponents(EntityUid target, ComponentRegistry registry, bool removeExisting = true)
        {
            if (registry.Count == 0)
                return;

            var metadata = MetaQuery.GetComponent(target);

            foreach (var (name, entry) in registry)
            {
                var reg = _componentFactory.GetRegistration(name);

                if (removeExisting)
                {
                    var comp = _componentFactory.GetComponent(reg);
                    _serManager.CopyTo(entry.Component, ref comp, notNullableOverride: true);
                    AddComponentInternal(target, comp, reg, overwrite: true, metadata: metadata);
                }
                else
                {
                    if (HasComponent(target, reg))
                    {
                        continue;
                    }

                    var comp = _componentFactory.GetComponent(reg);
                    _serManager.CopyTo(entry.Component, ref comp, notNullableOverride: true);
                    AddComponentInternal(target, comp, reg, overwrite: false, metadata: metadata);
                }
            }
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponents(EntityUid target, EntityPrototype prototype)
        {
            RemoveComponents(target, prototype.Components);
        }

        /// <inheritdoc />
        public void RemoveComponents(EntityUid target, ComponentRegistry registry)
        {
            if (registry.Count == 0)
                return;

            var metadata = MetaQuery.GetComponent(target);

            foreach (var entry in registry.Values)
            {
                RemoveComponent(target, entry.Component.GetType(), metadata);
            }
        }

        public IComponent AddComponent(EntityUid uid, ushort netId, MetaDataComponent? meta = null)
        {
            var newComponent = _componentFactory.GetComponent(netId);
            AddComponent(uid, newComponent, metadata: meta);
            return newComponent;
        }

        public T AddComponent<T>(EntityUid uid) where T : IComponent, new()
        {
            var newComponent = _componentFactory.GetComponent<T>();
            AddComponent(uid, newComponent);
            return newComponent;
        }

        public readonly struct CompInitializeHandle<T> : IDisposable
            where T : IComponent
        {
            private readonly IEntityManager _entMan;
            private readonly EntityUid _owner;
            public readonly CompIdx CompType;
            public readonly T Comp;

            public CompInitializeHandle(IEntityManager entityManager, EntityUid owner, T comp, CompIdx compType)
            {
                _entMan = entityManager;
                _owner = owner;
                Comp = comp;
                CompType = compType;
            }

            public void Dispose()
            {
                var metadata = _entMan.GetComponent<MetaDataComponent>(_owner);

                if (!metadata.EntityInitialized && !metadata.EntityInitializing)
                    return;

                if (!Comp.Initialized)
                    ((EntityManager) _entMan).LifeInitialize(_owner, Comp, CompType);

                if (metadata.EntityInitialized && !Comp.Running)
                    ((EntityManager) _entMan).LifeStartup(_owner, Comp, CompType);
            }

            public static implicit operator T(CompInitializeHandle<T> handle)
            {
                return handle.Comp;
            }
        }

        public void AddComponent(
            EntityUid uid,
            EntityPrototype.ComponentRegistryEntry entry,
            bool overwrite = false,
            MetaDataComponent? metadata = null)
        {
            var copy = _componentFactory.GetComponent(entry);
            AddComponent(uid, copy, overwrite, metadata);
        }

        /// <inheritdoc />
        public void AddComponent<T>(EntityUid uid, T component, bool overwrite = false, MetaDataComponent? metadata = null) where T : IComponent
        {
            if (!MetaQuery.Resolve(uid, ref metadata, false))
                throw new ArgumentException($"Entity {uid} is not valid.", nameof(uid));

            if (component == null)
                throw new ArgumentNullException(nameof(component));

#pragma warning disable CS0618 // Type or member is obsolete
            if (component.Owner == default)
            {
                component.Owner = uid;
            }
            else if (component.Owner != uid)
            {
                throw new InvalidOperationException("Component is not owned by entity.");
            }
#pragma warning restore CS0618 // Type or member is obsolete

            AddComponentInternal(uid, component, overwrite, false, metadata);
        }

        private void AddComponentInternal<T>(
            EntityUid uid,
            T component,
            ComponentRegistration compReg,
            bool overwrite = false,
            MetaDataComponent? metadata = null) where T : IComponent
        {
            if (!MetaQuery.Resolve(uid, ref metadata, false))
                throw new ArgumentException($"Entity {uid} is not valid.", nameof(uid));

            DebugTools.Assert(component.Owner == default);
            component.Owner = uid;

            AddComponentInternal(uid, component, compReg, overwrite, skipInit: false, metadata);
        }

        private void AddComponentInternal<T>(EntityUid uid, T component, bool overwrite, bool skipInit, MetaDataComponent? metadata) where T : IComponent
        {
            if (!MetaQuery.ResolveInternal(uid, ref metadata, false))
                throw new ArgumentException($"Entity {uid} is not valid.", nameof(uid));

            // get interface aliases for mapping
            var reg = _componentFactory.GetRegistration(component);
            AddComponentInternal(uid, component, reg, overwrite, skipInit, metadata);
        }

        private void AddComponentInternal<T>(EntityUid uid, T component, ComponentRegistration reg, bool overwrite, bool skipInit, MetaDataComponent metadata) where T : IComponent
        {
            ThreadCheck();

            // We can't use typeof(T) here in case T is just Component
            DebugTools.Assert(component is MetaDataComponent ||
                              (metadata ?? MetaQuery.GetComponent(uid)).EntityLifeStage < EntityLifeStage.Terminating,
                $"Attempted to add a {component.GetType().Name} component to an entity ({ToPrettyString(uid)}) while it is terminating");

            // Check that there is no existing component.
            var type = reg.Idx;
            var dict = _entTraitArray[type.Value];
            DebugTools.Assert(dict != null);

            // Code block to restrict access to ref comp.
            {
                ref var comp = ref CollectionsMarshal.GetValueRefOrAddDefault(dict, uid, out var exists);
                if (exists)
                {
                    if (!overwrite && !comp!.Deleted)
                    {
                        throw new InvalidOperationException(
                            $"Component reference type {reg.Name} already occupied by {comp}");
                    }

                    // This will invalidate the comp ref as it removes the key from the dictionary.
                    // This is inefficient, but component overriding rarely ever happens.
                    RemoveComponentImmediate(uid, comp!, type, false, metadata);
                    dict.Add(uid, component);
                }
                else
                {
                    comp = component;
                }
            }

            // actually ADD the component
            _entCompIndex.Add(uid, component);

            // add the component to the netId grid
            if (reg.NetID != null && component.NetSyncEnabled)
            {
                // the main comp grid keeps this in sync
                var netId = reg.NetID.Value;
                metadata ??= MetaQuery.GetComponentInternal(uid);
                metadata.NetComponents.Add(netId, component);
            }

            if (component is IComponentDelta delta)
            {
                var curTick = _gameTiming.CurTick;
                delta.LastModifiedFields = new GameTick[reg.NetworkedFields.Length];
                Array.Fill(delta.LastModifiedFields, curTick);
            }

            component.Networked = reg.NetID != null;

            var eventArgs = new AddedComponentEventArgs(new ComponentEventArgs(component, uid), reg);
            ComponentAdded?.Invoke(eventArgs);
            _eventBus.OnComponentAdded(eventArgs);

            LifeAddToEntity(uid, component, reg.Idx);

            if (skipInit)
                return;

            metadata ??= MetaQuery.GetComponentInternal(uid);

            if (!metadata.EntityInitialized && !metadata.EntityInitializing)
                return;

            if (component.Networked)
                DirtyEntity(uid, metadata);

            LifeInitialize(uid, component, reg.Idx);

            if (metadata.EntityInitialized)
                LifeStartup(uid, component, reg.Idx);

            if (metadata.EntityLifeStage >= EntityLifeStage.MapInitialized)
                EventBus.RaiseComponentEvent(uid, component, reg.Idx, MapInitEventInstance);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent<T>(EntityUid uid, MetaDataComponent? meta = null) where T : IComponent
        {
            if (!TryGetComponent(uid, out T? comp))
                return false;

            RemoveComponentImmediate(uid, comp, CompIdx.Index<T>(), false, meta);
            return true;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent(EntityUid uid, Type type, MetaDataComponent? meta = null)
        {
            if (!TryGetComponent(uid, type, out var comp))
                return false;

            RemoveComponentImmediate(uid, comp, _componentFactory.GetIndex(type), false, meta);
            return true;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent(EntityUid uid, ushort netId, MetaDataComponent? meta = null)
        {
            if (!MetaQuery.Resolve(uid, ref meta))
                return false;

            if (!TryGetComponent(uid, netId, out var comp, meta))
                return false;

            var idx = _componentFactory.GetIndex(comp.GetType());
            RemoveComponentImmediate(uid, comp, idx, false, meta);
            return true;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityUid uid, IComponent component, MetaDataComponent? meta = null)
        {
            var idx = _componentFactory.GetIndex(component.GetType());
            RemoveComponentImmediate(uid, component, idx, false, meta);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponentDeferred<T>(EntityUid uid)
        {
            return RemoveComponentDeferred(uid, typeof(T));
        }

        /// <inheritdoc />
        public bool RemoveComponentDeferred(EntityUid uid, Type type)
        {
            if (!TryGetComponent(uid, type, out var comp))
                return false;

            RemoveComponentDeferred(comp, uid, false);
            return true;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponentDeferred(EntityUid uid, ushort netId, MetaDataComponent? meta = null)
        {
            if (!MetaQuery.Resolve(uid, ref meta))
                return false;

            if (!TryGetComponent(uid, netId, out var comp, meta))
                return false;

            RemoveComponentDeferred(comp, uid, false);
            return true;
        }

        /// <inheritdoc />
        public void RemoveComponentDeferred(EntityUid owner, IComponent component)
        {
            RemoveComponentDeferred(component, owner, false);
        }

        /// <inheritdoc />
        public void RemoveComponentDeferred(EntityUid owner, Component component)
        {
            RemoveComponentDeferred(component, owner, false);
        }

        private static IEnumerable<IComponent> InSafeOrder(IEnumerable<IComponent> comps, bool forCreation = false)
        {
            static int Sequence(IComponent x)
                => x switch
                {
                    MetaDataComponent _ => 0,
                    TransformComponent _ => 1,
                    PhysicsComponent _ => 2,
                    _ => int.MaxValue
                };

            return forCreation
                ? comps.OrderBy(Sequence)
                : comps.OrderByDescending(Sequence);
        }

        /// <inheritdoc />
        public void RemoveComponents(EntityUid uid, MetaDataComponent? meta = null)
        {
            if (!MetaQuery.Resolve(uid, ref meta))
                return;

            foreach (var comp in InSafeOrder(_entCompIndex[uid]))
            {
                var idx = _componentFactory.GetIndex(comp.GetType());
                RemoveComponentImmediate(uid, comp, idx, false, meta);
            }
        }

        /// <inheritdoc />
        public void DisposeComponents(EntityUid uid, MetaDataComponent? meta = null)
        {
            if (!MetaQuery.Resolve(uid, ref meta))
                return;

            foreach (var comp in InSafeOrder(_entCompIndex[uid]))
            {
                try
                {
                    var idx = _componentFactory.GetIndex(comp.GetType());
                    RemoveComponentImmediate(uid, comp, idx, true, meta);
                }
                catch (Exception)
                {
                    _sawmill.Error($"Caught exception while trying to remove component {_componentFactory.GetComponentName(comp.GetType())} from entity '{ToPrettyString(uid)}'");
                }
            }

            _entCompIndex.Remove(uid);
        }

        private void RemoveComponentDeferred(IComponent component, EntityUid uid, bool terminating)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

#pragma warning disable CS0618 // Type or member is obsolete
            if (component.Owner != uid)
#pragma warning restore CS0618 // Type or member is obsolete
                throw new InvalidOperationException("Component is not owned by entity.");

            if (component.Deleted)
                return;

#if EXCEPTION_TOLERANCE
            try
            {
#endif
            // these two components are required on all entities and cannot be removed normally.
            if (!terminating && component is TransformComponent or MetaDataComponent)
            {
                DebugTools.Assert("Tried to remove a protected component.");
                return;
            }

            if (!_deleteSet.Add(component))
            {
                // Already deferring deletion
                DebugTools.Assert(component.LifeStage >= ComponentLifeStage.Stopped);
                return;
            }

            DebugTools.Assert(component.LifeStage >= ComponentLifeStage.Added);

            if (component.LifeStage is >= ComponentLifeStage.Initialized and < ComponentLifeStage.Stopping)
                LifeShutdown(uid, component, _componentFactory.GetIndex(component.GetType()));
            else if (component.LifeStage == ComponentLifeStage.Added)
            {
                // The component was added, but never initialized or started. It's kinda weird to add and then
                // immediately defer-remove a component, but oh well. Let's just set the life stage directly and not
                // raise shutdown events? The removal events will still get called later.
                // This is also what LifeShutdown() would also do, albeit behind a DebugAssert.
                component.LifeStage = ComponentLifeStage.Stopped;
            }
#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception while queuing deferred component removal. Entity={ToPrettyString(component.Owner)}, type={component.GetType()}");
                _runtimeLog.LogException(e, nameof(RemoveComponentDeferred));
            }
#endif
        }

        private void RemoveComponentImmediate(
            EntityUid uid,
            IComponent component,
            CompIdx idx,
            bool terminating,
            MetaDataComponent? meta)
        {
            ThreadCheck();

            if (component.Deleted)
            {
                _sawmill.Warning($"Deleting an already deleted component. Entity: {ToPrettyString(uid)}, Component: {_componentFactory.GetComponentName(component.GetType())}.");
                return;
            }

#if EXCEPTION_TOLERANCE
            try
            {
#endif
            // these two components are required on all entities and cannot be removed.
            if (!terminating && component is TransformComponent or MetaDataComponent)
            {
                DebugTools.Assert("Tried to remove a protected component.");
                return;
            }

            if (component.Running)
                LifeShutdown(uid, component, idx);

            if (component.LifeStage != ComponentLifeStage.PreAdd)
                LifeRemoveFromEntity(uid, component, idx); // Sets delete

#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception during immediate component removal. Entity={ToPrettyString(component.Owner)}, type={component.GetType()}");
                _runtimeLog.LogException(e, nameof(RemoveComponentImmediate));
            }
#endif
            DeleteComponent(uid, component, idx, terminating, meta);
        }

        /// <inheritdoc />
        public void CullRemovedComponents()
        {
            foreach (var component in InSafeOrder(_deleteSet))
            {
                if (component.Deleted)
                    continue;
                var uid = component.Owner;
                var idx = _componentFactory.GetIndex(component.GetType());

#if EXCEPTION_TOLERANCE
            try
            {
#endif
                // The component may have been restarted sometime after removal was deferred.
                if (component.Running)
                {
                    // TODO add options to cancel deferred deletion?
                    _sawmill.Warning($"Found a running component while culling deferred deletions, owner={ToPrettyString(uid)}, type={component.GetType()}");
                    LifeShutdown(uid, component, idx);
                }

                if (component.LifeStage != ComponentLifeStage.PreAdd)
                    LifeRemoveFromEntity(uid, component, idx);

#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception  while processing deferred component removal. Entity={ToPrettyString(component.Owner)}, type={component.GetType()}");
                _runtimeLog.LogException(e, nameof(CullRemovedComponents));
            }
#endif
                var meta = MetaQuery.GetComponent(uid);
                DeleteComponent(uid, component, idx, false, meta);
            }

            _deleteSet.Clear();
        }

        private void DeleteComponent(
            EntityUid entityUid,
            IComponent component,
            CompIdx idx,
            bool terminating,
            MetaDataComponent? metadata)
        {
            if (!MetaQuery.ResolveInternal(entityUid, ref metadata))
                return;

            var eventArgs = new RemovedComponentEventArgs(new ComponentEventArgs(component, entityUid), false, metadata, idx);
            ComponentRemoved?.Invoke(eventArgs);
            _eventBus.OnComponentRemoved(eventArgs);

            if (!terminating)
            {
                var reg = _componentFactory.GetRegistration(component);
                DebugTools.Assert(component.Networked == (reg.NetID != null));
                if (reg.NetID != null)
                {
                    if (!metadata.NetComponents.Remove(reg.NetID.Value))
                        _sawmill.Error($"Entity {ToPrettyString(entityUid, metadata)} did not have {component.GetType().Name} in its networked component dictionary during component deletion.");

                    if (component.NetSyncEnabled)
                    {
                        DirtyEntity(entityUid, metadata);
                        metadata.LastComponentRemoved = _gameTiming.CurTick;
                    }
                }
            }

            _entTraitArray[idx.Value].Remove(entityUid);

            // TODO if terminating the entity, maybe defer this?
            // _entCompIndex.Remove(uid) gets called later on anyways.
            _entCompIndex.Remove(entityUid, component);

            DebugTools.Assert(_netMan.IsClient // Client side prediction can set LastComponentRemoved to some future tick,
                              || metadata.EntityLastModifiedTick >= metadata.LastComponentRemoved);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent<T>(EntityUid uid) where T : IComponent
        {
            var dict = _entTraitArray[CompIdx.ArrayIndex<T>()];
            DebugTools.Assert(dict != null, $"Unknown component: {typeof(T).Name}");
            return dict.TryGetValue(uid, out var comp) && !comp.Deleted;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent<T>([NotNullWhen(true)] EntityUid? uid) where T : IComponent
        {
            return uid.HasValue && HasComponent<T>(uid.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent(EntityUid uid, ComponentRegistration reg)
        {
            var dict = _entTraitArray[reg.Idx.Value];
            return dict.TryGetValue(uid, out var comp) && !comp.Deleted;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent(EntityUid uid, Type type)
        {
            var dict = _entTraitDict[type];
            return dict.TryGetValue(uid, out var comp) && !comp.Deleted;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent([NotNullWhen(true)] EntityUid? uid, Type type)
        {
            if (!uid.HasValue)
            {
                return false;
            }

            var dict = _entTraitDict[type];
            return dict.TryGetValue(uid.Value, out var comp) && !comp.Deleted;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent(EntityUid uid, ushort netId, MetaDataComponent? meta = null)
        {
            if (!MetaQuery.Resolve(uid, ref meta))
                return false;

            return meta.NetComponents.ContainsKey(netId);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent([NotNullWhen(true)] EntityUid? uid, ushort netId, MetaDataComponent? meta = null)
        {
            if (!uid.HasValue)
            {
                DebugTools.AssertNull(meta);
                return false;
            }

            return HasComponent(uid.Value, netId, meta);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EnsureComponent<T>(EntityUid uid) where T : IComponent, new()
        {
            if (TryGetComponent<T>(uid, out var component))
            {
                // Check for deferred component removal.
                if (component.LifeStage <= ComponentLifeStage.Running)
                    return component;
                RemoveComponent(uid, component);
            }

            return AddComponent<T>(uid);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EnsureComponent<T>(ref Entity<T?> entity) where T : IComponent, new()
        {
            if (entity.Comp != null)
            {
                // Check for deferred component removal.
                if (entity.Comp.LifeStage <= ComponentLifeStage.Running)
                {
                    DebugTools.AssertOwner(entity, entity.Comp);
                    return true;
                }

                RemoveComponent(entity, entity.Comp);
            }

            entity.Comp = AddComponent<T>(entity);
            return false;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EnsureComponent<T>(EntityUid entity, out T component) where T : IComponent, new()
        {
            if (TryGetComponent<T>(entity, out var comp))
            {
                // Check for deferred component removal.
                if (comp.LifeStage <= ComponentLifeStage.Running)
                {
                    component = comp;
                    return true;
                }

                RemoveComponent(entity, comp);
            }

            component = AddComponent<T>(entity);
            return false;
        }

        /// <inheritdoc />
        [Pure]
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(EntityUid uid) where T : IComponent
        {
            var dict = _entTraitArray[CompIdx.ArrayIndex<T>()];
            DebugTools.Assert(dict != null, $"Unknown component: {typeof(T).Name}");
            if (dict.TryGetValue(uid, out var comp))
            {
                if (!comp.Deleted)
                {
                    return (T)comp;
                }
            }

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {typeof(T)}");
        }

        [Pure]
        public IComponent GetComponent(EntityUid uid, CompIdx type)
        {
            var dict = _entTraitArray[type.Value];
            if (dict.TryGetValue(uid, out var comp))
            {
                if (!comp.Deleted)
                    return comp;
            }

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {_componentFactory.IdxToType(type)}");
        }

        /// <inheritdoc />
        [Pure]
        public IComponent GetComponent(EntityUid uid, Type type)
        {
            // ReSharper disable once InvertIf
            var dict = _entTraitDict[type];
            if (dict.TryGetValue(uid, out var comp))
            {
                if (!comp.Deleted)
                {
                    return comp;
                }
            }

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {type}");
        }

        /// <inheritdoc />
        [Pure]
        public IComponent GetComponent(EntityUid uid, ushort netId, MetaDataComponent? meta = null)
        {
            return (meta ?? MetaQuery.GetComponentInternal(uid)).NetComponents[netId];
        }

        /// <inheritdoc />
        [Pure]
        public IComponent GetComponentInternal(EntityUid uid, CompIdx type)
        {
            var dict = _entTraitArray[type.Value];
            if (dict.TryGetValue(uid, out var comp))
            {
                return comp;
            }

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {type}");
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponent<T>(EntityUid uid, [NotNullWhen(true)] out T? component) where T : IComponent?
        {
            var dict = _entTraitArray[CompIdx.ArrayIndex<T>()];
            DebugTools.Assert(dict != null, $"Unknown component: {typeof(T).Name}");
            if (dict.TryGetValue(uid, out var comp))
            {
                if (!comp.Deleted)
                {
                    component = (T)comp;
                    return true;
                }
            }

            component = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent<T>([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out T? component) where T : IComponent?
        {
            if (!uid.HasValue)
            {
                component = default!;
                return false;
            }

            if (TryGetComponent(uid.Value, typeof(T), out var comp))
            {
                if (!comp.Deleted)
                {
                    component = (T)comp;
                    return true;
                }
            }

            component = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent(EntityUid uid, ComponentRegistration reg, [NotNullWhen(true)] out IComponent? component)
        {
            var dict = _entTraitArray[reg.Idx.Value];
            if (dict.TryGetValue(uid, out var comp))
            {
                if (!comp.Deleted)
                {
                    component = comp;
                    return true;
                }
            }

            component = null;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent(EntityUid uid, Type type, [NotNullWhen(true)] out IComponent? component)
        {
            var dict = _entTraitDict[type];
            if (dict.TryGetValue(uid, out var comp))
            {
                if (!comp.Deleted)
                {
                    component = comp;
                    return true;
                }
            }

            component = null;
            return false;
        }

        public bool TryGetComponent(EntityUid uid, CompIdx type, [NotNullWhen(true)] out IComponent? component)
        {
            var dict = _entTraitArray[type.Value];
            if (dict.TryGetValue(uid, out var comp))
            {
                if (!comp.Deleted)
                {
                    component = comp;
                    return true;
                }
            }

            component = null;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent([NotNullWhen(true)] EntityUid? uid, Type type,
            [NotNullWhen(true)] out IComponent? component)
        {
            if (!uid.HasValue)
            {
                component = null;
                return false;
            }

            var dict = _entTraitDict[type];
            if (dict.TryGetValue(uid.Value, out var comp))
            {
                if (!comp.Deleted)
                {
                    component = comp;
                    return true;
                }
            }

            component = null;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent(EntityUid uid, ushort netId, [MaybeNullWhen(false)] out IComponent component, MetaDataComponent? meta = null)
        {
            if (MetaQuery.TryGetComponentInternal(uid, out var metadata)
                && metadata.NetComponents.TryGetValue(netId, out var comp))
            {
                component = comp;
                return true;
            }

            component = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent([NotNullWhen(true)] EntityUid? uid, ushort netId,
            [MaybeNullWhen(false)] out IComponent component, MetaDataComponent? meta = null)
        {
            if (!uid.HasValue)
            {
                DebugTools.AssertNull(meta);
                component = default;
                return false;
            }

            return TryGetComponent(uid.Value, netId, out component, meta);
        }

        /// <inheritdoc/>
        public bool TryCopyComponent<T>(EntityUid source, EntityUid target, ref T? sourceComponent, [NotNullWhen(true)] out T? targetComp, MetaDataComponent? meta = null) where T : IComponent
        {
            if (!MetaQuery.Resolve(target, ref meta))
            {
                targetComp = default;
                return false;
            }

            if (sourceComponent == null && !TryGetComponent(source, out sourceComponent))
            {
                targetComp = default;
                return false;
            }

            targetComp = CopyComponentInternal(source, target, sourceComponent, meta);
            return true;
        }

        /// <inheritdoc/>
        public bool TryCopyComponents(
            EntityUid source,
            EntityUid target,
            MetaDataComponent? meta = null,
            params Type[] sourceComponents)
        {
            if (!MetaQuery.TryGetComponent(target, out meta))
                return false;

            var allCopied = true;

            foreach (var type in sourceComponents)
            {
                if (!TryGetComponent(source, type, out var srcComp))
                {
                    allCopied = false;
                    continue;
                }

                CopyComponent(source, target, srcComp, meta: meta);
            }

            return allCopied;
        }

        /// <inheritdoc/>
        public IComponent CopyComponent(EntityUid source, EntityUid target, IComponent sourceComponent, MetaDataComponent? meta = null)
        {
            if (!MetaQuery.Resolve(target, ref meta))
            {
                throw new InvalidOperationException();
            }

            return CopyComponentInternal(source, target, sourceComponent, meta);
        }

        /// <inheritdoc/>
        public T CopyComponent<T>(EntityUid source, EntityUid target, T sourceComponent,MetaDataComponent? meta = null) where T : IComponent
        {
            if (!MetaQuery.Resolve(target, ref meta))
            {
                throw new InvalidOperationException();
            }

            return CopyComponentInternal(source, target, sourceComponent, meta);
        }

        /// <inheritdoc/>
        public void CopyComponents(EntityUid source, EntityUid target, MetaDataComponent? meta = null, params IComponent[] sourceComponents)
        {
            if (!MetaQuery.Resolve(target, ref meta))
                return;

            foreach (var comp in sourceComponents)
            {
                CopyComponentInternal(source, target, comp, meta);
            }
        }

        private T CopyComponentInternal<T>(EntityUid source, EntityUid target, T sourceComponent, MetaDataComponent meta) where T : IComponent
        {
            var compReg = ComponentFactory.GetRegistration(sourceComponent.GetType());
            var component = (T)ComponentFactory.GetComponent(compReg);

            _serManager.CopyTo(sourceComponent, ref component, notNullableOverride: true);
            component.Owner = target;

            AddComponentInternal(target, component, compReg, true, false, meta);
            return component;
        }

        public EntityQuery<TComp1> GetEntityQuery<TComp1>() where TComp1 : IComponent
        {
            var comps = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            DebugTools.Assert(comps != null, $"Unknown component: {typeof(TComp1).Name}");
            return new EntityQuery<TComp1>(comps, _resolveSawmill);
        }

        public EntityQuery<IComponent> GetEntityQuery(Type type)
        {
            var comps = _entTraitDict[type];
            DebugTools.Assert(comps != null, $"Unknown component: {type.Name}");
            return new EntityQuery<IComponent>(comps, _resolveSawmill);
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetComponents(EntityUid uid)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (var comp in _entCompIndex[uid].ToArray())
            {
                if (comp.Deleted) continue;

                yield return comp;
            }
        }

        /// <summary>
        /// Internal variant of <see cref="GetComponents"/> that directly returns the actual component set.
        /// </summary>
        internal IReadOnlyCollection<IComponent> GetComponentsInternal(EntityUid uid) => _entCompIndex[uid];

        /// <inheritdoc />
        public int ComponentCount(EntityUid uid)
        {
            var comps = _entCompIndex[uid];
            return comps.Count;
        }

        /// <summary>
        /// Copy the components for an entity into the given span,
        /// or re-allocate the span as an array if there's not enough space.º
        /// </summary>
        private void CopyComponentsInto(ref Span<IComponent?> comps, EntityUid uid)
        {
            var set = _entCompIndex[uid];
            if (set.Count > comps.Length)
            {
                comps = new IComponent[set.Count];
            }

            var i = 0;
            foreach (var c in set)
            {
                comps[i++] = c;
            }
        }

        /// <inheritdoc />
        public IEnumerable<T> GetComponents<T>(EntityUid uid)
        {
            var comps = _entCompIndex[uid].ToArray();
            foreach (var comp in comps)
            {
                if (comp.Deleted || comp is not T tComp) continue;

                yield return tComp;
            }
        }

        /// <inheritdoc />
        public NetComponentEnumerable GetNetComponents(EntityUid uid, MetaDataComponent? meta = null)
        {
            meta ??= MetaQuery.GetComponentInternal(uid);
            return new NetComponentEnumerable(meta.NetComponents);
        }

        /// <inheritdoc />
        public NetComponentEnumerable? GetNetComponentsOrNull(EntityUid uid, MetaDataComponent? meta = null)
        {
            return MetaQuery.Resolve(uid, ref meta)
                ? new NetComponentEnumerable(meta.NetComponents)
                : null;
        }

        #region Join Functions

        public (EntityUid Uid, T Component)[] AllComponents<T>() where T : IComponent
        {
            var query = AllEntityQueryEnumerator<T>();
            var comps = new (EntityUid Uid, T Component)[Count<T>()];
            var i = 0;

            while (query.MoveNext(out var uid, out var comp))
            {
                comps[i] = (uid, comp);
                i++;
            }

            // Count<T> includes "deleted" components that are not returned by MoveNext()
            // This ensures that we dont return an array with empty/invalid entries
            Array.Resize(ref comps, i);
            return comps;
        }

        public Entity<T>[] AllEntities<T>() where T : IComponent
        {
            var query = AllEntityQueryEnumerator<T>();
            var comps = new Entity<T>[Count<T>()];
            var i = 0;

            while (query.MoveNext(out var uid, out var comp))
            {
                comps[i++] = (uid, comp);
            }

            // Count<T> includes "deleted" components that are not returned by MoveNext()
            // This ensures that we dont return an array with empty/invalid entries
            Array.Resize(ref comps, i);
            return comps;
        }

        public Entity<IComponent>[] AllEntities(Type tComp)
        {
            var query = AllEntityQueryEnumerator(tComp);
            var comps = new Entity<IComponent>[Count(tComp)];
            var i = 0;

            while (query.MoveNext(out var uid, out var comp))
            {
                comps[i++] = (uid, comp);
            }

            // Count() includes "deleted" components that are not returned by MoveNext()
            // This ensures that we dont return an array with empty/invalid entries
            Array.Resize(ref comps, i);
            return comps;
        }


        public EntityUid[] AllEntityUids<T>() where T : IComponent
        {
            var query = AllEntityQueryEnumerator<T>();
            var comps = new EntityUid[Count<T>()];
            var i = 0;

            while (query.MoveNext(out var uid, out _))
            {
                comps[i++] = uid;
            }

            // Count<T> includes "deleted" components that are not returned by MoveNext()
            // This ensures that we dont return an array with empty/invalid entries
            Array.Resize(ref comps, i);
            return comps;
        }

        public EntityUid[] AllEntityUids(Type tComp)
        {
            var query = AllEntityQueryEnumerator(tComp);
            var comps = new EntityUid[Count(tComp)];
            var i = 0;

            while (query.MoveNext(out var uid, out _))
            {
                comps[i++] = uid;
            }

            // Count() includes "deleted" components that are not returned by MoveNext()
            // This ensures that we dont return an array with empty/invalid entries
            Array.Resize(ref comps, i);
            return comps;
        }

        public List<(EntityUid Uid, T Component)> AllComponentsList<T>() where T : IComponent
        {
            var query = AllEntityQueryEnumerator<T>();
            var comps = new List<(EntityUid Uid, T Component)>(Count<T>());

            while (query.MoveNext(out var uid, out var comp))
            {
                comps.Add((uid, comp));
            }

            return comps;
        }

        /// <inheritdoc />
        public ComponentQueryEnumerator ComponentQueryEnumerator(ComponentRegistry registry)
        {
            if (registry.Count == 0)
            {
                return new ComponentQueryEnumerator(new Dictionary<EntityUid, IComponent>());
            }

            var comp1 = registry.First().Value;
            var trait1 = _entTraitArray[_componentFactory.GetArrayIndex(comp1.Component.GetType())];

            return new ComponentQueryEnumerator(trait1);
        }

        /// <inheritdoc />
        public CompRegistryEntityEnumerator CompRegistryQueryEnumerator(ComponentRegistry registry)
        {
            if (registry.Count == 0)
            {
                return new CompRegistryEntityEnumerator(this, new Dictionary<EntityUid, IComponent>(), registry);
            }

            var comp1 = registry.First().Value;
            var trait1 = _entTraitArray[_componentFactory.GetArrayIndex(comp1.Component.GetType())];

            return new CompRegistryEntityEnumerator(this, trait1, registry);
        }

        public AllEntityQueryEnumerator<IComponent> AllEntityQueryEnumerator(Type comp)
        {
            DebugTools.Assert(comp.IsAssignableTo(typeof(IComponent)));
            var trait = _entTraitArray[_componentFactory.GetIndex(comp).Value];
            return new AllEntityQueryEnumerator<IComponent>(trait);
        }

        public AllEntityQueryEnumerator<TComp1> AllEntityQueryEnumerator<TComp1>()
        where TComp1 : IComponent
        {
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            return new AllEntityQueryEnumerator<TComp1>(trait1);
        }

        public AllEntityQueryEnumerator<TComp1, TComp2> AllEntityQueryEnumerator<TComp1, TComp2>()
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            var trait2 = _entTraitArray[CompIdx.ArrayIndex<TComp2>()];
            return new AllEntityQueryEnumerator<TComp1, TComp2>(trait1, trait2);
        }

        public AllEntityQueryEnumerator<TComp1, TComp2, TComp3> AllEntityQueryEnumerator<TComp1, TComp2, TComp3>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            var trait2 = _entTraitArray[CompIdx.ArrayIndex<TComp2>()];
            var trait3 = _entTraitArray[CompIdx.ArrayIndex<TComp3>()];
            return new AllEntityQueryEnumerator<TComp1, TComp2, TComp3>(trait1, trait2, trait3);
        }

        public AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4> AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent
        {
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            var trait2 = _entTraitArray[CompIdx.ArrayIndex<TComp2>()];
            var trait3 = _entTraitArray[CompIdx.ArrayIndex<TComp3>()];
            var trait4 = _entTraitArray[CompIdx.ArrayIndex<TComp4>()];
            return new AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>(trait1, trait2, trait3, trait4);
        }

        public EntityQueryEnumerator<TComp1> EntityQueryEnumerator<TComp1>()
            where TComp1 : IComponent
        {
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            return new EntityQueryEnumerator<TComp1>(trait1, MetaQuery);
        }

        public EntityQueryEnumerator<TComp1, TComp2> EntityQueryEnumerator<TComp1, TComp2>()
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            var trait2 = _entTraitArray[CompIdx.ArrayIndex<TComp2>()];
            return new EntityQueryEnumerator<TComp1, TComp2>(trait1, trait2, MetaQuery);
        }

        public EntityQueryEnumerator<TComp1, TComp2, TComp3> EntityQueryEnumerator<TComp1, TComp2, TComp3>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            var trait2 = _entTraitArray[CompIdx.ArrayIndex<TComp2>()];
            var trait3 = _entTraitArray[CompIdx.ArrayIndex<TComp3>()];
            return new EntityQueryEnumerator<TComp1, TComp2, TComp3>(trait1, trait2, trait3, MetaQuery);
        }

        public EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4> EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent
        {
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            var trait2 = _entTraitArray[CompIdx.ArrayIndex<TComp2>()];
            var trait3 = _entTraitArray[CompIdx.ArrayIndex<TComp3>()];
            var trait4 = _entTraitArray[CompIdx.ArrayIndex<TComp4>()];

            return new EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>(trait1, trait2, trait3, trait4, MetaQuery);
        }

        /// <inheritdoc />
        public IEnumerable<T> EntityQuery<T>(bool includePaused = false) where T : IComponent
        {
            var comps = _entTraitArray[CompIdx.ArrayIndex<T>()];
            DebugTools.Assert(comps != null, $"Unknown component: {typeof(T).Name}");

            if (includePaused)
            {
                foreach (var t1Comp in comps.Values)
                {
                    if (t1Comp.Deleted) continue;

                    yield return (T)t1Comp;
                }
            }
            else
            {
                foreach (var (uid, t1Comp) in comps)
                {
                    if (t1Comp.Deleted || !MetaQuery.TryGetComponentInternal(uid, out var metaComp)) continue;

                    if (metaComp.EntityPaused) continue;

                    yield return (T)t1Comp;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2)> EntityQuery<TComp1, TComp2>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            // this would prob be faster if trait1 was a list (or an array of structs hue).
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            var trait2 = _entTraitArray[CompIdx.ArrayIndex<TComp2>()];

            // you really want trait1 to be the smaller set of components
            if (includePaused)
            {
                foreach (var (uid, t1Comp) in trait1)
                {
                    if (!trait2.TryGetValue(uid, out var t2Comp) || t2Comp.Deleted)
                        continue;

                    yield return (
                        (TComp1) t1Comp,
                        (TComp2) t2Comp);
                }
            }
            else
            {
                var metaComps = _entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()];

                foreach (var (uid, t1Comp) in trait1)
                {
                    // Check paused last because 90% of the time the component's likely not gonna be paused.
                    if (!trait2.TryGetValue(uid, out var t2Comp) || t2Comp.Deleted)
                        continue;

                    if (t1Comp.Deleted || !metaComps.TryGetValue(uid, out var metaComp)) continue;

                    var meta = (MetaDataComponent)metaComp;

                    if (meta.EntityPaused) continue;

                    yield return (
                        (TComp1) t1Comp,
                        (TComp2) t2Comp);
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2, TComp3)> EntityQuery<TComp1, TComp2, TComp3>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            var trait2 = _entTraitArray[CompIdx.ArrayIndex<TComp2>()];
            var trait3 = _entTraitArray[CompIdx.ArrayIndex<TComp3>()];

            if (includePaused)
            {
                foreach (var (uid, t1Comp) in trait1)
                {
                    if (!trait2.TryGetValue(uid, out var t2Comp) || t2Comp.Deleted)
                        continue;

                    if (!trait3.TryGetValue(uid, out var t3Comp) || t3Comp.Deleted)
                        continue;

                    yield return (
                        (TComp1) t1Comp,
                        (TComp2) t2Comp,
                        (TComp3) t3Comp);
                }
            }
            else
            {
                var metaComps = _entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()];

                foreach (var (uid, t1Comp) in trait1)
                {
                    // Check paused last because 90% of the time the component's likely not gonna be paused.
                    if (!trait2.TryGetValue(uid, out var t2Comp) || t2Comp.Deleted)
                        continue;

                    if (!trait3.TryGetValue(uid, out var t3Comp) || t3Comp.Deleted)
                        continue;

                    if (t1Comp.Deleted || !metaComps.TryGetValue(uid, out var metaComp)) continue;

                    var meta = (MetaDataComponent)metaComp;

                    if (meta.EntityPaused) continue;

                    yield return (
                        (TComp1) t1Comp,
                        (TComp2) t2Comp,
                        (TComp3) t3Comp);
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2, TComp3, TComp4)> EntityQuery<TComp1, TComp2, TComp3, TComp4>(
            bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent
        {
            var trait1 = _entTraitArray[CompIdx.ArrayIndex<TComp1>()];
            var trait2 = _entTraitArray[CompIdx.ArrayIndex<TComp2>()];
            var trait3 = _entTraitArray[CompIdx.ArrayIndex<TComp3>()];
            var trait4 = _entTraitArray[CompIdx.ArrayIndex<TComp4>()];

            if (includePaused)
            {
                foreach (var (uid, t1Comp) in trait1)
                {
                    if (!trait2.TryGetValue(uid, out var t2Comp) || t2Comp.Deleted)
                        continue;

                    if (!trait3.TryGetValue(uid, out var t3Comp) || t3Comp.Deleted)
                        continue;

                    if (!trait4.TryGetValue(uid, out var t4Comp) || t4Comp.Deleted)
                        continue;

                    yield return (
                        (TComp1) t1Comp,
                        (TComp2) t2Comp,
                        (TComp3) t3Comp,
                        (TComp4) t4Comp);
                }
            }
            else
            {
                var metaComps = _entTraitArray[CompIdx.ArrayIndex<MetaDataComponent>()];

                foreach (var (uid, t1Comp) in trait1)
                {
                    // Check paused last because 90% of the time the component's likely not gonna be paused.
                    if (!trait2.TryGetValue(uid, out var t2Comp) || t2Comp.Deleted)
                        continue;

                    if (!trait3.TryGetValue(uid, out var t3Comp) || t3Comp.Deleted)
                        continue;

                    if (!trait4.TryGetValue(uid, out var t4Comp) || t4Comp.Deleted)
                        continue;

                    if (t1Comp.Deleted || !metaComps.TryGetValue(uid, out var metaComp)) continue;

                    var meta = (MetaDataComponent)metaComp;

                    if (meta.EntityPaused) continue;

                    yield return (
                        (TComp1) t1Comp,
                        (TComp2) t2Comp,
                        (TComp3) t3Comp,
                        (TComp4) t4Comp);
                }
            }
        }

        #endregion

        /// <inheritdoc />
        public IEnumerable<(EntityUid Uid, IComponent Component)> GetAllComponents(Type type, bool includePaused = false)
        {
            var comps = _entTraitDict[type];

            if (includePaused)
            {
                foreach (var (uid, comp) in comps)
                {
                    if (comp.Deleted) continue;

                    yield return (uid, comp);
                }
            }
            else
            {
                foreach (var (uid, comp) in comps)
                {
                    if (comp.Deleted || !MetaQuery.TryGetComponent(uid, out var meta) || meta.EntityPaused) continue;

                    yield return (uid, comp);
                }
            }
        }

        /// <inheritdoc />
        [Pure]
        public IComponentState? GetComponentState(IEventBus eventBus, IComponent component, ICommonSession? session, GameTick fromTick)
        {
            DebugTools.Assert(component.NetSyncEnabled, $"Attempting to get component state for an un-synced component: {component.GetType()}");
            var getState = new ComponentGetState(session, fromTick);
            eventBus.RaiseComponentEvent(component.Owner, component, ref getState);

            return getState.State;
        }

        public bool CanGetComponentState(IEventBus eventBus, IComponent component, ICommonSession player)
        {
            var attempt = new ComponentGetStateAttemptEvent(player);
            eventBus.RaiseComponentEvent(component.Owner, component, ref attempt);
            return !attempt.Cancelled;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FillComponentDict()
        {
            _entTraitDict = FrozenDictionary<Type, Dictionary<EntityUid, IComponent>>.Empty;
            Array.Fill(_entTraitArray, null);
            RegisterComponents(_componentFactory.GetAllRegistrations());
        }
    }

    public readonly struct NetComponentEnumerable
    {
        private readonly Dictionary<ushort, IComponent> _dictionary;

        public NetComponentEnumerable(Dictionary<ushort, IComponent> dictionary) => _dictionary = dictionary;
        public NetComponentEnumerator GetEnumerator() => new(_dictionary);
    }

    public struct NetComponentEnumerator
    {
        // DO NOT MAKE THIS READONLY
        private Dictionary<ushort, IComponent>.Enumerator _dictEnum;

        public NetComponentEnumerator(Dictionary<ushort, IComponent> dictionary) =>
            _dictEnum = dictionary.GetEnumerator();

        public bool MoveNext() => _dictEnum.MoveNext();

        public (ushort netId, IComponent component) Current
        {
            get
            {
                var val = _dictEnum.Current;
                return (val.Key, val.Value);
            }
        }
    }

    public readonly struct EntityQuery<TComp1> where TComp1 : IComponent
    {
        private readonly Dictionary<EntityUid, IComponent> _traitDict;
        private readonly ISawmill _sawmill;

        public EntityQuery(Dictionary<EntityUid, IComponent> traitDict, ISawmill sawmill)
        {
            _traitDict = traitDict;
            _sawmill = sawmill;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public TComp1 GetComponent(EntityUid uid)
        {
            if (_traitDict.TryGetValue(uid, out var comp) && !comp.Deleted)
                return (TComp1) comp;

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {typeof(TComp1)}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public Entity<TComp1> Get(EntityUid uid)
        {
            if (_traitDict.TryGetValue(uid, out var comp) && !comp.Deleted)
                return new Entity<TComp1>(uid, (TComp1) comp);

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {typeof(TComp1)}");
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool TryGetComponent([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out TComp1? component)
        {
            if (uid == null)
            {
                component = default;
                return false;
            }

            return TryGetComponent(uid.Value, out component);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool TryGetComponent(EntityUid uid, [NotNullWhen(true)] out TComp1? component)
        {
            if (_traitDict.TryGetValue(uid, out var comp) && !comp.Deleted)
            {
                component = (TComp1) comp;
                return true;
            }

            component = default;
            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool TryComp(EntityUid uid, [NotNullWhen(true)] out TComp1? component)
            => TryGetComponent(uid, out component);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool TryComp([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out TComp1? component)
            => TryGetComponent(uid, out component);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComp(EntityUid uid) => HasComponent(uid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComp([NotNullWhen(true)] EntityUid? uid) => HasComponent(uid);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent(EntityUid uid)
        {
            return _traitDict.TryGetValue(uid, out var comp) && !comp.Deleted;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent([NotNullWhen(true)] EntityUid? uid)
        {
            return uid != null && HasComponent(uid.Value);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Resolve(EntityUid uid, [NotNullWhen(true)] ref TComp1? component, bool logMissing = true)
        {
            if (component != null)
            {
                DebugTools.AssertOwner(uid, component);
                return true;
            }

            if (_traitDict.TryGetValue(uid, out var comp) && !comp.Deleted)
            {
                component = (TComp1)comp;
                return true;
            }

            if (logMissing)
            {
                _sawmill.Error($"Can't resolve \"{typeof(TComp1)}\" on entity {uid}!\n{Environment.StackTrace}");
            }

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool Resolve(ref Entity<TComp1?> entity, bool logMissing = true)
        {
            return Resolve(entity.Owner, ref entity.Comp, logMissing);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public TComp1? CompOrNull(EntityUid uid)
        {
            if (TryGetComponent(uid, out var comp))
                return comp;

            return default;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public TComp1 Comp(EntityUid uid)
        {
            return GetComponent(uid);
        }

        #region Internal

        /// <summary>
        /// Elides the component.Deleted check of <see cref="GetComponent"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        internal TComp1 GetComponentInternal(EntityUid uid)
        {
            if (_traitDict.TryGetValue(uid, out var comp))
                return (TComp1) comp;

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {typeof(TComp1)}");
        }

        /// <summary>
        /// Elides the component.Deleted check of <see cref="TryGetComponent(System.Nullable{Robust.Shared.GameObjects.EntityUid},out TComp1?)"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        internal bool TryGetComponentInternal([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out TComp1? component)
        {
            if (uid == null)
            {
                component = default;
                return false;
            }

            return TryGetComponentInternal(uid.Value, out component);
        }

        /// <summary>
        /// Elides the component.Deleted check of <see cref="TryGetComponent(System.Nullable{Robust.Shared.GameObjects.EntityUid},out TComp1?)"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        internal bool TryGetComponentInternal(EntityUid uid, [NotNullWhen(true)] out TComp1? component)
        {
            if (_traitDict.TryGetValue(uid, out var comp))
            {
                component = (TComp1) comp;
                return true;
            }

            component = default;
            return false;
        }

        /// <summary>
        /// Elides the component.Deleted check of <see cref="HasComponent(EntityUid)"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        internal bool HasComponentInternal(EntityUid uid)
        {
            return _traitDict.TryGetValue(uid, out var comp) && !comp.Deleted;
        }

        /// <summary>
        /// Elides the component.Deleted check of <see cref="Resolve"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        internal bool ResolveInternal(EntityUid uid, [NotNullWhen(true)] ref TComp1? component, bool logMissing = true)
        {
            if (component != null)
            {
                DebugTools.AssertOwner(uid, component);
                return true;
            }

            if (_traitDict.TryGetValue(uid, out var comp))
            {
                component = (TComp1)comp;
                return true;
            }

            if (logMissing)
                _sawmill.Error($"Can't resolve \"{typeof(TComp1)}\" on entity {uid}!\n{new StackTrace(1, true)}");

            return false;
        }
        /// <summary>
        /// Elides the component.Deleted check of <see cref="CompOrNull"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        internal TComp1? CompOrNullInternal(EntityUid uid)
        {
            if (TryGetComponent(uid, out var comp))
                return comp;

            return default;
        }

        #endregion
    }

    #region ComponentRegistry Query

    /// <summary>
    /// Returns entities that match the ComponentRegistry.
    /// </summary>
    public struct CompRegistryEntityEnumerator : IDisposable
    {
        private IEntityManager _entManager;

        private Dictionary<EntityUid, IComponent>.Enumerator _traitDict;
        private ComponentRegistry _registry;

        public CompRegistryEntityEnumerator(
            IEntityManager entManager,
            Dictionary<EntityUid, IComponent> traitDict, ComponentRegistry registry)
        {
            _entManager = entManager;
            _traitDict = traitDict.GetEnumerator();
            _registry = registry;
        }

        public bool MoveNext(out EntityUid uid)
        {
            while (true)
            {
                if (!_traitDict.MoveNext())
                {
                    uid = default;
                    return false;
                }

                var current = _traitDict.Current;

                if (current.Value.Deleted)
                {
                    continue;
                }

                var idx = -1;
                var found = true;

                foreach (var comp in _registry)
                {
                    idx++;

                    // First one is us
                    if (idx == 0)
                        continue;

                    if (!_entManager.TryGetComponent(current.Key, comp.Value.Component.GetType(), out var nextComp) ||
                        nextComp.Deleted)
                    {
                        found = false;
                        break;
                    }
                }

                if (!found)
                    continue;

                uid = current.Key;
                return true;
            }
        }

        public void Dispose()
        {
            _traitDict.Dispose();
        }
    }

    /// <summary>
    /// Non-generic version of <see cref="AllEntityQueryEnumerator{TComp1}"/>
    /// </summary>
    public struct ComponentQueryEnumerator : IDisposable
    {
        private Dictionary<EntityUid, IComponent>.Enumerator _traitDict;

        public ComponentQueryEnumerator(
            Dictionary<EntityUid, IComponent> traitDict)
        {
            _traitDict = traitDict.GetEnumerator();
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out IComponent? comp1)
        {
            while (true)
            {
                if (!_traitDict.MoveNext())
                {
                    uid = default;
                    comp1 = default;
                    return false;
                }

                var current = _traitDict.Current;

                if (current.Value.Deleted)
                {
                    continue;
                }

                uid = current.Key;
                comp1 = current.Value;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext([NotNullWhen(true)] out IComponent? comp1)
        {
            return MoveNext(out _, out comp1);
        }

        public void Dispose()
        {
            _traitDict.Dispose();
        }
    }
    #endregion

    #region Query

    /// <summary>
    /// Returns all matching unpaused components.
    /// </summary>
    public struct EntityQueryEnumerator<TComp1> : IDisposable
        where TComp1 : IComponent
    {
        private Dictionary<EntityUid, IComponent>.Enumerator _traitDict;
        private readonly EntityQuery<MetaDataComponent> _metaQuery;

        public EntityQueryEnumerator(
            Dictionary<EntityUid, IComponent> traitDict,
            EntityQuery<MetaDataComponent> metaQuery)
        {
            _traitDict = traitDict.GetEnumerator();
            _metaQuery = metaQuery;
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1)
        {
            while (true)
            {
                if (!_traitDict.MoveNext())
                {
                    uid = default;
                    comp1 = default;
                    return false;
                }

                var current = _traitDict.Current;

                if (current.Value.Deleted)
                {
                    continue;
                }

                if (!_metaQuery.TryGetComponentInternal(current.Key, out var metaComp) || metaComp.EntityPaused)
                {
                    continue;
                }

                uid = current.Key;
                comp1 = (TComp1)current.Value;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext([NotNullWhen(true)] out TComp1? comp1)
        {
            return MoveNext(out _, out comp1);
        }

        public void Dispose()
        {
            _traitDict.Dispose();
        }
    }

    /// <summary>
    /// Returns all matching unpaused components.
    /// </summary>
    public struct EntityQueryEnumerator<TComp1, TComp2> : IDisposable
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        private Dictionary<EntityUid, IComponent>.Enumerator _traitDict;
        private readonly Dictionary<EntityUid, IComponent> _traitDict2;
        private readonly EntityQuery<MetaDataComponent> _metaQuery;

        public EntityQueryEnumerator(
            Dictionary<EntityUid, IComponent> traitDict,
            Dictionary<EntityUid, IComponent> traitDict2,
            EntityQuery<MetaDataComponent> metaQuery)
        {
            _traitDict = traitDict.GetEnumerator();
            _traitDict2 = traitDict2;
            _metaQuery = metaQuery;
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2)
        {
            while (true)
            {
                if (!_traitDict.MoveNext())
                {
                    uid = default;
                    comp1 = default;
                    comp2 = default;
                    return false;
                }

                var current = _traitDict.Current;

                if (current.Value.Deleted)
                {
                    continue;
                }

                if (!_metaQuery.TryGetComponentInternal(current.Key, out var metaComp) || metaComp.EntityPaused)
                {
                    continue;
                }

                if (!_traitDict2.TryGetValue(current.Key, out var comp2Obj) || comp2Obj.Deleted)
                {
                    continue;
                }

                uid = current.Key;
                comp1 = (TComp1)current.Value;
                comp2 = (TComp2)comp2Obj;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext([NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2)
        {
            return MoveNext(out _, out comp1, out comp2);
        }

        public void Dispose()
        {
            _traitDict.Dispose();
        }
    }

    /// <summary>
    /// Returns all matching unpaused components.
    /// </summary>
    public struct EntityQueryEnumerator<TComp1, TComp2, TComp3> : IDisposable
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
    {
        private Dictionary<EntityUid, IComponent>.Enumerator _traitDict;
        private readonly Dictionary<EntityUid, IComponent> _traitDict2;
        private readonly Dictionary<EntityUid, IComponent> _traitDict3;
        private readonly EntityQuery<MetaDataComponent> _metaQuery;

        public EntityQueryEnumerator(
            Dictionary<EntityUid, IComponent> traitDict,
            Dictionary<EntityUid, IComponent> traitDict2,
            Dictionary<EntityUid, IComponent> traitDict3,
            EntityQuery<MetaDataComponent> metaQuery)
        {
            _traitDict = traitDict.GetEnumerator();
            _traitDict2 = traitDict2;
            _traitDict3 = traitDict3;
            _metaQuery = metaQuery;
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3)
        {
            while (true)
            {
                if (!_traitDict.MoveNext())
                {
                    uid = default;
                    comp1 = default;
                    comp2 = default;
                    comp3 = default;
                    return false;
                }

                var current = _traitDict.Current;

                if (current.Value.Deleted)
                {
                    continue;
                }

                if (!_metaQuery.TryGetComponentInternal(current.Key, out var metaComp) || metaComp.EntityPaused)
                {
                    continue;
                }

                if (!_traitDict2.TryGetValue(current.Key, out var comp2Obj) || comp2Obj.Deleted)
                {
                    continue;
                }

                if (!_traitDict3.TryGetValue(current.Key, out var comp3Obj) || comp3Obj.Deleted)
                {
                    continue;
                }

                uid = current.Key;
                comp1 = (TComp1)current.Value;
                comp2 = (TComp2)comp2Obj;
                comp3 = (TComp3)comp3Obj;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(
            [NotNullWhen(true)] out TComp1? comp1,
            [NotNullWhen(true)] out TComp2? comp2,
            [NotNullWhen(true)] out TComp3? comp3)
        {
            return MoveNext(out _, out comp1, out comp2, out comp3);
        }

        public void Dispose()
        {
            _traitDict.Dispose();
        }
    }

    /// <summary>
    /// Returns all matching unpaused components.
    /// </summary>
    public struct EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4> : IDisposable
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent
    {
        private Dictionary<EntityUid, IComponent>.Enumerator _traitDict;
        private readonly Dictionary<EntityUid, IComponent> _traitDict2;
        private readonly Dictionary<EntityUid, IComponent> _traitDict3;
        private readonly Dictionary<EntityUid, IComponent> _traitDict4;
        private readonly EntityQuery<MetaDataComponent> _metaQuery;

        public EntityQueryEnumerator(
            Dictionary<EntityUid, IComponent> traitDict,
            Dictionary<EntityUid, IComponent> traitDict2,
            Dictionary<EntityUid, IComponent> traitDict3,
            Dictionary<EntityUid, IComponent> traitDict4,
            EntityQuery<MetaDataComponent> metaQuery)
        {
            _traitDict = traitDict.GetEnumerator();
            _traitDict2 = traitDict2;
            _traitDict3 = traitDict3;
            _traitDict4 = traitDict4;
            _metaQuery = metaQuery;
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3, [NotNullWhen(true)] out TComp4? comp4)
        {
            while (true)
            {
                if (!_traitDict.MoveNext())
                {
                    uid = default;
                    comp1 = default;
                    comp2 = default;
                    comp3 = default;
                    comp4 = default;
                    return false;
                }

                var current = _traitDict.Current;

                if (current.Value.Deleted)
                {
                    continue;
                }

                if (!_metaQuery.TryGetComponentInternal(current.Key, out var metaComp) || metaComp.EntityPaused)
                {
                    continue;
                }

                if (!_traitDict2.TryGetValue(current.Key, out var comp2Obj) || comp2Obj.Deleted)
                {
                    continue;
                }

                if (!_traitDict3.TryGetValue(current.Key, out var comp3Obj) || comp3Obj.Deleted)
                {
                    continue;
                }

                if (!_traitDict4.TryGetValue(current.Key, out var comp4Obj) || comp4Obj.Deleted)
                {
                    continue;
                }

                uid = current.Key;
                comp1 = (TComp1)current.Value;
                comp2 = (TComp2)comp2Obj;
                comp3 = (TComp3)comp3Obj;
                comp4 = (TComp4)comp4Obj;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(
            [NotNullWhen(true)] out TComp1? comp1,
            [NotNullWhen(true)] out TComp2? comp2,
            [NotNullWhen(true)] out TComp3? comp3,
            [NotNullWhen(true)] out TComp4? comp4)
        {
            return MoveNext(out _, out comp1, out comp2, out comp3, out comp4);
        }

        public void Dispose()
        {
            _traitDict.Dispose();
        }
    }

    #endregion

    #region All query

    /// <summary>
    /// Returns all matching components, paused or not.
    /// </summary>
    public struct AllEntityQueryEnumerator<TComp1> : IDisposable
        where TComp1 : IComponent
    {
        private Dictionary<EntityUid, IComponent>.Enumerator _traitDict;

        public AllEntityQueryEnumerator(
            Dictionary<EntityUid, IComponent> traitDict)
        {
            _traitDict = traitDict.GetEnumerator();
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1)
        {
            while (true)
            {
                if (!_traitDict.MoveNext())
                {
                    uid = default;
                    comp1 = default;
                    return false;
                }

                var current = _traitDict.Current;

                if (current.Value.Deleted)
                {
                    continue;
                }

                uid = current.Key;
                comp1 = (TComp1)current.Value;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext([NotNullWhen(true)] out TComp1? comp1)
        {
            return MoveNext(out _, out comp1);
        }

        public void Dispose()
        {
            _traitDict.Dispose();
        }
    }

    /// <summary>
    /// Returns all matching components, paused or not.
    /// </summary>
    public struct AllEntityQueryEnumerator<TComp1, TComp2> : IDisposable
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        private Dictionary<EntityUid, IComponent>.Enumerator _traitDict;
        private readonly Dictionary<EntityUid, IComponent> _traitDict2;

        public AllEntityQueryEnumerator(
            Dictionary<EntityUid, IComponent> traitDict,
            Dictionary<EntityUid, IComponent> traitDict2)
        {
            _traitDict = traitDict.GetEnumerator();
            _traitDict2 = traitDict2;
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2)
        {
            while (true)
            {
                if (!_traitDict.MoveNext())
                {
                    uid = default;
                    comp1 = default;
                    comp2 = default;
                    return false;
                }

                var current = _traitDict.Current;

                if (current.Value.Deleted)
                {
                    continue;
                }

                if (!_traitDict2.TryGetValue(current.Key, out var comp2Obj) || comp2Obj.Deleted)
                {
                    continue;
                }

                uid = current.Key;
                comp1 = (TComp1)current.Value;
                comp2 = (TComp2)comp2Obj;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext([NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2)
        {
            return MoveNext(out _, out comp1, out comp2);
        }

        public void Dispose()
        {
            _traitDict.Dispose();
        }
    }

    /// <summary>
    /// Returns all matching components, paused or not.
    /// </summary>
    public struct AllEntityQueryEnumerator<TComp1, TComp2, TComp3> : IDisposable
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
    {
        private Dictionary<EntityUid, IComponent>.Enumerator _traitDict;
        private readonly Dictionary<EntityUid, IComponent> _traitDict2;
        private readonly Dictionary<EntityUid, IComponent> _traitDict3;

        public AllEntityQueryEnumerator(
            Dictionary<EntityUid, IComponent> traitDict,
            Dictionary<EntityUid, IComponent> traitDict2,
            Dictionary<EntityUid, IComponent> traitDict3)
        {
            _traitDict = traitDict.GetEnumerator();
            _traitDict2 = traitDict2;
            _traitDict3 = traitDict3;
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3)
        {
            while (true)
            {
                if (!_traitDict.MoveNext())
                {
                    uid = default;
                    comp1 = default;
                    comp2 = default;
                    comp3 = default;
                    return false;
                }

                var current = _traitDict.Current;

                if (current.Value.Deleted)
                {
                    continue;
                }

                if (!_traitDict2.TryGetValue(current.Key, out var comp2Obj) || comp2Obj.Deleted)
                {
                    continue;
                }

                if (!_traitDict3.TryGetValue(current.Key, out var comp3Obj) || comp3Obj.Deleted)
                {
                    continue;
                }

                uid = current.Key;
                comp1 = (TComp1)current.Value;
                comp2 = (TComp2)comp2Obj;
                comp3 = (TComp3)comp3Obj;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(
            [NotNullWhen(true)] out TComp1? comp1,
            [NotNullWhen(true)] out TComp2? comp2,
            [NotNullWhen(true)] out TComp3? comp3)
        {
            return MoveNext(out _, out comp1, out comp2, out comp3);
        }

        public void Dispose()
        {
            _traitDict.Dispose();
        }
    }

    /// <summary>
    /// Returns all matching components, paused or not.
    /// </summary>
    public struct AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4> : IDisposable
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent
    {
        private Dictionary<EntityUid, IComponent>.Enumerator _traitDict;
        private readonly Dictionary<EntityUid, IComponent> _traitDict2;
        private readonly Dictionary<EntityUid, IComponent> _traitDict3;
        private readonly Dictionary<EntityUid, IComponent> _traitDict4;

        public AllEntityQueryEnumerator(
            Dictionary<EntityUid, IComponent> traitDict,
            Dictionary<EntityUid, IComponent> traitDict2,
            Dictionary<EntityUid, IComponent> traitDict3,
            Dictionary<EntityUid, IComponent> traitDict4)
        {
            _traitDict = traitDict.GetEnumerator();
            _traitDict2 = traitDict2;
            _traitDict3 = traitDict3;
            _traitDict4 = traitDict4;
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3, [NotNullWhen(true)] out TComp4? comp4)
        {
            while (true)
            {
                if (!_traitDict.MoveNext())
                {
                    uid = default;
                    comp1 = default;
                    comp2 = default;
                    comp3 = default;
                    comp4 = default;
                    return false;
                }

                var current = _traitDict.Current;

                if (current.Value.Deleted)
                {
                    continue;
                }

                if (!_traitDict2.TryGetValue(current.Key, out var comp2Obj) || comp2Obj.Deleted)
                {
                    continue;
                }

                if (!_traitDict3.TryGetValue(current.Key, out var comp3Obj) || comp3Obj.Deleted)
                {
                    continue;
                }

                if (!_traitDict4.TryGetValue(current.Key, out var comp4Obj) || comp4Obj.Deleted)
                {
                    continue;
                }

                uid = current.Key;
                comp1 = (TComp1)current.Value;
                comp2 = (TComp2)comp2Obj;
                comp3 = (TComp3)comp3Obj;
                comp4 = (TComp4)comp4Obj;
                return true;
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(
            [NotNullWhen(true)] out TComp1? comp1,
            [NotNullWhen(true)] out TComp2? comp2,
            [NotNullWhen(true)] out TComp3? comp3,
            [NotNullWhen(true)] out TComp4? comp4)
        {
            return MoveNext(out _, out comp1, out comp2, out comp3, out comp4);
        }

        public void Dispose()
        {
            _traitDict.Dispose();
        }
    }

    #endregion
}
