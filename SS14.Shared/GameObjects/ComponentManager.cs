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
        /// <summary>
        /// Dictionary of components -- this is the master list.
        /// </summary>
        private readonly Dictionary<Type, List<IComponent>> _components
            = new Dictionary<Type, List<IComponent>>();

        private readonly List<IComponent> _allComponents = new List<IComponent>();

        [Dependency]
        private readonly IComponentFactory ComponentFactory;

        /// <inheritdoc />
        public void PostInject()
        {
            foreach (var compType in ComponentFactory.AllRegisteredTypes)
            {
                _components[compType] = new List<IComponent>();
            }
        }

        private IEnumerable<IComponent> GetComponents(Type type)
        {
            return _components[type].Where(c => !c.Deleted);
        }

        public IEnumerable<T> GetComponents<T>() where T : IComponent
        {
            return GetComponents(typeof(T)).Cast<T>();
        }

        /// <summary>
        /// Adds a component to the component list.
        /// </summary>
        /// <param name="component"></param>
        public void AddComponent(IComponent component)
        {
            var reg = ComponentFactory.GetRegistration(component);

            foreach (Type type in reg.References)
            {
                if (!_components.ContainsKey(type))
                {
                    _components[type] = new List<IComponent>();
                }
                _components[type].Add(component);
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
                    var index = _components[type].FindIndex(c => c == component);
                    _components[type].RemoveSwap(index);
                }

                // Check the one we just swapped with next iteration.
                i--;
            }
        }

        public void Cull()
        {
            _allComponents.Clear();
            _components.Clear();
        }
    }
}
