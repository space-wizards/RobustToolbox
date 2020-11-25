using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;

namespace Robust.Shared.GameObjects
{
    public class ComponentFactory : IComponentFactory
    {
        [Dependency] private readonly IDynamicTypeFactoryInternal _typeFactory = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private class ComponentRegistration : IComponentRegistration
        {
            public string Name { get; }
            public uint? NetID { get; }
            public bool NetworkSynchronizeExistence { get; }
            public Type Type { get; }
            internal readonly List<Type> References = new();
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
        private readonly Dictionary<string, ComponentRegistration> names = new();

        /// <summary>
        /// Mapping of network ID to type.
        /// </summary>
        private readonly Dictionary<uint, ComponentRegistration> netIDs = new();

        /// <summary>
        /// Mapping of concrete component types to their registration.
        /// </summary>
        private readonly Dictionary<Type, ComponentRegistration> types = new();

        /// <summary>
        /// Set of components that should be ignored. Probably just the list of components unique to the other project.
        /// </summary>
        private readonly HashSet<string> IgnoredComponentNames = new();

        /// <inheritdoc />
        public IEnumerable<Type> AllRegisteredTypes => types.Keys;

        private IEnumerable<ComponentRegistration> AllRegistrations => types.Values;

        public void Register<T>(bool overwrite = false) where T : IComponent, new()
        {
            Register(typeof(T), overwrite);
        }

        private void Register(Type type, bool overwrite=false)
        {
            if (types.ContainsKey(type))
            {
                throw new InvalidOperationException($"Type is already registered: {type}");
            }

            // Create a dummy to be able to fetch instance properties like name.
            // Not clean but sadly C# doesn't have static virtual members.
            var dummy = (IComponent)Activator.CreateInstance(type)!;

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

            var registration = new ComponentRegistration(name, type, netID, netSyncExist);
            names[name] = registration;
            types[type] = registration;
            if (netID != null)
            {
                netIDs[netID.Value] = registration;
            }
        }

        public void RegisterReference<TTarget, TInterface>() where TTarget : TInterface, IComponent, new()
        {
            RegisterReference(typeof(TTarget), typeof(TInterface));
        }

        private void RegisterReference(Type target, Type @interface)
        {
            if (!types.ContainsKey(target))
            {
                throw new InvalidOperationException($"Unregistered type: {target}");
            }

            var registration = types[target];
            if (registration.References.Contains(@interface))
            {
                throw new InvalidOperationException($"Attempted to register a reference twice: {@interface}");
            }
            registration.References.Add(@interface);
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
            return _typeFactory.CreateInstanceUnchecked<IComponent>(types[componentType].Type);
        }

        public T GetComponent<T>() where T : IComponent, new()
        {
            if (!types.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"{typeof(T)} is not a registered component.");
            }
            return _typeFactory.CreateInstanceUnchecked<T>(types[typeof(T)].Type);
        }

        public IComponent GetComponent(string componentName)
        {
            return _typeFactory.CreateInstanceUnchecked<IComponent>(GetRegistration(componentName).Type);
        }

        public IComponent GetComponent(uint netId)
        {
            return _typeFactory.CreateInstanceUnchecked<IComponent>(GetRegistration(netId).Type);
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

        public IComponentRegistration GetRegistration(Type reference)
        {
            try
            {
                return types[reference];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownComponentException($"Unknown type: {reference}");
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

        public bool TryGetRegistration(string componentName, [NotNullWhen(true)] out IComponentRegistration? registration)
        {
            if (names.TryGetValue(componentName, out var tempRegistration))
            {
                registration = tempRegistration;
                return true;
            }

            registration = null;
            return false;
        }

        public bool TryGetRegistration(Type reference, [NotNullWhen(true)] out IComponentRegistration? registration)
        {
            if (types.TryGetValue(reference, out var tempRegistration))
            {
                registration = tempRegistration;
                return true;
            }

            registration = null;
            return false;
        }

        public bool TryGetRegistration<T>([NotNullWhen(true)] out IComponentRegistration? registration) where T : IComponent, new()
        {
            return TryGetRegistration(typeof(T), out registration);
        }

        public bool TryGetRegistration(uint netID, [NotNullWhen(true)] out IComponentRegistration? registration)
        {
            if (netIDs.TryGetValue(netID, out var tempRegistration))
            {
                registration = tempRegistration;
                return true;
            }

            registration = null;
            return false;
        }

        public bool TryGetRegistration(IComponent component, [NotNullWhen(true)] out IComponentRegistration? registration)
        {
            return TryGetRegistration(component.GetType(), out registration);
        }

        public void DoAutoRegistrations()
        {
            var iComponent = typeof(IComponent);

            foreach (var type in _reflectionManager.FindTypesWithAttribute<RegisterComponentAttribute>())
            {
                if (!iComponent.IsAssignableFrom(type))
                {
                    Logger.Error("Type {0} has RegisterComponentAttribute but does not implement IComponent.", type);
                    continue;
                }

                Register(type);

                foreach (var attribute in Attribute.GetCustomAttributes(type, typeof(ComponentReferenceAttribute)))
                {
                    var cast = (ComponentReferenceAttribute) attribute;

                    var refType = cast.ReferenceType;

                    if (!refType.IsAssignableFrom(type))
                    {
                        Logger.Error("Type {0} has reference for type it does not implement: {1}.", type, refType);
                        continue;
                    }

                    RegisterReference(type, refType);
                }
            }
        }

        public IEnumerable<Type> GetAllRefTypes()
        {
            return AllRegistrations.SelectMany(r => r.References).Distinct();
        }

        public IEnumerable<uint> GetAllNetIds()
        {
            foreach (var registration in AllRegistrations)
            {
                if (registration.NetID != null)
                {
                    yield return registration.NetID.Value;
                }
            }
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
