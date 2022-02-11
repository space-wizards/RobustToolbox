using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Net;
using System.Runtime.Serialization;
using System.Threading;
using JetBrains.Annotations;
using Robust.Shared.Asynchronous;
using Robust.Shared.ContentPack;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Log;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes
{



    [Virtual]
    public class PrototypeManager : IPrototypeManager
    {
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;
        [Dependency] protected readonly IResourceManager Resources = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] protected readonly ITaskManager TaskManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        private readonly Dictionary<string, Type> _types = new();
        private readonly Dictionary<Type, int> _prototypePriorities = new();

        private bool _initialized;
        private bool _hasEverBeenReloaded;
        private int mappingErrors;

        #region IPrototypeManager members

        private readonly Dictionary<Type, Dictionary<string, IPrototype>> _prototypes = new();
        private readonly Dictionary<Type, Dictionary<string, DeserializationResult>> _prototypeResults = new();
        private readonly Dictionary<Type, PrototypeInheritanceTree> _inheritanceTrees = new();

        private readonly HashSet<Type> LoadBeforeList = new ();
        private readonly HashSet<Type> LoadNormalList = new ();
        private readonly HashSet<Type> LoadAfterList = new ();

        private readonly HashSet<ErrorNode> ErrorNodes = new ();

        private readonly HashSet<string> _ignoredPrototypeTypes = new();
        public virtual void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException($"{nameof(PrototypeManager)} has already been initialized.");
            }

            mappingErrors = 0;
            ReloadTypes();
            _initialized = true;
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
            mappingErrors = 0;
            _types.Clear();
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
            var changed = filePaths.SelectMany(f => LoadFromFile(f.ToRootedPath(), true)).ToList();
            ReloadPrototypes(changed);
