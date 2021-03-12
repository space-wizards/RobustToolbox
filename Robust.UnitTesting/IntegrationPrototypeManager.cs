using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Utility;
using YamlDotNet.Core;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting
{
    public class IntegrationPrototypeManager : IIntegrationPrototypeManager, IPostInjectInit
    {
        private static readonly ResourcePath DefaultDirectory = new("/Prototypes");

        private static ImmutableDictionary<Type, ImmutableDictionary<string, IPrototype>> _defaultPrototypes =
            ImmutableDictionary<Type, ImmutableDictionary<string, IPrototype>>.Empty;

        private static ImmutableHashSet<string> _defaultFiles = ImmutableHashSet<string>.Empty;

        private static ImmutableDictionary<string, ImmutableHashSet<IPrototype>> _defaultFilePrototypes =
            ImmutableDictionary<string, ImmutableHashSet<IPrototype>>.Empty;

        private static ImmutableHashSet<(YamlStream data, string file)> _defaultData =
            ImmutableHashSet<(YamlStream data, string file)>.Empty;

        private static ImmutableDictionary<Type, PrototypeInheritanceTree> _defaultInheritanceTrees = ImmutableDictionary<Type, PrototypeInheritanceTree>.Empty;

        private static ImmutableHashSet<Type> _defaultTypes = ImmutableHashSet<Type>.Empty;

        private static ImmutableDictionary<Type, int> _defaultPriorities = ImmutableDictionary<Type, int>.Empty;

        private static ImmutableDictionary<Type, ImmutableDictionary<string, DeserializationResult>> _defaultResults = ImmutableDictionary<Type, ImmutableDictionary<string, DeserializationResult>>.Empty;

        private static ImmutableHashSet<string> _defaultIgnored = ImmutableHashSet<string>.Empty;

        [Dependency] private readonly IResourceManager _resourceManager = default!;
        [Dependency] private readonly ISerializationManager _serializationManager = default!;
        [Dependency] private readonly IReflectionManager _reflectionManager = default!;

        private readonly Dictionary<string, Type> _types = new();
        // todo dont fill this with empty placeholders
        private readonly Dictionary<Type, Dictionary<string, IPrototype>> _prototypes = new();
        private readonly Dictionary<Type, Dictionary<string, DeserializationResult>> _results = new();
        private readonly Dictionary<Type, PrototypeInheritanceTree> _inheritanceTrees = new();
        private readonly HashSet<string> _ignored = new();
        private readonly Dictionary<Type, int> _priorities = new();
        private readonly HashSet<string> _queuedStrings = new();

        // todo move this somewhere else
        private void Setup(IResourceManager resourceManager, ISerializationManager serializationManager)
        {
            var defaultPriorities = new Dictionary<Type, int>();
            var defaultTypes = new Dictionary<string, Type>();
            var defaultPrototypes = new Dictionary<Type, Dictionary<string, IPrototype>>();
            var defaultResults = new Dictionary<Type, Dictionary<string, DeserializationResult>>();
            var defaultInheritanceTrees = new Dictionary<Type, PrototypeInheritanceTree>();

            foreach (var type in _reflectionManager.GetAllChildren<IPrototype>())
            {
                var attribute = (PrototypeAttribute?) Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute))!;

                defaultTypes.Add(attribute.Type, type);
                defaultPriorities[type] = attribute.LoadPriority;

                if (typeof(IPrototype).IsAssignableFrom(type))
                {
                    defaultPrototypes[type] = new Dictionary<string, IPrototype>();
                    defaultResults[type] = new Dictionary<string, DeserializationResult>();
                    if (typeof(IInheritingPrototype).IsAssignableFrom(type))
                        defaultInheritanceTrees[type] = new PrototypeInheritanceTree();
                }
            }

            var files = new HashSet<string>();
            var allFilePrototypes = new Dictionary<string, HashSet<IPrototype>>();
            var data = new HashSet<(YamlStream data, string file)>();

            var streams = resourceManager
                .ContentFindFiles(new ResourcePath("/Prototypes"))
                .ToList()
                .AsParallel()
                .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."));

            foreach (var file in streams)
            {
                files.Add(file.ToString());

                var reader = new StreamReader(resourceManager.ContentFileRead(file), EncodingHelpers.UTF8);

                var yamlStream = new YamlStream();
                yamlStream.Load(reader);

                // todo vibe check this
                data.Add((yamlStream, file.ToString()));

                var filePrototypes = new HashSet<IPrototype>();

                foreach (var document in yamlStream.Documents)
                {
                    var rootNode = (YamlSequenceNode) document.RootNode;

                    foreach (YamlMappingNode node in rootNode.Cast<YamlMappingNode>())
                    {
                        var type = node.GetNode("type").AsString();
                        if (!defaultTypes.ContainsKey(type))
                        {
                            // Skip validating prototype ignores here
                            continue;
                        }

                        var prototypeType = _types[type];
                        var res = serializationManager.Read(prototypeType, node.ToDataNode(), skipHook: true);
                        var prototype = (IPrototype) res.RawValue!;

                        if (defaultPrototypes[prototypeType].ContainsKey(prototype.ID))
                        {
                            throw new PrototypeLoadException($"Duplicate ID: '{prototype.ID}'");
                        }

                        defaultResults[prototypeType][prototype.ID] = res;
                        if (prototype is IInheritingPrototype inheritingPrototype)
                        {
                            defaultInheritanceTrees[prototypeType].AddId(prototype.ID, inheritingPrototype.Parent, true);
                        }
                        else
                        {
                            //we call it here since it wont get called when pushing inheritance
                            res.CallAfterDeserializationHook();
                        }

                        defaultPrototypes[prototypeType][prototype.ID] = prototype;
                        filePrototypes.Add(prototype);
                    }
                }

                allFilePrototypes.Add(file.ToString(), filePrototypes);
            }

            Resync(defaultInheritanceTrees, defaultPriorities, defaultResults, defaultPrototypes);

            _defaultPrototypes = defaultPrototypes.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableDictionary());
            _defaultFiles = files.ToImmutableHashSet();
            _defaultFilePrototypes = allFilePrototypes.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableHashSet());
            _defaultData = data.ToImmutableHashSet();
            _defaultInheritanceTrees = defaultInheritanceTrees.ToImmutableDictionary();
            _defaultPriorities = defaultPriorities.ToImmutableDictionary();
        }

        public IntegrationPrototypeManager()
        {

        }

        public void PostInject()
        {
            _reflectionManager.OnAssemblyAdded += (_, _) => ReloadPrototypeTypes();
            ReloadPrototypeTypes();
        }

        private void ReloadPrototypeTypes()
        {
            Clear();

            foreach (var type in _reflectionManager.GetAllChildren<IPrototype>())
            {
                RegisterType(type);
            }
        }

        public void Initialize()
        {
        }

        public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
        {
            foreach (var prototype in _defaultPrototypes[typeof(T)].Values)
            {
                yield return (T) prototype;
            }

            foreach (var prototype in _prototypes[typeof(T)].Values)
            {
                yield return (T) prototype;
            }
        }

        public IEnumerable<IPrototype> EnumeratePrototypes(Type type)
        {
            return _defaultPrototypes[type].Values.Concat(_prototypes[type].Values);
        }

        public T Index<T>(string id) where T : class, IPrototype
        {
            if (_defaultPrototypes.TryGetValue(typeof(T), out var prototypeIds) &&
                prototypeIds.TryGetValue(id, out var prototype))
            {
                return (T) prototype;
            }

            return (T) _prototypes[typeof(T)][id];
        }

        public IPrototype Index(Type type, string id)
        {
            if (_defaultPrototypes[type].TryGetValue(id, out var prototype))
            {
                return prototype;
            }

            return _prototypes[type][id];
        }

        public bool HasIndex<T>(string id) where T : IPrototype
        {
            return _defaultPrototypes[typeof(T)].ContainsKey(id) ||
                   _prototypes[typeof(T)].ContainsKey(id);
        }

        public bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : IPrototype
        {
            if (_defaultPrototypes.TryGetValue(typeof(T), out var prototypeIds) &&
                prototypeIds.TryGetValue(id, out var prototypeUnCast))
            {
                prototype = (T) prototypeUnCast;
                return true;
            }

            if (_prototypes[typeof(T)].TryGetValue(id, out prototypeUnCast))
            {
                prototype = (T) prototypeUnCast;
                return true;
            }

            prototype = default;
            return false;
        }

        private void InitDefault()
        {
            lock (_defaultPrototypes)
            {
                if (_defaultPrototypes.IsEmpty)
                {
                    Setup(_resourceManager, _serializationManager);
                }
            }
        }

        protected HashSet<IPrototype> LoadFile(ResourcePath file, bool overwrite = false)
        {
            if (_defaultPrototypes.IsEmpty)
            {
                InitDefault();
            }

            // TODO make this not return a new hash set each time
            if (_defaultFilePrototypes.TryGetValue(file.ToString(), out var filePrototypes))
            {
                return filePrototypes.ToHashSet();
            }

            var changedPrototypes = new HashSet<IPrototype>();

            try
            {
                using var reader = new StreamReader(_resourceManager.ContentFileRead(file), EncodingHelpers.UTF8);

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

        public List<IPrototype> LoadDirectory(ResourcePath path)
        {
            // TODO cache this
            if (path == DefaultDirectory)
            {
                if (_defaultPrototypes.IsEmpty)
                {
                    InitDefault();
                }

                foreach (var (data, file) in _defaultData)
                {
                    LoadedData?.Invoke(data, file);
                }

                return _defaultPrototypes.Values.SelectMany(e => e.Values).ToList();
            }

            var changedPrototypes = new List<IPrototype>();

            var streams = _resourceManager
                .ContentFindFiles(path)
                .ToList()
                .AsParallel()
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
            throw new NotImplementedException();
        }

        private HashSet<IPrototype> LoadFromDocument(YamlDocument document, bool overwrite = false)
        {
            var changedPrototypes = new HashSet<IPrototype>();
            var rootNode = (YamlSequenceNode) document.RootNode;

            foreach (YamlMappingNode node in rootNode.Cast<YamlMappingNode>())
            {
                var type = node.GetNode("type").AsString();
                if (!_types.ContainsKey(type))
                {
                    if (_ignored.Contains(type))
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

                _results[prototypeType][prototype.ID] = res;
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
                changedPrototypes.Add(prototype);
            }

            return changedPrototypes;
        }

        public List<IPrototype> LoadFromStream(TextReader stream)
        {
            var changedPrototypes = new List<IPrototype>();
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

        public void Clear()
        {
            _types.Clear();
            _prototypes.Clear();
            _results.Clear();
            _inheritanceTrees.Clear();
        }

        public void ReloadPrototypes(ResourcePath file)
        {
        }

        public void Resync()
        {
            Resync(_inheritanceTrees, _priorities, _results, _prototypes);
        }

        public void Resync(
            Dictionary<Type, PrototypeInheritanceTree> trees,
            Dictionary<Type, int> priorities,
            Dictionary<Type, Dictionary<string, DeserializationResult>> results,
            Dictionary<Type, Dictionary<string, IPrototype>> prototypes)
        {
            var treeKeys = trees.Keys.ToList();
            treeKeys.Sort((a, b) => priorities[b].CompareTo(priorities[a]));

            foreach (var type in treeKeys)
            {
                var tree = trees[type];

                foreach (var baseNode in tree.BaseNodes)
                {
                    PushInheritance(type, baseNode, null, new HashSet<string>(), results, trees, prototypes);
                }
            }
        }

        public void PushInheritance(
            Type type,
            string id,
            DeserializationResult? baseResult,
            HashSet<string> changed,
            Dictionary<Type, Dictionary<string, DeserializationResult>> results,
            Dictionary<Type, PrototypeInheritanceTree> trees,
            Dictionary<Type, Dictionary<string, IPrototype>> prototypes)
        {
            changed.Add(id);

            var myRes = results[type][id];
            var newResult = baseResult != null ? myRes.PushInheritanceFrom(baseResult) : myRes;

            foreach (var childID in trees[type].Children(id))
            {
                PushInheritance(type, childID, newResult, changed, results, trees, prototypes);
            }

            if (newResult.RawValue is not IInheritingPrototype inheritingPrototype)
            {
                Logger.ErrorS("Serv3", $"PushInheritance was called on non-inheriting prototype! ({type}, {id})");
                return;
            }

            if (!inheritingPrototype.Abstract)
                newResult.CallAfterDeserializationHook();

            var populatedRes =
                _serializationManager.PopulateDataDefinition(prototypes[type][id], (IDeserializedDefinition) newResult);

            prototypes[type][id] = (IPrototype) populatedRes.RawValue!;
        }

        public void RegisterIgnore(string name)
        {
            _ignored.Add(name);
        }

        public void RegisterType(Type type)
        {
            var attribute = (PrototypeAttribute?) Attribute.GetCustomAttribute(type, typeof(PrototypeAttribute))!;

            _types.Add(attribute.Type, type);
            _priorities[type] = attribute.LoadPriority;

            if (typeof(IPrototype).IsAssignableFrom(type))
            {
                _prototypes[type] = new Dictionary<string, IPrototype>();
                _results[type] = new Dictionary<string, DeserializationResult>();
                if(typeof(IInheritingPrototype).IsAssignableFrom(type))
                    _inheritanceTrees[type] = new PrototypeInheritanceTree();
            }
        }

        public event Action<YamlStream, string>? LoadedData;

        public void QueueLoadString(string str)
        {
            _queuedStrings.Add(str);
        }
    }
}
