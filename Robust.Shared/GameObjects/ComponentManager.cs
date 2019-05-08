using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.IoC;

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

        [Dependency]
        private readonly IComponentFactory _componentFactory;

        [Dependency]
        private readonly IEntityManager _entityManager;

        /// <inheritdoc />
        public void Clear()
        {
            _dictComponents.Clear();
            _netComponents.Clear();
            _deleteList.Clear();
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
                    throw new InvalidOperationException($"Component reference type {type} already occupied by {duplicate}");

                RemoveComponentImmediate((Component) duplicate);
            }

            // add the component to the grid
            foreach (var type in reg.References)
            {
                // new types can be added at any time
                if (!_dictComponents.TryGetValue(type, out var typeDict))
                {
                    typeDict = new Dictionary<EntityUid, Component>(EntityCapacity);
                    _dictComponents.Add(type, typeDict);
                }

                typeDict.Add(entity.Uid, component);
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
            }

            component.OnAdd();

            if (entity.Initialized)
            {
                component.Initialize();
                component.Startup();
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
            RemoveComponentDeferred(component as Component);
        }

        /// <inheritdoc />
        public void RemoveComponent(EntityUid uid, uint netId)
        {
            var comp = GetComponent(uid, netId);
            RemoveComponentDeferred(comp as Component);
        }

        /// <inheritdoc />
        public void RemoveComponent(EntityUid uid, IComponent component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (component.Owner == null || component.Owner.Uid != uid)
                throw new InvalidOperationException("Component is not owned by entity.");

            RemoveComponentDeferred(component as Component);
        }

        /// <inheritdoc />
        public void RemoveComponents(EntityUid uid)
        {
            foreach (var kvTypeDict in _dictComponents)
            {
                if (kvTypeDict.Value.TryGetValue(uid, out var comp))
                {
                    RemoveComponentDeferred(comp);
                }
            }
        }

        private void RemoveComponentDeferred(Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (component.Deleted)
                return;

            _deleteList.Add(component);

            if (component.Running)
                component.Shutdown();

            component.OnRemove();
        }

        private void RemoveComponentImmediate(Component component)
        {
            if (component == null)
                throw new ArgumentNullException(nameof(component));

            if (component.Deleted)
                return;

            if (component.Running)
                component.Shutdown();

            component.OnRemove();

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

            _entityManager.RemoveSubscribedEvents(component);

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
        public bool TryGetComponent<T>(EntityUid uid, out T component)
            where T : class
        {
            if (TryGetComponent(uid, typeof(T), out var comp))
            {
                component = (T) comp;
                return true;
            }

            component = default;
            return false;
        }

        /// <inheritdoc />
        public bool TryGetComponent(EntityUid uid, Type type, out IComponent component)
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
        public bool TryGetComponent(EntityUid uid, uint netId, out IComponent component)
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
            foreach (var kvTypeDict in _dictComponents.Values)
            {
                if (kvTypeDict.TryGetValue(uid, out var comp))
                    yield return comp;
            }
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetComponentInstances(EntityUid uid)
        {
            var results = new List<IComponent>();

            foreach (var kvTypeDict in _dictComponents.Values)
            {
                if (kvTypeDict.TryGetValue(uid, out var comp) && !results.Contains(comp))
                {
                    results.Add(comp);
                }
            }

            return results;
        }

        /// <inheritdoc />
        public IEnumerable<T> GetComponents<T>(EntityUid uid)
        {
            return GetComponents(uid).OfType<T>();
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetNetComponents(EntityUid uid)
        {
            foreach (var kvNetComp in _netComponents[uid])
            {
                if (!kvNetComp.Value.Deleted)
                    yield return kvNetComp.Value;
            }
        }

        /// <inheritdoc />
        public IEnumerable<T> GetAllComponents<T>()
            where T : IComponent
        {
            if (_dictComponents.TryGetValue(typeof(T), out var typeDict))
                return typeDict.Values.Cast<T>().Where(c => !c.Deleted);

            return Enumerable.Empty<T>();
        }

        /// <inheritdoc />
        public IEnumerable<IComponent> GetAllComponents(Type type)
        {
            if (_dictComponents.TryGetValue(type, out var typeDict))
                return typeDict.Values.Where(c => !c.Deleted);

            return Enumerable.Empty<IComponent>();
        }
        #endregion
    }
}
