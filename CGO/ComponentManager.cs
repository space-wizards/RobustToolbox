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
        public Dictionary<ComponentFamily, List<IGameObjectComponent>> components;

        public ComponentManager()
        {
            components = new Dictionary<ComponentFamily, List<IGameObjectComponent>>();
            foreach (ComponentFamily family in Enum.GetValues(typeof(ComponentFamily)))
            {
                components.Add(family, new List<IGameObjectComponent>());
            }
        }

        public void AddComponent(IGameObjectComponent component)
        {
            components[component.Family].Add(component);
        }

        public void RemoveComponent(IGameObjectComponent component)
        {
            components[component.Family].Remove(component);
        }

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
