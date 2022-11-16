using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Robust.Shared.Log;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Prototypes;

public partial class PrototypeManager
{
    public event Action<YamlStream, string>? LoadedData;

    /// <inheritdoc />
    public void LoadDirectory(ResourcePath path, bool overwrite = false,
        Dictionary<Type, HashSet<string>>? changed = null)
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
                var rootNode = (YamlSequenceNode)yamlStream.Documents[i].RootNode;
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
                        LoadFromMapping((MappingDataNode)mapping, overwrite, changed);
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
        if (!_prototypeTypes.TryGetValue(type, out var prototypeType))
        {
            if (_ignoredPrototypeTypes.Contains(type))
                return;

            throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
        }

        if (!datanode.TryGet<ValueDataNode>(IdDataFieldAttribute.Name, out var idNode))
            throw new PrototypeLoadException($"Prototype type {type} is missing an 'id' datafield.");

        if (!overwrite && _prototypeResults[prototypeType].ContainsKey(idNode.Value))
            throw new PrototypeLoadException($"Duplicate ID: '{idNode.Value}'");

        _prototypeResults[prototypeType][idNode.Value] = datanode;
        if (prototypeType.IsAssignableTo(typeof(IInheritingPrototype)))
        {
            if (datanode.TryGet(ParentDataFieldAttribute.Name, out var parentNode))
            {
                var parents = _serializationManager.Read<string[]>(parentNode);
                _inheritanceTrees[prototypeType].Add(idNode.Value, parents);
            }
            else
            {
                _inheritanceTrees[prototypeType].Add(idNode.Value);
            }
        }

        if (changed == null)
            return;

        if (!changed.TryGetValue(prototypeType, out var set))
            changed[prototypeType] = set = new HashSet<string>();

        set.Add(idNode.Value);
    }

    public void LoadFromStream(TextReader stream, bool overwrite = false,
        Dictionary<Type, HashSet<string>>? changed = null)
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
            var root = (YamlSequenceNode)document.RootNode;
            foreach (var node in root.Cast<YamlMappingNode>())
            {
                var typeString = node.GetNode("type").AsString();
                if (!_prototypeTypes.TryGetValue(typeString, out var type))
                {
                    continue;
                }

                var id = node.GetNode("id").AsString();

                if (_inheritanceTrees.TryGetValue(type, out var tree))
                {
                    tree.Remove(id, true);
                }

                if (_prototypes.TryGetValue(type, out var prototypeIds))
                {
                    prototypeIds.Remove(id);
                    _prototypeResults[type].Remove(id);
                }
            }
        }
    }

    private void LoadFromDocument(YamlDocument document, bool overwrite = false,
        Dictionary<Type, HashSet<string>>? changed = null)
    {
        var rootNode = (YamlSequenceNode)document.RootNode;

        foreach (var node in rootNode.Cast<YamlMappingNode>())
        {
            var datanode = node.ToDataNodeCast<MappingDataNode>();
            LoadFromMapping(datanode, overwrite, changed);
        }
    }
}
