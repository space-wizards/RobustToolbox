using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Arch.Core;
using Arch.Core.Utils;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
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
        private const int EntityCapacity = 1024;
        private const int NetComponentCapacity = 8;

        private static readonly ComponentState DefaultComponentState = new();

        private readonly HashSet<IComponent> _deleteSet = new(TypeCapacity);

        /// <inheritdoc />
        public event Action<AddedComponentEventArgs>? ComponentAdded;

        /// <inheritdoc />
        public event Action<RemovedComponentEventArgs>? ComponentRemoved;

        /// <summary>
        ///     Instantly clears all components from the manager. This will NOT shut them down gracefully.
        ///     Any entities relying on existing components will be broken.
        /// </summary>
        public void ClearComponents()
        {
            _deleteSet.Clear();
        }

        #region Component Management

        /// <inheritdoc />
        public int Count<T>() where T : IComponent
        {
            return _world.CountEntities(new QueryDescription().WithAll<T>());
        }

        /// <inheritdoc />
        public int Count(Type component)
        {
            var query = new QueryDescription
            {
                All = new ComponentType[] { component }
            };
            return _world.CountEntities(in query);
        }

        public void InitializeComponents(EntityUid uid, MetaDataComponent? metadata = null)
        {
            DebugTools.AssertOwner(uid, metadata);
            metadata ??= GetComponent<MetaDataComponent>(uid);
            DebugTools.Assert(metadata.EntityLifeStage == EntityLifeStage.PreInit);
            metadata.EntityLifeStage = EntityLifeStage.Initializing;

            // Initialize() can modify the collection of components. Copy them.
            FixedArray32<IComponent?> compsFixed = default;

            var comps = compsFixed.AsSpan;
            CopyComponentsInto(ref comps, uid);

            foreach (var comp in comps)
            {
                if (comp is { LifeStage:  ComponentLifeStage.Added })
                    LifeInitialize(comp, CompIdx.Index(comp.GetType()));
            }

            DebugTools.Assert(metadata.EntityLifeStage == EntityLifeStage.Initializing);
            metadata.EntityLifeStage = EntityLifeStage.Initialized;
        }

        public void StartComponents(EntityUid uid)
        {
            // Startup() can modify _components
            // This code can only handle additions to the list. Is there a better way? Probably not.
            FixedArray32<IComponent?> compsFixed = default;

            var comps = compsFixed.AsSpan;
            CopyComponentsInto(ref comps, uid);

            // TODO: please for the love of god remove these initialization order hacks.

            // Init transform first, we always have it.
            var transform = GetComponent<TransformComponent>(uid);
            if (transform.LifeStage == ComponentLifeStage.Initialized)
                LifeStartup(transform);

            // Init physics second if it exists.
            if (TryGetComponent<PhysicsComponent>(uid, out var phys)
                && phys.LifeStage == ComponentLifeStage.Initialized)
            {
                LifeStartup(phys);
            }

            // Do rest of components.
            foreach (var comp in comps)
            {
                if (comp is { LifeStage: ComponentLifeStage.Initialized })
                    LifeStartup(comp);
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
                    ((EntityManager) _entMan).LifeInitialize(Comp, CompType);

                if (metadata.EntityInitialized && !Comp.Running)
                    ((EntityManager) _entMan).LifeStartup(Comp);
            }

            public static implicit operator T(CompInitializeHandle<T> handle)
            {
                return handle.Comp;
            }
        }

        /// <inheritdoc />
        [Obsolete]
        public CompInitializeHandle<T> AddComponentUninitialized<T>(EntityUid uid) where T : IComponent, new()
        {
            var reg = _componentFactory.GetRegistration<T>();
            var newComponent = _componentFactory.GetComponent<T>();
#pragma warning disable CS0618 // Type or member is obsolete
            newComponent.Owner = uid;
#pragma warning restore CS0618 // Type or member is obsolete

            if (!uid.IsValid() || !EntityExists(uid))
                throw new ArgumentException($"Entity {uid} is not valid.", nameof(uid));

            AddComponentInternal(uid, newComponent, false);

            return new CompInitializeHandle<T>(this, uid, newComponent, reg.Idx);
        }

        /// <inheritdoc />
        public void AddComponent<T>(EntityUid uid, T component, MetaDataComponent? metadata = null) where T : IComponent
        {
            if (!uid.IsValid() || !EntityExists(uid))
                throw new ArgumentException($"Entity {uid} is not valid.", nameof(uid));

            if (component == null) throw new ArgumentNullException(nameof(component));

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

            AddComponentInternal(uid, component, false, metadata);
        }

        private void AddComponentInternal<T>(EntityUid uid, T component, bool skipInit, MetaDataComponent? metadata = null) where T : IComponent
        {
            // get interface aliases for mapping
            var reg = _componentFactory.GetRegistration(component);
            AddComponentInternal(uid, component, reg, skipInit, metadata);
        }

        internal void AddComponentInternal<T>(EntityUid uid, T component, ComponentRegistration reg, bool skipInit, MetaDataComponent? metadata = null) where T : IComponent
        {
            // We can't use typeof(T) here in case T is just Component
            DebugTools.Assert(component is MetaDataComponent ||
                              (metadata ?? MetaQuery.GetComponent(uid)).EntityLifeStage < EntityLifeStage.Terminating,
                $"Attempted to add a {component.GetType().Name} component to an entity ({ToPrettyString(uid)}) while it is terminating");

            // We can't use typeof(T) here in case T is just Component
            DebugTools.Assert(component is MetaDataComponent ||
                (metadata ?? MetaQuery.GetComponent(uid)).EntityLifeStage < EntityLifeStage.Terminating,
                $"Attempted to add a {reg.Name} component to an entity ({ToPrettyString(uid)}) while it is terminating");

            // TODO optimize this
            // Need multi-comp adds so we can remove this call probably.
            if (!_world.Has(uid, reg.Idx.Type))
                _world.Add(uid, (object) component);
            else
            {
                // Okay so technically it may have an existing one not null but pointing to a stale component
                // hence just set it and act casual.
                _world.Set(uid, (object) component);
            }

            // add the component to the netId grid
            if (reg.NetID != null)
            {
                // the main comp grid keeps this in sync
                var netId = reg.NetID.Value;
                metadata ??= MetaQuery.GetComponentInternal(uid);
                metadata.NetComponents.Add(netId, component);
            }
            else
            {
                component.Networked = false;
            }

            var eventArgs = new AddedComponentEventArgs(new ComponentEventArgs(component, uid), reg);
            ComponentAdded?.Invoke(eventArgs);
            _eventBus.OnComponentAdded(eventArgs);

            LifeAddToEntity(component, reg.Idx);

            if (skipInit)
                return;

            metadata ??= MetaQuery.GetComponentInternal(uid);

            // Bur this overhead sucks.
            if (metadata.EntityLifeStage < EntityLifeStage.Initializing)
                return;

            if (component.Networked)
                DirtyEntity(uid, metadata);

            LifeInitialize(component, reg.Idx);

            if (metadata.EntityInitialized)
                LifeStartup(component);

            if (metadata.EntityLifeStage >= EntityLifeStage.MapInitialized)
                EventBus.RaiseComponentEvent(component, MapInitEventInstance);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent<T>(EntityUid uid, MetaDataComponent? meta = null)
        {
            return RemoveComponent(uid, typeof(T), meta);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent(EntityUid uid, Type type, MetaDataComponent? meta = null)
        {
            if (!TryGetComponent(uid, type, out var comp))
                return false;

            RemoveComponentImmediate(comp, uid, false, true, meta);
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

            RemoveComponentImmediate(comp, uid, false, true, meta);
            return true;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityUid uid, IComponent component, MetaDataComponent? meta = null)
        {
            RemoveComponentImmediate(component, uid, false, true, meta);
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
        public void RemoveComponents(EntityUid uid, MetaDataComponent? meta = null)
        {
            var objComps = _world.GetAllComponents(uid);
            // Reverse order
            for (var i = objComps.Length - 1; i >= 0; i--)
            {
                var comp = (IComponent) objComps[i]!;
                RemoveComponentImmediate(comp, uid, false, false, meta);
            }
        }

        /// <inheritdoc />
        public void DisposeComponents(EntityUid uid, MetaDataComponent? meta = null)
        {
            var objComps = _world.GetAllComponents(uid);

            // Reverse order
            for (var i = objComps.Length - 1; i >= 0; i--)
            {
                var comp = (IComponent) objComps[i]!;

                try
                {
                    RemoveComponentImmediate(comp, uid, true, false, meta);
                }
                catch (Exception exc)
                {
                    _sawmill.Error($"Caught exception while trying to remove component {_componentFactory.GetComponentName(comp.GetType())} from entity '{ToPrettyString(uid)}'\n{exc.StackTrace}");
                }
            }
        }

        private void RemoveComponentDeferred(IComponent component, EntityUid uid, bool terminating)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

#pragma warning disable CS0618 // Type or member is obsolete
            if (component.Owner != uid)
#pragma warning restore CS0618 // Type or member is obsolete
                throw new InvalidOperationException("Component is not owned by entity.");

            if (component.Deleted) return;

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
                // already deferred deletion
                return;
            }

            if (component.LifeStage >= ComponentLifeStage.Initialized && component.LifeStage <= ComponentLifeStage.Running)
                LifeShutdown(component);
#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception while queuing deferred component removal. Entity={ToPrettyString(component.Owner)}, type={component.GetType()}");
                _runtimeLog.LogException(e, nameof(RemoveComponentDeferred));
            }
#endif
        }

        /// <summary>
        /// WARNING: Do not call this unless you're sure of what you're doing!
        /// </summary>
        internal void RemoveComponentInternal(EntityUid uid, IComponent component, bool terminating, bool archetypeChange, MetaDataComponent? metadata = null)
        {
            // I hate this but also didn't want the MetaQuery.GetComponent overhead.
            // and with archetypes we want to avoid moves at all costs.
            RemoveComponentImmediate(component, uid, terminating, archetypeChange, metadata);
        }

        /// <summary>
        /// Removes a component.
        /// </summary>
        /// <param name="terminating">Is the entity terminating.</param>
        /// <param name="archetypeChange">Should we handle the archetype change or is it being handled externally.</param>
        private void RemoveComponentImmediate(IComponent component, EntityUid uid, bool terminating, bool archetypeChange, MetaDataComponent? meta = null)
        {
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
                LifeShutdown(component);

            if (component.LifeStage != ComponentLifeStage.PreAdd)
                LifeRemoveFromEntity(component); // Sets delete

#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception during immediate component removal. Entity={ToPrettyString(component.Owner)}, type={component.GetType()}");
                _runtimeLog.LogException(e, nameof(RemoveComponentImmediate));
            }
