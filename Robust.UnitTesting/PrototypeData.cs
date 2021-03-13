using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using Robust.Shared.ContentPack;
using Robust.Shared.Prototypes;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.UnitTesting
{
    public class PrototypeData
    {
        private readonly Dictionary<Type, Dictionary<string, IPrototype>> _defaultPrototypes;

        public ImmutableDictionary<Type, ImmutableDictionary<string, IPrototype>> DefaultPrototypes = ImmutableDictionary<Type, ImmutableDictionary<string, IPrototype>>.Empty;

        private ImmutableHashSet<string> _defaultFiles;

        public readonly ImmutableDictionary<string, ImmutableHashSet<IPrototype>> DefaultFilePrototypes;

        public readonly ImmutableHashSet<(YamlStream data, string file)> DefaultData;

        private readonly Dictionary<Type, PrototypeInheritanceTree> _defaultInheritanceTrees;

        private ImmutableHashSet<Type> _defaultTypes = ImmutableHashSet<Type>.Empty;

        private readonly Dictionary<Type, int> _defaultPriorities;

        private readonly Dictionary<Type, Dictionary<string, DeserializationResult>> _defaultResults;

        private ImmutableHashSet<string> _defaultIgnored = ImmutableHashSet<string>.Empty;

        public PrototypeData(
            IResourceManager resourceManager,
            ISerializationManager serializationManager,
            IReflectionManager reflectionManager,
            IIntegrationPrototypeManager prototypeManager)
        {
            var defaultPriorities = new Dictionary<Type, int>();
            var defaultTypes = new Dictionary<string, Type>();
            var defaultPrototypes = new Dictionary<Type, Dictionary<string, IPrototype>>();
            var defaultResults = new Dictionary<Type, Dictionary<string, DeserializationResult>>();
            var defaultInheritanceTrees = new Dictionary<Type, PrototypeInheritanceTree>();

            foreach (var type in reflectionManager.GetAllChildren<IPrototype>())
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

                        var prototypeType = defaultTypes[type];
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


            _defaultPrototypes = defaultPrototypes;
            _defaultFiles = files.ToImmutableHashSet();
            DefaultFilePrototypes = allFilePrototypes.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableHashSet());
            DefaultData = data.ToImmutableHashSet();
            _defaultInheritanceTrees = defaultInheritanceTrees;
            _defaultPriorities = defaultPriorities;
            _defaultResults = defaultResults;
        }

        public void Resync(IIntegrationPrototypeManager prototypeManager)
        {
            prototypeManager.Resync(_defaultInheritanceTrees, _defaultPriorities, _defaultResults, _defaultPrototypes);

            DefaultPrototypes = _defaultPrototypes.ToImmutableDictionary(k => k.Key, v => v.Value.ToImmutableDictionary());
        }
    }
}
