using SS14.Shared.Interfaces.GameObjects;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace SS14.Shared.GameObjects
{
    public class ComponentFactory : IComponentFactory
    {
        private class ComponentRegistration : IComponentRegistration
        {
            public string Name { get; }
            public uint? NetID { get; }
            public bool NetworkSynchronizeExistence { get; }
            public Type Type { get; }
            internal readonly List<Type> References = new List<Type>();
            IReadOnlyList<Type> IComponentRegistration.References => References;

            public ComponentRegistration(string name, Type type, uint? netID, bool networkSynchronizeExistence)
            {
                Name = name;
                NetID = netID;
                NetworkSynchronizeExistence = networkSynchronizeExistence;
                Type = type;
                References.Add(type);
            }

            public override string ToString()
            {
                return $"ComponentRegistration({Name}: {Type})";
            }
        }

        // Bunch of dictionaries to allow lookups in all directions.
        /// <summary>
        /// Mapping of component name to type.
        /// </summary>
        private readonly Dictionary<string, ComponentRegistration> names = new Dictionary<string, ComponentRegistration>();

        /// <summary>
        /// Mapping of network ID to type.
        /// </summary>
        private readonly Dictionary<uint, ComponentRegistration> netIDs = new Dictionary<uint, ComponentRegistration>();

        /// <summary>
        /// Mapping of concrete component types to their registration.
        /// </summary>
        private readonly Dictionary<Type, ComponentRegistration> types = new Dictionary<Type, ComponentRegistration>();

        /// <summary>
        /// Set of components that should be ignored. Probably just the list of components unique to the other project.
        /// </summary>
        private readonly HashSet<string> IgnoredComponentNames = new HashSet<string>();

        /// <inheritdoc />
        public IEnumerable<Type> AllRegisteredTypes => types.Keys;

        public void Register<T>(bool overwrite = false) where T : IComponent, new()
        {
            if (types.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"Type is already registered: {typeof(T)}");
            }

            // Create a dummy to be able to fetch instance properties like name.
            // Not clean but sadly C# doesn't have static virtual members.
            var dummy = new T();

            var name = dummy.Name;
            var netID = dummy.NetID;
            var netSyncExist = dummy.NetworkSynchronizeExistence;

            if (IgnoredComponentNames.Contains(name))
            {
                if (!overwrite)
                {
                    throw new InvalidOperationException($"{name} is already marked as ignored component");
                }

                IgnoredComponentNames.Remove(name);
            }

            if (names.ContainsKey(name))
            {
                if (!overwrite)
                {
                    throw new InvalidOperationException($"{name} is already registered, previous: {names[name]}");
                }

                RemoveComponent(name);
            }

            if (netID != null && netIDs.ContainsKey(netID.Value))
            {
                if (!overwrite)
                {
                    throw new InvalidOperationException($"{name} has duplicate network ID {netID}, previous: {netIDs[netID.Value]}");
                }

                RemoveComponent(netIDs[netID.Value].Name);
            }

            var registration = new ComponentRegistration(name, typeof(T), netID, netSyncExist);
            names[name] = registration;
            types[typeof(T)] = registration;
            if (netID != null)
            {
                netIDs[netID.Value] = registration;
            }
        }

        public void RegisterReference<TTarget, TInterface>() where TTarget : TInterface, IComponent, new()
        {
            if (!types.ContainsKey(typeof(TTarget)))
            {
                throw new InvalidOperationException($"Unregistered type: {typeof(TTarget)}");
            }

            var registration = types[typeof(TTarget)];
            if (registration.References.Contains(typeof(TInterface)))
            {
                throw new InvalidOperationException($"Attempted to register a reference twice: {typeof(TInterface)}");
            }
            registration.References.Add(typeof(TInterface));
        }

        public void RegisterIgnore(string name, bool overwrite = false)
        {
            if (IgnoredComponentNames.Contains(name))
            {
                throw new InvalidOperationException($"{name} is already registered as ignored");
            }

            if (names.ContainsKey(name))
            {
                if (!overwrite)
                {
                    throw new InvalidOperationException($"{name} is already registered as a component");
                }

                RemoveComponent(name);
            }

            IgnoredComponentNames.Add(name);
        }

        private void RemoveComponent(string name)
        {
            var registration = names[name];

            names.Remove(registration.Name);
            types.Remove(registration.Type);
            if (registration.NetID != null)
            {
                netIDs.Remove(registration.NetID.Value);
            }
        }

        public ComponentAvailability GetComponentAvailability(string componentName)
        {
            if (names.ContainsKey(componentName))
            {
                return ComponentAvailability.Available;
            }

            if (IgnoredComponentNames.Contains(componentName))
            {
                return ComponentAvailability.Ignore;
            }

            return ComponentAvailability.Unknown;
        }

        public IComponent GetComponent(Type componentType)
        {
            if (!types.ContainsKey(componentType))
            {
                throw new InvalidOperationException($"{componentType} is not a registered component.");
            }
            return (IComponent)Activator.CreateInstance(types[componentType].Type);
        }

        public T GetComponent<T>() where T : IComponent, new()
        {
            if (!types.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"{typeof(T)} is not a registered component.");
            }
            return (T)Activator.CreateInstance(types[typeof(T)].Type);
        }

        public IComponent GetComponent(string componentName)
        {
            return (IComponent)Activator.CreateInstance(GetRegistration(componentName).Type);
        }

        public IComponentRegistration GetRegistration(string componentName)
        {
            try
            {
                return names[componentName];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownComponentException($"Unknown name: {componentName}");
            }
        }

        public IComponentRegistration GetRegistration(uint netID)
        {
            try
            {
                return netIDs[netID];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownComponentException($"Unknown net ID: {netID}");
            }
        }

        public IComponentRegistration GetRegistration(Type type)
        {
            try
            {
                return types[type];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownComponentException($"Unknown type: {type}");
            }
        }

        public IComponentRegistration GetRegistration<T>() where T : IComponent, new()
        {
            return GetRegistration(typeof(T));
        }

        public IComponentRegistration GetRegistration(IComponent component)
        {
            return GetRegistration(component.GetType());
        }
    }

    [Serializable]
    public class UnknownComponentException : Exception
    {
        public UnknownComponentException()
        {
        }
        public UnknownComponentException(string message) : base(message)
        {
        }
        public UnknownComponentException(string message, Exception inner) : base(message, inner)
        {
        }
        protected UnknownComponentException(
          SerializationInfo info,
          StreamingContext context) : base(info, context) { }
    }
}
