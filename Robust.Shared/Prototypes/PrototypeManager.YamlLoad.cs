using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Robust.Shared.Random;
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
    public event Action<DataNodeDocument>? LoadedData;

    /// <inheritdoc />
    public void LoadDirectory(ResourcePath path, bool overwrite = false,
        Dictionary<Type, HashSet<string>>? changed = null)
    {
        _hasEverBeenReloaded = true;
        var streams = Resources.ContentFindFiles(path)
            .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
            .ToArray();

        // Shuffle to avoid input data patterns causing uneven thread workloads.
        RandomExtensions.Shuffle(streams.AsSpan(), new System.Random());

        var sawmill = _logManager.GetSawmill("eng");

        var results = streams.AsParallel()
            .Select<ResourcePath, (ResourcePath, IEnumerable<ExtractedMappingData>)>(file =>
            {
                try
                {
                    using var reader = ReadFile(file, !overwrite);

                    if (reader == null)
                        return (file, Array.Empty<ExtractedMappingData>());

                    var extractedList = new List<ExtractedMappingData>();
                    foreach (var document in DataNodeParser.ParseYamlStream(reader))
                    {
                        LoadedData?.Invoke(document);

                        var seq = (SequenceDataNode)document.Root;
                        foreach (var mapping in seq.Sequence)
                        {
                            var data = ExtractMapping((MappingDataNode)mapping);
                            if (data != null)
                                extractedList.Add(data);
                        }
                    }

                    return (file, extractedList);
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception whilst loading prototypes from {file}:\n{e}");
                    return (file, Array.Empty<ExtractedMappingData>());
                }
            });

        foreach (var (file, result) in results)
        {
            foreach (var mapping in result)
            {
                try
                {
                    MergeMapping(mapping, overwrite, changed);
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception whilst loading prototypes from {file}:\n{e}");
                }
            }
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

                    _sawmill.Error($"Error reloading prototypes in file {file}:\n{e}");
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

            var i = 0;
            foreach (var document in DataNodeParser.ParseYamlStream(reader))
            {
                LoadedData?.Invoke(document);

                try
                {
                    var seq = (SequenceDataNode)document.Root;
                    foreach (var mapping in seq.Sequence)
                    {
                        var extracted = ExtractMapping((MappingDataNode) mapping);
                        if (extracted == null)
                            continue;

                        MergeMapping(extracted, overwrite, changed);
                    }
                }
                catch (Exception e)
                {
                    _sawmill.Error($"Exception whilst loading prototypes from {file}#{i}:\n{e}");
                }

                i += 1;
            }
        }
        catch (Exception e)
        {
            _sawmill.Error("YamlException whilst loading prototypes from {0}: {1}", file, e.Message);
        }
    }

    private ExtractedMappingData? ExtractMapping(MappingDataNode dataNode)
    {
        var type = dataNode.Get<ValueDataNode>("type").Value;
        if (!_kindNames.TryGetValue(type, out var kind))
        {
            if (_ignoredPrototypeTypes.Contains(type))
                return null;

            throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
        }

        var kindData = _kinds[kind];

        if (!dataNode.TryGet<ValueDataNode>(IdDataFieldAttribute.Name, out var idNode))
            throw new PrototypeLoadException($"Prototype type {type} is missing an 'id' datafield.");

        var id = idNode.Value;
        string[]? parents = null;

        if (kindData.Inheritance != null)
        {
            if (dataNode.TryGet(ParentDataFieldAttribute.Name, out var parentNode))
            {
                parents = _serializationManager.Read<string[]>(parentNode, notNullableOverride: true);
            }
        }

        return new ExtractedMappingData(kind, id, parents, dataNode);
    }

    private void MergeMapping(
        ExtractedMappingData mapping,
        bool overwrite,
        Dictionary<Type, HashSet<string>>? changed)
    {
        var (kind, id, parents, data) = mapping;

        var kindData = _kinds[kind];

        if (!overwrite && kindData.Results.ContainsKey(id))
            throw new PrototypeLoadException($"Duplicate ID: '{id}' for kind '{kind}");

        kindData.Results[id] = data;

        if (kindData.Inheritance is { } inheritance)
        {
            if (parents != null)
            {
                inheritance.Add(id, parents);
            }
            else
            {
                inheritance.Add(id);
            }
        }

        if (changed == null)
            return;

        var set = changed.GetOrNew(kind);
        set.Add(id);
    }

    public void LoadFromStream(TextReader stream, bool overwrite = false,
        Dictionary<Type, HashSet<string>>? changed = null)
    {
        _hasEverBeenReloaded = true;

        var i = 0;
        foreach (var document in DataNodeParser.ParseYamlStream(stream))
        {
            LoadedData?.Invoke(document);

            try
            {
                var rootNode = (SequenceDataNode)document.Root;
                foreach (var node in rootNode.Cast<MappingDataNode>())
                {
                    var extracted = ExtractMapping(node);
                    if (extracted == null)
                        continue;

                    MergeMapping(extracted, overwrite, changed);
                }

                i += 1;
            }
            catch (Exception e)
            {
                throw new PrototypeLoadException($"Failed to load prototypes from document#{i}", e);
            }
        }
    }

    public void LoadString(string str, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null)
    {
        LoadFromStream(new StringReader(str), overwrite, changed);
    }

    public void RemoveString(string prototypes)
    {
        var reader = new StringReader(prototypes);

        foreach (var document in DataNodeParser.ParseYamlStream(reader))
        {
            var root = (SequenceDataNode)document.Root;
            foreach (var node in root.Cast<MappingDataNode>())
            {
                var typeString = node.Get<ValueDataNode>("type").Value;
                if (!_kindNames.TryGetValue(typeString, out var kind))
                {
                    continue;
                }

                var kindData = _kinds[kind];

                var id = node.Get<ValueDataNode>("id").Value;

                if (kindData.Inheritance is { } tree)
                    tree.Remove(id, true);

                kindData.Instances.Remove(id);
                kindData.Results.Remove(id);
            }
        }
    }

    // All these fields can be null in case the
    private sealed record ExtractedMappingData(
        Type Kind,
        string Id,
        string[]? Parents,
        MappingDataNode Data);
}
