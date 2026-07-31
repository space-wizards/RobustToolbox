using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Reflection
{
    public abstract partial class ReflectionManager : IReflectionManager
    {
        [Dependency] private ILogManager _logMan = default!;

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

        [ViewVariables] public IReadOnlyList<Assembly> Assemblies => assemblies;

        private readonly ConcurrentDictionary<(Type baseType, string typeName), Type?> _yamlTypeTagCache = new();

        private readonly Dictionary<string, Type> _looseTypeCache = new();

        private readonly ConcurrentDictionary<string, Enum?> _enumCache = new();
        private readonly ConcurrentDictionary<Enum, string> _reverseEnumCache = new();

        private readonly List<Type> _getAllTypesCache = new();
        private readonly ConcurrentDictionary<(Type BaseType, bool Inclusive), Type[]> _getAllChildrenCache = new();
        private ISawmill _sawmill = default!;

        private readonly ObjectPool<List<Type>> _childrenCache =
            new DefaultObjectPool<List<Type>>(new ListPolicy<Type>(), 128);

        public void Initialize()
        {
            _sawmill = _logMan.GetSawmill("Reflection");
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetAllChildren<T>(bool inclusive = false)
        {
            return GetAllChildren(typeof(T), inclusive);
        }

        /// <inheritdoc />
        public IEnumerable<Type> GetAllChildren(Type baseType, bool inclusive = false)
        {
            EnsureGetAllTypesCache();

            var key = (baseType, inclusive);
            return _getAllChildrenCache.GetOrAdd(key,
                _ =>
                {
                    var cache = _childrenCache.Get();
                    foreach (var type in _getAllTypesCache)
                    {
                        if (!baseType.IsAssignableFrom(type) || type.IsAbstract)
                            continue;

                        if (baseType == type && !inclusive)
                            continue;

                        cache.Add(type);
                    }

                    var cached = cache.ToArray();
                    _childrenCache.Return(cache);
                    return cached;
                });
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
                    var attribute = (ReflectAttribute?)Attribute.GetCustomAttribute(type, typeof(ReflectAttribute));

                    if (!(attribute?.Discoverable ?? ReflectAttribute.DEFAULT_DISCOVERABLE))
                        continue;

                    _getAllTypesCache.Add(type);
                }
            }
        }

        public void LoadAssemblies(params Assembly[] args) => LoadAssemblies(args.AsEnumerable());

        public void LoadAssemblies(IEnumerable<Assembly> assemblies)
        {
            var assembliesArray = assemblies.Distinct().ToArray();
            if (this.assemblies.Intersect(assembliesArray).Any())
                throw new InvalidOperationException("Attempted to load the same assembly multiple times!");

            this.assemblies.AddRange(assembliesArray);
            _getAllTypesCache.Clear();
            _getAllChildrenCache.Clear();
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
            lock (_looseTypeCache)
            {
                if (_looseTypeCache.TryGetValue(name, out type))
                    return true;

                // Check standard types first.
                switch (name)
                {
                    case "Byte":
                        type = typeof(byte);
                        _looseTypeCache[name] = type;
                        return true;
                    case "Bool":
                        type = typeof(bool);
                        _looseTypeCache[name] = type;
                        return true;
                    case "Double":
                        type = typeof(double);
                        _looseTypeCache[name] = type;
                        return true;
                    case "SByte":
                        type = typeof(sbyte);
                        _looseTypeCache[name] = type;
                        return true;
                    case "Single":
                        type = typeof(float);
                        _looseTypeCache[name] = type;
                        return true;
                    case "String":
                        type = typeof(string);
                        _looseTypeCache[name] = type;
                        return true;
                }

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
        public string GetEnumReference(Enum @enum)
        {
            return _reverseEnumCache.GetOrAdd(@enum,
                _ =>
                {
                    // if there is more than one enum with the same basic name, the reference may need to be the fully qualified name.
                    // but if possible we want to avoid that and use a shorter string.

                    string reference;
                    var fullName = @enum.GetType().FullName!;
                    var dotIndex = fullName.LastIndexOf('.');
                    if (dotIndex > 0 && dotIndex != fullName.Length)
                    {
                        var name = fullName.Substring(dotIndex + 1);
                        reference = $"enum.{name}.{@enum}";

                        if (_enumCache.TryAdd(reference, @enum))
                            return reference;
                    }

                    // If that failed, just use the full name.
                    reference = $"enum.{fullName}.{@enum}";
                    _enumCache.TryAdd(reference, @enum);
                    return reference;
                });
        }

        /// <inheritdoc />
        public bool TryParseEnumReference(string reference, [NotNullWhen(true)] out Enum? @enum,
            bool shouldThrow = true)
        {
            if (!reference.StartsWith("enum."))
            {
                @enum = default;
                return false;
            }

            @enum = _enumCache.GetOrAdd(reference,
                r =>
                {
                    var cropped = r.Substring(5);

                    // Doesn't exist, add it.
                    var dotIndex = cropped.LastIndexOf('.');
                    var typeName = cropped.Substring(0, dotIndex);

                    var value = cropped.Substring(dotIndex + 1);

                    foreach (var assembly in assemblies)
                    {
                        foreach (var type in assembly.DefinedTypes)
                        {
                            if (!type.IsEnum || !TypeNameMatchesEnumReference(type.FullName!, typeName))
                            {
                                continue;
                            }

                            var e = (Enum)Enum.Parse(type, value);
                            if (!_reverseEnumCache.TryAdd(e, r) &&
                                r != _reverseEnumCache[e])
                            {
                                _sawmill.Warning(
                                    $"Conflicting enum references encountered. Enum: {e}. Existing: {_reverseEnumCache[e]}. New: {r}");
                            }

                            return e;
                        }
                    }

                    if (shouldThrow)
                        throw new ArgumentException($"Could not resolve enum reference: {r}.");

                    return null;
                });

            return @enum != null;
        }

        private static bool TypeNameMatchesEnumReference(string fullName, string typeName)
        {
            if (fullName.Equals(typeName))
                return true;

            if (fullName.Length <= typeName.Length)
                return false;

            var prefixIndex = fullName.Length - typeName.Length - 1;
            var separator = fullName[prefixIndex];

            return (separator == '.' || separator == '+')
                   && fullName.AsSpan(prefixIndex + 1).SequenceEqual(typeName);
        }

        public Type? YamlTypeTagLookup(Type baseType, string typeName)
        {
            return _yamlTypeTagCache.GetOrAdd((baseType, typeName),
                _ =>
                {
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

                    // Fallback
                    if (found == null)
                    {
                        TryLooseGetType(typeName, out found);

                        // If we may have gotten the type but it's still abstract then don't return it.
                        if (found == null || found.IsAbstract || !found.IsAssignableTo(baseType))
                            found = null;
                    }

                    return found;
                });
        }
    }
}
