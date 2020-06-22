using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Exceptions;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using DependencyAttribute = Robust.Shared.IoC.DependencyAttribute;

namespace Robust.Shared.GameObjects
{

    /// <inheritdoc />
    public class ComponentManager : IComponentManager
    {

        [Dependency] private readonly IComponentFactory _componentFactory = default!;

#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

        private const int HighCapacity = 32 * 1024; // starting capacity for entities per component types

        private readonly Dictionary<(EntityUid, uint), Component> _entNetIdDict = new Dictionary<(EntityUid, uint), Component>(HighCapacity / 2);

        private readonly Dictionary<(EntityUid, Type), Component> _entTraitDict = new Dictionary<(EntityUid, Type), Component>(HighCapacity);

        private readonly HashSet<Component> _deleteList = new HashSet<Component>();

        private UniqueIndex<Type, Component> _traitCompIndex;

        private UniqueIndex<uint, Component> _netIdCompIndex;

        private UniqueIndex<EntityUid, Component> _entCompIndex;

        /// <inheritdoc />
        public event EventHandler<ComponentEventArgs>? ComponentAdded;

        /// <inheritdoc />
        public event EventHandler<ComponentEventArgs>? ComponentRemoved;

        /// <inheritdoc />
        public event EventHandler<ComponentEventArgs>? ComponentDeleted;

        public void Initialize()
        {
            FillComponentDict();
        }

        /// <inheritdoc />
        public void Clear()
        {
            _entNetIdDict.Clear();
            _entTraitDict.Clear();
            _traitCompIndex.Clear();
            _netIdCompIndex.Clear();
            _entCompIndex.Clear();
            _deleteList.Clear();
            FillComponentDict();
        }

        #region Component Management

        /// <inheritdoc />
        public T AddComponent<T>(IEntity entity) where T : Component, new()
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            var newComponent = _componentFactory.GetComponent<T>();

            newComponent.Owner = entity;

            AddComponent(entity, newComponent);

            return newComponent;
        }

        /// <inheritdoc />
        public void AddComponent<T>(IEntity entity, T component, bool overwrite = false) where T : Component
        {
            if (entity == null || !entity.IsValid()) throw new ArgumentException("Entity is not valid.", nameof(entity));

            if (component == null) throw new ArgumentNullException(nameof(component));

            if (component.Owner != entity) throw new InvalidOperationException("Component is not owned by entity.");

            var uid = entity.Uid;

            // get interface aliases for mapping
            var reg = _componentFactory.GetRegistration(component);

            // Check that there are no overlapping references.
            foreach (var type in reg.References)
            {
                if (!TryGetComponent(uid, type, out var duplicate)) continue;

                if (!overwrite) throw new InvalidOperationException($"Component reference type {type} already occupied by {duplicate}");

                // these two components are required on all entities and cannot be overwritten.
                if (duplicate is ITransformComponent || duplicate is IMetaDataComponent)
                {
                    throw new InvalidOperationException("Tried to overwrite a protected component.");
                }

                RemoveComponentImmediate((Component) duplicate);
            }

            // add the component to the grid
            foreach (var type in reg.References)
            {
                _entTraitDict.Add((uid, type), component);
                _traitCompIndex.Add(type, component);
                _entCompIndex.Add(uid, component);
            }

            // add the component to the netId grid
            if (component.NetID != null)
            {
                // the main comp grid keeps this in sync

                var netId = component.NetID.Value;
                if (_entNetIdDict.TryAdd((uid, netId), component))
                {
                    _netIdCompIndex.Add(netId, component);
                }

                // mark the component as dirty for networking
                component.Dirty();

                ComponentAdded?.Invoke(this, new AddedComponentEventArgs(component));
            }

            component.ExposeData(DefaultValueSerializer.Reader());

            component.OnAdd();

            if (!entity.Initialized && !entity.Initializing) return;

            component.Initialize();

            if (entity.Initialized)
            {
                component.Running = true;
            }
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
            RemoveComponentDeferred((Component) GetComponent(uid, type), false);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityUid uid, uint netId)
        {
            RemoveComponentDeferred((Component) GetComponent(uid, netId), false);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RemoveComponent(EntityUid uid, IComponent component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            if (component.Owner == null || component.Owner.Uid != uid) throw new InvalidOperationException("Component is not owned by entity.");

            RemoveComponentDeferred((Component) component, false);
        }

        /// <inheritdoc />
        public void RemoveComponents(EntityUid uid)
        {
            _entCompIndex.Remove(uid);

            foreach (var comp in _entCompIndex[uid])
            {
                RemoveComponentDeferred(comp, false);
            }
        }

        /// <inheritdoc />
        public void DisposeComponents(EntityUid uid)
        {
            foreach (var comp in _entCompIndex[uid])
            {
                RemoveComponentDeferred(comp, true);
            }
        }

        private void RemoveComponentDeferred(Component component, bool removeProtected)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            if (component.Deleted) return;

#if EXCEPTION_TOLERANCE
            try
            {
#endif
                // these two components are required on all entities and cannot be removed normally.
                if (!removeProtected && (component is ITransformComponent || component is IMetaDataComponent))
                {
                    DebugTools.Assert("Tried to remove a protected component.");
                    return;
                }

                if (!_deleteList.Add(component))
                {
                    // already deferred deletion
                    return;
                }

                component.Running = false;
                component.OnRemove();
                ComponentRemoved?.Invoke(this, new RemovedComponentEventArgs(component));
#if EXCEPTION_TOLERANCE
            }
            catch (Exception e)
            {
                _runtimeLog.LogException(e,
                    $"RemoveComponentDeferred, owner={component.Owner}, type={component.GetType()}");
            }
#endif
        }

