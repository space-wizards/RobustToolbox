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
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.IoC.Exceptions;
using Robust.Shared.Log;
using Robust.Shared.Network;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
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
        bool HasIndex<T>(string id) where T : IPrototype;
        bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : IPrototype;

        /// <summary>
        /// Load prototypes from files in a directory, recursively.
        /// </summary>
        List<IPrototype> LoadDirectory(ResourcePath path);

        Dictionary<string, HashSet<ErrorNode>> ValidateDirectory(ResourcePath path);

        List<IPrototype> LoadFromStream(TextReader stream);

        List<IPrototype> LoadString(string str);

        /// <summary>
        /// Clear out all prototypes and reset to a blank slate.
        /// </summary>
        void Clear();

        /// <summary>
        ///     Performs a reload on all prototypes, updating the game state accordingly
        /// </summary>
        void ReloadPrototypes(ResourcePath file);

        /// <summary>
        /// Syncs all inter-prototype data. Call this when operations adding new prototypes are done.
        /// </summary>
        void Resync();

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
    }

    /// <summary>
    /// Quick attribute to give the prototype its type string.
    /// To prevent needing to instantiate it because interfaces can't declare statics.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
    [BaseTypeRequired(typeof(IPrototype))]
    [MeansImplicitUse]
    [MeansDataDefinition]
    public class PrototypeAttribute : Attribute
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

    public class PrototypeManager : IPrototypeManager, IPostInjectInit
    {
        [Dependency] private readonly IReflectionManager ReflectionManager = default!;
        [Dependency] private readonly IDynamicTypeFactoryInternal _dynamicTypeFactory = default!;
        [Dependency] public readonly IResourceManager Resources = default!;
        [Dependency] private readonly IEntityManager _entityManager = default!;
        [Dependency] public readonly ITaskManager TaskManager = default!;
        [Dependency] public readonly INetManager NetManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;

        private readonly Dictionary<string, Type> prototypeTypes = new();
        private readonly Dictionary<Type, int> prototypePriorities = new();

        private bool _initialized;
        private bool _hasEverBeenReloaded;
        private bool _hasEverResynced;

        #region IPrototypeManager members
        private readonly Dictionary<Type, Dictionary<string, IPrototype>> prototypes = new();
        private readonly Dictionary<Type, Dictionary<string, DeserializationResult>> _prototypeResults = new();
        private readonly Dictionary<Type, PrototypeInheritanceTree> _inheritanceTrees = new();

        private readonly HashSet<string> IgnoredPrototypeTypes = new();

        public virtual void Initialize()
        {
            if (_initialized)
            {
                throw new InvalidOperationException($"{nameof(PrototypeManager)} has already been initialized.");
            }

            _initialized = true;
        }

        public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return prototypes[typeof(T)].Values.Select(p => (T) p);
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(Type type)
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }

            return prototypes[type].Values;
        }

        public T Index<T>(string id) where T : class, IPrototype
        {
            if (!_hasEverBeenReloaded)
            {
                throw new InvalidOperationException("No prototypes have been loaded yet.");
            }
            try
            {
                return (T)prototypes[typeof(T)][id];
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

            return prototypes[type][id];
        }

        public void Clear()
        {
            prototypeTypes.Clear();
            prototypes.Clear();
            _prototypeResults.Clear();
            _inheritanceTrees.Clear();
        }

        private int SortPrototypesByPriority(Type a, Type b)
        {
            return prototypePriorities[b].CompareTo(prototypePriorities[a]);
        }

        public virtual void ReloadPrototypes(ResourcePath file)
        {
#if !FULL_RELEASE
            var changed = LoadFile(file.ToRootedPath(), true).ToList();
            changed.Sort((prototype, prototype1) => SortPrototypesByPriority(prototype.GetType(), prototype1.GetType()));
            var pushed = new Dictionary<Type, HashSet<string>>();

            foreach (var prototype in changed)
            {
                if(prototype is not IInheritingPrototype inheritingPrototype) continue;
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

            // TODO filter by entity prototypes changed
            if (!pushed.ContainsKey(typeof(EntityPrototype))) return;

            var entityPrototypes = prototypes[typeof(EntityPrototype)];

            foreach (var prototype in pushed[typeof(EntityPrototype)])
            {
                foreach (var entity in _entityManager.GetEntities(new PredicateEntityQuery(e => e.Prototype != null && e.Prototype.ID == prototype)))
                {
                    ((EntityPrototype) entityPrototypes[prototype]).UpdateEntity((Entity) entity);
                }
            }
#endif
        }

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
            }
        }

        public void PushInheritance(Type type, string id, string child, DeserializationResult? baseResult, HashSet<string> changed)
        {
            changed.Add(id);

            var myRes = _prototypeResults[type][id];
            var newResult = baseResult != null ? myRes.PushInheritanceFrom(baseResult) : myRes;

            PushInheritance(type, child, newResult, changed);

            newResult.CallAfterDeserializationHook();
            var populatedRes = _serializationManager.PopulateDataDefinition(prototypes[type][id], (IDeserializedDefinition)newResult);
            prototypes[type][id] = (IPrototype) populatedRes.RawValue!;
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

            if(newResult.RawValue is not IInheritingPrototype inheritingPrototype)
            {
                Logger.ErrorS("Serv3", $"PushInheritance was called on non-inheriting prototype! ({type}, {id})");
                return;
            }

            if(!inheritingPrototype.Abstract)
                newResult.CallAfterDeserializationHook();
            var populatedRes = _serializationManager.PopulateDataDefinition(prototypes[type][id], (IDeserializedDefinition)newResult);
            prototypes[type][id] = (IPrototype) populatedRes.RawValue!;
        }

        /// <inheritdoc />
        public List<IPrototype> LoadDirectory(ResourcePath path)
        {
            var changedPrototypes = new List<IPrototype>();

            _hasEverBeenReloaded = true;
            var streams = Resources.ContentFindFiles(path).ToList().AsParallel()
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."));

            foreach (var resourcePath in streams)
            {
                var filePrototypes = LoadFile(resourcePath);
                changedPrototypes.AddRange(filePrototypes);
            }

            return changedPrototypes;
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
                        if (!prototypeTypes.ContainsKey(type))
                        {
                            if (IgnoredPrototypeTypes.Contains(type))
                            {
                                continue;
                            }

                            throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
                        }

                        var mapping = node.ToDataNodeCast<MappingDataNode>();
                        mapping.RemoveNode("type");
                        var errorNodes = _serializationManager.ValidateNode(prototypeTypes[type], mapping).GetErrors().ToHashSet();
                        if (errorNodes.Count != 0)
                        {
                            if (!dict.TryGetValue(resourcePath.ToString(), out var hashSet))
                                dict[resourcePath.ToString()] = new HashSet<ErrorNode>();
                            dict[resourcePath.ToString()].UnionWith(errorNodes);
                        }
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

        public HashSet<IPrototype> LoadFile(ResourcePath file, bool overwrite = false)
        {
            var changedPrototypes = new HashSet<IPrototype>();

            try
            {
                using var reader = ReadFile(file, !overwrite);

                if (reader == null)
                {
                    return changedPrototypes;
                }

                var yamlStream = new YamlStream();
                yamlStream.Load(reader);

                LoadedData?.Invoke(yamlStream, file.ToString());

                for (var i = 0; i < yamlStream.Documents.Count; i++)
                {
                    try
                    {
                        var documentPrototypes = LoadFromDocument(yamlStream.Documents[i], overwrite);
                        changedPrototypes.UnionWith(documentPrototypes);
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

            return changedPrototypes;
        }

        public List<IPrototype> LoadFromStream(TextReader stream)
        {
            var changedPrototypes = new List<IPrototype>();
            _hasEverBeenReloaded = true;
            var yaml = new YamlStream();
            yaml.Load(stream);

            for (var i = 0; i < yaml.Documents.Count; i++)
            {
                try
                {
                    var documentPrototypes = LoadFromDocument(yaml.Documents[i]);
                    changedPrototypes.AddRange(documentPrototypes);
                }
                catch (Exception e)
                {
                    throw new PrototypeLoadException($"Failed to load prototypes from document#{i}", e);
                }
            }

            LoadedData?.Invoke(yaml, "anonymous prototypes YAML stream");

            return changedPrototypes;
        }

        public List<IPrototype> LoadString(string str)
        {
            return LoadFromStream(new StringReader(str));
        }

        #endregion IPrototypeManager members

        public void PostInject()
        {
            ReflectionManager.OnAssemblyAdded += (_, _) => ReloadPrototypeTypes();
            ReloadPrototypeTypes();
        }

        private void ReloadPrototypeTypes()
        {
            Clear();
            foreach (var type in ReflectionManager.GetAllChildren<IPrototype>())
            {
                RegisterType(type);
            }
        }

        private HashSet<IPrototype> LoadFromDocument(YamlDocument document, bool overwrite = false)
        {
            var changedPrototypes = new HashSet<IPrototype>();
            var rootNode = (YamlSequenceNode) document.RootNode;

            foreach (YamlMappingNode node in rootNode.Cast<YamlMappingNode>())
            {
                var type = node.GetNode("type").AsString();
                if (!prototypeTypes.ContainsKey(type))
                {
                    if (IgnoredPrototypeTypes.Contains(type))
                    {
                        continue;
                    }

                    throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
                }

                var prototypeType = prototypeTypes[type];
                var res = _serializationManager.Read(prototypeType, node.ToDataNode(), skipHook: true);
                var prototype = (IPrototype) res.RawValue!;

                if (!overwrite && prototypes[prototypeType].ContainsKey(prototype.ID))
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

                prototypes[prototypeType][prototype.ID] = prototype;
                changedPrototypes.Add(prototype);
            }

            return changedPrototypes;
        }

        public bool HasIndex<T>(string id) where T : IPrototype
        {
            if (!prototypes.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }
            return index.ContainsKey(id);
        }

        public bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : IPrototype
        {
            if (!prototypes.TryGetValue(typeof(T), out var index))
            {
                throw new UnknownPrototypeException(id);
            }
            var returned = index.TryGetValue(id, out var uncast);
            prototype = (T) uncast!;
            return returned;
        }

        public void RegisterIgnore(string name)
        {
            IgnoredPrototypeTypes.Add(name);
        }

        /// <inheritdoc />
        public void RegisterType(Type type)
        {
            if(!(typeof(IPrototype).IsAssignableFrom(type)))
                throw new InvalidOperationException("Type must implement IPrototype.");

            var attribute = (PrototypeAttribute?)Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute));

            if (attribute == null)
            {
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    "No " + nameof(PrototypeAttribute) + " to give it a type string.");
            }

            if (prototypeTypes.ContainsKey(attribute.Type))
            {
                throw new InvalidImplementationException(type,
                    typeof(IPrototype),
                    $"Duplicate prototype type ID: {attribute.Type}. Current: {prototypeTypes[attribute.Type]}");
            }

            prototypeTypes[attribute.Type] = type;
            prototypePriorities[type] = attribute.LoadPriority;

            if (typeof(IPrototype).IsAssignableFrom(type))
            {
                prototypes[type] = new Dictionary<string, IPrototype>();
                _prototypeResults[type] = new Dictionary<string, DeserializationResult>();
                if(typeof(IInheritingPrototype).IsAssignableFrom(type))
                    _inheritanceTrees[type] = new PrototypeInheritanceTree();
            }
        }

        public event Action<YamlStream, string>? LoadedData;

    }

    [Serializable]
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
            Prototype = (string?)info.GetValue("prototype", typeof(string));
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("prototype", Prototype, typeof(string));
        }
    }
}
