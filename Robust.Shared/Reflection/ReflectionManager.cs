using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.Interfaces.Reflection;
using Robust.Shared.Log;
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

        /// <inheritdoc />
        public IEnumerable<Type> GetAllChildren<T>(bool inclusive = false)
        {
            return GetAllChildren(typeof(T), inclusive);
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetAllChildren(Type baseType, bool inclusive = false)
        {
            var typeLists = new List<Type[]>(Assemblies.Count);
            try
            {
                // There's very little assemblies, so storing these temporarily is cheap.
                // We need to do it ahead of time so that we can catch ReflectionTypeLoadException HERE,
                // so whoever is using us doesn't have to handle them.
                foreach (var assembly in Assemblies)
                {
                    typeLists.Add(assembly.GetTypes());
                }
            }
            catch (ReflectionTypeLoadException e)
            {
                Logger.Error("Caught ReflectionTypeLoadException! Dumping child exceptions:");
                if (e.LoaderExceptions != null)
                {
                    foreach (var inner in e.LoaderExceptions)
                    {
                        if (inner != null)
                        {
                            Logger.Error(inner.ToString());
                        }
                    }
                }

                throw;
            }

            foreach (var t in typeLists)
            {
                foreach (var type in t)
                {
                    if (!baseType.IsAssignableFrom(type) || type.IsAbstract)
                    {
                        continue;
                    }

                    var attribute = (ReflectAttribute?) Attribute.GetCustomAttribute(type, typeof(ReflectAttribute));

                    if (!(attribute?.Discoverable ?? ReflectAttribute.DEFAULT_DISCOVERABLE))
                    {
                        continue;
                    }

                    if (baseType == type && !inclusive)
                    {
                        continue;
                    }

                    yield return type;
                }
            }
        }

        public void LoadAssemblies(params Assembly[] args) => LoadAssemblies(args.AsEnumerable());

        public void LoadAssemblies(IEnumerable<Assembly> assemblies)
        {
            this.assemblies.AddRange(assemblies);
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

            throw new ArgumentException("Unable to find type.");
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
        public IEnumerable<Type> FindTypesWithAttribute<T>()
        {
            var types = new List<Type>();

            foreach (var assembly in Assemblies)
            {
                types.AddRange(assembly.GetTypes().Where(type => Attribute.IsDefined(type, typeof(T))));
            }

            return types;
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

            throw new ArgumentException("Could not resolve enum reference.");
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
                if (derivedType.Name == typeName && (derivedType.IsPublic))
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
