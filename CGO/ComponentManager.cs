using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CGO
{
    public class ComponentManager
    {
        private static ComponentManager singleton;
        public static ComponentManager Singleton
        {
            get
            {
                if (singleton == null)
                    singleton = new ComponentManager();
                return singleton;
            }
            private set { }
        }
        /// <summary>
        /// Dictionary of components -- this is the master list.
        /// </summary>
        public Dictionary<ComponentFamily, List<IGameObjectComponent>> components;

        /// <summary>
        /// Constructor.
        /// </summary>
        public ComponentManager()
        {
            components = new Dictionary<ComponentFamily, List<IGameObjectComponent>>();
            foreach (ComponentFamily family in Enum.GetValues(typeof(ComponentFamily)))
            {
                components.Add(family, new List<IGameObjectComponent>());
            }
        }

        /// <summary>
        /// Adds a component to the component list.
        /// </summary>
        /// <param name="component"></param>
        public void AddComponent(IGameObjectComponent component)
        {
            components[component.Family].Add(component);
        }

        /// <summary>
        /// Removes a component from the list.
        /// </summary>
        /// <param name="component"></param>
        public void RemoveComponent(IGameObjectComponent component)
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
                foreach (IGameObjectComponent component in components[family])
                {
                    component.Update(frameTime);
                }
            }
        }
    }
}
