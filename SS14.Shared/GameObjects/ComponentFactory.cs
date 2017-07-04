using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using SS14.Shared.IoC;
using SS14.Shared.IoC.Exceptions;
using SS14.Shared.Interfaces.GameObjects;
using SS14.Shared.Interfaces.Reflection;

namespace SS14.Shared.GameObjects
{
    public class ComponentFactory : IComponentFactory, IPostInjectInit
    {
        [Dependency]
        private readonly IReflectionManager ReflectionManager;

        private readonly Dictionary<string, Type> componentNames = new Dictionary<string, Type>();

        public void PostInject()
        {
            ReloadComponents();
            ReflectionManager.OnAssemblyAdded += (_, __) => ReloadComponents();
        }

        /// <summary>
        /// Gets a new component instantiated of the specified type.
        /// </summary>
        /// <param name="componentType">type of component to make</param>
        /// <returns>A Component</returns>
        public IComponent GetComponent(Type componentType)
        {
            if (componentType.GetInterface(nameof(IComponent)) == null)
            {
                throw new ArgumentException(string.Format("{0} does not implement {1}", componentType, nameof(IComponent)), nameof(componentType));
            }
            return (IComponent)Activator.CreateInstance(componentType);
        }

        public T GetComponent<T>() where T : IComponent
        {
            return (T)Activator.CreateInstance(typeof(T));
        }

        /// <summary>
        /// Gets a new component instantiated of the specified name.
        /// </summary>
        /// <param name="componentName">name of component to make</param>
        /// <returns>A Component</returns>
        public IComponent GetComponent(string componentName)
        {
            try
            {
                return (IComponent)Activator.CreateInstance(componentNames[componentName]);
            }
            catch (KeyNotFoundException)
            {
                throw new UnknowComponentException(componentName);
            }
        }

        public Type GetComponentType(string componentType)
        {
            return componentNames[componentType];
        }

        private void ReloadComponents()
        {
            foreach (var type in ReflectionManager.GetAllChildren<IComponent>())
            {
                IComponent instance = (IComponent)Activator.CreateInstance(type);
                if (instance.Name == null || instance.Name == "")
                {
                    throw new InvalidImplementationException(type, typeof(IComponent), "Does not have a " + nameof(IComponent.Name));
                }

                if (componentNames.ContainsKey(instance.Name))
                {
                    throw new InvalidImplementationException(type, typeof(IComponent), "Duplicate Name for component: " + instance.Name);
                }

                componentNames[instance.Name] = type;
            }
        }
    }

    public class UnknowComponentException : Exception
    {
        public readonly string ComponentType;
        public UnknowComponentException(string componentType)
        {
            ComponentType = componentType;
        }

        public override string Message => "Unknown component type: " + ComponentType;
    }
}
