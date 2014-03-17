using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace GameObject
{
    public class ComponentFactory
    {
        private readonly string _componentNamespace;
        private readonly List<Type> _types;

        public ComponentFactory(EntityManager entityManager, string componentNamespace)
        {
            EntityManager = entityManager;
            _componentNamespace = componentNamespace;

            Type type = typeof (IComponent);
            List<Assembly> asses = AppDomain.CurrentDomain.GetAssemblies().ToList();
            _types = asses.SelectMany(t => t.GetTypes()).Where(p => type.IsAssignableFrom(p)).ToList();
        }

        public EntityManager EntityManager { get; private set; }

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A Component</returns>
        public IComponent GetComponent(Type componentType)
        {
            if (componentType.GetInterface("IComponent") == null)
                return null;
            return (IComponent) Activator.CreateInstance(componentType);
        }

        public T GetComponent<T>() where T:IComponent
        {
            return (T) Activator.CreateInstance(typeof (T));
        }

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A Component</returns>
        public IComponent GetComponent(string componentTypeName)
        {
            if (componentTypeName == "KeyBindingInputMoverComponent")
                componentTypeName = "PlayerInputMoverComponent";
            if (componentTypeName == "NetworkMoverComponent")
                componentTypeName = "BasicMoverComponent";
            if (string.IsNullOrWhiteSpace(componentTypeName))
                return null;
            string fullName = _componentNamespace + "." + componentTypeName;
            //Type t = Assembly.GetExecutingAssembly().GetType(componentTypeName); //Get the type
            Type t = _types.FirstOrDefault(type => type.FullName == fullName);
            //Type t = Type.GetType(_componentNamespace + "." + componentTypeName); //Get the type
            if (t == null || t.GetInterface("IComponent") == null)
                throw new TypeLoadException("Cannot find specified component type: " + fullName);

            return (IComponent) Activator.CreateInstance(t); // Return an instance
        }
    }
}