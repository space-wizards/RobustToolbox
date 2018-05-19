using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Shared.GameObjects
{
    public class ComponentManager : IComponentManager, IPostInjectInit
    {
        private const int EntityCapacity = 1024; // starting capacity for entities
        private const int CompTypeCapacity = 32; // starting capacity for component types

        /// <summary>
        /// Dictionary of components -- this is the master list.
        /// </summary>
        private readonly Dictionary<Type, List<IComponent>> _listComponents
            = new Dictionary<Type, List<IComponent>>();

        private readonly List<IComponent> _allComponents = new List<IComponent>();

        private readonly Dictionary<Type, Dictionary<EntityUid, Component>> _dictComponents
            = new Dictionary<Type, Dictionary<EntityUid, Component>>(CompTypeCapacity);

        private readonly Dictionary<EntityUid, Dictionary<uint, Component>> _netComponents
            = new Dictionary<EntityUid, Dictionary<uint, Component>>(EntityCapacity);

        [Dependency]
        private readonly IComponentFactory ComponentFactory;

        [Dependency]
        private readonly IEntityManager EntityManager;

        /// <inheritdoc />
        public void PostInject()
        {
            foreach (var compType in ComponentFactory.AllRegisteredTypes)
            {
                _listComponents[compType] = new List<IComponent>();
            }
        }

        private IEnumerable<IComponent> GetComponents(Type type)
        {
            if (_listComponents.TryGetValue(type, out var compList))
                return compList.Where(c => !c.Deleted);

            return Enumerable.Empty<IComponent>();
        }

        public IEnumerable<T> GetComponents<T>() where T : IComponent
        {
            return GetComponents(typeof(T)).Cast<T>();
        }

        /// <summary>
        /// Adds a component to the component list.
        /// </summary>
        /// <param name="component"></param>
        public void AddComponentOld(IComponent component)
        {
            var reg = ComponentFactory.GetRegistration(component);

            foreach (Type type in reg.References)
            {
                if (!_listComponents.ContainsKey(type))
                {
                    _listComponents[type] = new List<IComponent>();
                }
                _listComponents[type].Add(component);
            }

            _allComponents.Add(component);
        }

        /// <summary>
        /// Big update method -- loops through all components in order of family and calls Update() on them.
        /// </summary>
        /// <param name="frameTime">Time since the last frame was rendered.</param>
        public void Update(float frameTime)
        {
            // Cull components and update them too.
            for (var i = 0; i < _allComponents.Count; i++)
            {
                var component = _allComponents[i];
                if (!component.Deleted)
                {
                    component.Update(frameTime);
                    continue;
                }

                var reg = ComponentFactory.GetRegistration(component);
                _allComponents.RemoveSwap(i);

                foreach (Type type in reg.References)
                {
                    // TODO: This is ridiculously slow due to O(n) removal times.
                    var index = _listComponents[type].FindIndex(c => c == component);
                    _listComponents[type].RemoveSwap(index);
                }

                // Check the one we just swapped with next iteration.
                i--;
            }
        }

        public void Cull()
        {
            _allComponents.Clear();
            _listComponents.Clear();
        }

        #region Component Management

        public T AddComponent<T>(IEntity entity)
            where T : Component, new()
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));

            var newComponent = (Component)ComponentFactory.GetComponent<T>();

            newComponent.Owner = entity;
            AddComponent(entity, newComponent);

            return (T)newComponent;
        }

        public void AddComponent(IEntity entity, Component component)
        {
            // get interface aliases for mapping
            var reg = ComponentFactory.GetRegistration(component);

            // Check that there are no overlapping references.
            foreach (var type in reg.References)
            {
                if (!TryGetComponent(entity.Uid, type, out var duplicate))
                    continue;

                throw new InvalidOperationException($"Component reference type {type} already occupied by {duplicate}");
            }

            // add the component to the grid
            foreach (var type in reg.References)
            {
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
            }
            component.OnAdd();

            if (entity.Initialized)
            {
                component.Initialize();
            }
        }

        public void RemoveComponent<T>(EntityUid uid)
        {
            RemoveComponent(uid, typeof(T));
        }

        public void RemoveComponent(EntityUid uid, Type type)
        {
            var component = GetComponent(uid, type);
            var reg = ComponentFactory.GetRegistration(type);

            EntityManager.RemoveSubscribedEvents(component);
            component.OnRemove();

            foreach (var refType in reg.References)
            {
                var typeDict = _dictComponents[refType];
                typeDict.Remove(uid);
            }

            if (component.NetID != null)
            {
                var netDict = _netComponents[uid];
                netDict.Remove(component.NetID.Value);
            }
        }

        public void RemoveComponent(EntityUid uid, uint netID)
        {
            throw new NotImplementedException();
        }

        public bool HasComponent<T>(EntityUid uid)
        {
            return HasComponent(uid, typeof(T));
        }

        public bool HasComponent(EntityUid uid, Type type)
        {
            if (!_dictComponents.TryGetValue(type, out var typeDict))
                return false;

            return typeDict.ContainsKey(uid);
        }

        public bool HasComponent(EntityUid uid, uint netID)
        {
            throw new NotImplementedException();
        }

        public T GetComponent<T>(EntityUid uid)
            where T : Component
        {
            return (T)GetComponent(uid, typeof(T));
        }

        public IComponent GetComponent(EntityUid uid, Type type)
        {
            var typeDict = _dictComponents[type];
            return typeDict[uid];
        }

        public IComponent GetComponent(EntityUid uid, uint netID)
        {
            throw new NotImplementedException();
        }

        public bool TryGetComponent<T>(EntityUid uid, out T component)
            where T : Component
        {
            if (TryGetComponent(uid, typeof(T), out var comp))
            {
                component = (T)comp;
                return true;
            }

            component = default(T);
            return false;
        }

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

        public bool TryGetComponent(EntityUid uid, uint netID, out IComponent component)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<IComponent> GetComponents(EntityUid uid)
        {
            throw new NotImplementedException();
        }

        public IEnumerable<T> GetComponents<T>(EntityUid uid)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
