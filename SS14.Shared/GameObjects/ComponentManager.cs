using SS14.Shared.GameObjects;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.IoC;
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
        private readonly Dictionary<ComponentFamily, List<IComponent>> components
            = new Dictionary<ComponentFamily, List<IComponent>>();

        public ComponentManager()
        {
            foreach (ComponentFamily family in Enum.GetValues(typeof(ComponentFamily)))
            {
                components[family] = new List<IComponent>();
            }
        }

        public IEnumerable<IComponent> GetComponents(ComponentFamily family)
        {
            return components[family];
        }

        /// <summary>
        /// Adds a component to the component list.
        /// </summary>
        /// <param name="component"></param>
        public void AddComponent(IComponent component)
        {
            components[component.Family].Add(component);
        }

        /// <summary>
        /// Removes a component from the list.
        /// </summary>
        /// <param name="component"></param>
        public void RemoveComponent(IComponent component)
        {
            components[component.Family].Remove(component);
        }

        /// <summary>
        /// Big update method -- loops through all components in order of family and calls Update() on them.
        /// </summary>
        /// <param name="frameTime">Time since the last frame was rendered.</param>
        public void Update(float frameTime)
        {
            foreach (ComponentFamily family in Enum.GetValues(typeof(ComponentFamily)))
            {
                // Hack the update loop to allow us to render somewhere in the GameScreen render loop
                /*if (family == ComponentFamily.Renderable)
                    continue;*/
                foreach (IComponent component in components[family])
                {
                    component.Update(frameTime);
                }
            }
        }
    }
}
