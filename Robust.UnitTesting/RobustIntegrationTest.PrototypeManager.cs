using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Network;
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
    public partial class RobustIntegrationTest
    {
        internal class IntegrationPrototypeManager : IIntegrationPrototypeManager, IPostInjectInit
        {
            private static readonly ResourcePath DefaultDirectory = new("/Prototypes");

            private static volatile PrototypeData? _defaultClientData;
            private static readonly object ClientDataLock = new();

            private static volatile PrototypeData? _defaultServerData;
            private static readonly object ServerDataLock = new();

            [Dependency] private readonly IResourceManager _resourceManager = default!;
            [Dependency] private readonly ISerializationManager _serializationManager = default!;
            [Dependency] private readonly IReflectionManager _reflectionManager = default!;
            [Dependency] private readonly INetManager _netManager = default!;

            private PrototypeData _defaultData = default!;

            private readonly Dictionary<string, Type> _types = new();
            private readonly Dictionary<Type, Dictionary<string, IPrototype>> _prototypes = new();
            private readonly Dictionary<Type, Dictionary<string, IPrototype>> _testPrototypes = new();
            private readonly Dictionary<Type, Dictionary<string, DeserializationResult>> _results = new();
            private readonly Dictionary<Type, PrototypeInheritanceTree> _inheritanceTrees = new();
            private readonly HashSet<string> _ignored = new();
            private readonly Dictionary<Type, int> _priorities = new();
            private readonly HashSet<string> _queuedStrings = new();
            private readonly Dictionary<string, HashSet<IPrototype>> _defaultFilePrototypes = new();

            private PrototypeData GetData()
            {
                // TODO clean this up to a single property
                if (_netManager.IsClient)
                {
                    if (_defaultClientData == null)
                    {
                        lock (ClientDataLock)
                        {
                            if (_defaultClientData == null)
                            {
                                _defaultClientData = new PrototypeData(
                                    _resourceManager,
                                    _serializationManager,
                                    _reflectionManager);

                                _defaultClientData.Resync(this);
                            }
                        }
                    }

                    return _defaultClientData;
                }

                if (_defaultServerData == null)
                {
                    lock (ServerDataLock)
                    {
                        if (_defaultServerData == null)
                        {
                            _defaultServerData = new PrototypeData(
                                _resourceManager,
                                _serializationManager,
                                _reflectionManager);

                            _defaultServerData.Resync(this);
                        }
                    }
                }

                return _defaultServerData;
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
                _defaultData = GetData();

                foreach (var (type, prototypes) in _defaultData.DefaultPrototypes)
                {
                    foreach (var (id, prototype) in prototypes)
                    {
                        var copy = _serializationManager.CreateCopy(prototype)
                                    ?? throw new NullReferenceException();

                        _prototypes[type].Add(id, copy);
                    }
                }

                foreach (var (type, (id, result)) in _defaultData.DefaultResults)
                {
                    _results.GetOrNew(type).Add(id, result);
                }

                foreach (var (type, tree) in _defaultData.DefaultInheritanceTrees)
                {
                    _inheritanceTrees[type] = tree;
                }

                foreach (var (type, priority) in _defaultData.DefaultPriorities)
                {
                    _priorities[type] = priority;
                }

                foreach (var (file, prototypes) in _defaultData.DefaultFilePrototypes)
                {
                    var prototypesCopy = new HashSet<IPrototype>();

                    foreach (var prototype in prototypes)
                    {
                        var copy = _serializationManager.CreateCopy(prototype)
                                   ?? throw new NullReferenceException();
                        prototypesCopy.Add(copy);
                    }

                    _defaultFilePrototypes.Add(file, prototypesCopy);
                }

                foreach (var str in _queuedStrings)
                {
                    LoadString(str);
                }
            }

            public IEnumerable<T> EnumeratePrototypes<T>() where T : class, IPrototype
            {
                foreach (var prototype in _prototypes[typeof(T)].Values)
                {
                    yield return (T) prototype;
                }
            }

            public IEnumerable<IPrototype> EnumeratePrototypes(Type type)
            {
                return _prototypes[type].Values;
            }

            public T Index<T>(string id) where T : class, IPrototype
            {
                if (!_prototypes[typeof(T)].TryGetValue(id, out var prototype))
                {
                    throw new UnknownPrototypeException(id);
                }

                return (T) prototype;
            }

            public IPrototype Index(Type type, string id)
            {
                return _prototypes[type][id];
            }

            public bool HasIndex<T>(string id) where T : IPrototype
            {
                return _prototypes[typeof(T)].ContainsKey(id);
            }

            public bool TryIndex<T>(string id, [NotNullWhen(true)] out T? prototype) where T : IPrototype
            {
                if (_prototypes[typeof(T)].TryGetValue(id, out var prototypeUnCast))
                {
                    prototype = (T) prototypeUnCast;
                    return true;
                }

                prototype = default;
                return false;
            }

            protected HashSet<IPrototype> LoadFile(ResourcePath file, bool overwrite = false)
            {
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
                    foreach (var (stream, file) in _defaultData.DefaultStreams)
                    {
                        LoadedData?.Invoke(stream, file);
                    }

                    return _defaultFilePrototypes.SelectMany(x => x.Value).ToList();
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
                    _testPrototypes.GetOrNew(prototypeType)[prototype.ID] = prototype;
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
                _testPrototypes.Clear();
                _results.Clear();
                _inheritanceTrees.Clear();
            }

            public void ReloadPrototypes(ResourcePath file)
            {
            }

            public void Resync()
            {
                Resync(_inheritanceTrees, _priorities, _results, _testPrototypes);
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

                foreach (var childId in trees[type].Children(id))
                {
                    PushInheritance(type, childId, newResult, changed, results, trees, prototypes);
                }

                if (newResult.RawValue is not IInheritingPrototype inheritingPrototype)
                {
                    Logger.ErrorS("Serv3", $"PushInheritance was called on non-inheriting prototype! ({type}, {id})");
                    return;
                }

                if (!inheritingPrototype.Abstract)
                    newResult.CallAfterDeserializationHook();

                var obj =
                    prototypes.TryGetValue(type, out var typePrototypes) &&
                    typePrototypes.TryGetValue(id, out var prototype)
                        ? prototype
                        : Index(type, id);

                var populatedRes =
                    _serializationManager.PopulateDataDefinition(obj, (IDeserializedDefinition) newResult);

                prototypes.GetOrNew(type)[id] = (IPrototype) populatedRes.RawValue!;
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
                    if (typeof(IInheritingPrototype).IsAssignableFrom(type))
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
}
