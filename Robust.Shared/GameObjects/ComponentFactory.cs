using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.Serialization;
using JetBrains.Annotations;
using Robust.Shared.Console;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    internal class ComponentFactory : IComponentFactory
    {
        private readonly IDynamicTypeFactoryInternal _typeFactory;
        private readonly IReflectionManager _reflectionManager;

        private class ComponentRegistration : IComponentRegistration
        {
            public string Name { get; }
            public ushort? NetID { get; set; }
            public Type Type { get; }
            internal readonly List<Type> References = new();
            IReadOnlyList<Type> IComponentRegistration.References => References;

            public ComponentRegistration(string name, Type type)
            {
                Name = name;
                NetID = null;
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
        /// Mapping of lowercase component names to their registration.
        /// </summary>
        private readonly Dictionary<string, string> _lowerCaseNames = new();

        /// <summary>
        /// Mapping of network ID to type.
        /// </summary>
        private List<IComponentRegistration>? _networkedComponents;

        /// <summary>
        /// Mapping of concrete component types to their registration.
        /// </summary>
        private readonly Dictionary<Type, ComponentRegistration> types = new();

        /// <summary>
        /// Set of components that should be ignored. Probably just the list of components unique to the other project.
        /// </summary>
        private readonly HashSet<string> IgnoredComponentNames = new();

        /// <inheritdoc />
        public event Action<IComponentRegistration>? ComponentAdded;

        /// <inheritdoc />
        public event Action<(IComponentRegistration, Type)>? ComponentReferenceAdded;

        /// <inheritdoc />
        public event Action<string>? ComponentIgnoreAdded;

        /// <inheritdoc />
        public IEnumerable<Type> AllRegisteredTypes => types.Keys;

        /// <inheritdoc />
        public IReadOnlyList<IComponentRegistration>? NetworkedComponents => _networkedComponents;

        private IEnumerable<ComponentRegistration> AllRegistrations => types.Values;

        public ComponentFactory(IDynamicTypeFactoryInternal typeFactory, IReflectionManager reflectionManager, IConsoleHost conHost)
        {
            _typeFactory = typeFactory;
            _reflectionManager = reflectionManager;

            conHost.RegisterCommand("dump_net_comps", "Prints the table of networked components.", "dump_net_comps", (shell, argStr, args) =>
            {
                if (_networkedComponents is null)
                {
                    shell.WriteError("Registration still writeable, network ids have not been generated.");
                    return;
                }

                shell.WriteLine("Networked Component Registrations:");

                for (int netId = 0; netId < _networkedComponents.Count; netId++)
                {
                    var registration = _networkedComponents[netId];
                    shell.WriteLine($"  [{netId,4}] {registration.Name,-16} {registration.Type.Name}");
                }
            });
        }

        private void Register(Type type, bool overwrite = false)
        {
            if (_networkedComponents is not null)
                throw new ComponentRegistrationLockException();

            if (types.ContainsKey(type))
            {
                throw new InvalidOperationException($"Type is already registered: {type}");
            }

            if (!type.IsSubclassOf(typeof(Component)))
            {
                throw new InvalidOperationException($"Type is not derived from component: {type}");
            }

            var name = CalculateComponentName(type);
            var lowerCaseName = name.ToLowerInvariant();

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

            if (_lowerCaseNames.ContainsKey(lowerCaseName))
            {
                if (!overwrite)
                {
                    throw new InvalidOperationException($"{lowerCaseName} is already registered, previous: {_lowerCaseNames[lowerCaseName]}");
                }
            }

            var registration = new ComponentRegistration(name, type);
            names[name] = registration;
            _lowerCaseNames[lowerCaseName] = name;
            types[type] = registration;

            ComponentAdded?.Invoke(registration);

            static string CalculateComponentName(Type type)
            {
                // Backward compatible fallback
                if (type.GetProperty(nameof(Component.Name))!.DeclaringType != typeof(Component))
                {
                    var instance = (IComponent) Activator.CreateInstance(type)!;
                    return instance.Name;
                }

                // Attributes can use any name they want, they are for bypassing the automatic names
                // If a parent class has this attribute, a child class will use the same name, unless it also uses this attribute
                if (Attribute.GetCustomAttribute(type, typeof(ComponentProtoNameAttribute)) is ComponentProtoNameAttribute attribute)
                    return attribute.PrototypeName;

                const string component = "Component";
                var typeName = type.Name;
                if (!typeName.EndsWith(component))
                {
                    throw new InvalidComponentNameException($"Component {type} must end with the word Component");
                }

                string name = typeName[..^component.Length];
                const string client = "Client";
                const string server = "Server";
                const string shared = "Shared";
                if (typeName.StartsWith(client, StringComparison.Ordinal))
                {
                    name = typeName[client.Length..^component.Length];
                }
                else if (typeName.StartsWith(server, StringComparison.Ordinal))
                {
                    name = typeName[server.Length..^component.Length];
                }
                else if (typeName.StartsWith(shared, StringComparison.Ordinal))
                {
                    name = typeName[shared.Length..^component.Length];
                }
                DebugTools.Assert(name != String.Empty, $"Component {type} has invalid name {type.Name}");
                return name;
            }
        }

        private void RegisterReference(Type target, Type @interface)
        {
            if (_networkedComponents is not null)
                throw new ComponentRegistrationLockException();

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
            ComponentReferenceAdded?.Invoke((registration, @interface));
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
            ComponentIgnoreAdded?.Invoke(name);
        }

        private void RemoveComponent(string name)
        {
            if (_networkedComponents is not null)
                throw new ComponentRegistrationLockException();

            var registration = names[name];

            names.Remove(registration.Name);
            _lowerCaseNames.Remove(registration.Name.ToLowerInvariant());
            types.Remove(registration.Type);
        }

        public ComponentAvailability GetComponentAvailability(string componentName, bool ignoreCase = false)
        {
            if (ignoreCase && _lowerCaseNames.TryGetValue(componentName, out var lowerCaseName))
            {
                componentName = lowerCaseName;
            }

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

        public IComponent GetComponent(string componentName, bool ignoreCase = false)
        {
            if (ignoreCase && _lowerCaseNames.TryGetValue(componentName, out var lowerCaseName))
            {
                componentName = lowerCaseName;
            }

            return _typeFactory.CreateInstanceUnchecked<IComponent>(GetRegistration(componentName).Type);
        }

        public IComponent GetComponent(ushort netId)
        {
            return _typeFactory.CreateInstanceUnchecked<IComponent>(GetRegistration(netId).Type);
        }

        public IComponentRegistration GetRegistration(string componentName, bool ignoreCase = false)
        {
            if (ignoreCase && _lowerCaseNames.TryGetValue(componentName, out var lowerCaseName))
            {
                componentName = lowerCaseName;
            }

            try
            {
                return names[componentName];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownComponentException($"Unknown name: {componentName}");
            }
        }

        public string GetComponentName(Type componentType)
        {
            return GetRegistration(componentType).Name;
        }

        public IComponentRegistration GetRegistration(ushort netID)
        {
            if (_networkedComponents is null)
                throw new ComponentRegistrationLockException();

            try
            {
                return _networkedComponents[netID];
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

        public bool TryGetRegistration(string componentName, [NotNullWhen(true)] out IComponentRegistration? registration, bool ignoreCase = false)
        {
            if (ignoreCase && _lowerCaseNames.TryGetValue(componentName, out var lowerCaseName))
            {
                componentName = lowerCaseName;
            }

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

        public bool TryGetRegistration(ushort netID, [NotNullWhen(true)] out IComponentRegistration? registration)
        {
            if (_networkedComponents is not null && _networkedComponents.TryGetValue(netID, out var tempRegistration))
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
            foreach (var type in _reflectionManager.FindTypesWithAttribute<RegisterComponentAttribute>())
            {
                RegisterClass(type);
            }
        }

        /// <inheritdoc />
        public void RegisterClass<T>(bool overwrite = false)
            where T : IComponent, new()
        {
            RegisterClass(typeof(T));
        }

        private void RegisterClass(Type type)
        {
            if (!typeof(IComponent).IsAssignableFrom(type))
            {
                Logger.Error("Type {0} has RegisterComponentAttribute but does not implement IComponent.", type);
                return;
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

        public IEnumerable<Type> GetAllRefTypes()
        {
            return AllRegistrations.SelectMany(r => r.References).Distinct();
        }

        /// <inheritdoc />
        public void GenerateNetIds()
        {
            // assumptions:
            // component names are guaranteed to be unique
            // component names are 1:1 with component concrete types

            // a subset of component names are networked
            var networkedRegs = new List<IComponentRegistration>(names.Count);

            foreach (var kvRegistration in names)
            {
                var registration = kvRegistration.Value;
                if (Attribute.GetCustomAttribute(registration.Type, typeof(NetworkedComponentAttribute)) is NetworkedComponentAttribute)
                {
                    networkedRegs.Add(registration);
                }
            }

            // The sorting implementation is unstable, but there are no duplicate names, so that isn't a problem.
            // Ordinal comparison is used so that the resulting order is always identical on every computer.
            networkedRegs.Sort((a, b) => string.Compare(a.Name, b.Name, StringComparison.Ordinal));

            for (ushort i = 0; i < networkedRegs.Count; i++)
            {
                var registration = (ComponentRegistration) networkedRegs[i];
                registration.NetID = i;
            }

            _networkedComponents = networkedRegs;
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

    public class ComponentRegistrationLockException : Exception { }

    public class InvalidComponentNameException : Exception { public InvalidComponentNameException(string message) : base(message) { } }
}
