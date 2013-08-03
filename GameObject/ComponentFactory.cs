using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;

namespace GameObject
{
    public class ComponentFactory
    {
        public EntityManager EntityManager { get; private set; }
        private string _componentNamespace;
        private List<Type> _types; 
        public ComponentFactory(EntityManager entityManager, string componentNamespace)
        {
            EntityManager = entityManager;
            _componentNamespace = componentNamespace;

            Type type = typeof(IComponent);
            List<Assembly> asses = AppDomain.CurrentDomain.GetAssemblies().ToList();
            _types = asses.SelectMany(t => t.GetTypes()).Where(p => type.IsAssignableFrom(p)).ToList();
        }

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A Component</returns>
        public IComponent GetComponent(Type componentType)
        {
            if (componentType.GetInterface("IComponent") == null)
                return null;
            return (IComponent)Activator.CreateInstance(componentType);
        }

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A Component</returns>
        public IComponent GetComponent(string componentTypeName)
        {
            if (string.IsNullOrWhiteSpace(componentTypeName))
                return null;
            var fullName = _componentNamespace + "." + componentTypeName;
            //Type t = Assembly.GetExecutingAssembly().GetType(componentTypeName); //Get the type
            var t = _types.FirstOrDefault(type => type.FullName == fullName);
            //Type t = Type.GetType(_componentNamespace + "." + componentTypeName); //Get the type
            if (t == null || t.GetInterface("IComponent") == null)
                throw new TypeLoadException("Cannot find specified component type: " + fullName);

            return (IComponent)Activator.CreateInstance(t); // Return an instance
        }
    }
}
