using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    [Virtual]
    internal class ComponentFactory(
            IDynamicTypeFactoryInternal _typeFactory,
            IReflectionManager _reflectionManager,
            ISerializationManager _serManager,
            ILogManager logManager) : IComponentFactory
    {
        private readonly ISawmill _sawmill = logManager.GetSawmill("ent.componentFactory");

        // Bunch of dictionaries to allow lookups in all directions.
        /// <summary>
        /// Mapping of component name to type.
        /// </summary>
        private FrozenDictionary<string, ComponentRegistration> _names
            = FrozenDictionary<string, ComponentRegistration>.Empty;

        /// <summary>
        /// Mapping of lowercase component names to their registration.
        /// </summary>
        private FrozenDictionary<string, string> _lowerCaseNames
            = FrozenDictionary<string, string>.Empty;

        /// <summary>
        /// Mapping of network ID to type.
        /// </summary>
        private List<ComponentRegistration>? _networkedComponents;

        /// <summary>
        /// Mapping of concrete component types to their registration.
        /// </summary>
        private FrozenDictionary<Type, ComponentRegistration> _types
            = FrozenDictionary<Type, ComponentRegistration>.Empty;

        private ComponentRegistration[] _array = Array.Empty<ComponentRegistration>();

        /// <summary>
        /// Any component name requested that ends with this postfix, and is missing
        /// will be treated as ignored, instead of throwing an error
        /// </summary>
        private string? _ignoreMissingComponentPostfix = null;

        /// <summary>
        /// Set of components that should be ignored. Probably just the list of components unique to the other project.
        /// </summary>
        private FrozenSet<string> _ignored = FrozenSet<string>.Empty;

        private FrozenDictionary<CompIdx, Type> _idxToType
            = FrozenDictionary<CompIdx, Type>.Empty;

        /// <summary>
        /// Slow-path for Type -> CompIdx mapping without generics.
        /// </summary>
        private FrozenDictionary<Type, CompIdx> _typeToIdx = FrozenDictionary<Type, CompIdx>.Empty;

        /// <inheritdoc />
        public event Action<ComponentRegistration[]>? ComponentsAdded;

        /// <inheritdoc />
        public event Action<string>? ComponentIgnoreAdded;

        /// <inheritdoc />
        public IEnumerable<Type> AllRegisteredTypes => _types.Keys;

        /// <inheritdoc />
        public IReadOnlyList<ComponentRegistration>? NetworkedComponents => _networkedComponents;

        private IEnumerable<ComponentRegistration> AllRegistrations => _types.Values;

        private ComponentRegistration Register(Type type,
            CompIdx idx,
            Dictionary<string, ComponentRegistration> names,
            Dictionary<string, string> lowerCaseNames,
            Dictionary<Type, ComponentRegistration> types,
            Dictionary<CompIdx, Type> idxToType,
            HashSet<string> ignored,
            bool overwrite = false)
        {
            if (_networkedComponents is not null)
                throw new ComponentRegistrationLockException();

            if (types.ContainsKey(type))
                throw new InvalidOperationException($"Type is already registered: {type}");

            if (!type.IsSubclassOf(typeof(Component)))
                throw new InvalidOperationException($"Type is not derived from component: {type}");

            if (!typeof(IComponent).IsAssignableFrom(type))
                throw new InvalidOperationException($"Type {type} has RegisterComponentAttribute but does not implement IComponent.");

            var name = CalculateComponentName(type);
            var lowerCaseName = name.ToLowerInvariant();

            if (ignored.Contains(name))
            {
                if (!overwrite)
                    throw new InvalidOperationException($"{name} is already marked as ignored component");

                ignored.Remove(name);
            }

            if (names.TryGetValue(name, out var prev))
            {
                if (!overwrite)
                    throw new InvalidOperationException($"{name} is already registered, previous: {prev}");

                types.Remove(prev.Type);
                names.Remove(prev.Name);
                lowerCaseNames.Remove(prev.Name);
            }

            if (!overwrite && lowerCaseNames.TryGetValue(lowerCaseName, out var prevName))
                throw new InvalidOperationException($"{lowerCaseName} is already registered, previous: {prevName}");

            var unsaved = type.HasCustomAttribute<UnsavedComponentAttribute>();

            var registration = new ComponentRegistration(name, type, idx, unsaved);

            idxToType[idx] = type;
            names[name] = registration;
            lowerCaseNames[lowerCaseName] = name;
            types[type] = registration;
            CompIdx.AssignArray(ref _array, idx, registration);
            return registration;
        }

        private static string CalculateComponentName(Type type)
        {
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

        /// <inheritdoc />
        public void RegisterNetworkedFields<T>(params string[] fields) where T : IComponent
        {
            var compReg = GetRegistration(CompIdx.Index<T>());
            RegisterNetworkedFields(compReg, fields);
        }

        /// <inheritdoc />
        public void RegisterNetworkedFields(ComponentRegistration compReg, params string[] fields)
        {
            // Nothing to do.
            if (compReg.NetworkedFields.Length > 0 || fields.Length == 0)
                return;

            DebugTools.Assert(fields.Length <= 32);

            if (fields.Length > 32)
            {
                throw new NotSupportedException(
                    "Components with more than 32 networked fields unsupported! Consider splitting it up or making a pr for 64-bit flags");
            }

            compReg.NetworkedFields = fields;
            var lookup = new Dictionary<string, int>(fields.Length);
            var i = 0;

            foreach (var field in fields)
            {
                lookup[field] = i;
                i++;
            }

            compReg.NetworkedFieldLookup = lookup.ToFrozenDictionary();
        }

        public void IgnoreMissingComponents(string postfix = "")
        {
            if (_ignoreMissingComponentPostfix != null && _ignoreMissingComponentPostfix != postfix)
            {
                throw new InvalidOperationException("Ignoring multiple prefixes is not supported");
            }
            _ignoreMissingComponentPostfix = postfix ?? throw new ArgumentNullException(nameof(postfix));
        }

        public IComponent GetComponent(EntityPrototype.ComponentRegistryEntry entry)
        {
            var copy = GetComponent(entry.Component.GetType());
            _serManager.CopyTo(entry.Component, ref copy, notNullableOverride: true);
            return copy;
        }

        public void RegisterIgnore(params string[] names)
        {
            foreach (var name in names)
            {
                if (_names.ContainsKey(name))
                    throw new InvalidOperationException($"Cannot add {name} to ignored components: It is already registered as a component");
            }

            var set = _ignored.ToHashSet();
            foreach (var name in names)
            {
                if (!set.Add(name))
                    _sawmill.Warning($"Duplicate ignored component: {name}");
            }

            _ignored = set.ToFrozenSet();

            foreach (var name in names)
            {
                ComponentIgnoreAdded?.Invoke(name);
            }
        }

        public ComponentAvailability GetComponentAvailability(string componentName, bool ignoreCase = false)
        {
            if (ignoreCase && _lowerCaseNames.TryGetValue(componentName, out var lowerCaseName))
            {
                componentName = lowerCaseName;
            }

            if (_names.ContainsKey(componentName))
            {
                return ComponentAvailability.Available;
            }

            if (_ignored.Contains(componentName) ||
                (_ignoreMissingComponentPostfix != null &&
                componentName.EndsWith(_ignoreMissingComponentPostfix)))
            {
                return ComponentAvailability.Ignore;
            }

            return ComponentAvailability.Unknown;
        }

        public IComponent GetComponent(Type componentType)
        {
            if (!_types.ContainsKey(componentType))
            {
                throw new InvalidOperationException($"{componentType} is not a registered component.");
            }
            return _typeFactory.CreateInstanceUnchecked<IComponent>(_types[componentType].Type);
        }

        public IComponent GetComponent(CompIdx componentType)
        {
            return _typeFactory.CreateInstanceUnchecked<IComponent>(_array[componentType.Value].Type);
        }

        public T GetComponent<T>() where T : IComponent, new()
        {
            if (!_types.ContainsKey(typeof(T)))
            {
                throw new InvalidOperationException($"{typeof(T)} is not a registered component.");
            }
            return _typeFactory.CreateInstanceUnchecked<T>(_types[typeof(T)].Type);
        }

        public IComponent GetComponent(ComponentRegistration reg)
        {
            return (IComponent) _typeFactory.CreateInstanceUnchecked(reg.Type);
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

        public ComponentRegistration GetRegistration(string componentName, bool ignoreCase = false)
        {
            if (ignoreCase && _lowerCaseNames.TryGetValue(componentName, out var lowerCaseName))
            {
                componentName = lowerCaseName;
            }

            try
            {
                return _names[componentName];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownComponentException($"Unknown name: {componentName}");
            }
        }

        [Pure]
        public string GetComponentName(Type componentType)
        {
            return GetRegistration(componentType).Name;
        }

        [Pure]
        public string GetComponentName<T>() where T : IComponent, new()
        {
            return GetRegistration<T>().Name;
        }

        [Pure]
        public string GetComponentName(ushort netID)
        {
            return GetRegistration(netID).Name;
        }

        public ComponentRegistration GetRegistration(ushort netID)
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

        public ComponentRegistration GetRegistration(Type reference)
        {
            try
            {
                return _types[reference];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownComponentException($"Unknown type: {reference}");
            }
        }

        public ComponentRegistration GetRegistration<T>() where T : IComponent, new()
        {
            return GetRegistration(CompIdx.Index<T>());
        }

        public ComponentRegistration GetRegistration(IComponent component)
        {
            return GetRegistration(component.GetType());
        }

        public ComponentRegistration GetRegistration(CompIdx idx) => _array[idx.Value];

        public bool IsIgnored(string componentName) => _ignored.Contains(componentName);

        public bool TryGetRegistration(string componentName, [NotNullWhen(true)] out ComponentRegistration? registration, bool ignoreCase = false)
        {
            if (ignoreCase && _lowerCaseNames.TryGetValue(componentName, out var lowerCaseName))
            {
                componentName = lowerCaseName;
            }

            if (_names.TryGetValue(componentName, out var tempRegistration))
            {
                registration = tempRegistration;
                return true;
            }

            registration = null;
            return false;
        }

        public bool TryGetRegistration(Type reference, [NotNullWhen(true)] out ComponentRegistration? registration)
        {
            if (_types.TryGetValue(reference, out var tempRegistration))
            {
                registration = tempRegistration;
                return true;
            }

            registration = null;
            return false;
        }

        public bool TryGetRegistration<T>([NotNullWhen(true)] out ComponentRegistration? registration) where T : IComponent, new()
        {
            return TryGetRegistration(typeof(T), out registration);
        }

        public bool TryGetRegistration(ushort netID, [NotNullWhen(true)] out ComponentRegistration? registration)
        {
            if (_networkedComponents is not null && _networkedComponents.TryGetValue(netID, out var tempRegistration))
            {
                registration = tempRegistration;
                return true;
            }

            registration = null;
            return false;
        }

        public bool TryGetRegistration(IComponent component, [NotNullWhen(true)] out ComponentRegistration? registration)
        {
            return TryGetRegistration(component.GetType(), out registration);
        }

        public void DoAutoRegistrations()
        {
            var types = _reflectionManager.FindTypesWithAttribute<RegisterComponentAttribute>().ToArray();
            RegisterTypesInternal(types, false);
        }

        /// <inheritdoc />
        [Pure]
        public CompIdx GetIndex(Type type)
        {
            return _typeToIdx[type];
        }

        /// <inheritdoc />
        [Pure]
        public int GetArrayIndex(Type type)
        {
            return _typeToIdx[type].Value;
        }

        private void RegisterTypesInternal(Type[] types, bool overwrite)
        {
            var names = _names.ToDictionary();
            var lowerCaseNames = _lowerCaseNames.ToDictionary();
            var typesDict = _types.ToDictionary();
            var idxToType = _idxToType.ToDictionary();
            var ignored = _ignored.ToHashSet();

            var added = new ComponentRegistration[types.Length];
            var typeToidx = _typeToIdx.ToDictionary();

            for (int i = 0; i < types.Length; i++)
            {
                var type = types[i];
                var idx = CompIdx.GetIndex(type);
                typeToidx[type] = idx;

                added[i] = Register(type, idx, names, lowerCaseNames, typesDict, idxToType, ignored, overwrite);
            }

            var st = RStopwatch.StartNew();
            _typeToIdx = typeToidx.ToFrozenDictionary();
            _names = names.ToFrozenDictionary();
            _lowerCaseNames = lowerCaseNames.ToFrozenDictionary();
            _types = typesDict.ToFrozenDictionary();
            _idxToType = idxToType.ToFrozenDictionary();
            _ignored = ignored.ToFrozenSet();
            _sawmill.Verbose($"Freezing component factory took {st.Elapsed.TotalMilliseconds:f2}ms");
            ComponentsAdded?.Invoke(added);
        }

        /// <inheritdoc />
        public void RegisterClass<T>(bool overwrite = false)
            where T : IComponent, new()
        {
            RegisterTypesInternal(new []{typeof(T)}, overwrite);
        }

        /// <inheritdoc />
        public void RegisterTypes(params Type[] types)
        {
            foreach (var type in types)
            {
                if (!type.IsAssignableTo(typeof(IComponent)) || !type.HasParameterlessConstructor())
                    throw new InvalidOperationException($"Invalid type: {type}");
            }

            RegisterTypesInternal(types, false);
        }

        public IEnumerable<CompIdx> GetAllRefTypes()
        {
            return AllRegistrations.Select(x => x.Idx).Distinct();
        }

        public IEnumerable<ComponentRegistration> GetAllRegistrations()
        {
            return _types.Values;
        }

        /// <inheritdoc />
        public void GenerateNetIds()
        {
            // assumptions:
            // component names are guaranteed to be unique
            // component names are 1:1 with component concrete types

            // a subset of component names are networked
            var networkedRegs = new List<ComponentRegistration>(_names.Count);

            foreach (var kvRegistration in _names)
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
                var registration = networkedRegs[i];
                registration.NetID = i;
            }

            _networkedComponents = networkedRegs;
        }

        public Type IdxToType(CompIdx idx) => _idxToType[idx];

        public byte[] GetHash(bool networkedOnly)
        {
            if (_networkedComponents is null)
                throw new ComponentRegistrationLockException();

            return GetHash(networkedOnly ? _networkedComponents : _array);
        }

        public byte[] GetHash(IEnumerable<ComponentRegistration> comps)
        {
            comps = comps.OrderBy(x => x.Name, StringComparer.InvariantCulture);
            var stream = new MemoryStream();
            using (var writer = new StreamWriter(stream, leaveOpen: true))
            {
                foreach (var item in comps)
                {
                    writer.Write(item.Name);
                    writer.Write(item.NetID);
                }
            }

            stream.Position = 0;
            var sha256 = System.Security.Cryptography.SHA256.Create();
            return sha256.ComputeHash(stream);
        }
    }

    [Serializable]
    public sealed class UnknownComponentException : Exception
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
    }

    public sealed class ComponentRegistrationLockException : Exception
    {
    }

    public sealed class InvalidComponentNameException : Exception
    {
        public InvalidComponentNameException(string message) : base(message)
        {
        }
    }
}
