using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using Arch.Core;
using Arch.Core.Utils;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Log;
using Robust.Shared.Physics.Components;
using Robust.Shared.Players;
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

        private static readonly ComponentState DefaultComponentState = new();

        private readonly Dictionary<EntityUid, Dictionary<ushort, Component>> _netComponents
            = new(EntityCapacity);

        private readonly HashSet<Component> _deleteSet = new(TypeCapacity);

        private UniqueIndexHkm<EntityUid, Component> _entCompIndex =
            new(ComponentCollectionCapacity);

        /// <inheritdoc />
        public event Action<AddedComponentEventArgs>? ComponentAdded;

        /// <inheritdoc />
        public event Action<RemovedComponentEventArgs>? ComponentRemoved;

        /// <inheritdoc />
        public event Action<DeletedComponentEventArgs>? ComponentDeleted;

        public void InitializeComponents()
        {
            if (Initialized)
                throw new InvalidOperationException("Already initialized.");
        }

        /// <summary>
        ///     Instantly clears all components from the manager. This will NOT shut them down gracefully.
        ///     Any entities relying on existing components will be broken.
        /// </summary>
        public void ClearComponents()
        {
            _netComponents.Clear();
            _entCompIndex.Clear();
            _deleteSet.Clear();
        }

        #region Component Management

        /// <inheritdoc />
        public int Count<T>() where T : Component
        {
            return _world.CountEntities(new QueryDescription().WithAll<T>());
        }

        /// <inheritdoc />
        public int Count(Type component)
        {
            var query = new QueryDescription
            {
                // TODO arch pool
                All = new ComponentType[] { component }
            };
            return _world.CountEntities(in query);
        }

        public void InitializeComponents(EntityUid uid, MetaDataComponent? metadata = null)
        {
            DebugTools.Assert(metadata == null || metadata.Owner == uid);
            metadata ??= GetComponent<MetaDataComponent>(uid);
            DebugTools.Assert(metadata.EntityLifeStage == EntityLifeStage.PreInit);
            metadata.EntityLifeStage = EntityLifeStage.Initializing;

            // Initialize() can modify the collection of components. Copy them.
            FixedArray32<Component?> compsFixed = default;

            var comps = compsFixed.AsSpan;
            CopyComponentsInto(ref comps, uid);

            foreach (var comp in comps)
            {
                if (comp is { LifeStage:  ComponentLifeStage.Added })
                    comp.LifeInitialize(this, CompIdx.Index(comp.GetType()));
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
            metadata.EntityLifeStage = EntityLifeStage.Initialized;
        }

        public void StartComponents(EntityUid uid)
        {
            // Startup() can modify _components
            // This code can only handle additions to the list. Is there a better way? Probably not.
            FixedArray32<Component?> compsFixed = default;

            var comps = compsFixed.AsSpan;
            CopyComponentsInto(ref comps, uid);

            // TODO: please for the love of god remove these initialization order hacks.

            // Init transform first, we always have it.
            var transform = GetComponent<TransformComponent>(uid);
            if (transform.LifeStage == ComponentLifeStage.Initialized)
                transform.LifeStartup(this);

            // Init physics second if it exists.
            if (TryGetComponent<PhysicsComponent>(uid, out var phys)
                && phys.LifeStage == ComponentLifeStage.Initialized)
            {
                phys.LifeStartup(this);
            }

            // Do rest of components.
            foreach (var comp in comps)
            {
                if (comp is { LifeStage: ComponentLifeStage.Initialized })
                    comp.LifeStartup(this);
            }
        }

        public Component AddComponent(EntityUid uid, ushort netId)
        {
            var newComponent = (Component)_componentFactory.GetComponent(netId);
            newComponent.Owner = uid;
            AddComponent(uid, newComponent);
            return newComponent;
        }

        public T AddComponent<T>(EntityUid uid) where T : Component, new()
        {
            var newComponent = _componentFactory.GetComponent<T>();
            newComponent.Owner = uid;
            AddComponent(uid, newComponent);
            return newComponent;
        }

        public readonly struct CompInitializeHandle<T> : IDisposable
            where T : Component
        {
            private readonly IEntityManager _entMan;
            public readonly CompIdx CompType;
            public readonly T Comp;

            public CompInitializeHandle(IEntityManager entityManager, T comp, CompIdx compType)
            {
                _entMan = entityManager;
                Comp = comp;
                CompType = compType;
            }

            public void Dispose()
            {
                var metadata = _entMan.GetComponent<MetaDataComponent>(Comp.Owner);

                if (!metadata.EntityInitialized && !metadata.EntityInitializing)
                    return;

                if (!Comp.Initialized)
                    Comp.LifeInitialize(_entMan, CompType);

                if (metadata.EntityInitialized && !Comp.Running)
                    Comp.LifeStartup(_entMan);
            }

            public static implicit operator T(CompInitializeHandle<T> handle)
            {
                return handle.Comp;
            }
        }

        /// <inheritdoc />
        public CompInitializeHandle<T> AddComponentUninitialized<T>(EntityUid uid) where T : Component, new()
        {
            var reg = _componentFactory.GetRegistration<T>();
            var newComponent = (T)_componentFactory.GetComponent(reg);
            newComponent.Owner = uid;

            if (!uid.IsValid() || !EntityExists(uid))
                throw new ArgumentException($"Entity {uid} is not valid.", nameof(uid));

            if (newComponent == null) throw new ArgumentNullException(nameof(newComponent));

            if (newComponent.Owner != uid) throw new InvalidOperationException("Component is not owned by entity.");

            AddComponentInternal(uid, newComponent, false, true);

            return new CompInitializeHandle<T>(this, newComponent, reg.Idx);
        }

        /// <inheritdoc />
        public void AddComponent<T>(EntityUid uid, T component, bool overwrite = false) where T : Component
        {
            if (!uid.IsValid() || !EntityExists(uid))
                throw new ArgumentException($"Entity {uid} is not valid.", nameof(uid));

            if (component == null) throw new ArgumentNullException(nameof(component));

            if (component.Owner != uid) throw new InvalidOperationException("Component is not owned by entity.");

            AddComponentInternal(uid, component, overwrite, false);
        }

        private void AddComponentInternal<T>(EntityUid uid, T component, bool overwrite, bool skipInit) where T : Component
        {
            // We can't use typeof(T) here in case T is just Component
            DebugTools.Assert(component is MetaDataComponent ||
                GetComponent<MetaDataComponent>(uid).EntityLifeStage < EntityLifeStage.Terminating,
                $"Attempted to add a {component.GetType().Name} component to an entity ({ToPrettyString(uid)}) while it is terminating");

            // get interface aliases for mapping
            var reg = _componentFactory.GetRegistration(component);

            // Check that there are no overlapping references.
            foreach (var type in reg.References)
            {
                // TODO arch don't use CompIdx
                var compType = _componentFactory.IdxToType(type);
                if (!_world.IsAlive(uid) || !_world.TryGet(uid, compType, out var comp))
                    continue;

                var duplicate = (Component) comp;

                if (!overwrite && !duplicate.Deleted)
                    throw new InvalidOperationException(
                        $"Component reference type {component.GetType().Name} already occupied by {duplicate}");

                RemoveComponentImmediate(duplicate, uid, false);
            }

            // add the component to the grid
            foreach (var type in reg.References)
            {
                // We can't use typeof(T) here in case T is just Component
                // TODO arch please fix this god
                if (!_world.Has(uid, component.GetType()))
                    _world.Add(uid, (object) component);

                _entCompIndex.Add(uid, component);
            }

            // add the component to the netId grid
            if (reg.NetID != null)
            {
                // the main comp grid keeps this in sync
                var netId = reg.NetID.Value;

                if (!_netComponents.TryGetValue(uid, out var netSet))
                {
                    netSet = new Dictionary<ushort, Component>(NetComponentCapacity);
                    _netComponents.Add(uid, netSet);
                }

                netSet.Add(netId, component);
            }
            else
            {
                component.Networked = false;
            }

            var eventArgs = new AddedComponentEventArgs(new ComponentEventArgs(component, uid), reg.Idx);
            ComponentAdded?.Invoke(eventArgs);
            _eventBus.OnComponentAdded(eventArgs);

            component.LifeAddToEntity(this, reg.Idx);

            if (skipInit)
                return;

            var metadata = GetComponent<MetaDataComponent>(uid);

            if (!metadata.EntityInitialized && !metadata.EntityInitializing)
                return;

            if (component.Networked)
                DirtyEntity(uid, metadata);

            component.LifeInitialize(this, reg.Idx);

            if (metadata.EntityInitialized)
                component.LifeStartup(this);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent<T>(EntityUid uid)
        {
            return RemoveComponent(uid, typeof(T));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent(EntityUid uid, Type type)
        {
            if (!TryGetComponent(uid, type, out var comp))
                return false;

            RemoveComponentImmediate((Component)comp, uid, false);
            return true;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponent(EntityUid uid, ushort netId)
        {
            if (!TryGetComponent(uid, netId, out var comp))
                return false;

            RemoveComponentImmediate((Component)comp, uid, false);
            return true;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityUid uid, IComponent component)
        {
            RemoveComponent(uid, (Component)component);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityUid uid, Component component)
        {
            RemoveComponentImmediate(component, uid, false);
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

            RemoveComponentDeferred((Component)comp, uid, false);
            return true;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool RemoveComponentDeferred(EntityUid uid, ushort netId)
        {
            if (!TryGetComponent(uid, netId, out var comp))
                return false;

            RemoveComponentDeferred((Component)comp, uid, false);
            return true;
        }

        /// <inheritdoc />
        public void RemoveComponentDeferred(EntityUid owner, IComponent component)
        {
            RemoveComponentDeferred((Component)component, owner, false);
        }

        /// <inheritdoc />
        public void RemoveComponentDeferred(EntityUid owner, Component component)
        {
            RemoveComponentDeferred(component, owner, false);
        }

        private static IEnumerable<Component> InSafeOrder(IEnumerable<Component> comps, bool forCreation = false)
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
        public void RemoveComponents(EntityUid uid)
        {
            foreach (var comp in InSafeOrder(_entCompIndex[uid]))
            {
                RemoveComponentImmediate(comp, uid, false);
            }
        }

        /// <inheritdoc />
        public void DisposeComponents(EntityUid uid)
        {
            foreach (var comp in InSafeOrder(_entCompIndex[uid]))
            {
                try
                {
                    RemoveComponentImmediate(comp, uid, true);
                }
                catch (Exception)
                {
                    _sawmill.Error($"Caught exception while trying to remove component {_componentFactory.GetComponentName(comp.GetType())} from entity '{ToPrettyString(uid)}'");
                }
            }

            _entCompIndex.Remove(uid);
            _netComponents.Remove(uid);
        }

        private void RemoveComponentDeferred(Component component, EntityUid uid, bool terminating)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            if (component.Owner != uid)
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
                component.LifeShutdown(this);
#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception while queuing deferred component removal. Entity={ToPrettyString(component.Owner)}, type={component.GetType()}");
                _runtimeLog.LogException(e, nameof(RemoveComponentDeferred));
            }
#endif
        }

        private void RemoveComponentImmediate(Component component, EntityUid uid, bool terminating)
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
                component.LifeShutdown(this);

            if (component.LifeStage != ComponentLifeStage.PreAdd)
                component.LifeRemoveFromEntity(this); // Sets delete

#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception during immediate component removal. Entity={ToPrettyString(component.Owner)}, type={component.GetType()}");
                _runtimeLog.LogException(e, nameof(RemoveComponentImmediate));
            }
#endif
            var eventArgs = new RemovedComponentEventArgs(new ComponentEventArgs(component, uid), terminating);
            ComponentRemoved?.Invoke(eventArgs);
            _eventBus.OnComponentRemoved(eventArgs);
            DeleteComponent(uid, component, terminating);
        }

        /// <inheritdoc />
        public void CullRemovedComponents()
        {
            foreach (var component in InSafeOrder(_deleteSet))
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
                    component.LifeShutdown(this);
                }

                if (component.LifeStage != ComponentLifeStage.PreAdd)
                    component.LifeRemoveFromEntity(this);

#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _sawmill.Error($"Caught exception  while processing deferred component removal. Entity={ToPrettyString(component.Owner)}, type={component.GetType()}");
                _runtimeLog.LogException(e, nameof(CullRemovedComponents));
            }