#endif
        }

        internal void ReloadPrototypes(List<IPrototype> prototypes)
        {
#if !FULL_RELEASE
            prototypes.Sort((a, b) => SortPrototypesByPriority(a.GetType(), b.GetType()));

            var pushed = new Dictionary<Type, HashSet<string>>();

            foreach (var prototype in prototypes)
            {
                if (prototype is not IInheritingPrototype inheritingPrototype) continue;
                var type = prototype.GetType();
                if (!pushed.ContainsKey(type)) pushed[type] = new HashSet<string>();
                var baseNode = prototype.ID;

                if (pushed[type].Contains(baseNode))
                {
                    continue;
                }

                var tree = _inheritanceTrees[type];
                var currentNode = inheritingPrototype.Parent;

                if (currentNode == null)
                {
                    PushInheritance(type, baseNode, null, pushed[type]);
                    continue;
                }

                while (true)
                {
                    var parent = tree.GetParent(currentNode);

                    if (parent == null)
                    {
                        break;
                    }

                    baseNode = currentNode;
                    currentNode = parent;
                }

                PushInheritance(type, currentNode, baseNode, null, pushed[type]);
            }

            PrototypesReloaded?.Invoke(
                new PrototypesReloadedEventArgs(
                    prototypes
                        .GroupBy(p => p.GetType())
                        .ToDictionary(
                            g => g.Key,
                            g => new PrototypesReloadedEventArgs.PrototypeChangeSet(
                                g.ToDictionary(a => a.ID, a => a)))));

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

        #region Inheritance Tree
        public void Resync()
        {
            var trees = _inheritanceTrees.Keys.ToList();
            trees.Sort(SortPrototypesByPriority);
            foreach (var type in trees)
            {
                var tree = _inheritanceTrees[type];
                foreach (var baseNode in tree.BaseNodes)
                {
                    PushInheritance(type, baseNode, null, new HashSet<string>());
                }

                // Go over all prototypes and double check that their parent actually exists.
                var typePrototypes = _prototypes[type];
                foreach (var (id, proto) in typePrototypes)
                {
                    var iProto = (IInheritingPrototype) proto;

                    var parent = iProto.Parent;
                    if (parent != null && !typePrototypes.ContainsKey(parent!))
                    {
                        Logger.ErrorS("Serv3", $"{iProto.GetType().Name} '{id}' has invalid parent: {parent}");
                    }
                }
            }
        }

        public void PushInheritance(Type type, string id, string child, DeserializationResult? baseResult,
            HashSet<string> changed)
        {
            changed.Add(id);

            var myRes = _prototypeResults[type][id];
            var newResult = baseResult != null ? myRes.PushInheritanceFrom(baseResult) : myRes;

            PushInheritance(type, child, newResult, changed);

            newResult.CallAfterDeserializationHook();
            var populatedRes =
                _serializationManager.PopulateDataDefinition(_prototypes[type][id], (IDeserializedDefinition) newResult);
            _prototypes[type][id] = (IPrototype) populatedRes.RawValue!;
        }

        public void PushInheritance(Type type, string id, DeserializationResult? baseResult, HashSet<string> changed)
        {
            changed.Add(id);

            var myRes = _prototypeResults[type][id];
            var newResult = baseResult != null ? myRes.PushInheritanceFrom(baseResult) : myRes;

            foreach (var childID in _inheritanceTrees[type].Children(id))
            {
                PushInheritance(type, childID, newResult, changed);
            }

            if (newResult.RawValue is not IInheritingPrototype inheritingPrototype)
            {
                Logger.ErrorS("Serv3", $"PushInheritance was called on non-inheriting prototype! ({type}, {id})");
                return;
            }

            if (!inheritingPrototype.Abstract)
                newResult.CallAfterDeserializationHook();
            var populatedRes =
                _serializationManager.PopulateDataDefinition(_prototypes[type][id], (IDeserializedDefinition) newResult);
            _prototypes[type][id] = (IPrototype) populatedRes.RawValue!;
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
                    var type = _types[typeString];

                    var id = node.GetNode("id").AsString();

                    if (_inheritanceTrees.TryGetValue(type, out var tree))
                    {
                        tree.RemoveId(id);
                    }

                    _prototypes[type].Remove(id);
                }
            }
        }
        #endregion

        public Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResourcePath path)
        {
            var streams = Resources.ContentFindFiles(path).ToList().AsParallel()
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."));

            var dict = new Dictionary<string, HashSet<ErrorNode>>();

            foreach (var resourcePath in streams)
            {
                LoadFromFile(resourcePath, false, true);

                if (!ErrorNodes.Any())
                    continue;

                if (!dict.TryGetValue(resourcePath.ToString(), out var hashSet))
                    dict[resourcePath.ToString()] = new HashSet<ErrorNode>();

                dict[resourcePath.ToString()].UnionWith(ErrorNodes);

            }

            return dict;
        }

    #region Prototype Loading


        /// <summary>
        /// Loads Prototypes from a path.
        /// </summary>
        /// <param name="path">path to files to load as prototype.</param>
        /// <param name="overwrite">Overwrite if prototype already is loaded and exists</param>
        /// <returns>HashSet of Loaded Prototypes</returns>
        public List<IPrototype> LoadDirectory(ResourcePath path, bool overwrite = false)
        {
            var changedPrototypes = new List<IPrototype>();

            _hasEverBeenReloaded = true;
            var streams = Resources.ContentFindFiles(path).ToList().AsParallel()
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."));

            foreach (var resourcePath in streams)
            {
                var filePrototypes = LoadFromFile(resourcePath, overwrite);
                changedPrototypes.AddRange(filePrototypes);
            }

            return changedPrototypes;
        }

        /// <summary>
        /// Loads Prototypes from a file.
        /// </summary>
        /// <param name="file">file to load as prototype.</param>
        /// <param name="overwrite">Overwrite if prototype already is loaded and exists</param>
        ///      /// <param name="validateMapping">toggle true to receive a count of total mapping errors</param>
        /// <returns>HashSet of Loaded Prototypes</returns>
        public HashSet<IPrototype> LoadFromFile(ResourcePath file, bool overwrite = false, bool validateMapping = false)
        {
            HashSet<IPrototype> LoadedPrototypes = new();
            try
            {
                try
                {
                    var reader = new StreamReader(Resources.ContentFileRead(file), EncodingHelpers.UTF8);

                    var yamlStream = new YamlStream();
                    yamlStream.Load(reader);

                    LoadedPrototypes = LoadFromDocument(yamlStream, overwrite, validateMapping, actionMessage: file.ToString());
                }
                catch (IOException e)
                {
                    Logger.Error($"Error loading prototypes in file {file}.", e);
                }
            }
            catch (YamlException e)
            {
                var sawmill = Logger.GetSawmill("eng");
                sawmill.Error("Caught YamlException whilst loading prototypes from a File {0}: {1}", file.Filename, e.Message);
            }

            return LoadedPrototypes;
        }

        /// <summary>
        /// Loads Prototypes from a string.
        /// </summary>
        /// <param name="str">Input string to load as prototype.</param>
        /// <param name="overwrite">Overwrite if prototype already is loaded and exists</param>
        /// <param name="actionMessage">String that will be included in the LoadedData Event</param>
        /// <returns>HashSet of Loaded Prototypes</returns>
        public HashSet<IPrototype> LoadFromString(string str, bool overwrite = false, string actionMessage = "")
        {
            var yamlStream = new YamlStream();
            yamlStream.Load(new StringReader(str));

            return LoadFromDocument(yamlStream, overwrite, actionMessage: actionMessage);
        }

        /// <summary>
        /// Loads YAML Prototypes via a Yaml Stream
        /// </summary>
        /// <param name="yamlStream">YamlStream to process</param>
        /// <param name="mappingErrors">a count of mapping errors. is 0 until you toggle validateMapping on.</param>
        /// <param name="validateMapping">toggle true to receive a count of total mapping errors</param>
        /// <param name="overwrite">Overwrite if prototype already is loaded and exists.</param>
        /// <param name="actionMessage">String that will be included in the LoadedData Event</param>
        /// <returns>HashSet of Loaded Prototypes</returns>
        /// <exception cref="PrototypeLoadException">Thrown when Prototype failed to load</exception>
        private HashSet<IPrototype> LoadFromDocument(YamlStream yamlStream, bool overwrite = false,
            bool validateMapping = false, string actionMessage = "")
        {
            var loadedPrototypes = new HashSet<IPrototype>();


            for (var i = 0; i < yamlStream.Documents.Count(); i++)
            {
                var prototypeDocument = yamlStream.Documents[i];
                var rootNode = (YamlSequenceNode) prototypeDocument.RootNode;

                foreach (YamlMappingNode node in rootNode.Cast<YamlMappingNode>())
                {
                    var type = node.GetNode("type").AsString();
                    if (!_types.ContainsKey(type))
                    {
                        if (_ignoredPrototypeTypes.Contains(type))
                        {
                            continue;
                        }

                        throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
                    }

                    var prototypeType = _types[type];

                    var res = _serializationManager.Read(prototypeType, node.ToDataNode(), skipHook: true);
                    var prototype = (IPrototype) res.RawValue!;

                    if (!overwrite && _prototypes[prototypeType].ContainsKey(prototype.ID))
                    {
                        throw new PrototypeLoadException($"Duplicate ID: '{prototype.ID}'");
                    }

                    _prototypeResults[prototypeType][prototype.ID] = res;
                    if (prototype is IInheritingPrototype inheritingPrototype)
                    {
                        _inheritanceTrees[prototypeType].AddId(prototype.ID, inheritingPrototype.Parent, true);
                    }
                    else
                    {
                        //we call it here since it wont get called when pushing inheritance
                        res.CallAfterDeserializationHook();
                    }

                    _prototypes[prototypeType][prototype.ID] = prototype;
                    loadedPrototypes.Add(prototype);

                    if (validateMapping)
                    {
                        var mapping = node.ToDataNodeCast<MappingDataNode>();
                        mapping.Remove("type");
                        var errorNodes = _serializationManager.ValidateNode(_types[type], mapping).GetErrors()
                            .ToHashSet();
                        mappingErrors = errorNodes.Count;

                    }
                }
            }
            LoadedData?.Invoke(yamlStream, actionMessage);
            return loadedPrototypes;
        }
        #endregion
        #endregion IPrototypeManager members

        private void ReloadTypes()
        {
            Clear();
            foreach (var type in _reflectionManager.GetAllChildren<IPrototype>())
            {
                var prototypeAttributes = (PrototypeAttribute?) Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));

                if ( prototypeAttributes!.LoadBefore != string.Empty)
                    LoadBeforeList.Add(type);
                else if (prototypeAttributes!.LoadAfter != string.Empty)
                    LoadAfterList.Add(type);
                else
                    LoadNormalList.Add(type);

                RegisterType(type);
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

        /// <inheritdoc />
        public bool HasVariant(string variant)
        {
            return _types.ContainsKey(variant);
        }

        /// <inheritdoc />
        public Type GetVariantType(string variant)
        {
            return _types[variant];
        }

        /// <inheritdoc />
        public bool TryGetVariantType(string variant, [NotNullWhen(true)] out Type? prototype)
        {
            return _types.TryGetValue(variant, out prototype);
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

            if (_types.ContainsKey(attribute.Type))
            {
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    $"Duplicate prototype type ID: {attribute.Type}. Current: {_types[attribute.Type]}");
            }

            _types[attribute.Type] = type;
            _prototypePriorities[type] = attribute.LoadPriority;

            if (typeof(IPrototype).IsAssignableFrom(type))
            {
                _prototypes[type] = new Dictionary<string, IPrototype>();
                _prototypeResults[type] = new Dictionary<string, DeserializationResult>();
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
