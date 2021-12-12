using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameStates;
using Robust.Shared.Physics;
using Robust.Shared.Players;
using Robust.Shared.Utility;
using System.Runtime.CompilerServices;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    internal class ComponentCollection : IComponentCollection
    {
        [IoC.Dependency] private readonly IComponentFactory _componentFactory = default!;
        [IoC.Dependency] private readonly IComponentDependencyManager _componentDependencyManager = default!;

#if EXCEPTION_TOLERANCE
        [IoC.Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

        public IComponentFactory ComponentFactory => _componentFactory;

        private const int TypeCapacity = 32;
        private const int ComponentCollectionCapacity = 1024;
        private const int EntityCapacity = 1024;
        private const int NetComponentCapacity = 8;

        private readonly Dictionary<EntityUid, Dictionary<ushort, Component>> _netComponents
            = new(EntityCapacity);

        protected readonly Dictionary<Type, Dictionary<EntityUid, Component>> _entTraitDict
            = new();

        private readonly HashSet<Component> _deleteSet = new(TypeCapacity);

        protected UniqueIndexHkm<EntityUid, Component> _entCompIndex =
            new(ComponentCollectionCapacity);

        /// <inheritdoc />
        public event EventHandler<ComponentEventArgs>? ComponentAdded;

        /// <inheritdoc />
        public event EventHandler<ComponentEventArgs>? ComponentRemoved;

        /// <inheritdoc />
        public event EventHandler<ComponentEventArgs>? ComponentDeleted;

        /// <summary>
        ///     Instantly clears all components from the manager. This will NOT shut them down gracefully.
        ///     Any entities relying on existing components will be broken.
        /// </summary>
        public void ClearComponents()
        {
            _componentFactory.ComponentAdded -= OnComponentAdded;
            _componentFactory.ComponentReferenceAdded -= OnComponentReferenceAdded;
            _netComponents.Clear();
            _entCompIndex.Clear();
            _deleteSet.Clear();
            FillComponentDict();
        }

        internal void OnComponentAdded(IComponentRegistration obj)
        {
            _entTraitDict.Add(obj.Type, new Dictionary<EntityUid, Component>());
        }

        internal void OnComponentReferenceAdded((IComponentRegistration, Type) obj)
        {
            _entTraitDict.Add(obj.Item2, new Dictionary<EntityUid, Component>());
        }

        #region Component Management

        public void StartComponents(EntityUid uid)
        {
            // TODO: Move this to EntityManager.
            // Startup() can modify _components
            // This code can only handle additions to the list. Is there a better way? Probably not.
            var comps = GetComponents(uid)
                .OrderBy(x => x switch
                {
                    TransformComponent _ => 0,
                    IPhysBody _ => 1,
                    _ => int.MaxValue
                });

            foreach (var component in comps)
            {
                var comp = (Component) component;
                if (comp.LifeStage == ComponentLifeStage.Initialized)
                {
                    comp.LifeStartup();
                }
            }
        }

        public T AddComponent<T>(EntityUid uid) where T : Component, new()
        {
            var newComponent = _componentFactory.GetComponent<T>();
            newComponent.Owner = uid;
            AddComponent(uid, newComponent);
            return newComponent;
        }

        public void AddComponent<T>(EntityUid uid, T component, bool overwrite = false) where T : Component
        {
            if (!uid.IsValid() || !EntityExists(uid))
                throw new ArgumentException("Entity is not valid.", nameof(uid));

            if (component == null) throw new ArgumentNullException(nameof(component));

            if (component.Owner != uid) throw new InvalidOperationException("Component is not owned by entity.");

            AddComponentInternal(uid, component, overwrite);
        }

        protected void AddComponentInternal<T>(EntityUid uid, T component, bool overwrite = false) where T : Component
        {
            // get interface aliases for mapping
            var reg = _componentFactory.GetRegistration(component);

            // Check that there are no overlapping references.
            foreach (var type in reg.References)
            {
                var dict = _entTraitDict[type];
                if (!dict.TryGetValue(uid, out var duplicate))
                    continue;

                if (!overwrite && !duplicate.Deleted)
                {
                    throw new InvalidOperationException(
                        $"Component reference type {type} already occupied by {duplicate}");
                }

                // these two components are required on all entities and cannot be overwritten.
                if (duplicate is TransformComponent || duplicate is MetaDataComponent)
                    throw new InvalidOperationException("Tried to overwrite a protected component.");

                RemoveComponentImmediate(duplicate, uid, false);
            }

            // add the component to the grid
            foreach (var type in reg.References)
            {
                _entTraitDict[type].Add(uid, component);
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

                // mark the component as dirty for networking
                component.Dirty();
            }

            ComponentAdded?.Invoke(this, new AddedComponentEventArgs(component, uid));

            _componentDependencyManager.OnComponentAdd(uid, component);

            component.LifeAddToEntity();

            var metadata = GetComponent<MetaDataComponent>(uid);

            if (!metadata.EntityInitialized && !metadata.EntityInitializing)
                return;

            component.LifeInitialize();

            if (metadata.EntityInitialized)
                component.LifeStartup();
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent<T>(EntityUid uid)
        {
            RemoveComponent(uid, typeof(T));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityUid uid, Type type)
        {
            RemoveComponentImmediate((Component)GetComponent(uid, type), uid, false);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityUid uid, ushort netId)
        {
            RemoveComponentImmediate((Component)GetComponent(uid, netId), uid, false);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityUid uid, IComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            if (component.Owner != uid)
                throw new InvalidOperationException("Component is not owned by entity.");

            RemoveComponentImmediate((Component)component, uid, false);
        }

        protected static IEnumerable<Component> InSafeOrder(IEnumerable<Component> comps, bool forCreation = false)
        {
            static int Sequence(IComponent x)
                => x switch
                {
                    MetaDataComponent _ => 0,
                    TransformComponent _ => 1,
                    IPhysBody _ => 2,
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
                RemoveComponentImmediate(comp, uid, true);
            }

            // DisposeComponents means the entity is getting deleted.
            // Safe to wipe the entity out of the index.
            _entCompIndex.Remove(uid);
        }

        private void RemoveComponentDeferred(Component component, EntityUid uid, bool removeProtected)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            if (component.Deleted) return;

#if EXCEPTION_TOLERANCE
            try
            {
#endif
            // these two components are required on all entities and cannot be removed normally.
            if (!removeProtected && component is TransformComponent or MetaDataComponent)
            {
                DebugTools.Assert("Tried to remove a protected component.");
                return;
            }

            if (!_deleteSet.Add(component))
            {
                // already deferred deletion
                return;
            }

            if (component.Running)
                component.LifeShutdown();

            if (component.LifeStage != ComponentLifeStage.PreAdd)
                component.LifeRemoveFromEntity();
            _componentDependencyManager.OnComponentRemove(uid, component);
            ComponentRemoved?.Invoke(this, new RemovedComponentEventArgs(component, uid));
#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _runtimeLog.LogException(e,
                    $"RemoveComponentDeferred, owner={component.Owner}, type={component.GetType()}");
            }
#endif
        }

        private void RemoveComponentImmediate(Component component, EntityUid uid, bool removeProtected)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

#if EXCEPTION_TOLERANCE
            try
            {
#endif
            if (!component.Deleted)
            {
                // these two components are required on all entities and cannot be removed.
                if (!removeProtected && component is TransformComponent or MetaDataComponent)
                {
                    DebugTools.Assert("Tried to remove a protected component.");
                    return;
                }

                if (component.Running)
                    component.LifeShutdown();

                if (component.LifeStage != ComponentLifeStage.PreAdd)
                    component.LifeRemoveFromEntity(); // Sets delete

                _componentDependencyManager.OnComponentRemove(uid, component);
                ComponentRemoved?.Invoke(this, new RemovedComponentEventArgs(component, uid));
            }
#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _runtimeLog.LogException(e,
                    $"RemoveComponentImmediate, owner={component.Owner}, type={component.GetType()}");
            }
