using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Robust.Shared.IoC;
using Robust.Shared.Log;
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

        private ImmutableArray<Type> _getAllTypesCache = ImmutableArray<Type>.Empty;

        private ImmutableDictionary<Type, ImmutableArray<Type>> _inheritanceCache =
            ImmutableDictionary<Type, ImmutableArray<Type>>.Empty;

        private ImmutableDictionary<Type, ImmutableHashSet<Type>> _attributeCache =
            ImmutableDictionary<Type, ImmutableHashSet<Type>>.Empty;

        private ImmutableDictionary<string, ImmutableArray<Type>> _allEnumCache =
            ImmutableDictionary<string, ImmutableArray<Type>>.Empty;

        private ISawmill _sawmill = default!;

        public void Initialize()
        {
            _sawmill = _logMan.GetSawmill("Reflection");
            EnsureGetAllTypesCache();
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

            if (inclusive)
                yield return baseType;

            if (!_inheritanceCache.TryGetValue(baseType, out var inheritors))
                yield break;

            foreach (var inheritor in inheritors)
            {
                if (!inheritor.IsAbstract)
                    yield return inheritor;
            }
        }

        internal void EnsureGetAllTypesCache()
        {
            if (_getAllTypesCache.Length != 0)
                return;

            var totalLength = 0;
            var typeSets = new List<Type[]>();

            foreach (var assembly in assemblies)
            {
                var types = assembly.GetTypes();
                typeSets.Add(types);
                totalLength += types.Length;
            }

            var typesCache = ImmutableArray.CreateBuilder<Type>(totalLength);
            var inheritanceCache = ImmutableDictionary.CreateBuilder<Type, (List<Type> List, HashSet<Type> Set)>();
            var attributeCache = ImmutableDictionary.CreateBuilder<Type, HashSet<Type>>();
            var enumCache = ImmutableDictionary.CreateBuilder<string, List<Type>>();

            foreach (var typeSet in typeSets)
            {
                foreach (var type in typeSet)
                {
                    var attribute = (ReflectAttribute?)Attribute.GetCustomAttribute(type, typeof(ReflectAttribute));

                    if (!(attribute?.Discoverable ?? ReflectAttribute.DEFAULT_DISCOVERABLE))
                        continue;

                    typesCache.Add(type);

                    var baseType = type.BaseType;
                    foreach (var @interface in type.GetInterfaces())
                    {
                        if (!inheritanceCache.TryGetValue(@interface, out var interfaces))
                        {
                            interfaces = ([], []);
                            inheritanceCache[@interface] = interfaces;
                        }

                        if (interfaces.Set.Add(type))
                            interfaces.List.Add(type);
                    }

                    while (baseType != null)
                    {
                        if (!inheritanceCache.TryGetValue(baseType, out var subTypes))
                        {
                            subTypes = ([], []);
                            inheritanceCache[baseType] = subTypes;
                        }

                        if (subTypes.Set.Add(type))
                            subTypes.List.Add(type);

                        foreach (var @interface in baseType.GetInterfaces())
                        {
                            if (!inheritanceCache.TryGetValue(@interface, out var interfaces))
                            {
                                interfaces = ([], []);
                                inheritanceCache[@interface] = interfaces;
                            }

                            if (interfaces.Set.Add(type))
                                interfaces.List.Add(type);
                        }

                        baseType = baseType.BaseType;
                    }

                    foreach (var typeAttribute in type.CustomAttributes)
                    {
                        if (!attributeCache.TryGetValue(typeAttribute.AttributeType, out var attributes))
                        {
                            attributes = [];
                            attributeCache[typeAttribute.AttributeType] = attributes;
                        }

                        attributes.Add(type);
                    }

                    if (type.IsEnum)
                    {
                        var fullName = type.FullName!;
                        var types = enumCache.GetOrNew(fullName);
                        types.Add(type);

                        types = enumCache.GetOrNew(type.Name);
                        types.Add(type);

                        var declaringType = type.DeclaringType;
                        var lastIndexOf = fullName.LastIndexOf('.');
                        while (declaringType != null && lastIndexOf != -1)
                        {
                            types = enumCache.GetOrNew(fullName[(lastIndexOf + 1)..]);
                            types.Add(type);

                            declaringType = declaringType.DeclaringType;
                            lastIndexOf = fullName.LastIndexOf('.', lastIndexOf - 1, lastIndexOf - 1);
                        }
                    }
                }
            }

            var toAdd = new HashSet<Type>();
            foreach (var (attributeType, types) in attributeCache)
            {
                if (attributeType.GetCustomAttribute<AttributeUsageAttribute>() is not { Inherited: true })
                {
                    continue;
                }

                toAdd.Clear();
                foreach (var type in types)
                {
                    if (inheritanceCache.TryGetValue(type, out var inheritors))
                        toAdd.UnionWith(inheritors.Set);
                }

                types.UnionWith(toAdd);
            }

            _getAllTypesCache = typesCache.ToImmutable();
            _inheritanceCache = inheritanceCache
                .ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.List.ToImmutableArray());
            _attributeCache = attributeCache
                .ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableHashSet());
            _allEnumCache = enumCache.ToImmutableDictionary(kvp => kvp.Key, kvp => kvp.Value.ToImmutableArray());
        }

        public void LoadAssemblies(params Assembly[] args) => LoadAssemblies(args.AsEnumerable());

        public void LoadAssemblies(IEnumerable<Assembly> assemblies)
        {
            var assembliesArray = assemblies.Distinct().ToArray();
            if (this.assemblies.Intersect(assembliesArray).Any())
                throw new InvalidOperationException("Attempted to load the same assembly multiple times!");

            this.assemblies.AddRange(assembliesArray);
            _getAllTypesCache = ImmutableArray<Type>.Empty;
            _inheritanceCache = ImmutableDictionary<Type, ImmutableArray<Type>>.Empty;
            _allEnumCache = ImmutableDictionary<string, ImmutableArray<Type>>.Empty;
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
            return _attributeCache.GetValueOrDefault(attributeType) ?? Enumerable.Empty<Type>();
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
        public bool TryParseEnumReference(
            string reference,
            [NotNullWhen(true)] out Enum? @enum,
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
                    var cropped = r.AsSpan(5);

                    // Doesn't exist, add it.
                    var dotIndex = cropped.LastIndexOf('.');
                    var typeName = cropped[..dotIndex];

                    var firstDot = typeName.IndexOf('.');
                    if (firstDot != -1)
                        typeName = typeName[(firstDot + 1)..];

                    var value = cropped[(dotIndex + 1)..];

                    if (!_allEnumCache.TryGetValue(typeName.ToString(), out var enums))
                        return null;

                    foreach (var @enum in enums)
                    {
                        if (!TypeNameMatchesEnumReference(@enum.FullName!, typeName))
                            continue;

                        var e = (Enum)Enum.Parse(@enum, value);
                        if (!_reverseEnumCache.TryAdd(e, r) &&
                            r != _reverseEnumCache[e])
                        {
                            _sawmill.Warning(
                                $"Conflicting enum references encountered. Enum: {e}. Existing: {_reverseEnumCache[e]}. New: {r}");
                        }

                        return e;
                    }

                    return null;
                });

            if (@enum == null && shouldThrow)
                throw new ArgumentException($"Could not resolve enum reference: {reference}.");

            return @enum != null;
        }

        private static bool TypeNameMatchesEnumReference(ReadOnlySpan<char> fullName, ReadOnlySpan<char> typeName)
        {
            if (fullName.SequenceEqual(typeName))
                return true;

            if (fullName.Length <= typeName.Length)
                return false;

            var prefixIndex = fullName.Length - typeName.Length - 1;
            var separator = fullName[prefixIndex];

            return separator is '.' or '+'
                   && fullName[(prefixIndex + 1)..].SequenceEqual(typeName);
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

        public bool IsAttributeDefined(Type type, Type attribute)
        {
            return _attributeCache.TryGetValue(attribute, out var attributes) &&
                   attributes.Contains(type);
        }

        public ImmutableHashSet<Type> FindTypesWithAttributeSet<T>()
        {
            EnsureGetAllTypesCache();
            return _attributeCache.GetValueOrDefault(typeof(T)) ?? ImmutableHashSet<Type>.Empty;
        }
    }
}