        private void RemoveComponentImmediate(Component component)
        {
            if (component == null) throw new ArgumentNullException(nameof(component));

            if (component.Deleted) return;

            // these two components are required on all entities and cannot be removed.
            if (component is ITransformComponent || component is IMetaDataComponent)
            {
                DebugTools.Assert("Tried to remove a protected component.");
                return;
            }

            component.Running = false;
            component.OnRemove();
            ComponentRemoved?.Invoke(this, new RemovedComponentEventArgs(component));

            DeleteComponent(component);
        }

        /// <inheritdoc />
        public void CullRemovedComponents()
        {
            foreach (var component in _deleteList)
            {
                DeleteComponent(component);
            }

            _deleteList.Clear();
        }

        private void DeleteComponent(Component component)
        {
            var reg = _componentFactory.GetRegistration(component.GetType());

            var entityUid = component.Owner.Uid;

            foreach (var refType in reg.References)
            {
                _entTraitDict.Remove((entityUid, refType));
                _traitCompIndex.Remove(refType, component);
            }

            if (component.NetID == null) return;

            var netId = component.NetID.Value;
            _entNetIdDict.Remove((entityUid, netId));
            _netIdCompIndex.Remove(netId, component);

            // mark the owning entity as dirty for networking
            component.Owner.Dirty();

            ComponentDeleted?.Invoke(this, new DeletedComponentEventArgs(component));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent<T>(EntityUid uid)
        {
            return HasComponent(uid, typeof(T));
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityUid uid, Type type)
        {
            return _entTraitDict.TryGetValue((uid, type), out var comp) && !comp.Deleted;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool HasComponent(EntityUid uid, uint netId)
        {
            return _entNetIdDict.TryGetValue((uid, netId), out var comp) && !comp.Deleted;
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public T GetComponent<T>(EntityUid uid)
        {
            return (T) GetComponent(uid, typeof(T));
        }

        /// <inheritdoc />
        public IComponent GetComponent(EntityUid uid, Type type)
        {
            // ReSharper disable once InvertIf
            if (_entTraitDict.TryGetValue((uid, type), out var comp))
            {
                if (!comp.Deleted)
                {
                    return comp;
                }
            }

            throw new KeyNotFoundException($"Entity {uid} does not have a component of type {type}");
        }

        /// <inheritdoc />
        public IComponent GetComponent(EntityUid uid, uint netId)
        {
            // ReSharper disable once InvertIf
            if (_entNetIdDict.TryGetValue((uid, netId), out var comp))
            {
                if (!comp.Deleted)
                {
                    return comp;
                }
            }

            throw new KeyNotFoundException($"Entity {uid} does not have a component of NetID {netId}");
        }

        /// <inheritdoc />
        public bool TryGetComponent<T>(EntityUid uid, [NotNullWhen(true)] out T component)
        {
            if (TryGetComponent(uid, typeof(T), out var comp))
            {
                if (!comp.Deleted)
                {
                    component = (T) comp;
                    return true;
                }
            }

            component = default!;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent(EntityUid uid, Type type, [NotNullWhen(true)] out IComponent? component)
        {
            if (_entTraitDict.TryGetValue((uid, type), out var comp))
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
        public bool TryGetComponent(EntityUid uid, uint netId, [NotNullWhen(true)] out IComponent? component)
        {
            if (_entNetIdDict.TryGetValue((uid, netId), out var comp))
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
        public IEnumerable<IComponent> GetComponents(EntityUid uid)
        {
            // ReSharper disable once LoopCanBeConvertedToQuery
            foreach (Component comp in _entCompIndex[uid])
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
                if (comp.Deleted || !(comp is T tComp)) continue;

                yield return tComp;
            }
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetNetComponents(EntityUid uid)
        {
            var comps = _entCompIndex[uid];
            foreach (var comp in comps)
            {
                if (comp.Deleted || comp.NetID == null) continue;

                yield return comp;
            }
        }

        /// <inheritdoc />
        public IEnumerable<T> GetAllComponents<T>()
        {
            var comps = _traitCompIndex[typeof(T)];
            foreach (var comp in comps)
            {
                if (comp.Deleted) continue;

                if (comp is T typedComp)
                {
                    yield return typedComp;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetAllComponents(Type type)
        {
            var comps = _traitCompIndex[type];
            foreach (var comp in comps)
            {
                if (comp.Deleted) continue;

                yield return comp;
            }
        }

        #endregion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void FillComponentDict()
        {
            _traitCompIndex.Initialize(_componentFactory.GetAllRefTypes());
        }

    }

}
