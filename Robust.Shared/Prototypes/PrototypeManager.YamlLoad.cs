using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Robust.Shared.Prototypes;

public partial class PrototypeManager
{
    /// <summary>
    ///     Which files to force all prototypes within to be abstract.
    /// </summary>
    private readonly List<ResPath> _abstractFiles = new();

    /// <summary>
    ///     Which directories to force all prototypes recursively within to be abstract.
    /// </summary>
    private readonly List<ResPath> _abstractDirectories = new();

    /// <summary>
    ///     Which directories to force all prototypes recursively within to be partial.
    /// </summary>
    private readonly List<(ResPath File, int Index)> _partialFiles = new();

    /// <summary>
    ///     Which directories to force all prototypes recursively within to be partial.
    /// </summary>
    private readonly List<ResPath> _partialDirectories = new();

    public event Action<DataNodeDocument>? LoadedData;

    /// <inheritdoc />
    public void LoadDirectory(ResPath path, bool overwrite = false,
        Dictionary<Type, HashSet<string>>? changed = null)
    {
        _hasEverBeenReloaded = true;
        var streams = Resources.ContentFindFiles(path)
            .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith("."))
            .ToArray();

        // Shuffle to avoid input data patterns causing uneven thread workloads.
        (new System.Random()).Shuffle(streams.AsSpan());

        var sawmill = _logManager.GetSawmill("eng");

        var results = streams.AsParallel()
            .Select<ResPath, (ResPath, IEnumerable<ExtractedMappingData>)>(file =>
            {
                try
                {
                    var ignored = IsFileAbstract(file);
                    using var reader = ReadFile(file, !overwrite);

                    if (reader == null)
                        return (file, Array.Empty<ExtractedMappingData>());

                    var extractedList = new List<ExtractedMappingData>();
                    var i = 0;
                    foreach (var document in DataNodeParser.ParseYamlStream(reader, internStrings: true))
                    {
                        i += 1;
                        LoadedData?.Invoke(document);

                        switch (document.Root)
                        {
                            case SequenceDataNode seq:
                                foreach (var mapping in seq.Sequence)
                                {
                                    var data = ExtractMapping((MappingDataNode)mapping);
                                    if (data != null)
                                    {
                                        if (ignored)
                                            AbstractPrototype(data.Data);

                                        extractedList.Add(data);
                                    }
                                }

                                break;
                            case ValueDataNode { Value: "" }:
                                // Documents with absolutely nothing in them get deserialized as this.
                                // How does this happen? Text file merger generates separate documents for each file.
                                // Just skip it.
                                break;
                            default:
                                sawmill.Error($"{file} document #{i} is not a sequence! Did you forget to indent your prototype with a '-'?");
                                break;
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

        var queue = Array.Empty<Queue<(ResPath File, IEnumerable<ExtractedMappingData> Result)>>();
        foreach (var (file, result) in results)
        {
            if (IsFilePartial(file, out var index))
            {
                Array.Resize(ref queue, index + 1);
                queue[index].Enqueue((file, result));
                continue;
            }

            foreach (var mapping in result)
            {
                try
                {
                    MergeMapping(mapping, overwrite, changed, false);
                }
                catch (Exception e)
                {
                    sawmill.Error($"Exception whilst loading prototypes from {file}:\n{e}");
                }
            }
        }

        foreach (var array in queue)
        {
            foreach (var (file, result) in array)
            {
                foreach (var mapping in result)
                {
                    try
                    {
                        MergeMapping(mapping, overwrite, changed, true);
                    }
                    catch (Exception e)
                    {
                        sawmill.Error($"Exception whilst loading prototypes from {file}:\n{e}");
                    }
                }
            }
        }
    }

    private StreamReader? ReadFile(ResPath file, bool @throw = true)
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

                    Sawmill.Error($"Error reloading prototypes in file {file}:\n{e}");
                    return null;
                }

                retries++;
                Thread.Sleep(10);
            }
        }
    }

    public void LoadFile(ResPath file, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null)
    {
        try
        {
            var ignored = IsFileAbstract(file);
            var partial = IsFilePartial(file, out _);
            using var reader = ReadFile(file, !overwrite);

            if (reader == null)
                return;

            var i = 0;
            foreach (var document in DataNodeParser.ParseYamlStream(reader, internStrings: true))
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

                        if (ignored)
                            AbstractPrototype(extracted.Data);

                        MergeMapping(extracted, overwrite, changed, partial);
                    }
                }
                catch (Exception e)
                {
                    Sawmill.Error($"Exception whilst loading prototypes from {file}#{i}:\n{e}");
                }

