using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
using SS14.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SS14.Shared.GameObjects
{
    public class ComponentManager : IComponentManager
    {
        /// <summary>
        /// Dictionary of components -- this is the master list.
        /// </summary>
        private readonly Dictionary<Type, List<IComponent>> components
            = new Dictionary<Type, List<IComponent>>();

        private readonly List<IComponent> allComponents = new List<IComponent>();

        [Dependency]
        private readonly IComponentFactory ComponentFactory;

        public IEnumerable<IComponent> GetComponents(Type type)
        {
            return components[type].Where(c => !c.Deleted);
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
                if (!components.ContainsKey(type))
                {
                    components[type] = new List<IComponent>();
                }
                components[type].Add(component);
            }

            allComponents.Add(component);
        }

        /// <summary>
        /// Big update method -- loops through all components in order of family and calls Update() on them.
        /// </summary>
        /// <param name="frameTime">Time since the last frame was rendered.</param>
        public void Update(float frameTime)
        {
            // Cull components and update them too.
            for (var i = 0; i < allComponents.Count; i++)
            {
                var component = allComponents[i];
                if (!component.Deleted)
                {
                    component.Update(frameTime);
                    continue;
                }

                var reg = ComponentFactory.GetRegistration(component);
                allComponents.RemoveSwap(i);

                foreach (Type type in reg.References)
                {
                    // TODO: This is ridiculously slow due to O(n) removal times.
                    var index = components[type].FindIndex(c => c == component);
                    components[type].RemoveSwap(index);
                }

                // Check the one we just swapped with next iteration.
                i--;
            }
        }

        public void Cull()
        {
            allComponents.Clear();
            components.Clear();
        }
    }
}