#endif
            DeleteComponent(uid, component, terminating, archetypeChange, meta);
        }

        /// <inheritdoc />
        public void CullRemovedComponents()
        {
            foreach (var component in _deleteSet)
            {
                if (component.Deleted)
                    continue;
                var uid = component.Owner;

#if EXCEPTION_TOLERANCE
            try
            {
#endif
                // The component may have been restarted sometime after removal was deferred.
                if (component.Running)
                {
                    // TODO add options to cancel deferred deletion?
                    _sawmill.Warning($"Found a running component while culling deferred deletions, owner={ToPrettyString(uid)}, type={component.GetType()}");
                    LifeShutdown(component);
                }

                if (component.LifeStage != ComponentLifeStage.PreAdd)
                    LifeRemoveFromEntity(component);

#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception  while processing deferred component removal. Entity={ToPrettyString(component.Owner)}, type={component.GetType()}");
                _runtimeLog.LogException(e, nameof(CullRemovedComponents));
            }
#endif
                DeleteComponent(uid, component, false, true);
            }

            _deleteSet.Clear();
        }

        private void DeleteComponent(EntityUid entityUid, IComponent component, bool terminating, bool archetypeChange, MetaDataComponent? metadata = null)
        {
            if (!MetaQuery.ResolveInternal(entityUid, ref metadata))
                return;

            var eventArgs = new RemovedComponentEventArgs(new ComponentEventArgs(component, entityUid), false, metadata);
            ComponentRemoved?.Invoke(eventArgs);
            _eventBus.OnComponentRemoved(eventArgs);

            var reg = _componentFactory.GetRegistration(component);
            DebugTools.Assert(component.Networked == (reg.NetID != null));

            if (!terminating && reg.NetID != null)
            {
                if (!metadata.NetComponents.Remove(reg.NetID.Value))
                    _sawmill.Error($"Entity {ToPrettyString(entityUid, metadata)} did not have {component.GetType().Name} in its networked component dictionary during component deletion.");

                if (component.NetSyncEnabled)
                {
                    DirtyEntity(entityUid, metadata);
                    metadata.LastComponentRemoved = _gameTiming.CurTick;
                }
            }

            if (archetypeChange)
            {
                DebugTools.Assert(_world.Has(entityUid, reg.Idx.Type));
                if (_world.Has(entityUid, reg.Idx.Type))
                    _world.Remove(entityUid, reg.Idx.Type);
            }


            DebugTools.Assert(_netMan.IsClient // Client side prediction can set LastComponentRemoved to some future tick,
                              || metadata.EntityLastModifiedTick >= metadata.LastComponentRemoved);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityUid uid) where T : IComponent
        {
            if (!IsAlive(uid) || !_world.TryGet(uid, out T? comp))
                return false;

            return !comp!.Deleted;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityUid? uid) where T : IComponent
        {
            return uid.HasValue && HasComponent<T>(uid.Value);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityUid uid, Type type)
        {
            if (!IsAlive(uid) || !_world.TryGet(uid, type, out var comp))
                return false;

            return !((IComponent)comp!).Deleted;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityUid? uid, Type type)
        {
            if (!uid.HasValue)
            {
                return false;
            }

            return HasComponent(uid.Value, type);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityUid uid, ushort netId, MetaDataComponent? meta = null)
        {
            if (!MetaQuery.Resolve(uid, ref meta))
                return false;

            return meta.NetComponents.ContainsKey(netId);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityUid? uid, ushort netId, MetaDataComponent? meta = null)
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(EntityUid uid) where T : IComponent
        {
            if (IsAlive(uid) && _world.TryGet(uid, out T? comp))
                return comp!;

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {typeof(T)}");
        }

        public IComponent GetComponent(EntityUid uid, CompIdx type)
        {
            if (TryGetComponent(uid, type, out var comp))
                return comp;

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {_componentFactory.IdxToType(type)}");
        }

        /// <inheritdoc />
        public IComponent GetComponent(EntityUid uid, Type type)
        {
            if (TryGetComponent(uid, type, out var component))
                return component;

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {type}");
        }

        /// <inheritdoc />
        public IComponent GetComponent(EntityUid uid, ushort netId, MetaDataComponent? meta = null)
        {
            return (meta ?? MetaQuery.GetComponentInternal(uid)).NetComponents[netId];
        }

        /// <inheritdoc />
        public IComponent GetComponentInternal(EntityUid uid, CompIdx type)
        {
            if (TryGetComponent(uid, type, out var component))
                return component;

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {type}");
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponent<T>(EntityUid uid, [NotNullWhen(true)] out T? component) where T : IComponent?
        {
            if (IsAlive(uid) && _world.TryGet(uid, out component))
            {
                if (!component!.Deleted)
                {
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
        public bool TryGetComponent(EntityUid uid, Type type, [NotNullWhen(true)] out IComponent? component)
        {
            if (IsAlive(uid) && _world.TryGet(uid, type, out var comp))
            {
                component = (IComponent)(comp!);
                if (!component.Deleted)
                {
                    return true;
                }
            }

            component = null;
            return false;
        }

        public bool TryGetComponent(EntityUid uid, CompIdx type, [NotNullWhen(true)] out IComponent? component)
        {
            if (IsAlive(uid) && _world.TryGet(uid, type.Type, out var comp))
            {
                component = (IComponent)comp!;
                if (component != null! && !component.Deleted)
                    return true;
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

            return TryGetComponent(uid.Value, type, out component);
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

        public EntityQuery<TComp1> GetEntityQuery<TComp1>() where TComp1 : IComponent
        {
            return new EntityQuery<TComp1>(this, _world, null, _resolveSawmill);
        }

        public EntityQuery<IComponent> GetEntityQuery(Type type)
        {
            return new EntityQuery<IComponent>(this, _world, type, _resolveSawmill);
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetComponents(EntityUid uid)
        {
            foreach (var obj in _world.GetAllComponents(uid))
            {
                var comp = (IComponent)(obj!);

                if (comp.Deleted) continue;

                yield return comp;
            }
        }

        /// <inheritdoc />
        public int ComponentCount(EntityUid uid)
        {
            return _world.GetArchetype(uid).Types.Length;
        }

        /// <summary>
        /// Copy the components for an entity into the given span,
        /// or re-allocate the span as an array if there's not enough space.º
        /// </summary>
        private void CopyComponentsInto(ref Span<IComponent?> comps, EntityUid uid)
        {
            var set = _world.GetAllComponents(uid);

            if (set.Length > comps.Length)
            {
                comps = new IComponent[set.Length];
            }

            var i = 0;
            foreach (var c in set)
            {
                comps[i++] = (IComponent)c!;
            }
        }

        /// <inheritdoc />
        public IEnumerable<T> GetComponents<T>(EntityUid uid)
        {
            var comps = _world.GetAllComponents(uid);

            foreach (var comp in comps)
            {
                var component = (IComponent)comp!;
                if (component.Deleted || component is not T tComp) continue;

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

        public AllEntityQueryEnumerator<TComp1> AllEntityQueryEnumerator<TComp1>()
        where TComp1 : IComponent
        {
            return new AllEntityQueryEnumerator<TComp1>(_world);
        }

        public AllEntityQueryEnumerator<TComp1, TComp2> AllEntityQueryEnumerator<TComp1, TComp2>()
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            return new AllEntityQueryEnumerator<TComp1, TComp2>(_world);
        }

        public AllEntityQueryEnumerator<TComp1, TComp2, TComp3> AllEntityQueryEnumerator<TComp1, TComp2, TComp3>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            return new AllEntityQueryEnumerator<TComp1, TComp2, TComp3>(_world);
        }

        public AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4> AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent
        {
            return new AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>(_world);
        }

        public EntityQueryEnumerator<TComp1> EntityQueryEnumerator<TComp1>()
            where TComp1 : IComponent
        {
            return new EntityQueryEnumerator<TComp1>(_world);
        }

        public EntityQueryEnumerator<TComp1, TComp2> EntityQueryEnumerator<TComp1, TComp2>()
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            return new EntityQueryEnumerator<TComp1, TComp2>(_world);
        }

        public EntityQueryEnumerator<TComp1, TComp2, TComp3> EntityQueryEnumerator<TComp1, TComp2, TComp3>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            return new EntityQueryEnumerator<TComp1, TComp2, TComp3>(_world);
        }

        public EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4> EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>()
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent
        {
            return new EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>(_world);
        }

        /// <inheritdoc />
        public IEnumerable<T> EntityQuery<T>(bool includePaused = false) where T : IComponent
        {
            if (includePaused)
            {
                var query = new AllEntityQueryEnumerator<T>(_world);
                while (query.MoveNext(out var comp))
                {
                    yield return comp;
                }
            }
            else
            {
                var query = new EntityQueryEnumerator<T>(_world);
                while (query.MoveNext(out var comp))
                {
                    yield return comp;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2)> EntityQuery<TComp1, TComp2>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            if (includePaused)
            {
                var query = new AllEntityQueryEnumerator<TComp1, TComp2>(_world);
                while (query.MoveNext(out var comp1, out var comp2))
                {
                    yield return (comp1, comp2);
                }
            }
            else
            {
                var query = new EntityQueryEnumerator<TComp1, TComp2>(_world);
                while (query.MoveNext(out var comp1, out var comp2))
                {
                    yield return (comp1, comp2);
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2, TComp3)> EntityQuery<TComp1, TComp2, TComp3>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            if (includePaused)
            {
                var query = new AllEntityQueryEnumerator<TComp1, TComp2, TComp3>(_world);
                while (query.MoveNext(out var comp1, out var comp2, out var comp3))
                {
                    yield return (comp1, comp2, comp3);
                }
            }
            else
            {
                var query = new EntityQueryEnumerator<TComp1, TComp2, TComp3>(_world);
                while (query.MoveNext(out var comp1, out var comp2, out var comp3))
                {
                    yield return (comp1, comp2, comp3);
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
            if (includePaused)
            {
                var query = new AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>(_world);
                while (query.MoveNext(out var comp1, out var comp2, out var comp3, out var comp4))
                {
                    yield return (comp1, comp2, comp3, comp4);
                }
            }
            else
            {
                var query = new EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>(_world);
                while (query.MoveNext(out var comp1, out var comp2, out var comp3, out var comp4))
                {
                    yield return (comp1, comp2, comp3, comp4);
                }
            }
        }

        #endregion

        /// <inheritdoc />
        public IEnumerable<(EntityUid Uid, IComponent Component)> GetAllComponents(Type type, bool includePaused = false)
        {
            var query = new QueryDescription();

            // TODO arch paused component
            if (includePaused)
            {
                // TODO arch pool
                query.All = new ComponentType[] { type };
            }
            else
            {
                query.All = new ComponentType[] { type, typeof(MetaDataComponent) };
            }

            foreach (var chunk in _world.Query(query).ChunkIterator(_world))
            {
                var components = chunk.GetArray(type);
                var metas = includePaused ? default : chunk.GetArray<MetaDataComponent>();

                for (var i = 0; i < chunk.Size; i++)
                {
                    var comp = (IComponent)(components.GetValue(i))!;
                    if (comp.Deleted)
                        continue;

                    if (!includePaused && metas![i].EntityPaused)
                        continue;

                    yield return (_world.Reference(chunk.Entities[i]), comp);
                }
            }
        }

        /// <inheritdoc />
        public ComponentState GetComponentState(IEventBus eventBus, IComponent component, ICommonSession? session, GameTick fromTick)
        {
            DebugTools.Assert(component.NetSyncEnabled, $"Attempting to get component state for an un-synced component: {component.GetType()}");
            var getState = new ComponentGetState(session, fromTick);
            eventBus.RaiseComponentEvent(component, ref getState);

            return getState.State ?? DefaultComponentState;
        }

        public bool CanGetComponentState(IEventBus eventBus, IComponent component, ICommonSession player)
        {
            var attempt = new ComponentGetStateAttemptEvent(player);
            eventBus.RaiseComponentEvent(component, ref attempt);
            return !attempt.Cancelled;
        }

        #endregion
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
        private readonly IEntityManager _manager;
        private readonly World _world;
        private readonly ComponentType? _type;
        private readonly ISawmill _sawmill;

        public EntityQuery(IEntityManager manager, World world, ComponentType? type, ISawmill sawmill)
        {
            _manager = manager;
            _world = world;
            _type = type;
            _sawmill = sawmill;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public TComp1 GetComponent(EntityUid uid)
        {
            return _type == null
                ? _manager.GetComponent<TComp1>(uid)
                : (TComp1)_manager.GetComponent(uid, _type.Value.Type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
        public Entity<TComp1> Get(EntityUid uid)
        {
            if (_world.TryGet(uid, out TComp1? comp) && comp != null && !comp.Deleted)
                return new Entity<TComp1>(uid, comp);

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
            if (_type != null)
            {
                if (_manager.TryGetComponent(uid, _type.Value.Type, out var comp))
                {
                    component = (TComp1) comp;
                    return true;
                }

                component = default;
                return false;
            }
            else
            {
                return _manager.TryGetComponent(uid, out component);
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent(EntityUid uid)
        {
            return _type == null
                ? _manager.HasComponent<TComp1>(uid)
                : _manager.HasComponent(uid, _type.Value.Type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool HasComponent(EntityUid? uid)
        {
            return _type == null
                ? _manager.HasComponent<TComp1>(uid)
                : _manager.HasComponent(uid, _type.Value.Type);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public bool Resolve(EntityUid uid, [NotNullWhen(true)] ref TComp1? component, bool logMissing = true)
        {
            if (component != null)
            {
                DebugTools.AssertOwner(uid, component);
                return true;
            }

            if (TryGetComponent(uid, out component))
            {
                return true;
            }

            if (logMissing)
                _sawmill.Error($"Can't resolve \"{typeof(TComp1)}\" on entity {uid}!\n{new StackTrace(1, true)}");

            return false;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining), Pure]
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

        #region Internal

        /// <summary>
        /// Elides the component.Deleted check of <see cref="GetComponent"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        internal TComp1 GetComponentInternal(EntityUid uid)
        {
            if (TryGetComponentInternal(uid, out var comp))
                return comp;

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
            if (_world.TryGet(uid, out component!))
            {
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
            // TODO fix checking deleted
            return _world.TryGet(uid, out TComp1? comp) && !comp!.Deleted;
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

            if (TryGetComponentInternal(uid, out component))
            {
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

    #region Query

    /// <summary>
    /// Returns all matching unpaused components.
    /// </summary>
    public struct EntityQueryEnumerator<TComp1>
        where TComp1 : IComponent
    {
        private readonly World _world;
        private ArchChunkEnumerator _chunkEnumerator;
        private int _index;

        public EntityQueryEnumerator(World world)
        {
            _world = world;
            _chunkEnumerator = world.Query(new QueryDescription().WithAll<TComp1, MetaDataComponent>()).ChunkIterator(world).GetEnumerator();
            if (_chunkEnumerator.MoveNext())
            {
                _index = _chunkEnumerator.Current.Size;
            }
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1)
        {
            uid = default;
            comp1 = default;

            if (--_index < 0)
            {
                if (!_chunkEnumerator.MoveNext())
                {
                    return false;
                }

                _index = _chunkEnumerator.Current.Size - 1;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, MetaDataComponent>(_index);

            if (comps.t0.Deleted || comps.t1.EntityPaused)
                return false;

            uid = _world.Reference(entity);
            comp1 = comps.t0;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext([NotNullWhen(true)] out TComp1? comp1)
        {
            return MoveNext(out _, out comp1);
        }
    }

    /// <summary>
    /// Returns all matching unpaused components.
    /// </summary>
    public struct EntityQueryEnumerator<TComp1, TComp2>
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        private readonly World _world;
        private ArchChunkEnumerator _chunkEnumerator;
        private int _index;

        public EntityQueryEnumerator(World world)
        {
            _world = world;
            _chunkEnumerator = world.Query(new QueryDescription().WithAll<TComp1, TComp2, MetaDataComponent>()).ChunkIterator(world).GetEnumerator();
            if (_chunkEnumerator.MoveNext())
            {
                _index = _chunkEnumerator.Current.Size;
            }
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1,  [NotNullWhen(true)] out TComp2? comp2)
        {
            uid = default;
            comp1 = default;
            comp2 = default;

            if (--_index < 0)
            {
                if (!_chunkEnumerator.MoveNext())
                {
                    return false;
                }

                _index = _chunkEnumerator.Current.Size - 1;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2, MetaDataComponent>(_index);

            if (comps.t0.Deleted || comps.t1.Deleted || comps.t2.EntityPaused)
                return false;

            uid = _world.Reference(entity);
            comp1 = comps.t0;
            comp2 = comps.t1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext([NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2)
        {
            return MoveNext(out _, out comp1, out comp2);
        }
    }

    /// <summary>
    /// Returns all matching unpaused components.
    /// </summary>
    public struct EntityQueryEnumerator<TComp1, TComp2, TComp3>
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
    {
        private readonly World _world;
        private ArchChunkEnumerator _chunkEnumerator;
        private int _index;

        public EntityQueryEnumerator(World world)
        {
            _world = world;
            _chunkEnumerator = world.Query(new QueryDescription().WithAll<TComp1, TComp2, TComp3, MetaDataComponent>()).ChunkIterator(world).GetEnumerator();
            if (_chunkEnumerator.MoveNext())
            {
                _index = _chunkEnumerator.Current.Size;
            }
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3)
        {
            uid = default;
            comp1 = default;
            comp2 = default;
            comp3 = default;

            if (--_index < 0)
            {
                if (!_chunkEnumerator.MoveNext())
                {
                    return false;
                }

                _index = _chunkEnumerator.Current.Size - 1;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2, TComp3, MetaDataComponent>(_index);

            if (comps.t0.Deleted || comps.t1.Deleted || comps.t2.Deleted || comps.t3.EntityPaused)
                return false;

            uid = _world.Reference(entity);
            comp1 = comps.t0;
            comp2 = comps.t1;
            comp3 = comps.t2;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(
            [NotNullWhen(true)] out TComp1? comp1,
            [NotNullWhen(true)] out TComp2? comp2,
            [NotNullWhen(true)] out TComp3? comp3)
        {
            return MoveNext(out _, out comp1, out comp2, out comp3);
        }
    }

    /// <summary>
    /// Returns all matching unpaused components.
    /// </summary>
    public struct EntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent
    {
        private readonly World _world;
        private ArchChunkEnumerator _chunkEnumerator;
        private int _index;

        public EntityQueryEnumerator(World world)
        {
            _world = world;
            _chunkEnumerator = world.Query(new QueryDescription().WithAll<TComp1, TComp2, TComp3, TComp4, MetaDataComponent>()).ChunkIterator(world).GetEnumerator();
            if (_chunkEnumerator.MoveNext())
            {
                _index = _chunkEnumerator.Current.Size;
            }
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3, [NotNullWhen(true)] out TComp4? comp4)
        {
            uid = default;
            comp1 = default;
            comp2 = default;
            comp3 = default;
            comp4 = default;

            if (--_index < 0)
            {
                if (!_chunkEnumerator.MoveNext())
                {
                    return false;
                }

                _index = _chunkEnumerator.Current.Size - 1;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2, TComp3, TComp4, MetaDataComponent>(_index);

            if (comps.t0.Deleted || comps.t1.Deleted || comps.t2.Deleted || comps.t3.Deleted || comps.t4.EntityPaused)
                return false;

            uid = _world.Reference(entity);
            comp1 = comps.t0;
            comp2 = comps.t1;
            comp3 = comps.t2;
            comp4 = comps.t3;
            return true;
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
    }

    #endregion

    #region All query

    /// <summary>
    /// Returns all matching components, paused or not.
    /// </summary>
    public struct AllEntityQueryEnumerator<TComp1>
        where TComp1 : IComponent
    {
        private readonly World _world;
        private ArchChunkEnumerator _chunkEnumerator;
        private int _index;

        public AllEntityQueryEnumerator(World world)
        {
            _world = world;
            _chunkEnumerator = world.Query(new QueryDescription().WithAll<TComp1>()).ChunkIterator(world).GetEnumerator();
            if (_chunkEnumerator.MoveNext())
            {
                _index = _chunkEnumerator.Current.Size;
            }
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1)
        {
            uid = default;
            comp1 = default;

            if (--_index < 0)
            {
                if (!_chunkEnumerator.MoveNext())
                {
                    return false;
                }

                _index = _chunkEnumerator.Current.Size - 1;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comp = _chunkEnumerator.Current.Get<TComp1>(_index);

            if (comp.Deleted)
                return false;

            uid = _world.Reference(entity);
            comp1 = comp;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext([NotNullWhen(true)] out TComp1? comp1)
        {
            return MoveNext(out _, out comp1);
        }
    }

    /// <summary>
    /// Returns all matching components, paused or not.
    /// </summary>
    public struct AllEntityQueryEnumerator<TComp1, TComp2>
        where TComp1 : IComponent
        where TComp2 : IComponent
    {
        private readonly World _world;
        private ArchChunkEnumerator _chunkEnumerator;
        private int _index;

        public AllEntityQueryEnumerator(World world)
        {
            _world = world;
            _chunkEnumerator = world.Query(new QueryDescription().WithAll<TComp1, TComp2>()).ChunkIterator(world).GetEnumerator();
            if (_chunkEnumerator.MoveNext())
            {
                _index = _chunkEnumerator.Current.Size;
            }
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2)
        {
            uid = default;
            comp1 = default;
            comp2 = default;

            if (--_index < 0)
            {
                if (!_chunkEnumerator.MoveNext())
                {
                    return false;
                }

                _index = _chunkEnumerator.Current.Size - 1;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2>(_index);

            if (comps.t0.Deleted || comps.t1.Deleted)
                return false;

            uid = _world.Reference(entity);
            comp1 = comps.t0;
            comp2 = comps.t1;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext([NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2)
        {
            return MoveNext(out _, out comp1, out comp2);
        }
    }

    /// <summary>
    /// Returns all matching components, paused or not.
    /// </summary>
    public struct AllEntityQueryEnumerator<TComp1, TComp2, TComp3>
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
    {
        private readonly World _world;
        private ArchChunkEnumerator _chunkEnumerator;
        private int _index;

        public AllEntityQueryEnumerator(World world)
        {
            _world = world;
            _chunkEnumerator = world.Query(new QueryDescription().WithAll<TComp1, TComp2, TComp3>()).ChunkIterator(world).GetEnumerator();
            if (_chunkEnumerator.MoveNext())
            {
                _index = _chunkEnumerator.Current.Size;
            }
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3)
        {
            uid = default;
            comp1 = default;
            comp2 = default;
            comp3 = default;

            if (--_index < 0)
            {
                if (!_chunkEnumerator.MoveNext())
                {
                    return false;
                }

                _index = _chunkEnumerator.Current.Size - 1;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2, TComp3>(_index);

            if (comps.t0.Deleted || comps.t1.Deleted || comps.t2.Deleted)
                return false;

            uid = _world.Reference(entity);
            comp1 = comps.t0;
            comp2 = comps.t1;
            comp3 = comps.t2;
            return true;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool MoveNext(
            [NotNullWhen(true)] out TComp1? comp1,
            [NotNullWhen(true)] out TComp2? comp2,
            [NotNullWhen(true)] out TComp3? comp3)
        {
            return MoveNext(out _, out comp1, out comp2, out comp3);
        }
    }

    /// <summary>
    /// Returns all matching components, paused or not.
    /// </summary>
    public struct AllEntityQueryEnumerator<TComp1, TComp2, TComp3, TComp4>
        where TComp1 : IComponent
        where TComp2 : IComponent
        where TComp3 : IComponent
        where TComp4 : IComponent
    {
        private readonly World _world;
        private ArchChunkEnumerator _chunkEnumerator;
        private int _index;

        public AllEntityQueryEnumerator(World world)
        {
            _world = world;
            _chunkEnumerator = world.Query(new QueryDescription().WithAll<TComp1, TComp2, TComp3, TComp4>()).ChunkIterator(world).GetEnumerator();
            if (_chunkEnumerator.MoveNext())
            {
                _index = _chunkEnumerator.Current.Size;
            }
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3, [NotNullWhen(true)] out TComp4? comp4)
        {
            uid = default;
            comp1 = default;
            comp2 = default;
            comp3 = default;
            comp4 = default;

            if (--_index < 0)
            {
                if (!_chunkEnumerator.MoveNext())
                {
                    return false;
                }

                _index = _chunkEnumerator.Current.Size - 1;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2, TComp3, TComp4>(_index);

            if (comps.t0.Deleted || comps.t1.Deleted || comps.t2.Deleted || comps.t3.Deleted)
                return false;

            uid = _world.Reference(entity);
            comp1 = comps.t0;
            comp2 = comps.t1;
            comp3 = comps.t2;
            comp4 = comps.t3;
            return true;
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
    }

    #endregion
}
