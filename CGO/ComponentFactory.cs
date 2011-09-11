using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;

namespace CGO
{
    public class ComponentFactory
    {
        private static ComponentFactory singleton;
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

        public ComponentFactory()
        {
            Type type = typeof(IGameObjectComponent);
            List<Assembly> asses = AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<Type> types = asses.SelectMany(t => t.GetTypes()).Where(p => type.IsAssignableFrom(p)).ToList();
            //TODO: Go through the current app domain and get all types that derive from IGameObjectComponent.
            // There should be a type list that has all of these in this class, so instead of instantiating by
            // Type.GetType, we can just hit the type list and pull the right type.
        }

        public IGameObjectComponent GetComponent(Type componentType)
        {
            if (componentType.GetInterface("IGameObjectComponent") == null)
                return null;
            return (IGameObjectComponent)Activator.CreateInstance(componentType);
        }

        public IGameObjectComponent GetComponent(string componentTypeName)
        {
            if (componentTypeName == null || componentTypeName == "")
                return null;
            //Type t = Assembly.GetExecutingAssembly().GetType(componentTypeName); //Get the type
            Type t = Type.GetType("CGO." + componentTypeName); //Get the type
            if (t == null || t.GetInterface("IGameObjectComponent") == null)
                return null;

            return (IGameObjectComponent)Activator.CreateInstance(t); // Return an instance
        }
    }
}
