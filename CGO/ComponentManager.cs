using System;
using System.Collections.Generic;
using System.Linq;
using ClientInterfaces.GOC;
using GorgonLibrary;
using SS13.IoC;
using SS13_Shared.GO;
using System.Drawing;

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
                // Hack the update loop to allow us to render somewhere in the GameScreen render loop
                if (family == ComponentFamily.Renderable) 
                    continue;
                foreach (IGameObjectComponent component in components[family])
                {
                    component.Update(frameTime);
                }
            }
        }

        /// <summary>
        /// Render the renderables
        /// </summary>
        /// <param name="frametime">time since the last frame was rendered.</param>
        public void Render(float frametime, RectangleF viewPort)
        {
            var renderables = from IRenderableComponent c in components[ComponentFamily.Renderable]
                              orderby c.DrawDepth ascending
                              //orderby c.Owner.Position.Y ascending
                              select c;
                            
            foreach (var component in renderables.ToList())
            {
                component.Render(new Vector2D(viewPort.Left, viewPort.Top), new Vector2D(viewPort.Right, viewPort.Bottom));
            }
        }
    }
}