#endif
                var eventArgs = new RemovedComponentEventArgs(new ComponentEventArgs(component, uid), false);
                ComponentRemoved?.Invoke(eventArgs);
                _eventBus.OnComponentRemoved(eventArgs);

                DeleteComponent(uid, component, false);
            }

            _deleteSet.Clear();
        }

        private void DeleteComponent(EntityUid entityUid, Component component, bool terminating)
        {
            var reg = _componentFactory.GetRegistration(component.GetType());

            if (!terminating && reg.NetID != null && _netComponents.TryGetValue(entityUid, out var netSet))
            {
                if (netSet.Count == 1)
                    _netComponents.Remove(entityUid);
                else
                    netSet.Remove(reg.NetID.Value);

                if (component.NetSyncEnabled)
                    DirtyEntity(entityUid);
            }

            foreach (var refType in reg.References)
            {
                // TODO arch don't use CompIdx
                var type = _componentFactory.IdxToType(refType);
                if (_world.Has(entityUid, type))
                    _world.Remove(entityUid, type);
            }

            // TODO if terminating the entity, maybe defer this?
            // _entCompIndex.Remove(uid) gets called later on anyways.
            _entCompIndex.Remove(entityUid, component);

            ComponentDeleted?.Invoke(new DeletedComponentEventArgs(new ComponentEventArgs(component, entityUid), terminating));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityUid uid)
        {
            if (!_world.IsAlive(uid) || !_world.TryGet(uid, out T comp))
                return false;

            return !((IComponent) comp!).Deleted;
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
            if (!_world.IsAlive(uid) || !_world.TryGet(uid, type, out var comp))
                return false;

            return !((IComponent) comp!).Deleted;
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
        public bool HasComponent(EntityUid uid, ushort netId)
        {
            return _netComponents.TryGetValue(uid, out var netSet)
                   && netSet.ContainsKey(netId);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityUid? uid, ushort netId)
        {
            if (!uid.HasValue)
            {
                return false;
            }

            return _netComponents.TryGetValue(uid.Value, out var netSet)
                   && netSet.ContainsKey(netId);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EnsureComponent<T>(EntityUid uid) where T : Component, new()
        {
            if (TryGetComponent<T>(uid, out var component))
            {
                // Check for deferred component removal.
                if (component.LifeStage <= ComponentLifeStage.Running)
                    return component;
                else
                    RemoveComponent(uid, component);
            }

            return AddComponent<T>(uid);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool EnsureComponent<T>(EntityUid entity, out T component) where T : Component, new()
        {
            if (TryGetComponent<T>(entity, out var comp))
            {
                // Check for deferred component removal.
                if (comp.LifeStage <= ComponentLifeStage.Running)
                {
                    component = comp;
                    return true;
                }
                else
                    RemoveComponent(entity, comp);
            }

            component = AddComponent<T>(entity);
            return false;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(EntityUid uid) where T : IComponent
        {
            if (_world.IsAlive(uid) && _world.TryGet(uid, out T comp))
                return comp;

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
        public IComponent GetComponent(EntityUid uid, ushort netId)
        {
            return _netComponents[uid][netId];
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool TryGetComponent<T>(EntityUid uid, [NotNullWhen(true)] out T? component) where T : IComponent
        {
            if (_world.IsAlive(uid) && _world.TryGet(uid, out component))
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
        public bool TryGetComponent<T>([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out T? component)
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
            if (_world.IsAlive(uid) && _world.TryGet(uid, type, out var comp))
            {
                component = (IComponent) comp;
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
            // TODO arch don't use CompIdx
            var compType = _componentFactory.IdxToType(type);
            if (_world.IsAlive(uid) && _world.TryGet(uid, compType, out var comp))
            {
                component = (IComponent) comp;
                if (!component.Deleted)
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
        public bool TryGetComponent(EntityUid uid, ushort netId, [MaybeNullWhen(false)] out IComponent component)
        {
            if (_netComponents.TryGetValue(uid, out var netSet)
                && netSet.TryGetValue(netId, out var comp))
            {
                component = comp;
                return true;
            }

            component = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent([NotNullWhen(true)] EntityUid? uid, ushort netId,
            [MaybeNullWhen(false)] out IComponent component)
        {
            if (!uid.HasValue)
            {
                component = default;
                return false;
            }

            if (_netComponents.TryGetValue(uid.Value, out var netSet)
                && netSet.TryGetValue(netId, out var comp))
            {
                component = comp;
                return true;
            }

            component = default;
            return false;
        }

        public EntityQuery<TComp1> GetEntityQuery<TComp1>() where TComp1 : Component
        {
            return new EntityQuery<TComp1>(this, _world, null, _resolveSawmill);
        }

        public EntityQuery<Component> GetEntityQuery(Type type)
        {
            return new EntityQuery<Component>(this, _world, type, _resolveSawmill);
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetComponents(EntityUid uid)
        {
            foreach (Component comp in _entCompIndex[uid].ToArray())
            {
                if (comp.Deleted) continue;

                yield return comp;
            }
        }

        /// <inheritdoc />
        public int ComponentCount(EntityUid uid)
        {
            var comps = _entCompIndex[uid];
            return comps.Count;
        }

        /// <summary>
        /// Copy the components for an entity into the given span,
        /// or re-allocate the span as an array if there's not enough space.
        /// </summary>
        private void CopyComponentsInto(ref Span<Component?> comps, EntityUid uid)
        {
            var set = _entCompIndex[uid];
            if (set.Count > comps.Length)
            {
                comps = new Component[set.Count];
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
        public NetComponentEnumerable GetNetComponents(EntityUid uid)
        {
            return new NetComponentEnumerable(_netComponents[uid]);
        }

        /// <inheritdoc />
        public NetComponentEnumerable? GetNetComponentsOrNull(EntityUid uid)
        {
            return _netComponents.TryGetValue(uid, out var data)
                    ? new NetComponentEnumerable(data)
                    : null;
        }

        #region Join Functions

        public (EntityUid Uid, T Component)[] AllComponents<T>() where T : Component
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

        public List<(EntityUid Uid, T Component)> AllComponentsList<T>() where T : Component
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
        public IEnumerable<(EntityUid Uid, Component Component)> GetAllComponents(Type type, bool includePaused = false)
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
                    var comp = (Component) components.GetValue(i)!;
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
        private readonly Dictionary<ushort, Component> _dictionary;

        public NetComponentEnumerable(Dictionary<ushort, Component> dictionary) => _dictionary = dictionary;
        public NetComponentEnumerator GetEnumerator() => new(_dictionary);
    }

    public struct NetComponentEnumerator
    {
        // DO NOT MAKE THIS READONLY
        private Dictionary<ushort, Component>.Enumerator _dictEnum;

        public NetComponentEnumerator(Dictionary<ushort, Component> dictionary) =>
            _dictEnum = dictionary.GetEnumerator();

        public bool MoveNext() => _dictEnum.MoveNext();

        public (ushort netId, Component component) Current
        {
            get
            {
                var val = _dictEnum.Current;
                return (val.Key, val.Value);
            }
        }
    }

    public readonly struct EntityQuery<TComp1> where TComp1 : Component
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

                component = null;
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
                DebugTools.Assert(uid == component.Owner, "Specified Entity is not the component's Owner!");
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public TComp1? CompOrNull(EntityUid uid)
        {
            if (TryGetComponent(uid, out var comp))
                return comp;

            return null;
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
        /// Elides the component.Deleted check of <see cref="HasComponent"/>
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        internal bool HasComponentInternal(EntityUid uid)
        {
            // TODO fix checking deleted
            return _world.TryGet(uid, out TComp1 comp) && !comp.Deleted;
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
                DebugTools.Assert(uid == component.Owner, "Specified Entity is not the component's Owner!");
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

            return null;
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
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1)
        {
            uid = default;
            comp1 = default;

            if (_index >= _chunkEnumerator.Current.Size)
            {
                if (!_chunkEnumerator.MoveNext())
                    return false;

                _index = 0;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, MetaDataComponent>(_index);
            _index++;

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
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1,  [NotNullWhen(true)] out TComp2? comp2)
        {
            uid = default;
            comp1 = default;
            comp2 = default;

            if (_index >= _chunkEnumerator.Current.Size)
            {
                if (!_chunkEnumerator.MoveNext())
                    return false;

                _index = 0;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2, MetaDataComponent>(_index);
            _index++;

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
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3)
        {
            uid = default;
            comp1 = default;
            comp2 = default;
            comp3 = default;

            if (_index >= _chunkEnumerator.Current.Size)
            {
                if (!_chunkEnumerator.MoveNext())
                    return false;

                _index = 0;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2, TComp3, MetaDataComponent>(_index);
            _index++;

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
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3, [NotNullWhen(true)] out TComp4? comp4)
        {
            uid = default;
            comp1 = default;
            comp2 = default;
            comp3 = default;
            comp4 = default;

            if (_index >= _chunkEnumerator.Current.Size)
            {
                if (!_chunkEnumerator.MoveNext())
                    return false;

                _index = 0;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2, TComp3, TComp4, MetaDataComponent>(_index);
            _index++;

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
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1)
        {
            uid = default;
            comp1 = default;

            while (_index >= _chunkEnumerator.Current.Size)
            {
                if (!_chunkEnumerator.MoveNext())
                    return false;

                _index = 0;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comp = _chunkEnumerator.Current.Get<TComp1>(_index);
            _index++;

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
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2)
        {
            uid = default;
            comp1 = default;
            comp2 = default;

            while (_index >= _chunkEnumerator.Current.Size)
            {
                if (!_chunkEnumerator.MoveNext())
                    return false;

                _index = 0;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2>(_index);
            _index++;

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
            _chunkEnumerator.MoveNext();
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3)
        {
            uid = default;
            comp1 = default;
            comp2 = default;
            comp3 = default;

            while (_index >= _chunkEnumerator.Current.Size)
            {
                if (!_chunkEnumerator.MoveNext())
                    return false;

                _index = 0;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2, TComp3>(_index);
            _index++;

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
            _chunkEnumerator.MoveNext();
        }

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp1? comp1, [NotNullWhen(true)] out TComp2? comp2, [NotNullWhen(true)] out TComp3? comp3, [NotNullWhen(true)] out TComp4? comp4)
        {
            uid = default;
            comp1 = default;
            comp2 = default;
            comp3 = default;
            comp4 = default;

            while (_index >= _chunkEnumerator.Current.Size)
            {
                if (!_chunkEnumerator.MoveNext())
                    return false;

                _index = 0;
            }

            var entity = _chunkEnumerator.Current.Entities[_index];
            var comps = _chunkEnumerator.Current.GetRow<TComp1, TComp2, TComp3, TComp4>(_index);
            _index++;

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