#endif

            DeleteComponent(component);
        }

        /// <inheritdoc />
        public void CullRemovedComponents()
        {
            foreach (var component in InSafeOrder(_deleteSet))
            {
                DeleteComponent(component);
            }

            _deleteSet.Clear();
        }

        private void DeleteComponent(Component component)
        {
            var reg = _componentFactory.GetRegistration(component.GetType());

            var entityUid = component.Owner;

            // ReSharper disable once InvertIf
            if (reg.NetID != null)
            {
                var netSet = _netComponents[entityUid];
                if (netSet.Count == 1)
                    _netComponents.Remove(entityUid);
                else
                    netSet.Remove(reg.NetID.Value);

                DirtyEntity(entityUid);
            }

            foreach (var refType in reg.References)
            {
                _entTraitDict[refType].Remove(entityUid);
            }

            _entCompIndex.Remove(entityUid, component);
            ComponentDeleted?.Invoke(this, new DeletedComponentEventArgs(component, entityUid));
        }

        public virtual void DirtyEntity(EntityUid uid)
        {

        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityUid uid)
        {
            return HasComponent(uid, typeof(T));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityUid? uid)
        {
            return uid.HasValue && HasComponent(uid.Value, typeof(T));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityUid uid, Type type)
        {
            var dict = _entTraitDict[type];
            return dict.TryGetValue(uid, out var comp) && !comp.Deleted;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityUid? uid, Type type)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T EnsureComponent<T>(EntityUid uid) where T : Component, new()
        {
            if (TryGetComponent<T>(uid, out var component))
                return component;

            return AddComponent<T>(uid);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(EntityUid uid)
        {
            return (T)GetComponent(uid, typeof(T));
        }

        /// <inheritdoc />
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
        public IComponent GetComponent(EntityUid uid, ushort netId)
        {
            return _netComponents[uid][netId];
        }

        /// <inheritdoc />
        public bool TryGetComponent<T>(EntityUid uid, [NotNullWhen(true)] out T component)
        {
            if (TryGetComponent(uid, typeof(T), out var comp))
            {
                if (!comp.Deleted)
                {
                    component = (T)comp;
                    return true;
                }
            }

            component = default!;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent<T>([NotNullWhen(true)] EntityUid? uid, [NotNullWhen(true)] out T component)
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

            component = default!;
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

        /// <inheritdoc />
        public bool TryGetComponent([NotNullWhen(true)] EntityUid? uid, Type type, [NotNullWhen(true)] out IComponent? component)
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
        public bool TryGetComponent([NotNullWhen(true)] EntityUid? uid, ushort netId, [MaybeNullWhen(false)] out IComponent component)
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

        /// <inheritdoc />
        public IEnumerable<IComponent> GetComponents(EntityUid uid)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (Component comp in _entCompIndex[uid].ToArray())
            {
                if (comp.Deleted) continue;

                yield return comp;
            }
        }

        /// <inheritdoc />
        public IEnumerable<T> GetComponents<T>(EntityUid uid)
        {
            var comps = _entCompIndex[uid];
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

        #region Join Functions

        /// <inheritdoc />
        public IEnumerable<T> EntityQuery<T>(bool includePaused = false)
        {
            var comps = _entTraitDict[typeof(T)];
            foreach (var comp in comps.Values)
            {
                if (comp.Deleted || !includePaused && comp.Paused) continue;

                yield return (T)(object)comp;
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2)> EntityQuery<TComp1, TComp2>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
        {
            // this would prob be faster if trait1 was a list (or an array of structs hue).
            var trait1 = _entTraitDict[typeof(TComp1)];
            var trait2 = _entTraitDict[typeof(TComp2)];

            // you really want trait1 to be the smaller set of components
            foreach (var kvComp in trait1)
            {
                var uid = kvComp.Key;

                if (!trait2.TryGetValue(uid, out var t2Comp) || t2Comp.Deleted || !includePaused && kvComp.Value.Paused)
                    continue;

                yield return ((TComp1)(object)kvComp.Value, (TComp2)(object)t2Comp);
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2, TComp3)> EntityQuery<TComp1, TComp2, TComp3>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
        {
            var trait1 = _entTraitDict[typeof(TComp1)];
            var trait2 = _entTraitDict[typeof(TComp2)];
            var trait3 = _entTraitDict[typeof(TComp3)];

            foreach (var kvComp in trait1)
            {
                var uid = kvComp.Key;

                if (!trait2.TryGetValue(uid, out var t2Comp) || t2Comp.Deleted || !includePaused && kvComp.Value.Paused)
                    continue;

                if (!trait3.TryGetValue(uid, out var t3Comp) || t3Comp.Deleted)
                    continue;

                yield return ((TComp1)(object)kvComp.Value,
                    (TComp2)(object)t2Comp,
                    (TComp3)(object)t3Comp);
            }
        }

        /// <inheritdoc />
        public IEnumerable<(TComp1, TComp2, TComp3, TComp4)> EntityQuery<TComp1, TComp2, TComp3, TComp4>(bool includePaused = false)
            where TComp1 : IComponent
            where TComp2 : IComponent
            where TComp3 : IComponent
            where TComp4 : IComponent
        {
            var trait1 = _entTraitDict[typeof(TComp1)];
            var trait2 = _entTraitDict[typeof(TComp2)];
            var trait3 = _entTraitDict[typeof(TComp3)];
            var trait4 = _entTraitDict[typeof(TComp4)];

            foreach (var kvComp in trait1)
            {
                var uid = kvComp.Key;

                if (!trait2.TryGetValue(uid, out var t2Comp) || t2Comp.Deleted || !includePaused && kvComp.Value.Paused)
                    continue;

                if (!trait3.TryGetValue(uid, out var t3Comp) || t3Comp.Deleted)
                    continue;

                if (!trait4.TryGetValue(uid, out var t4Comp) || t4Comp.Deleted)
                    continue;

                yield return ((TComp1)(object)kvComp.Value,
                    (TComp2)(object)t2Comp,
                    (TComp3)(object)t3Comp,
                    (TComp4)(object)t4Comp);
            }
        }

        #endregion

        /// <inheritdoc />
        public IEnumerable<IComponent> GetAllComponents(Type type, bool includePaused = false)
        {
            var comps = _entTraitDict[type];
            foreach (var comp in comps.Values)
            {
                if (comp.Deleted || !includePaused && comp.Paused) continue;

                yield return comp;
            }
        }

        /// <inheritdoc />
        public ComponentState GetComponentState(IEventBus eventBus, IComponent component)
        {
            var getState = new ComponentGetState();
            eventBus.RaiseComponentEvent(component, ref getState);

            return getState.State ?? component.GetComponentState();
        }

        public bool CanGetComponentState(IEventBus eventBus, IComponent component, ICommonSession player)
        {
            var attempt = new ComponentGetStateAttemptEvent(player);
            eventBus.RaiseComponentEvent(component, attempt);
            return !attempt.Cancelled;
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void FillComponentDict()
        {
            _entTraitDict.Clear();
            foreach (var refType in _componentFactory.GetAllRefTypes())
            {
                _entTraitDict.Add(refType, new Dictionary<EntityUid, Component>());
            }
        }

        public bool EntityExists(EntityUid uid)
        {
            return _entTraitDict[typeof(MetaDataComponent)].ContainsKey(uid);
        }
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

        public NetComponentEnumerator(Dictionary<ushort, Component> dictionary) => _dictEnum = dictionary.GetEnumerator();
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
}
