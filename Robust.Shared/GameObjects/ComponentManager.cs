using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.Exceptions;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.GameObjects.Components;
using Robust.Shared.IoC;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    /// <inheritdoc />
    public class ComponentManager : IComponentManager
    {
        private const int EntityCapacity = 1024; // starting capacity for entities
        private const int CompTypeCapacity = 32; // starting capacity for component types

        private readonly Dictionary<Type, Dictionary<EntityUid, Component>> _dictComponents
            = new Dictionary<Type, Dictionary<EntityUid, Component>>(CompTypeCapacity);

        private readonly Dictionary<EntityUid, Dictionary<uint, Component>> _netComponents
            = new Dictionary<EntityUid, Dictionary<uint, Component>>(EntityCapacity);

        private readonly List<Component> _deleteList = new List<Component>();

        [Dependency] private readonly IComponentFactory _componentFactory = default!;
#if EXCEPTION_TOLERANCE
        [Dependency] private readonly IRuntimeLog _runtimeLog = default!;
#endif

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
            _dictComponents.Clear();
            _netComponents.Clear();
            _deleteList.Clear();
            FillComponentDict();
        }

        #region Component Management

        /// <inheritdoc />
        public T AddComponent<T>(IEntity entity)
            where T : Component, new()
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var newComponent = (Component) _componentFactory.GetComponent<T>();

            newComponent.Owner = entity;
            AddComponent(entity, newComponent);

            return (T) newComponent;
        }

        /// <inheritdoc />
        public void AddComponent(IEntity entity, Component component, bool overwrite = false)
        {
            if (entity == null || !entity.IsValid())
                throw new ArgumentException("Entity is not valid.", nameof(entity));

            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (component.Owner != entity)
                throw new InvalidOperationException("Component is not owned by entity.");

            // get interface aliases for mapping
            var reg = _componentFactory.GetRegistration(component);

            // Check that there are no overlapping references.
            foreach (var type in reg.References)
            {
                if (!TryGetComponent(entity.Uid, type, out var duplicate))
                    continue;

                if (!overwrite)
                    throw new InvalidOperationException(
                        $"Component reference type {type} already occupied by {duplicate}");

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
                _dictComponents[type].Add(entity.Uid, component);
            }

            // add the component to the netId grid
            if (component.NetID != null)
            {
                // the main comp grid keeps this in sync
                if (!_netComponents.TryGetValue(entity.Uid, out var netDict))
                {
                    netDict = new Dictionary<uint, Component>(CompTypeCapacity);
                    _netComponents.Add(entity.Uid, netDict);
                }

                netDict.Add(component.NetID.Value, component);

                // mark the component as dirty for networking
                component.Dirty();

                ComponentAdded?.Invoke(this, new ComponentEventArgs(component));
            }

            component.OnAdd();

            if (entity.Initialized || entity.Initializing)
            {
                component.Initialize();

                if (entity.Initialized)
                {
                    component.Running = true;
                }
            }
        }

        /// <inheritdoc />
        public void RemoveComponent<T>(EntityUid uid)
        {
            RemoveComponent(uid, typeof(T));
        }

        /// <inheritdoc />
        public void RemoveComponent(EntityUid uid, Type type)
        {
            var component = GetComponent(uid, type);
            RemoveComponentDeferred((Component) component, false);
        }

        /// <inheritdoc />
        public void RemoveComponent(EntityUid uid, uint netId)
        {
            var component = GetComponent(uid, netId);
            RemoveComponentDeferred((Component) component, false);
        }

        /// <inheritdoc />
        public void RemoveComponent(EntityUid uid, IComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (component.Owner == null || component.Owner.Uid != uid)
                throw new InvalidOperationException("Component is not owned by entity.");

            RemoveComponentDeferred((Component)component, false);
        }

        /// <inheritdoc />
        public void RemoveComponents(EntityUid uid)
        {
            foreach (var kvTypeDict in _dictComponents)
            {
                // because we are iterating over references instead of instances, and a comp instance
                // can have multiple references, we filter out already deleted instances.
                if (kvTypeDict.Value.TryGetValue(uid, out var comp) && !comp.Deleted)
                {
                    RemoveComponentDeferred(comp, false);
                }
            }
        }

        /// <inheritdoc />
        public void DisposeComponents(EntityUid uid)
        {
            foreach (var kvTypeDict in _dictComponents)
            {
                if (kvTypeDict.Value.TryGetValue(uid, out var comp))
                {
                    #if EXCEPTION_TOLERANCE
                    try
                    {
#endif

                        comp.Running = false;
#if EXCEPTION_TOLERANCE
                    }
                    catch (Exception e)
                    {
                        _runtimeLog.LogException(e,
                            $"DisposeComponents comp.Running=false, owner={uid}, type={comp.GetType()}");
                    }
#endif
                }
            }

            foreach (var kvTypeDict in _dictComponents)
            {
                // because we are iterating over references instead of instances, and a comp instance
                // can have multiple references, we filter out already deleted instances.
                if (kvTypeDict.Value.TryGetValue(uid, out var comp) && !comp.Deleted)
                {
                    RemoveComponentDeferred(comp, true);
                }
            }
        }

        private void RemoveComponentDeferred(Component component, bool removeProtected)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (component.Deleted)
                return;

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

                _deleteList.Add(component);

                component.Running = false;
                component.OnRemove();
                ComponentRemoved?.Invoke(this, new ComponentEventArgs(component));
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
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (component.Deleted)
                return;

            // these two components are required on all entities and cannot be removed.
            if (component is ITransformComponent || component is IMetaDataComponent)
            {
                DebugTools.Assert("Tried to remove a protected component.");
                return;
            }

            component.Running = false;
            component.OnRemove();
            ComponentRemoved?.Invoke(this, new ComponentEventArgs(component));

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
                var typeDict = _dictComponents[refType];
                typeDict.Remove(entityUid);
            }

            if (component.NetID == null)
                return;

            var netDict = _netComponents[entityUid];
            netDict.Remove(component.NetID.Value);

            // mark the owning entity as dirty for networking
            component.Owner.Dirty();

            ComponentDeleted?.Invoke(this, new ComponentEventArgs(component));
        }

        /// <inheritdoc />
        public bool HasComponent<T>(EntityUid uid)
        {
            return HasComponent(uid, typeof(T));
        }

        /// <inheritdoc />
        public bool HasComponent(EntityUid uid, Type type)
        {
            if (!_dictComponents.TryGetValue(type, out var typeDict))
                return false;

            return typeDict.ContainsKey(uid);
        }

        /// <inheritdoc />
        public bool HasComponent(EntityUid uid, uint netId)
        {
            if (!_netComponents.TryGetValue(uid, out var comp))
                return false;

            return comp.ContainsKey(netId);
        }

        /// <inheritdoc />
        public T GetComponent<T>(EntityUid uid)
            where T : Component
        {
            return (T) GetComponent(uid, typeof(T));
        }

        /// <inheritdoc />
        public IComponent GetComponent(EntityUid uid, Type type)
        {
            var typeDict = _dictComponents[type];
            try
            {
                return typeDict[uid];
            }
            catch (KeyNotFoundException)
            {
                throw new KeyNotFoundException($"Entity {uid} does not have a component of type {type}");
            }
        }

        /// <inheritdoc />
        public IComponent GetComponent(EntityUid uid, uint netId)
        {
            var netDict = _netComponents[uid];
            return netDict[netId];
        }

        /// <inheritdoc />
        public bool TryGetComponent<T>(EntityUid uid, [NotNullWhen(true)] out T component)
        {
            if (TryGetComponent(uid, typeof(T), out var comp))
            {
                component = (T) comp;
                return true;
            }

            component = default!;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent(EntityUid uid, Type type, [NotNullWhen(true)] out IComponent? component)
        {
            if (_dictComponents.TryGetValue(type, out var typeDict))
            {
                if (typeDict != null && typeDict.TryGetValue(uid, out var comp))
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
            if (_netComponents.TryGetValue(uid, out var netDict))
            {
                if (netDict != null && netDict.TryGetValue(netId, out var comp))
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
            foreach (var (type, kvTypeDict) in _dictComponents)
            {
                if (kvTypeDict.TryGetValue(uid, out var comp) && type == comp.GetType() && !comp.Deleted)
                {
                    yield return comp;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<T> GetComponents<T>(EntityUid uid)
        {
            foreach (var (type, kvTypeDict) in _dictComponents)
            {
                if (kvTypeDict.TryGetValue(uid, out var comp) && comp is T t && type == comp.GetType() && !comp.Deleted)
                {
                    yield return t;
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetNetComponents(EntityUid uid)
        {
            return _netComponents[uid].Values;
        }

        /// <inheritdoc />
        public IEnumerable<T> GetAllComponents<T>()
            where T : IComponent
        {
            if (!_dictComponents.TryGetValue(typeof(T), out var typeDict))
            {
                return Enumerable.Empty<T>();
            }

            var list = new List<T>(typeDict.Count);
            foreach (var comp in typeDict.Values)
            {
                if (comp.Deleted)
                {
                    continue;
                }

                list.Add((T) (object) comp);
            }

            return list;
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetAllComponents(Type type)
        {
            if (!_dictComponents.TryGetValue(type, out var typeDict))
            {
                return Enumerable.Empty<IComponent>();
            }

            var list = new List<IComponent>(typeDict.Count);
            foreach (var comp in typeDict.Values)
            {
                if (comp.Deleted)
                {
                    continue;
                }

                list.Add(comp);
            }

            return list;
        }

        #endregion

        private void FillComponentDict()
        {
            foreach (var refType in _componentFactory.GetAllRefTypes())
            {
                _dictComponents.Add(refType, new Dictionary<EntityUid, Component>());
            }
        }
    }

    /// <summary>
    ///
    /// </summary>
    public class ComponentEventArgs : EventArgs
    {
        /// <summary>
        /// Component that this event relates to.
        /// </summary>
        public IComponent Component { get; }

        /// <summary>
        /// Constructs a new instance of <see cref="ComponentEventArgs"/>.
        /// </summary>
        /// <param name="component"></param>
        public ComponentEventArgs(IComponent component)
        {
            Component = component;
        }
    }
}
