using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading;
using JetBrains.Annotations;
using Robust.Shared.Serialization.TypeSerializers.Implementations;

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
    /// Which directories to force all prototypes recursively within to be partial.
    /// </summary>
    private readonly List<(ResPath File, int Index)> _partialFiles = new();

    /// <summary>
    /// Which directories to force all prototypes recursively within to be partial.
    /// </summary>
    private readonly List<ResPath> _partialDirectories = new();

    public event Action<DataNodeDocument>? LoadedData;

    /// <summary>
    /// DataNodes with this tag will be replaced with a new node using data supplied by <see cref="CreateVariants"/>.
    /// </summary>
    private const string CreateVariantsTag = "!type:CreateVariants";

    /// <summary>
    /// Mapping data nodes with this tag will not throw an error when the type
    /// key is missing in <see cref="ComponentRegistrySerializer"/>.
    /// This tag marks a component as having been modified by a partial.
    /// This is necessary as components pretend to be a list but are actually
    /// a dictionary.
    /// </summary>
    internal const string PartialModifiedTag = "!PartialModified";

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

                                        // If the prototype has variants, we need to add each of these to the extracted list as well
                                        if (data.VariantData != null)
                                        {
                                            foreach (var (variantId, variantExtracted) in data.VariantData)
                                            {
                                                if (variantExtracted is null)
                                                    continue;

                                                if (ignored)
                                                    AbstractPrototype(variantExtracted.Data);

                                                extractedList.Add(variantExtracted);
                                            }
                                        }
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

        var queue = Array.Empty<Queue<(ResPath File, IEnumerable<ExtractedMappingData> Result)>?>();
        foreach (var (file, result) in results)
        {
            if (IsFilePartial(file, out var index))
            {
                if (index >= queue.Length)
                    Array.Resize(ref queue, index + 1);

                ref var indexQueue = ref queue[index];
                indexQueue ??= new Queue<(ResPath File, IEnumerable<ExtractedMappingData> Result)>();
                indexQueue.Enqueue((file, result));
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
            if (array == null)
                continue;

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
                        var extracted = ExtractMapping((MappingDataNode)mapping);
                        if (extracted == null)
                            continue;

                        if (ignored)
                            AbstractPrototype(extracted.Data);

                        MergeMapping(extracted, overwrite, changed, partial);

                        // If the prototype has variants, we need to add each of these to the extracted list as well
                        if (extracted.VariantData is not null)
                        {
                            foreach (var (variantId, variantExtracted) in extracted.VariantData)
                            {
                                if (variantExtracted is null)
                                    continue;

                                if (ignored)
                                    AbstractPrototype(variantExtracted.Data);

                                MergeMapping(variantExtracted, overwrite, changed, partial);
                            }
                        }
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
        Dictionary<string, ExtractedMappingData>? variantData = null;

        var partialOnly = NodeHasTag(typeNode, "!PartialOnly");
        if (!dataNode.TryGet<ValueDataNode>(IdDataFieldAttribute.Name, out var idNode))
        {
            // Check if the ID node is a CreateVariants node instead of a value.
            if (dataNode.TryGet<MappingDataNode>(IdDataFieldAttribute.Name, out var mappingNode) &&
                mappingNode.Tag?.Equals(CreateVariantsTag) == true)
            {
                variantData = new Dictionary<string, ExtractedMappingData>();
                var variantCollection = new List<string>();

                // We need to generate a collection of prototype variants.
                // Extract the IDs of the variants to generate as a sequence of strings.
                // The number of extracted strings (minus one) is the number of clones to generate.
                if (mappingNode.TryGet<SequenceDataNode>(VariantValuesFieldAttribute.Name, out var sequenceNode))
                {
                    variantCollection.EnsureCapacity(sequenceNode.Count);

                    for (int i = 1; i < sequenceNode.Count; i++)
                    {
                        // Clone the data node, then recursively search through it for any CreateVariants nodes.
                        // Replace these nodes with data appropriate for the current variant index.
                        var clonedNode = dataNode.Copy();
                        RecursivelySearchForVariantNodes(clonedNode, i);

                        // Check that the ID node was replaced with a ValueDataNode after variantization.
                        if (!clonedNode.TryGet<ValueDataNode>(IdDataFieldAttribute.Name, out var clonedIdNode))
                        {
                            throw new PrototypeLoadException($"A prototype variant cloned from {type} is missing an 'id' datafield.");
                        }

                        // Gather the outputs.
                        TryGetParents(kindData, clonedNode, out var clonedNodeParents);
                        variantData.Add(clonedIdNode.Value, new ExtractedMappingData(kind, clonedIdNode.Value, clonedNodeParents, clonedNode, partialOnly));
                        variantCollection.Add(clonedIdNode.Value);
                    }

                    // Recursively search through and updated any CreateVariants nodes in the original data node.
                    RecursivelySearchForVariantNodes(dataNode, 0);

                    // Check that the ID node was replaced with a ValueDataNode after variantization.
                    if (!dataNode.TryGet(IdDataFieldAttribute.Name, out idNode))
                    {
                        throw new PrototypeLoadException($"Prototype type {type} is missing an 'id' datafield.");
                    }

                    // Add this ID to the top of the variant collection to maintain the correct ordering.
                    variantCollection.Insert(0, idNode.Value);

                    // Register all variants of the source prototype as a collection for later reference.
                    RegisterVariantCollection(kindData, variantCollection);
                }
                else
                {
                    throw new PrototypeLoadException($"The 'id' datafield of prototype type {type} has an invalid value assigned.");
                }
            }

            else
            {
                throw new PrototypeLoadException($"Prototype type {type} is missing an 'id' datafield.");
            }
        }

        TryGetParents(kindData, dataNode, out var parents);
        return new ExtractedMappingData(kind, idNode.Value, parents, dataNode, partialOnly, variantData);
    }

    private bool TryGetParents(KindData kindData, MappingDataNode mappingDataNode, [NotNullWhen(true)] out string[]? parents)
    {
        parents = null;

        if (kindData.Inheritance is null
            || !mappingDataNode.TryGet(ParentDataFieldAttribute.Name, out var parentNode))
            return false;

        parents = _serializationManager.Read<string[]>(parentNode, notNullableOverride: true);

        return true;
    }

    private void MergeMapping(
        ExtractedMappingData mapping,
        bool overwrite,
        Dictionary<Type, HashSet<string>>? changed,
        bool partial)
    {
        var (kind, id, parents, data, partialOnly, _) = mapping;

        var kindData = _kinds[kind];

        if (kindData.RawResults.TryGetValue(id, out var existing) &&
            !overwrite &&
            !partial)
        {
            throw new PrototypeLoadException($"Duplicate ID: '{id}' for kind '{kind}'");
        }

        if (existing != null && partial)
        {
            CombineMapNode(existing, data, static parent => parent.Clear(), existing, out var fullDeleted);
            if (fullDeleted && existing.IsEmpty)
            {
                kindData.RawResults.Remove(id);
            }
            else
            {
                kindData.RawResults[id] = existing;
            }
        }
        else if (partialOnly)
        {
            return;
        }
        else
        {
            kindData.RawResults[id] = data;
        }

        if (kindData.Inheritance is { } inheritance)
        {
            if (parents != null)
                inheritance.Add(id, parents);
            else
                inheritance.Add(id);
        }

        if (changed == null)
            return;

        var set = changed.GetOrNew(kind);
        set.Add(id);
    }

    public void LoadFromStream(
        TextReader stream,
        bool overwrite = false,
        Dictionary<Type, HashSet<string>>? changed = null,
        bool partial = false)
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

                    MergeMapping(extracted, overwrite, changed, partial);

                    // If the prototype has variants, we need to add each of these to the extracted list as well
                    if (extracted.VariantData is null)
                        continue;

                    foreach (var (_, variantExtracted) in extracted.VariantData)
                    {
                        if (variantExtracted is null)
                            continue;

                        MergeMapping(variantExtracted, overwrite, changed, partial);
                    }
                }

                i += 1;
            }
            catch (Exception e)
            {
                throw new PrototypeLoadException($"Failed to load prototypes from document#{i}", e);
            }
        }
    }

    public void LoadString(string str, bool overwrite = false, Dictionary<Type, HashSet<string>>? changed = null, bool partial = false)
    {
        LoadFromStream(new StringReader(str), overwrite, changed, partial);
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

        if (modified.Any(x => x.Type == typeof(EntityPrototype)))
            RebuildEntityComponentCache();
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
                if (!file.TryRelativeTo(partialDirectory, out _))
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

    private static void CombineMapNode<T>(
        MappingDataNode existing,
        MappingDataNode data,
        [RequireStaticDelegate] Action<T> fullDelete,
        T parent,
        out bool fullDeleted
    ) where T : DataNode
    {
        fullDeleted = false;
        foreach (var (key, dataNode) in data)
        {
            // !Remove "a": 1 -> Clear the whole dict
            if (IsRemoveTag(data.GetKeyTag(key)))
            {
                fullDelete(parent);
                fullDeleted = true;
                continue;
            }

            // "a": !Remove 1 -> Remove the element matching 1
            // "a": !Remove
            // - "b": 2 -> Remove the element matching b
            if (IsRemoveTag(dataNode))
            {
                existing.Remove(key);
                existing.Tag = "!PartialModified";
                if (dataNode is ValueDataNode)
                    continue;
            }

            if (existing.TryGetValue(key, out var existingNode) &&
                Combine(existingNode, dataNode, out _))
            {
                continue;
            }

            if (!dataNode.IsEmpty)
                existing[key] = dataNode;
        }
    }

    private static void CombineSeqNode(SequenceDataNode existing, SequenceDataNode data)
    {
        for (var i = data.Count - 1; i >= 0; i--)
        {
            var dataNode = data[i];
            if (existing.TryGetValue(i, out var existingNode) &&
                Combine(existingNode, dataNode, out var fullDeleted))
            {
                if (fullDeleted && existingNode.IsEmpty)
                    existing.RemoveAt(i);

                continue;
            }

            if (IsRemoveTag(dataNode))
            {
                existing.Remove(dataNode);
                continue;
            }

            existing.Add(dataNode);
        }
    }

    private static bool Combine(DataNode existing, DataNode data, out bool fullDeleted)
    {
        fullDeleted = false;
        switch (existing, data)
        {
            case (MappingDataNode existingMapping, MappingDataNode dataMapping):
                CombineMapNode(
                    existingMapping,
                    dataMapping,
                    static parent => parent.Clear(),
                    existingMapping,
                    out fullDeleted
                );
                return true;
            case (SequenceDataNode existingSequence, SequenceDataNode dataSequence):
                CombineSeqNode(existingSequence, dataSequence);
                return true;
            default:
                return false;
        }
    }

    private static bool IsTag(string? nodeTag, string tag)
    {
        return nodeTag != null &&
               nodeTag.Equals(tag, StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsRemoveTag(string? tag)
    {
        return IsTag(tag, "!Remove");
    }

    private static bool IsRemoveTag(DataNode node)
    {
        return IsRemoveTag(node.Tag);
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
        bool PartialOnly,
        Dictionary<string, ExtractedMappingData>? VariantData = null
    );
}
