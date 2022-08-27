using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;
using JetBrains.Annotations;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
{
    /// <summary>
    /// Handle storage and loading of YAML prototypes.
    /// </summary>
    /// <remarks>
    /// Terminology:
    /// "Kinds" are the types of prototypes there are, like <see cref="EntityPrototype"/>.
    /// "Prototypes" are simply filled-in prototypes from YAML.
    /// </remarks>
    public interface IPrototypeManager
    {
        void Initialize();

        /// <summary>
        /// Return an <see cref="IEnumerable{T}"/> to iterate all prototypes of a certain kind.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the kind of prototype is not registered.
        /// </exception>
        IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype;

        /// <summary>
        /// Return an IEnumerable to iterate all prototypes of a certain kind.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the kind of prototype is not registered.
        /// </exception>
        IEnumerable<IPrototype> EnumeratePrototypes(Type kind);

        /// <summary>
        /// Return an <see cref="IEnumerable{T}"/> to iterate all prototypes of a certain kind.
        /// </summary>
        /// <exception cref="KeyNotFoundException">
        /// Thrown if the kind of prototype is not registered.
        /// </exception>
        IEnumerable<IPrototype> EnumeratePrototypes(string kind);

        /// <summary>
        /// Returns an IEnumerable to iterate all parents of a prototype of a certain kind.
        /// </summary>
        IEnumerable<T> EnumerateParents<T>(string id, bool includeSelf = false) where T : class, IPrototype, IInheritingPrototype;

        /// <summary>
        /// Returns an IEnumerable to iterate all parents of a prototype of a certain kind.
        /// </summary>
        IEnumerable<IPrototype> EnumerateParents(Type kind, string id, bool includeSelf = false);

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
        /// Thrown if the ID does not exist or the kind of prototype is not registered.
        /// </exception>
        IPrototype Index(Type kind, string id);

        /// <summary>
        ///     Returns whether a prototype of type <typeparamref name="T"/> with the specified <param name="id"/> exists.
        /// </summary>
        bool HasIndex<T>(string id) where T : class, IPrototype;
        bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype;
        bool TryIndex(Type kind, string id, [NotNullWhen(true)] out IPrototype? prototype);

        bool HasMapping<T>(string id);
        bool TryGetMapping(Type kind, string id, [NotNullWhen(true)] out MappingDataNode? mappings);

        /// <summary>
        ///     Returns whether a prototype variant <param name="variant"/> exists.
        /// </summary>
        /// <param name="variant">Identifier for the prototype variant.</param>
        /// <returns>Whether the prototype variant exists.</returns>
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        bool HasVariant(string variant);

        /// <summary>
        ///     Returns the Type for a prototype variant.
        /// </summary>
        /// <param name="variant">Identifier for the prototype variant.</param>
        /// <returns>The specified prototype Type.</returns>
        /// <exception cref="KeyNotFoundException">
        ///     Thrown when the specified prototype variant isn't registered or doesn't exist.
        /// </exception>
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        Type GetVariantType(string variant);

        /// <summary>
        ///     Attempts to get the Type for a prototype variant.
        /// </summary>
        /// <param name="variant">Identifier for the prototype variant.</param>
        /// <param name="prototype">The specified prototype Type, or null.</param>
        /// <returns>Whether the prototype type was found and <see cref="prototype"/> isn't null.</returns>
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        bool TryGetVariantType(string variant, [NotNullWhen(true)] out Type? prototype);

        /// <summary>
        ///     Attempts to get a prototype's variant.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="variant"></param>
        /// <returns></returns>
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        bool TryGetVariantFrom(Type type, [NotNullWhen(true)] out string? variant);

        /// <summary>
        ///     Attempts to get a prototype's variant.
        /// </summary>
        /// <param name="prototype">The prototype in question.</param>
        /// <param name="variant">Identifier for the prototype variant, or null.</param>
        /// <returns>Whether the prototype variant was successfully retrieved.</returns>
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        bool TryGetVariantFrom(IPrototype prototype, [NotNullWhen(true)] out string? variant);

        /// <summary>
        ///     Attempts to get a prototype's variant.
        /// </summary>
        /// <param name="variant">Identifier for the prototype variant, or null.</param>
        /// <typeparam name="T">The prototype in question.</typeparam>
        /// <returns>Whether the prototype variant was successfully retrieved.</returns>
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        bool TryGetVariantFrom<T>([NotNullWhen(true)] out string? variant) where T : class, IPrototype;

        /// <summary>
        ///     Returns whether a prototype kind <param name="kind"/> exists.
        /// </summary>
        /// <param name="kind">Identifier for the prototype kind.</param>
        /// <returns>Whether the prototype kind exists.</returns>
        bool HasKind(string kind);

        /// <summary>
        ///     Returns the Type for a prototype kind.
        /// </summary>
        /// <param name="kind">Identifier for the prototype kind.</param>
        /// <returns>The specified prototype Type.</returns>
        /// <exception cref="KeyNotFoundException">
        ///     Thrown when the specified prototype kind isn't registered or doesn't exist.
        /// </exception>
        Type GetKindType(string kind);

        /// <summary>
        ///     Attempts to get the Type for a prototype kind.
        /// </summary>
        /// <param name="kind">Identifier for the prototype kind.</param>
        /// <param name="prototype">The specified prototype Type, or null.</param>
        /// <returns>Whether the prototype type was found and <see cref="prototype"/> isn't null.</returns>
        bool TryGetKindType(string kind, [NotNullWhen(true)] out Type? prototype);

        /// <summary>
        ///     Attempts to get a prototype's kind.
        /// </summary>
        /// <param name="type"></param>
        /// <param name="kind"></param>
        /// <returns></returns>
        bool TryGetKindFrom(Type type, [NotNullWhen(true)] out string? kind);

        /// <summary>
        ///     Attempts to get a prototype's kind.
        /// </summary>
        /// <param name="prototype">The prototype in question.</param>
        /// <param name="kind">Identifier for the prototype kind, or null.</param>
        /// <returns>Whether the prototype kind was successfully retrieved.</returns>
        bool TryGetKindFrom(IPrototype prototype, [NotNullWhen(true)] out string? kind);

        /// <summary>
        ///     Attempts to get a prototype's kind.
        /// </summary>
        /// <param name="kind">Identifier for the prototype kind, or null.</param>
        /// <typeparam name="T">The prototype in question.</typeparam>
        /// <returns>Whether the prototype kind was successfully retrieved.</returns>
        bool TryGetKindFrom<T>([NotNullWhen(true)] out string? kind) where T : class, IPrototype;


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
        /// Reload the changes from LoadString
        /// </summary>
        /// <param name="prototypes">Changes from load string</param>
        void ReloadPrototypes(Dictionary<Type, HashSet<string>> prototypes);

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
        [Dependency] protected readonly ITaskManager TaskManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        private readonly Dictionary<string, Type> _kindNames = new();
        private readonly Dictionary<Type, int> _prototypePriorities = new();

        private bool _initialized;
        private bool _hasEverBeenReloaded;

        #region IPrototypeManager members

        private readonly Dictionary<Type, KindData> _kinds = new();

        private readonly HashSet<string> _ignoredPrototypeTypes = new();

        public virtual void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException($"{nameof(PrototypeManager)} has already been initialized.");
            }

            _initialized = true;
            ReloadPrototypeKinds();
        }

        public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            var data = _kinds[typeof(T)];

            foreach (var proto in data.Instances.Values)
            {
                yield return (T) proto;
            }
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(Type kind)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _kinds[kind].Instances.Values;
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(string kind)
        {
            return EnumeratePrototypes(GetKindType(kind));
        }

        public IEnumerable<T> EnumerateParents<T>(string id, bool includeSelf = false)  where T : class, IPrototype, IInheritingPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            if(!TryIndex<T>(id, out var prototype))
                yield break;
            if (includeSelf) yield return prototype;
            if (prototype.Parents == null) yield break;

            var queue = new Queue<string>(prototype.Parents);
            while (queue.TryDequeue(out var prototypeId))
            {
                if(!TryIndex<T>(prototypeId, out var parent))
                    yield break;
                yield return parent;
                if (parent.Parents == null) continue;

                foreach (var parentId in parent.Parents)
                {
                    queue.Enqueue(parentId);
                }
            }
        }

        public IEnumerable<IPrototype> EnumerateParents(Type kind, string id, bool includeSelf = false)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            if (!kind.IsAssignableTo(typeof(IInheritingPrototype)))
            {
                throw new InvalidOperationException("The provided prototype kind is not an inheriting prototype");
            }

            if(!TryIndex(kind, id, out var prototype))
                yield break;
            if (includeSelf) yield return prototype;
            var iPrototype = (IInheritingPrototype)prototype;
            if (iPrototype.Parents == null) yield break;

            var queue = new Queue<string>(iPrototype.Parents);
            while (queue.TryDequeue(out var prototypeId))
            {
                if (!TryIndex(kind, id, out var parent))
                    continue;
                yield return parent;
                iPrototype = (IInheritingPrototype)parent;
                if (iPrototype.Parents == null) continue;

                foreach (var parentId in iPrototype.Parents)
                {
                    queue.Enqueue(parentId);
                }
            }
        }

        public T Index<T>(string id) where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            try
            {
                return (T) _kinds[typeof(T)].Instances[id];
            }
            catch (KeyNotFoundException)
            {
                throw new UnknownPrototypeException(id);
            }
        }

        public IPrototype Index(Type kind, string id)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return _kinds[kind].Instances[id];
        }

        public void Clear()
        {
            _kindNames.Clear();
            _kinds.Clear();
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

        public void ReloadPrototypes(Dictionary<Type, HashSet<string>> prototypes)
        {
#if !FULL_RELEASE
            var prototypeTypeOrder = prototypes.Keys.ToList();
            prototypeTypeOrder.Sort(SortPrototypesByPriority);

            var pushed = new Dictionary<Type, HashSet<string>>();

            foreach (var type in prototypeTypeOrder)
            {
                var typeData = _kinds[type];
                if (!type.IsAssignableTo(typeof(IInheritingPrototype)))
                {
                    foreach (var id in prototypes[type])
                    {
                        var prototype = (IPrototype)_serializationManager.Read(type, typeData.Results[id])!;
                        typeData.Instances[id] = prototype;
                    }
                    continue;
                }

                var tree = typeData.Inheritance!;
                var processQueue = new Queue<string>();
                foreach (var id in prototypes[type])
                {
                    processQueue.Enqueue(id);
                }

                while(processQueue.TryDequeue(out var id))
                {
                    var pushedSet = pushed.GetOrNew(type);

                    if (tree.TryGetParents(id, out var parents))
                    {
                        var nonPushedParent = false;
                        foreach (var parent in parents)
                        {
                            //our parent has been reloaded and has not been added to the pushedSet yet
                            if (prototypes[type].Contains(parent) && !pushedSet.Contains(parent))
                            {
                                //we re-queue ourselves at the end of the queue
                                processQueue.Enqueue(id);
                                nonPushedParent = true;
                                break;
                            }
                        }
                        if(nonPushedParent) continue;

                        foreach (var parent in parents)
                        {
                            PushInheritance(type, id, parent);
                        }
                    }

                    TryReadPrototype(type, id, typeData.Results[id]);

                    pushedSet.Add(id);
                }
            }

            //todo paul i hate it but i am not opening that can of worms in this refactor
            PrototypesReloaded?.Invoke(
                new PrototypesReloadedEventArgs(
                    prototypes
                        .ToDictionary(
                            g => g.Key,
                            g => new PrototypesReloadedEventArgs.PrototypeChangeSet(
                                g.Value.Where(x => _kinds[g.Key].Instances.ContainsKey(x)).ToDictionary(a => a, a => _kinds[g.Key].Instances[a])))));
#endif
        }

        /// <summary>
        /// Resolves the mappings stored in memory to actual prototypeinstances.
        /// </summary>
        public void ResolveResults()
        {
            var types = _kinds.Keys.ToList();
            types.Sort(SortPrototypesByPriority);
            foreach (var type in types)
            {
                var typeData = _kinds[type];
                if (typeData.Inheritance is { } tree)
                {
                    var processed = new HashSet<string>();
                    var workList = new Queue<string>(tree.RootNodes);

                    while (workList.TryDequeue(out var id))
                    {
                        processed.Add(id);
                        if (tree.TryGetParents(id, out var parents))
                        {
                            foreach (var parent in parents)
                            {
                                PushInheritance(type, id, parent);
                            }
                        }

                        if (tree.TryGetChildren(id, out var children))
                        {
                            foreach (var child in children)
                            {
                                var childParents = tree.GetParents(child)!;
                                if(childParents.All(p => processed.Contains(p)))
                                    workList.Enqueue(child);
                            }
                        }
                    }
                }

                foreach (var (id, mapping) in typeData.Results)
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
                _kinds[type].Instances[id] = (IPrototype) _serializationManager.Read(type, mapping)!;
            }
            catch (Exception e)
            {
                Logger.ErrorS("PROTO", $"Reading {type}({id}) threw the following exception: {e}");
            }
        }

        private void PushInheritance(Type type, string id, string parent)
        {
            var kindData = _kinds[type];

            kindData.Results[id] = _serializationManager.PushCompositionWithGenericNode(
                type,
                new[] { kindData.Results[parent] },
                kindData.Results[id]);
        }

        /// <inheritdoc />
        public void LoadDirectory(ResourcePath path, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null)
        {
            _hasEverBeenReloaded = true;
            var streams = Resources.ContentFindFiles(path)
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
                .ToArray();

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
                        if (!_kindNames.ContainsKey(type))
                        {
                            if (_ignoredPrototypeTypes.Contains(type))
                            {
                                continue;
                            }

                            throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
                        }

                        var mapping = node.ToDataNodeCast<MappingDataNode>();
                        mapping.Remove("type");
                        var errorNodes = _serializationManager.ValidateNode(_kindNames[type], mapping).GetErrors()
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
                    return;

                // LoadedData?.Invoke(yamlStream, file.ToString());

                var i = 0;
                foreach (var document in DataNodeParser.ParseYamlStream(reader))
                {
                    try
                    {
                        var seq = (SequenceDataNode)document.Root;
                        foreach (var mapping in seq.Sequence)
                        {
                            LoadFromMapping((MappingDataNode) mapping, overwrite, changed);
                        }
                    }
                    catch (Exception e)
                    {
                        Logger.ErrorS("eng", $"Exception whilst loading prototypes from {file}#{i}:\n{e}");
                    }

                    i += 1;
                }
            }
            catch (Exception e)
            {
                var sawmill = Logger.GetSawmill("eng");
                sawmill.Error("YamlException whilst loading prototypes from {0}: {1}", file, e.Message);
            }
        }

        private void LoadFromMapping(
            MappingDataNode datanode,
            bool overwrite = false,
            Dictionary<Type, HashSet<string>>? changed = null)
        {
            var type = datanode.Get<ValueDataNode>("type").Value;
            if (!_kindNames.TryGetValue(type, out var kind))
            {
                if (_ignoredPrototypeTypes.Contains(type))
                    return;

                throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
            }

            if (!datanode.TryGet<ValueDataNode>(IdDataFieldAttribute.Name, out var idNode))
                throw new PrototypeLoadException($"Prototype type {type} is missing an 'id' datafield.");

            var kindData = _kinds[kind];

            if (!overwrite && kindData.Instances.ContainsKey(idNode.Value))
                throw new PrototypeLoadException($"Duplicate ID: '{idNode.Value}'");

            kindData.Results[idNode.Value] = datanode;
            if (kindData.Inheritance is { } inheritance)
            {
                if (datanode.TryGet(ParentDataFieldAttribute.Name, out var parentNode))
                {
                    var parents = _serializationManager.Read<string[]>(parentNode);
                    inheritance.Add(idNode.Value, parents);
                }
                else
                {
                    inheritance.Add(idNode.Value);
                }
            }

            if (changed == null)
                return;

            if (!changed.TryGetValue(kind, out var set))
                changed[kind] = set = new HashSet<string>();

            set.Add(idNode.Value);
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
                    if (!_kindNames.TryGetValue(typeString, out var kind))
                        continue;

                    var kindData = _kinds[kind];

                    var id = node.GetNode("id").AsString();

                    if (kindData.Inheritance is { } tree)
                        tree.Remove(id, true);

                    kindData.Instances.Remove(id);
                    kindData.Results.Remove(id);
                }
            }
        }

        #endregion IPrototypeManager members

        private void ReloadPrototypeKinds()
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
                LoadFromMapping(datanode, overwrite, changed);
            }
        }

        public bool HasIndex<T>(string id) where T : class, IPrototype
        {
            if (!_kinds.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.Instances.ContainsKey(id);
        }

        public bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : class, IPrototype
        {
            var returned = TryIndex(typeof(T), id, out var proto);
            prototype = (proto ?? null) as T;
            return returned;
        }

        public bool TryIndex(Type kind, string id, [NotNullWhen(true)] out IPrototype? prototype)
        {
            if (!_kinds.TryGetValue(kind, out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.Instances.TryGetValue(id, out prototype);
        }

        public bool HasMapping<T>(string id)
        {
            if (!_kinds.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }

            return index.Results.ContainsKey(id);
        }

        public bool TryGetMapping(Type kind, string id, [NotNullWhen(true)] out MappingDataNode? mappings)
        {
            return _kinds[kind].Results.TryGetValue(id, out mappings);
        }

        /// <inheritdoc />
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public bool HasVariant(string variant)
        {
            return _kindNames.ContainsKey(variant);
        }

        /// <inheritdoc />
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public Type GetVariantType(string variant)
        {
            return _kindNames[variant];
        }

        /// <inheritdoc />
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public bool TryGetVariantType(string variant, [NotNullWhen(true)] out Type? prototype)
        {
            return _kindNames.TryGetValue(variant, out prototype);
        }

        /// <inheritdoc />
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
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
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public bool TryGetVariantFrom(IPrototype prototype, [NotNullWhen(true)] out string? variant)
        {
            return TryGetVariantFrom(prototype.GetType(), out variant);
        }

        /// <inheritdoc />
        [Obsolete("Variant is outdated naming, use *kind* functions instead")]
        public bool TryGetVariantFrom<T>([NotNullWhen(true)] out string? variant) where T : class, IPrototype
        {
            return TryGetVariantFrom(typeof(T), out variant);
        }

        public bool HasKind(string kind)
        {
            return _kindNames.ContainsKey(kind);
        }

        public Type GetKindType(string kind)
        {
            return _kindNames[kind];
        }

        public bool TryGetKindType(string kind, [NotNullWhen(true)] out Type? prototype)
        {
            return _kindNames.TryGetValue(kind, out prototype);
        }

        public bool TryGetKindFrom(Type type, [NotNullWhen(true)] out string? kind)
        {
            kind = null;

            // If the type doesn't implement IPrototype, this fails.
            if (!type.IsAssignableTo(typeof(IPrototype)))
                return false;

            var attribute = (PrototypeAttribute?) Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));

            // If the prototype type doesn't have the attribute, this fails.
            if (attribute == null)
                return false;

            // If the variant isn't registered, this fails.
            if (!HasKind(attribute.Type))
                return false;

            kind = attribute.Type;
            return true;
        }

        public bool TryGetKindFrom(IPrototype prototype, [NotNullWhen(true)] out string? kind)
        {
            return TryGetKindFrom(prototype.GetType(), out kind);
        }

        public bool TryGetKindFrom<T>([NotNullWhen(true)] out string? kind) where T : class, IPrototype
        {
            return TryGetKindFrom(typeof(T), out kind);
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

            if (_kindNames.ContainsKey(attribute.Type))
            {
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    $"Duplicate prototype type ID: {attribute.Type}. Current: {_kindNames[attribute.Type]}");
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

            _kindNames[attribute.Type] = type;
            _prototypePriorities[type] = attribute.LoadPriority;

            var kindData = new KindData();
            _kinds[type] = kindData;
            if (type.IsAssignableTo(typeof(IInheritingPrototype)))
                kindData.Inheritance = new MultiRootInheritanceGraph<string>();
        }

        public event Action<YamlStream, string>? LoadedData;
        public event Action<PrototypesReloadedEventArgs>? PrototypesReloaded;

        private sealed class KindData
        {
            public readonly Dictionary<string, IPrototype> Instances = new();
            public readonly Dictionary<string, MappingDataNode> Results = new();

            // Only initialized if prototype is inheriting.
            public MultiRootInheritanceGraph<string>? Inheritance;
        }
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