                i += 1;
            }
        }
        catch (Exception e)
        {
            Sawmill.Error("YamlException whilst loading prototypes from {0}: {1}", file, e.Message);
        }
    }

    private ExtractedMappingData? ExtractMapping(MappingDataNode dataNode)
    {
        var typeNode = dataNode.Get<ValueDataNode>("type");
        var type = typeNode.Value;
        if (_ignoredPrototypeTypes.Contains(type))
            return null;

        if (!_kindNames.TryGetValue(type, out var kind))
        {
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

        return new ExtractedMappingData(kind, id, parents, dataNode, NodeHasTag(typeNode, "!PartialOnly"));
    }

    private void MergeMapping(
        ExtractedMappingData mapping,
        bool overwrite,
        Dictionary<Type, HashSet<string>>? changed,
        bool partial)
    {
        var (kind, id, parents, data, partialOnly) = mapping;

        var kindData = _kinds[kind];

        if (kindData.RawResults.TryGetValue(id, out var existing) &&
            !overwrite &&
            !partial)
        {
            throw new PrototypeLoadException($"Duplicate ID: '{id}' for kind '{kind}'");
        }

        if (existing != null)
        {
            CombineMapNode(existing, data);

            static void CombineMapNode(MappingDataNode existing, MappingDataNode data)
            {
                foreach (var (key, dataNode) in data)
                {
                    if (IsRemoveTag(dataNode))
                    {
                        existing.Remove(key);
                        continue;
                    }

                    if (existing.TryGetValue(key, out var existingNode) &&
                        Combine(existingNode, dataNode))
                    {
                        continue;
                    }

                    existing[key] = dataNode;
                }
            }

            static void CombineSeqNode(SequenceDataNode existing, SequenceDataNode data)
            {
                for (var i = 0; i < data.Count; i++)
                {
                    var dataNode = data[i];
                    if (existing.TryGetValue(i, out var existingNode) &&
                        Combine(existingNode, dataNode))
                    {
                        continue;
                    }

                    switch (dataNode)
                    {
                        case ValueDataNode dataValue:
                            if (IsRemoveTag(dataValue))
                            {
                                existing.Remove(dataValue);
                                continue;
                            }

                            existing.Add(dataNode);
                            break;
                    }
                }
            }

            static bool Combine(DataNode existing, DataNode data)
            {
                switch (existing, data)
                {
                    case (MappingDataNode existingMapping, MappingDataNode dataMapping):
                        CombineMapNode(existingMapping, dataMapping);
                        return true;
                    case (SequenceDataNode existingSequence, SequenceDataNode dataSequence):
                        CombineSeqNode(existingSequence, dataSequence);
                        return true;
                    default:
                        return false;
                }
            }

            static bool IsTag(DataNode node, string tag)
            {
                return node.Tag != null &&
                       node.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase);
            }

            static bool IsRemoveTag(DataNode node)
            {
                return IsTag(node, "!Remove");
            }
        }
        else if (partialOnly)
        {
            return;
        }

        kindData.RawResults[id] = data;

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
        foreach (var document in DataNodeParser.ParseYamlStream(stream, internStrings: true))
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

                    MergeMapping(extracted, overwrite, changed, false);
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

        var modified = new HashSet<KindData>();
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

                kindData.UnfrozenInstances ??= kindData.Instances.ToDictionary();
                kindData.UnfrozenInstances.Remove(id);
                kindData.Results.Remove(id);
                kindData.RawResults.Remove(id);
                modified.Add(kindData);
            }
        }

        Freeze(modified);
    }

    public void AbstractFile(ResPath path)
    {
        _abstractFiles.Add(path);
    }

    public void AbstractDirectory(ResPath path)
    {
        _abstractDirectories.Add(path);
    }

    /// <inheritdoc/>
    public void PartialFile(IEnumerable<(ResPath File, int Index)> path)
    {
        _partialFiles.AddRange(path);
    }

    /// <inheritdoc/>
    public void PartialDirectory(params ResPath[] paths)
    {
        _partialDirectories.AddRange(paths);
    }

    private bool IsFileAbstract(ResPath file)
    {
        if (_abstractFiles.Count > 0)
        {
            foreach (var abstractFile in _abstractFiles)
            {
                if (file.TryRelativeTo(abstractFile, out _))
                    return true;
            }
        }

        if (_abstractDirectories.Count > 0)
        {
            foreach (var abstractDirectory in _abstractDirectories)
            {
                if (file.TryRelativeTo(abstractDirectory, out _))
                    return true;
            }
        }

        return false;
    }

    private bool IsFilePartial(ResPath file, out int index)
    {
        if (_partialFiles.Count > 0)
        {
            foreach (var partialFile in _partialFiles)
            {
                if (!file.TryRelativeTo(partialFile.File, out _))
                    continue;

                index = partialFile.Index;
                return true;
            }
        }

        if (_partialDirectories.Count > 0)
        {
            for (index = 0; index < _partialDirectories.Count; index++)
            {
                var partialDirectory = _partialDirectories[index];
                if (!file.TryRelativeTo(partialDirectory.Directory, out _))
                    continue;

                return true;
            }
        }

        index = 0;
        return false;
    }

    private void AbstractPrototype(MappingDataNode mapping)
    {
        if (mapping.TryGet(AbstractDataFieldAttribute.Name, out var abstractNode))
        {
            if (abstractNode is not ValueDataNode abstractValueNode)
            {
                mapping["abstract"] = new ValueDataNode("true");
                return;
            }

            abstractValueNode.Value = "true";
            return;
        }

        mapping.Add("abstract", "true");
    }

    private static bool NodeHasTag(DataNode node, string tag)
    {
        return node.Tag != null &&
               node.Tag.Equals(tag, StringComparison.OrdinalIgnoreCase);
    }

    // All these fields can be null in case the
    private sealed record ExtractedMappingData(
        Type Kind,
        string Id,
        string[]? Parents,
        MappingDataNode Data,
        bool PartialOnly
    );
}
