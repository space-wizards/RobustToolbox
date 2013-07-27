using System;
using System.Collections.Generic;
using System.Linq;
using SS13_Shared.GO;

namespace GameObject
{
    public interface IEntity
    {
        string Name { get; set; }
        EntityManager EntityManager { get; }

        EntityTemplate Template { get; set; }
        /// <summary>
        /// Match
        /// 
        /// Allows us to fetch entities with a defined SET of components
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        bool Match(EntityQuery query);

        /// <summary>
        /// Public method to add a component to an entity.
        /// Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="family">the family of component -- there can only be one at a time per family.</param>
        /// <param name="component">The component.</param>
        void AddComponent(ComponentFamily family, IComponent component);

        /// <summary>
        /// Public method to remove a component from an entity.
        /// Calls the onRemove method of the component, which handles removing it 
        /// from the component manager and shutting down the component.
        /// </summary>
        /// <param name="family"></param>
        void RemoveComponent(ComponentFamily family);
        
        /// <summary>
        /// Checks to see if a component of a certain family exists
        /// </summary>
        /// <param name="family">componentfamily to check</param>
        /// <returns>true if component exists, false otherwise</returns>
        bool HasComponent(ComponentFamily family);

        T GetComponent<T>(ComponentFamily family) where T : class;

        /// <summary>
        /// Gets the component of the specified family, if it exists
        /// </summary>
        /// <param name="family">componentfamily to get</param>
        /// <returns></returns>
        IComponent GetComponent(ComponentFamily family);

        void Shutdown();
        List<IComponent> GetComponents();
        List<ComponentFamily> GetComponentFamilies();
    }

    public class Entity : IEntity
    {
        #region Members
        private List<Type> _componentTypes = new List<Type>();

        public EntityTemplate Template { get; set; }
        public string Name { get; set; }

        /// <summary>
        /// Holds this entity's components
        /// </summary>
        private readonly Dictionary<ComponentFamily, IComponent> _components =
            new Dictionary<ComponentFamily, IComponent>();

        #endregion

        #region constructor 

        public EntityManager EntityManager { get; private set; }
        public Entity(EntityManager entityManager)
        {
            EntityManager = entityManager;
        }
        #endregion


        #region Entity Systems
        /// <summary>
        /// Match
        /// 
        /// Allows us to fetch entities with a defined SET of components
        /// </summary>
        /// <param name="query"></param>
        /// <returns></returns>
        public bool Match(EntityQuery query)
        {
            // Empty queries always result in a match - equivalent to SELECT * FROM ENTITIES
            if (!(query.Exclusionset.Any() || query.OneSet.Any() || query.AllSet.Any()))
                return true;

            //If there is an EXCLUDE set, and the entity contains any component types in that set, or subtypes of them, the entity is excluded.
            bool matched = !(query.Exclusionset.Any() && query.Exclusionset.Any(t => _componentTypes.Any(t.IsAssignableFrom)));

            //If there are no matching exclusions, and the entity matches the ALL set, the entity is included
            if (matched && (query.AllSet.Any() && query.AllSet.Any(t => !_componentTypes.Any(t.IsAssignableFrom))))
                matched = false;
            //If the entity matches so far, and it matches the ONE set, it matches.
            if (matched && (query.OneSet.Any() && query.OneSet.Any(t => _componentTypes.Any(t.IsAssignableFrom))))
                matched = false;
            return matched;
        }
        #endregion

        #region Components
        /// <summary>
        /// Public method to add a component to an entity.
        /// Calls the component's onAdd method, which also adds it to the component manager.
        /// </summary>
        /// <param name="family">the family of component -- there can only be one at a time per family.</param>
        /// <param name="component">The component.</param>
        public void AddComponent(ComponentFamily family, IComponent component)
        {
            if (_components.Keys.Contains(family))
                RemoveComponent(family);
            _components.Add(family, component);
            component.OnAdd(this);
            UpdateComponentTypes();
        }

        /// <summary>
        /// Public method to remove a component from an entity.
        /// Calls the onRemove method of the component, which handles removing it 
        /// from the component manager and shutting down the component.
        /// </summary>
        /// <param name="family"></param>
        public void RemoveComponent(ComponentFamily family)
        {
            if (!_components.Keys.Contains(family)) return;
            UpdateComponentTypes();
            _components[family].OnRemove();
            _components.Remove(family);
        }

        protected void UpdateComponentTypes()
        {
            _componentTypes = _components.Values.Select(t => t.GetType()).ToList();
        }

        /// <summary>
        /// Checks to see if a component of a certain family exists
        /// </summary>
        /// <param name="family">componentfamily to check</param>
        /// <returns>true if component exists, false otherwise</returns>
        public bool HasComponent(ComponentFamily family)
        {
            return _components.ContainsKey(family);
        }

        public T GetComponent<T>(ComponentFamily family) where T : class
        {
            if (GetComponent(family) is T)
                return (T)GetComponent(family);
            return null;
        }

        /// <summary>
        /// Gets the component of the specified family, if it exists
        /// </summary>
        /// <param name="family">componentfamily to get</param>
        /// <returns></returns>
        public IComponent GetComponent(ComponentFamily family)
        {
            return _components.ContainsKey(family) ? _components[family] : null;
        }

        public virtual void Shutdown()
        {
            foreach (var component in _components.Values)
            {
                component.OnRemove();
            }
            _components.Clear();
            _componentTypes.Clear();
        }

        public List<IComponent> GetComponents()
        {
            return _components.Values.ToList();
        } 

        public List<ComponentFamily> GetComponentFamilies()
        {
            return _components.Keys.ToList();
        } 
        #endregion
    }
}
