using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using NUnit.Framework;
using Robust.Shared.ContentPack;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;
using Dependency = Robust.Shared.IoC.DependencyAttribute;

[SetUpFixture]
public class IntegrationPrototypeManager : PrototypeManager
{
    private static readonly ResourcePath DefaultDirectory = new("/Prototypes")
        ;
    private static ImmutableDictionary<Type, ImmutableDictionary<string, IPrototype>> _defaultPrototypes = ImmutableDictionary<Type, ImmutableDictionary<string, IPrototype>>.Empty;

    private static ImmutableHashSet<string> _defaultFiles = ImmutableHashSet<string>.Empty;

    private static ImmutableDictionary<string, ImmutableHashSet<IPrototype>> _defaultFilePrototypes = ImmutableDictionary<string, ImmutableHashSet<IPrototype>>.Empty;

    private static ImmutableHashSet<(YamlStream data, string file)> _defaultData = ImmutableHashSet<(YamlStream data, string file)>.Empty;

    [Dependency] private readonly IResourceManager _resourceManager = default!;
    [Dependency] private readonly ISerializationManager _serializationManager = default!;
    [Dependency] private readonly INetManager _netManager = default!;

    public void Setup(IResourceManager resourceManager, ISerializationManager serializationManager)
    {
        var changedPrototypes = new Dictionary<Type, Dictionary<string, IPrototype>>();
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
                    if (!prototypeTypes.ContainsKey(type))
                    {
                        if (IgnoredPrototypeTypes.Contains(type))
                        {
                            continue;
                        }

                        throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
                    }

                    var prototypeType = prototypeTypes[type];
                    var res = serializationManager.Read(prototypeType, node.ToDataNode(), skipHook: true);
                    var prototype = (IPrototype) res.RawValue!;

                    if (prototypes[prototypeType].ContainsKey(prototype.ID))
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
                    changedPrototypes.GetOrNew(prototypeType).Add(prototype.ID, prototype);
                    filePrototypes.Add(prototype);
                }
            }

            allFilePrototypes.Add(file.ToString(), filePrototypes);
        }

        _defaultPrototypes = changedPrototypes.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableDictionary());
        _defaultFiles = files.ToImmutableHashSet();
        _defaultFilePrototypes = allFilePrototypes.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableHashSet());
        _defaultData = data.ToImmutableHashSet();
    }

    [OneTimeTearDown]
    public void Teardown()
    {
        _defaultPrototypes = null!;
    }

    public override void Initialize()
    {
        if (_netManager.IsServer)
        {
            RegisterIgnore("shader");
        }
    }

    public override IEnumerable<T> EnumeratePrototypes<T>() where T : class
    {
        foreach (var prototype in _defaultPrototypes[typeof(T)].Values)
        {
            yield return (T) prototype;
        }

        foreach (var prototype in prototypes[typeof(T)].Values)
        {
            yield return (T) prototype;
        }
    }

    public override IEnumerable<IPrototype> EnumeratePrototypes(Type type)
    {
        return _defaultPrototypes[type].Values.Concat(prototypes[type].Values);
    }

    public override T Index<T>(string id) where T : class
    {
        if (_defaultPrototypes[typeof(T)].TryGetValue(id, out var prototype))
        {
            return (T) prototype;
        }

        return (T) prototypes[typeof(T)][id];
    }

    public override IPrototype Index(Type type, string id)
    {
        if (_defaultPrototypes[type].TryGetValue(id, out var prototype))
        {
            return prototype;
        }

        return prototypes[type][id];
    }

    public override bool HasIndex<T>(string id)
    {
        return _defaultPrototypes[typeof(T)].ContainsKey(id) ||
               prototypes[typeof(T)].ContainsKey(id);
    }

    public override bool TryIndex<T>(string id, out T? prototype) where T : default
    {
        if (_defaultPrototypes[typeof(T)].TryGetValue(id, out var prototypeUnCast) ||
            prototypes[typeof(T)].TryGetValue(id, out prototypeUnCast))
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

    protected override HashSet<IPrototype> LoadFile(ResourcePath file, bool overwrite = false)
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

        return base.LoadFile(file, overwrite);
    }

    public override List<IPrototype> LoadDirectory(ResourcePath path)
    {
        HasEverBeenReloaded = true;

        // TODO cache this
        if (path == DefaultDirectory)
        {
            if (_defaultPrototypes.IsEmpty)
            {
                InitDefault();
            }

            foreach (var (data, file) in _defaultData)
            {
                DataLoaded(data, file);
            }

            return _defaultPrototypes.Values.SelectMany(e => e.Values).ToList();
        }

        return base.LoadDirectory(path);
    }
}
