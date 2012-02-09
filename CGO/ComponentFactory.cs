using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using ClientInterfaces.GOC;

namespace CGO
{
    public class ComponentFactory
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
            Type type = typeof(IGameObjectComponent);
            List<Assembly> asses = AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<Type> types = asses.SelectMany(t => t.GetTypes()).Where(p => type.IsAssignableFrom(p)).ToList();
            //TODO: Go through the current app domain and get all types that derive from IGameObjectComponent.
            // There should be a type list that has all of these in this class, so instead of instantiating by
            // Type.GetType, we can just hit the type list and pull the right type.
        }

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A GameObjectComponent</returns>
        public IGameObjectComponent GetComponent(Type componentType)
        {
            if (componentType.GetInterface("IGameObjectComponent") == null)
                return null;
            return (IGameObjectComponent)Activator.CreateInstance(componentType);
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
            Type t = Type.GetType("CGO." + componentTypeName); //Get the type
            if (t == null || t.GetInterface("IGameObjectComponent") == null)
                return null;

            return (IGameObjectComponent)Activator.CreateInstance(t); // Return an instance
        }

        /// <summary>
        /// Gets a component type from given name.
        /// </summary>
        /// <param name="componentType">Name of component type required.</param>
        /// <returns>A component Type</returns>
        public Type GetComponentType(string componentTypeName)
        {
            if (string.IsNullOrWhiteSpace(componentTypeName))
                return null;
            return Type.GetType("CGO." + componentTypeName, false);
        }
    }
}
