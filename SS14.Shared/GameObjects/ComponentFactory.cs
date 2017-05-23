using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;

namespace SS14.Shared.GameObjects
{
    public class ComponentFactory
    {
        private readonly Dictionary<string, Type> componentTypes;

        public ComponentFactory(EntityManager entityManager)
        {
            EntityManager = entityManager;
            componentTypes = new Dictionary<string, Type>();

            ReloadComponents();

            IoCManager.AssemblyAdded += ReloadComponents;
        }

        public EntityManager EntityManager { get; private set; }

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A Component</returns>
        public IComponent GetComponent(Type componentType)
        {
            if (componentType.GetInterface(nameof(IComponent)) == null)
            {
                throw new Exception(string.Format("{0} does not implement {1}", nameof(IComponent)));
            }
            return (IComponent) Activator.CreateInstance(componentType);
        }

        public T GetComponent<T>() where T: IComponent
        {
            return (T)Activator.CreateInstance(typeof(T));
        }

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A Component</returns>
        public IComponent GetComponent(string componentType)
        {
            return (IComponent)Activator.CreateInstance(componentTypes[componentType]);
        }

        public Type GetComponentType(string componentType)
        {
            return componentTypes[componentType];
        }

        private void ReloadComponents()
        {
            foreach (var type in IoCManager.ResolveEnumerable<IComponent>())
            {
                var attribute = (ComponentAttribute)Attribute.GetCustomAttribute(type, typeof(ComponentAttribute));
                if (attribute == null)
                {
                    throw new InvalidImplementationException(type, typeof(ComponentAttribute), "No " + nameof(ComponentAttribute));
                }

                if (componentTypes.ContainsKey(attribute.ID))
                {
                    throw new Exception("Duplicate ID for component: " + attribute.ID);
                }

                componentTypes[attribute.ID] = type;
            }
        }
    }
}
