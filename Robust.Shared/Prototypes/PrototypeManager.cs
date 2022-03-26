using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using JetBrains.Annotations;
using Prometheus;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
{
    /// <summary>
    /// Handle storage and loading of YAML prototypes.
    /// </summary>
    public interface IPrototypeManager
    {
        void Initialize();

        /// <summary>
        /// Return an IEnumerable to iterate all prototypes of a certain type.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the type of prototype is not registered.
        /// </exception>
        IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype;

        /// <summary>
        /// Return an IEnumerable to iterate all prototypes of a certain type.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the type of prototype is not registered.
        /// </exception>
        IEnumerable<IPrototype> EnumeratePrototypes(Type type);

        /// <summary>
        /// Return an IEnumerable to iterate all prototypes of a certain variant.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the variant of prototype is not registered.
        /// </exception>
        IEnumerable<IPrototype> EnumeratePrototypes(string variant);

        /// <summary>
        /// Index for a <see cref="IPrototype"/> by ID.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the type of prototype is not registered.
        /// </exception>
        T Index<T>(string id) where T : class, IPrototype;

        /// <summary>
        /// Index for a <see cref="IPrototype"/> by ID.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the ID does not exist or the type of prototype is not registered.
        /// </exception>
        IPrototype Index(Type type, string id);

        /// <summary>
        ///     Returns whether a prototype of type <typeparamref name="T"/> with the specified <param name="id"/> exists.
        /// </summary>
        bool HasIndex<T>(string id) where T : class, IPrototype;
        bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype;
        bool TryIndex(Type type, string id, [NotNullWhen(true)] out IPrototype? prototype);

        bool TryGetMapping(Type type, string id, [NotNullWhen(true)] out MappingDataNode? mappings);

        /// <summary>
        ///     Returns whether a prototype variant <param name="variant"/> exists.
        /// </summary>
        /// <param name="variant">Identifier for the prototype variant.</param>
        /// <returns>Whether the prototype variant exists.</returns>
        bool HasVariant(string variant);

        /// <summary>
        ///     Returns the Type for a prototype variant.
        /// </summary>
        /// <param name="variant">Identifier for the prototype variant.</param>
        /// <returns>The specified prototype Type.</returns>
        /// <exception cref="KeyNotFoundException">
        ///     Thrown when the specified prototype variant isn't registered or doesn't exist.
        /// </exception>
        Type GetVariantType(string variant);

        /// <summary>
        ///     Attempts to get the Type for a prototype variant.
        /// </summary>
        /// <param name="variant">Identifier for the prototype variant.</param>
        /// <param name="prototype">The specified prototype Type, or null.</param>
        /// <returns>Whether the prototype type was found and <see cref="prototype"/> isn't null.</returns>
        bool TryGetVariantType(string variant, [NotNullWhen(true)] out Type? prototype);

        /// <summary>
        ///     Attempts to get a prototype's variant.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="variant"></param>
        /// <returns></returns>
        bool TryGetVariantFrom(Type type, [NotNullWhen(true)] out string? variant);

        /// <summary>
        ///     Attempts to get a prototype's variant.
        /// </summary>
        /// <param name="prototype">The prototype in question.</param>
        /// <param name="variant">Identifier for the prototype variant, or null.</param>
        /// <returns>Whether the prototype variant was successfully retrieved.</returns>
        bool TryGetVariantFrom(IPrototype prototype, [NotNullWhen(true)] out string? variant);

        /// <summary>
        ///     Attempts to get a prototype's variant.
        /// </summary>
        /// <param name="variant">Identifier for the prototype variant, or null.</param>
        /// <typeparam name="T">The prototype in question.</typeparam>
        /// <returns>Whether the prototype variant was successfully retrieved.</returns>
        bool TryGetVariantFrom<T>([NotNullWhen(true)] out string? variant) where T : class, IPrototype;

        /// <summary>
        /// Load prototypes from files in a directory, recursively.
        /// </summary>
        void LoadDirectory(ResourcePath path, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null);

        Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResourcePath path);

        void LoadFromStream(TextReader stream, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null);

        void LoadString(string str, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null);

        void RemoveString(string prototypes);

        /// <summary>
        /// Clear out all prototypes and reset to a blank slate.
        /// </summary>
        void Clear();

        /// <summary>
        /// Syncs all inter-prototype data. Call this when operations adding new prototypes are done.
        /// </summary>
        void ResolveResults();

        /// <summary>
        ///     Registers a specific prototype name to be ignored.
        /// </summary>
        void RegisterIgnore(string name);

        /// <summary>
        /// Loads a single prototype class type into the manager.
        /// </summary>
        /// <param name="protoClass">A prototype class type that implements IPrototype. This type also
        /// requires a <see cref="PrototypeAttribute"/> with a non-empty class string.</param>
        void RegisterType(Type protoClass);

        event Action<YamlStream, string>? LoadedData;

        /// <summary>
        ///     Fired when prototype are reloaded. The event args contain the modified prototypes.
        /// </summary>
        /// <remarks>
        ///     This does NOT fire on initial prototype load.
        /// </remarks>
        event Action<PrototypesReloadedEventArgs> PrototypesReloaded;
    }

    /// <summary>
    /// Quick attribute to give the prototype its type string.
    /// To prevent needing to instantiate it because interfaces can't declare statics.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false)]
    [BaseTypeRequired(typeof(IPrototype))]
    [MeansImplicitUse]
    [MeansDataDefinition]
    public sealed class PrototypeAttribute : Attribute
    {
        private readonly string type;
        public string Type => type;
        public readonly int LoadPriority = 1;

        public PrototypeAttribute(string type, int loadPriority = 1)
        {
            this.type = type;
            LoadPriority = loadPriority;
        }
    }

    [Virtual]
    public class PrototypeManager : IPrototypeManager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] protected readonly IResourceManager Resources = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] protected readonly ITaskManager TaskManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        private readonly Dictionary<string, Type> _prototypeTypes = new();
        private readonly Dictionary<Type, int> _prototypePriorities = new();

        private bool _initialized;
        private bool _hasEverBeenReloaded;

        #region IPrototypeManager members

        private readonly Dictionary<Type, Dictionary<string, IPrototype>> _prototypes = new();
        private readonly Dictionary<Type, Dictionary<string, MappingDataNode>> _prototypeResults = new();
        private readonly Dictionary<Type, PrototypeInheritanceTree> _inheritanceTrees = new();

        private readonly HashSet<string> _ignoredPrototypeTypes = new();

        public virtual void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException($"{nameof(PrototypeManager)} has already been initialized.");
            }

            _initialized = true;
            ReloadPrototypeTypes();
        }

        public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _prototypes[typeof(T)].Values.Select(p => (T) p);
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(Type type)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _prototypes[type].Values;
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(string variant)
        {
            return EnumeratePrototypes(GetVariantType(variant));
        }

        public T Index<T>(string id) where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            try
            {
                return (T) _prototypes[typeof(T)][id];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownPrototypeException(id);
            }
        }

        public IPrototype Index(Type type, string id)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _prototypes[type][id];
        }

        public void Clear()
        {
            _prototypeTypes.Clear();
            _prototypes.Clear();
            _prototypeResults.Clear();
            _inheritanceTrees.Clear();
        }

        private int SortPrototypesByPriority(Type a, Type b)
        {
            return _prototypePriorities[b].CompareTo(_prototypePriorities[a]);
        }

        protected void ReloadPrototypes(IEnumerable<ResourcePath> filePaths)
        {
#if !FULL_RELEASE
            var changed = new Dictionary<Type, HashSet<string>>();
            foreach (var filePath in filePaths)
            {
                LoadFile(filePath.ToRootedPath(), true, changed);
            }
            ReloadPrototypes(changed);
#endif
        }

        internal void ReloadPrototypes(Dictionary<Type, HashSet<string>> prototypes)
        {
#if !FULL_RELEASE
            var prototypeTypeOrder = prototypes.Keys.ToList();
            prototypeTypeOrder.Sort(SortPrototypesByPriority);

            var pushed = new Dictionary<Type, HashSet<string>>();

            foreach (var type in prototypeTypeOrder)
            {
                if (!type.IsAssignableTo(typeof(IInheritingPrototype)))
                {
                    foreach (var id in prototypes[type])
                    {
                        _prototypes[type][id] = (IPrototype) _serializationManager.Read(type, _prototypeResults[type][id])!;
                    }
                    continue;
                }

                foreach (var id in prototypes[type])
                {
                    if (!pushed.ContainsKey(type))
                        pushed[type] = new HashSet<string>();
                    if (pushed[type].Contains(id))
                    {
                        continue;
                    }

                    var set = new HashSet<string>();
                    set.Add(id);
                    PushInheritance(type, id, _inheritanceTrees[type].GetParent(id), set);
                    foreach (var changedId in set)
                    {
                        TryReadPrototype(type, changedId, _prototypeResults[type][changedId]);
                    }
                    pushed[type].UnionWith(set);
                }
            }

            //todo paul i hate it but i am not opening that can of worms in this refactor
            PrototypesReloaded?.Invoke(
                new PrototypesReloadedEventArgs(
                    prototypes
                        .ToDictionary(
                            g => g.Key,
                            g => new PrototypesReloadedEventArgs.PrototypeChangeSet(
                                g.Value.ToDictionary(a => a, a => _prototypes[g.Key][a])))));

            // TODO filter by entity prototypes changed
            if (!pushed.ContainsKey(typeof(EntityPrototype))) return;

            var entityPrototypes = _prototypes[typeof(EntityPrototype)];

            foreach (var prototype in pushed[typeof(EntityPrototype)])
            {
                foreach (var entity in _entityManager.GetEntities())
                {
                    var metaData = _entityManager.GetComponent<MetaDataComponent>(entity);
                    if (metaData.EntityPrototype != null && metaData.EntityPrototype.ID == prototype)
                    {
                        ((EntityPrototype) entityPrototypes[prototype]).UpdateEntity(entity);
                    }
                }
            }
#endif
        }

        /// <summary>
        /// Resolves the mappings stored in memory to actual prototypeinstances.
        /// </summary>
        public void ResolveResults()
        {
            var types = _prototypeResults.Keys.ToList();
            types.Sort(SortPrototypesByPriority);
            foreach (var type in types)
            {
                if(_inheritanceTrees.TryGetValue(type, out var tree)){
                    foreach (var baseNode in tree.BaseNodes)
                    {
                        PushInheritance(type, baseNode);
                    }
                }

                foreach (var (id, mapping) in _prototypeResults[type])
                {
                    TryReadPrototype(type, id, mapping);
                }
            }
        }

        private void TryReadPrototype(Type type, string id, MappingDataNode mapping)
        {
            if(mapping.TryGet<ValueDataNode>(AbstractDataFieldAttribute.Name, out var abstractNode) && abstractNode.AsBool())
                return;
            try
            {
                _prototypes[type][id] = (IPrototype) _serializationManager.Read(type, mapping)!;
            }
            catch (Exception e)
            {
                Logger.ErrorS("PROTO", $"Reading {type}({id}) threw the following exception: {e}");
            }
        }

        private void PushInheritance(Type type, string id, string? parent = null, HashSet<string>? changed = null)
        {
            if (parent != null)
            {
                PushInheritanceWithoutRecursion(type, id, parent, changed);
            }

            if(!_inheritanceTrees[type].HasId(id)) return;

            foreach (var child in _inheritanceTrees[type].Children(id))
            {
                PushInheritance(type, child, id, changed);
            }
        }

        private void PushInheritanceWithoutRecursion(Type type, string id, string parent,
            HashSet<string>? changed = null)
        {
            _prototypeResults[type][id] = _serializationManager.PushCompositionWithGenericNode(type,
                new[] { _prototypeResults[type][parent] }, _prototypeResults[type][id]);
            changed?.Add(id);
        }

        /// <inheritdoc />
        public void LoadDirectory(ResourcePath path, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null)
        {
            _hasEverBeenReloaded = true;
            var streams = Resources.ContentFindFiles(path).ToList().AsParallel()
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."));

            foreach (var resourcePath in streams)
            {
                LoadFile(resourcePath, overwrite, changed);
            }
        }

        public Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResourcePath path)
        {
            var streams = Resources.ContentFindFiles(path).ToList().AsParallel()
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."));

            var dict = new Dictionary<string, HashSet<ErrorNode>>();
            foreach (var resourcePath in streams)
            {
                using var reader = ReadFile(resourcePath);

                if (reader == null)
                {
                    continue;
                }

                var yamlStream = new YamlStream();
                yamlStream.Load(reader);

                for (var i = 0; i < yamlStream.Documents.Count; i++)
                {
                    var rootNode = (YamlSequenceNode) yamlStream.Documents[i].RootNode;
                    foreach (YamlMappingNode node in rootNode.Cast<YamlMappingNode>())
                    {
                        var type = node.GetNode("type").AsString();
                        if (!_prototypeTypes.ContainsKey(type))
                        {
                            if (_ignoredPrototypeTypes.Contains(type))
                            {
                                continue;
                            }

                            throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
                        }

                        var mapping = node.ToDataNodeCast<MappingDataNode>();
                        mapping.Remove("type");
                        var errorNodes = _serializationManager.ValidateNode(_prototypeTypes[type], mapping).GetErrors()
                            .ToHashSet();
                        if (errorNodes.Count == 0) continue;
                        if (!dict.TryGetValue(resourcePath.ToString(), out var hashSet))
                            dict[resourcePath.ToString()] = new HashSet<ErrorNode>();
                        dict[resourcePath.ToString()].UnionWith(errorNodes);
                    }
                }
            }

            return dict;
        }

        private StreamReader? ReadFile(ResourcePath file, bool @throw = true)
        {
            var retries = 0;

            // This might be shit-code, but its pjb-responded-idk-when-asked shit-code.
            while (true)
            {
                try
                {
                    var reader = new StreamReader(Resources.ContentFileRead(file), EncodingHelpers.UTF8);
                    return reader;
                }
                catch (IOException e)
                {
                    if (retries > 10)
                    {
                        if (@throw)
                        {
                            throw;
                        }

                        Logger.Error($"Error reloading prototypes in file {file}.", e);
                        return null;
                    }

                    retries++;
                    Thread.Sleep(10);
                }
            }
        }

        public void LoadFile(ResourcePath file, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null)
        {
            try
            {
                using var reader = ReadFile(file, !overwrite);

                if (reader == null)
                {
                    return;
                }

                var yamlStream = new YamlStream();
                yamlStream.Load(reader);

                LoadedData?.Invoke(yamlStream, file.ToString());

                for (var i = 0; i < yamlStream.Documents.Count; i++)
                {
                    try
                    {
                        LoadFromDocument(yamlStream.Documents[i], overwrite, changed);
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("eng", $"Exception whilst loading prototypes from {file}#{i}:\n{e}");
                    }
                }
            }
            catch (YamlException e)
            {
                var sawmill = Logger.GetSawmill("eng");
                sawmill.Error("YamlException whilst loading prototypes from {0}: {1}", file, e.Message);
            }
        }

        public void LoadFromStream(TextReader stream, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null)
        {
            _hasEverBeenReloaded = true;
            var yaml = new YamlStream();
            yaml.Load(stream);

            for (var i = 0; i < yaml.Documents.Count; i++)
            {
                try
                {
                    LoadFromDocument(yaml.Documents[i], overwrite, changed);
                }
                catch (Exception e)
                {
                    throw new PrototypeLoadException($"Failed to load prototypes from document#{i}", e);
                }
            }

            LoadedData?.Invoke(yaml, "anonymous prototypes YAML stream");
        }

        public void LoadString(string str, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null)
        {
            LoadFromStream(new StringReader(str), overwrite, changed);
        }

        public void RemoveString(string prototypes)
        {
            var reader = new StringReader(prototypes);
            var yaml = new YamlStream();

            yaml.Load(reader);

            foreach (var document in yaml.Documents)
            {
                var root = (YamlSequenceNode) document.RootNode;
                foreach (var node in root.Cast<YamlMappingNode>())
                {
                    var typeString = node.GetNode("type").AsString();
                    var type = _prototypeTypes[typeString];

                    var id = node.GetNode("id").AsString();

                    if (_inheritanceTrees.TryGetValue(type, out var tree))
                    {
                        tree.RemoveId(id);
                    }

                    _prototypes[type].Remove(id);
                }
            }
        }

        #endregion IPrototypeManager members

        private void ReloadPrototypeTypes()
        {
            Clear();
            foreach (var type in _reflectionManager.GetAllChildren<IPrototype>())
            {
                RegisterType(type);
            }
        }

        private void LoadFromDocument(YamlDocument document, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null)
        {
            var rootNode = (YamlSequenceNode) document.RootNode;

            foreach (var node in rootNode.Cast<YamlMappingNode>())
            {
                var datanode = node.ToDataNodeCast<MappingDataNode>();
                var type = datanode.Get<ValueDataNode>("type").Value;
                if (!_prototypeTypes.ContainsKey(type))
                {
                    if (_ignoredPrototypeTypes.Contains(type))
                    {
                        continue;
                    }

                    throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
                }

                var prototypeType = _prototypeTypes[type];

                if (!datanode.TryGet<ValueDataNode>(IdDataFieldAttribute.Name, out var idNode))
                    throw new PrototypeLoadException($"Prototype type {type} is missing an 'id' datafield.");

                if (!overwrite && _prototypes[prototypeType].ContainsKey(idNode.Value))
                {
                    throw new PrototypeLoadException($"Duplicate ID: '{idNode.Value}'");
                }

                _prototypeResults[prototypeType][idNode.Value] = datanode;
                if (prototypeType.IsAssignableTo(typeof(IInheritingPrototype)))
                {
                    datanode.TryGet<ValueDataNode>(ParentDataFieldAttribute.Name, out var parentNode);
                    _inheritanceTrees[prototypeType].AddId(idNode.Value, parentNode?.Value, true);
                }

                if (changed == null) continue;

                if (!changed.TryGetValue(prototypeType, out var set))
                    changed[prototypeType] = set = new HashSet<string>();
                set.Add(idNode.Value);
            }
        }

        public bool HasIndex<T>(string id) where T : class, IPrototype
        {
            if (!_prototypes.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.ContainsKey(id);
        }

        public bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype
        {
            var returned = TryIndex(typeof(T), id, out var proto);
            prototype = (proto ?? null) as T;
            return returned;
        }

        public bool TryIndex(Type type, string id, [NotNullWhen(true)] out IPrototype? prototype)
        {
            if (!_prototypes.TryGetValue(type, out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.TryGetValue(id, out prototype);
        }

        public bool TryGetMapping(Type type, string id, [NotNullWhen(true)] out MappingDataNode? mappings)
        {
            var ret = _prototypeResults[type].TryGetValue(id, out var originalMappings);
            mappings = originalMappings?.Copy();
            return ret;
        }

        /// <inheritdoc />
        public bool HasVariant(string variant)
        {
            return _prototypeTypes.ContainsKey(variant);
        }

        /// <inheritdoc />
        public Type GetVariantType(string variant)
        {
            return _prototypeTypes[variant];
        }

        /// <inheritdoc />
        public bool TryGetVariantType(string variant, [NotNullWhen(true)] out Type? prototype)
        {
            return _prototypeTypes.TryGetValue(variant, out prototype);
        }

        /// <inheritdoc />
        public bool TryGetVariantFrom(Type type, [NotNullWhen(true)] out string? variant)
        {
            variant = null;

            // If the type doesn't implement IPrototype, this fails.
            if (!(typeof(IPrototype).IsAssignableFrom(type)))
                return false;

            var attribute = (PrototypeAttribute?) Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));

            // If the prototype type doesn't have the attribute, this fails.
            if (attribute == null)
                return false;

            // If the variant isn't registered, this fails.
            if (!HasVariant(attribute.Type))
                return false;

            variant = attribute.Type;
            return true;
        }

        /// <inheritdoc />
        public bool TryGetVariantFrom<T>([NotNullWhen(true)] out string? variant) where T : class, IPrototype
        {
            return TryGetVariantFrom(typeof(T), out variant);
        }

        /// <inheritdoc />
        public bool TryGetVariantFrom(IPrototype prototype, [NotNullWhen(true)] out string? variant)
        {
            return TryGetVariantFrom(prototype.GetType(), out variant);
        }

        public void RegisterIgnore(string name)
        {
            _ignoredPrototypeTypes.Add(name);
        }

        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            if (!(typeof(IPrototype).IsAssignableFrom(type)))
                throw new InvalidOperationException("Type must implement IPrototype.");

            var attribute = (PrototypeAttribute?) Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));

            if (attribute == null)
            {
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    "No " + nameof(PrototypeAttribute) + " to give it a type string.");
            }

            if (_prototypeTypes.ContainsKey(attribute.Type))
            {
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    $"Duplicate prototype type ID: {attribute.Type}. Current: {_prototypeTypes[attribute.Type]}");
            }

            var foundIdAttribute = false;
            var foundParentAttribute = false;
            var foundAbstractAttribute = false;
            foreach (var info in type.GetAllPropertiesAndFields())
            {
                var hasId = info.HasAttribute<IdDataFieldAttribute>();
                var hasParent = info.HasAttribute<ParentDataFieldAttribute>();
                if (hasId)
                {
                    if (foundIdAttribute)
                        throw new InvalidImplementationException(type,
                            typeof(IPrototype),
                            $"Found two {nameof(IdDataFieldAttribute)}");

                    foundIdAttribute = true;
                }

                if (hasParent)
                {
                    if (foundParentAttribute)
                        throw new InvalidImplementationException(type,
                            typeof(IInheritingPrototype),
                            $"Found two {nameof(ParentDataFieldAttribute)}");

                    foundParentAttribute = true;
                }

                if (hasId && hasParent)
                    throw new InvalidImplementationException(type,
                        typeof(IPrototype),
                        $"Prototype {type} has the Id- & ParentDatafield on single member {info.Name}");

                if (info.HasAttribute<AbstractDataFieldAttribute>())
                {
                    if (foundAbstractAttribute)
                        throw new InvalidImplementationException(type,
                            typeof(IInheritingPrototype),
                            $"Found two {nameof(AbstractDataFieldAttribute)}");

                    foundAbstractAttribute = true;
                }
            }

            if (!foundIdAttribute)
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    $"Did not find any member annotated with the {nameof(IdDataFieldAttribute)}");

            if (type.IsAssignableTo(typeof(IInheritingPrototype)) && (!foundParentAttribute || !foundAbstractAttribute))
                throw new InvalidImplementationException(type,
                    typeof(IInheritingPrototype),
                    $"Did not find any member annotated with the {nameof(ParentDataFieldAttribute)} and/or {nameof(AbstractDataFieldAttribute)}");

            _prototypeTypes[attribute.Type] = type;
            _prototypePriorities[type] = attribute.LoadPriority;

            if (typeof(IPrototype).IsAssignableFrom(type))
            {
                _prototypes[type] = new Dictionary<string, IPrototype>();
                _prototypeResults[type] = new Dictionary<string, MappingDataNode>();
                if (typeof(IInheritingPrototype).IsAssignableFrom(type))
                    _inheritanceTrees[type] = new PrototypeInheritanceTree();
            }
        }

        public event Action<YamlStream, string>? LoadedData;
        public event Action<PrototypesReloadedEventArgs>? PrototypesReloaded;
    }

    [Serializable]
    [Virtual]
    public class PrototypeLoadException : Exception
    {
        public PrototypeLoadException()
        {
        }

        public PrototypeLoadException(string message) : base(message)
        {
        }

        public PrototypeLoadException(string message, Exception inner) : base(message, inner)
        {
        }

        public PrototypeLoadException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }

    [Serializable]
    [Virtual]
    public class UnknownPrototypeException : Exception
    {
        public override string Message => "Unknown prototype: " + Prototype;
        public readonly string? Prototype;

        public UnknownPrototypeException(string prototype)
        {
            Prototype = prototype;
        }

        public UnknownPrototypeException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Prototype = (string?) info.GetValue("prototype", typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("prototype", Prototype, typeof(string));
        }
    }

    public sealed record PrototypesReloadedEventArgs(IReadOnlyDictionary<Type, PrototypesReloadedEventArgs.PrototypeChangeSet> ByType)
    {
        public sealed record PrototypeChangeSet(IReadOnlyDictionary<string, IPrototype> Modified);
    }
}
