using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using ServerInterfaces.GameObject;

namespace SGO
{
    public class ComponentFactory : IComponentFactory
    {
        /// <summary>
        /// Singleton
        /// </summary>
        private static ComponentFactory singleton;

        /// <summary>
        /// Singleton
        /// </summary>
        public static ComponentFactory Singleton
        {
            get
            {
                if (singleton == null)
                    singleton = new ComponentFactory();
                return singleton;
            }
            private set { }
        }

        /// <summary>
        /// Constructor
        /// </summary>
        public ComponentFactory()
        {
            Type type = typeof (IGameObjectComponent);
            List<Assembly> asses = AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<Type> types = asses.SelectMany(t => t.GetTypes()).Where(p => type.IsAssignableFrom(p)).ToList();
            //TODO: Go through the current app domain and get all types that derive from IGameObjectComponent.
            // There should be a type list that has all of these in this class, so instead of instantiating by
            // Type.GetType, we can just hit the type list and pull the right type.
        }


        #region IComponentFactory Members

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A GameObjectComponent</returns>
        public IGameObjectComponent GetComponent(Type componentType)
        {
            if (componentType.GetInterface("IGameObjectComponent") == null)
                return null;
            return (IGameObjectComponent) Activator.CreateInstance(componentType);
        }

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A GameObjectComponent</returns>
        public IGameObjectComponent GetComponent(string componentTypeName)
        {
            if (string.IsNullOrWhiteSpace(componentTypeName))
                return null;
            //Type t = Assembly.GetExecutingAssembly().GetType(componentTypeName); //Get the type
            Type t = Type.GetType("SGO." + componentTypeName); //Get the type
            if (t == null || t.GetInterface("IGameObjectComponent") == null)
                return null;

            return (IGameObjectComponent) Activator.CreateInstance(t); // Return an instance
        }

        #endregion

    }
}