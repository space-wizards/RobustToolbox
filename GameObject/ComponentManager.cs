using System;
using System.Collections.Generic;
using System.Linq;
using SS13_Shared.GO;

namespace GameObject
{
    public class ComponentManager
    {
        /// <summary>
        /// Dictionary of components -- this is the master list.
        /// </summary>
        public Dictionary<ComponentFamily, List<Component>> components;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ComponentManager()
        {
            components = new Dictionary<ComponentFamily, List<Component>>();
            foreach (ComponentFamily family in Enum.GetValues(typeof (ComponentFamily)))
            {
                components.Add(family, new List<Component>());
            }
        }

        public List<Component> GetComponents(ComponentFamily family)
        {
            return components[family].Cast<Component>().ToList();
        }

        /// <summary>
        /// Adds a component to the component list.
        /// </summary>
        /// <param name="component"></param>
        public void AddComponent(Component component)
        {
            components[component.Family].Add(component);
        }

        /// <summary>
        /// Removes a component from the list.
        /// </summary>
        /// <param name="component"></param>
        public void RemoveComponent(Component component)
        {
            components[component.Family].Remove(component);
        }

        /// <summary>
        /// Big update method -- loops through all components in order of family and calls Update() on them.
        /// </summary>
        /// <param name="frameTime">Time since the last frame was rendered.</param>
        public void Update(float frameTime)
        {
            foreach (ComponentFamily family in Enum.GetValues(typeof (ComponentFamily)))
            {
                // Hack the update loop to allow us to render somewhere in the GameScreen render loop
                /*if (family == ComponentFamily.Renderable)
                    continue;*/
                foreach (Component component in components[family])
                {
                    component.Update(frameTime);
                }
            }
        }
    }
}