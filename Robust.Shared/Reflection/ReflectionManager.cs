using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Reflection
{
    public abstract class ReflectionManager : IReflectionManager
    {
        /// <summary>
        /// Enumerable over prefixes that are added to the type provided to <see cref="GetType(string)"/>
        /// if the type can't be found in any assemblies.
        /// </summary>
        /// <remarks>
        /// First prefix should probably be <code>""</code>.
        /// </remarks>
        protected abstract IEnumerable<string> TypePrefixes { get; }

        private readonly List<Assembly> assemblies = new();

        public event EventHandler<ReflectionUpdateEventArgs>? OnAssemblyAdded;

        [ViewVariables]
        public IReadOnlyList<Assembly> Assemblies => assemblies;

        private readonly Dictionary<(Type baseType, string typeName), Type?> _yamlTypeTagCache = new();

        private readonly Dictionary<string, Type> _looseTypeCache = new();

        private readonly Dictionary<string, Enum> _enumCache = new();

        private readonly List<Type> _getAllTypesCache = new();

        /// <inheritdoc />
        public IEnumerable<Type> GetAllChildren<T>(bool inclusive = false)
        {
            return GetAllChildren(typeof(T), inclusive);
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetAllChildren(Type baseType, bool inclusive = false)
        {
            EnsureGetAllTypesCache();

            foreach (var type in _getAllTypesCache)
            {
                if (!baseType.IsAssignableFrom(type) || type.IsAbstract)
                    continue;

                if (baseType == type && !inclusive)
                    continue;

                yield return type;
            }
        }

        private void EnsureGetAllTypesCache()
        {
            if (_getAllTypesCache.Count != 0)
                return;

            var totalLength = 0;
            var typeSets = new List<Type[]>();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                typeSets.Add(types);
                totalLength += types.Length;
            }

            _getAllTypesCache.Capacity = totalLength;

            foreach (var typeSet in typeSets)
            {
                foreach (var type in typeSet)
                {
                    var attribute = (ReflectAttribute?) Attribute.GetCustomAttribute(type, typeof(ReflectAttribute));

                    if (!(attribute?.Discoverable ?? ReflectAttribute.DEFAULT_DISCOVERABLE))
                        continue;

                    _getAllTypesCache.Add(type);
                }
            }
        }

        public void LoadAssemblies(params Assembly[] args) => LoadAssemblies(args.AsEnumerable());

        public void LoadAssemblies(IEnumerable<Assembly> assemblies)
        {
            this.assemblies.AddRange(assemblies);
            _getAllTypesCache.Clear();
            OnAssemblyAdded?.Invoke(this, new ReflectionUpdateEventArgs(this));
        }

        /// <seealso cref="TypePrefixes"/>
        public Type? GetType(string name)
        {
            // The priority in which types are retrieved is based on the TypePrefixes list.
            // This is an implementation detail. If you need it: make a better API.
            foreach (string prefix in TypePrefixes)
            {
                string appendedName = prefix + name;
                foreach (var assembly in Assemblies)
                {
                    var theType = assembly.GetType(appendedName);
                    if (theType != null)
                    {
                        return theType;
                    }
                }
            }

            return null;
        }

        /// <inheritdoc />
        public Type LooseGetType(string name)
        {
            if (TryLooseGetType(name, out var ret))
            {
                return ret;
            }

            throw new ArgumentException($"Unable to find type: {name}.");
        }

        public bool TryLooseGetType(string name, [NotNullWhen(true)] out Type? type)
        {
            if (_looseTypeCache.TryGetValue(name, out type))
                return true;

            foreach (var assembly in assemblies)
            {
                foreach (var tryType in assembly.DefinedTypes)
                {
                    if (tryType.FullName!.EndsWith(name))
                    {
                        type = tryType;
                        _looseTypeCache[name] = type;
                        return true;
                    }
                }
            }

            type = default;
            return false;
        }

        /// <inheritdoc />
        public IEnumerable<Type> FindTypesWithAttribute<T>() where T : Attribute
        {
            return FindTypesWithAttribute(typeof(T));
        }

        /// <inheritdoc />
        public IEnumerable<Type> FindTypesWithAttribute(Type attributeType)
        {
            EnsureGetAllTypesCache();
            return _getAllTypesCache.Where(type => Attribute.IsDefined(type, attributeType));
        }

        public IEnumerable<Type> FindAllTypes()
        {
            EnsureGetAllTypesCache();
            return _getAllTypesCache;
        }

        /// <inheritdoc />
        public bool TryParseEnumReference(string reference, [NotNullWhen(true)] out Enum? @enum)
        {
            if (!reference.StartsWith("enum."))
            {
                @enum = default;
                return false;
            }

            reference = reference.Substring(5);

            if (_enumCache.TryGetValue(reference, out @enum))
                return true;

            var dotIndex = reference.LastIndexOf('.');
            var typeName = reference.Substring(0, dotIndex);

            var value = reference.Substring(dotIndex + 1);

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.DefinedTypes)
                {
                    if (!type.IsEnum || !type.FullName!.EndsWith(typeName))
                    {
                        continue;
                    }

                    @enum = (Enum) Enum.Parse(type, value);
                    _enumCache[reference] = @enum;
                    return true;
                }
            }

            throw new ArgumentException($"Could not resolve enum reference: {reference}.");
        }

        public Type? YamlTypeTagLookup(Type baseType, string typeName)
        {
            if (_yamlTypeTagCache.TryGetValue((baseType, typeName), out var type))
            {
                return type;
            }

            Type? found = null;
            foreach (var derivedType in GetAllChildren(baseType))
            {
                if (!derivedType.IsPublic)
                {
                    continue;
                }

                if (derivedType.Name == typeName)
                {
                    found = derivedType;
                    break;
                }

                var serializedAttribute = derivedType.GetCustomAttribute<SerializedTypeAttribute>();

                if (serializedAttribute != null &&
                    serializedAttribute.SerializeName == typeName)
                {
                    found = derivedType;
                    break;
                }
            }

            _yamlTypeTagCache.Add((baseType, typeName), found);
            return found;
        }
    }
}
