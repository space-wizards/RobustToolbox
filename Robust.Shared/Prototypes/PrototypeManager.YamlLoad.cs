using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using JetBrains.Annotations;
using Robust.Shared.Log;
using Robust.Shared.Random;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations;
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
    /// Which directories to force all prototypes recursively within to be partial.
    /// </summary>
    private readonly List<(ResPath File, int Index)> _partialFiles = new();

    /// <summary>
    /// Which directories to force all prototypes recursively within to be partial.
    /// </summary>
    private readonly List<(ResPath Path, int Index)> _partialDirectories = new();

    public event Action<DataNodeDocument>? LoadedData;

    /// <summary>
    /// Custom context use when reading YML into prototypes.
    /// It ensures that the same mapping always returns the same component instance.
    /// </summary>
    private PrototypeLoadContext _prototypeLoadContext = default!;

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

    /// <summary>
    /// Partial prototypes that have this tag in their type node will not
    /// create a new prototype if a non-partial one is not found.
    /// </summary>
    internal const string PartialOnlyTag = "!PartialOnly";

    /// <summary>
    /// Tag used to position a partial addition to a sequence at a specific index.
    /// </summary>
    private const string PartialIndexTag = "!Index:";

    /// <summary>
    /// Tag used to remove a node.
    /// </summary>
    internal const string PartialRemoveTag = "!Remove";

    /// <summary>
    /// Tag used to clear a node.
    /// </summary>
    private const string PartialClearTag = "!Clear";

    /// <summary>
    /// Tag used to combine a node at an index on a sequence, instead of adding to it.
    /// </summary>
    private const string PartialCombineIndexTag = "!CombineIndex:";

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

        var partialOnly = IsTag(typeNode, PartialOnlyTag);
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

        parents = NodeToParentArray(parentNode);

        return true;
    }

    private void MergeMapping(
        ExtractedMappingData mapping,
        bool overwrite,
        Dictionary<Type, HashSet<string>>? changed,
        bool partial)
    {
        var (kind, id, _, _, _, _) = mapping;

        var kindData = _kinds[kind];

        if (kindData.RawResults.TryGetValue(id, out var existing) &&
            !overwrite &&
            !partial)
        {
            throw new PrototypeLoadException($"Duplicate ID: '{id}' for kind '{kind}'");
        }

        MergeMappingExisting(mapping, changed, partial, kindData, existing);
    }

    private MappingDataNode MergeMappingExisting(
        ExtractedMappingData mapping,
        Dictionary<Type, HashSet<string>>? changed,
        bool partial,
        KindData kindData,
        MappingDataNode? existing)
    {
        var (kind, id, parents, data, partialOnly, _) = mapping;
        if (existing != null && partial)
        {
            if (kindData.PartialOriginals.TryGetValue(id, out var original))
            {
                if (changed == null ||
                    !changed.TryGetValue(kind, out var kindChanged) ||
                    !kindChanged.Contains(id))
                {
                    existing = original;
                }
            }
            else
            {
                kindData.PartialOriginals[id] = existing;
            }

            LogVerbose($"Combining {kind.Name} {id} with partial");

            // Also known as, components pretend to be a list, while being a dictionary
            // Chaos ensues
            // You could expose this someway if Content decided to also do this
            // for whatever reason
            Func<string, string?>? onProcessAsMappingKey = kind == typeof(EntityPrototype)
                ? key => key == "components" ? "type" : null
                : null;

            CombineMapNode(existing, data, out var fullDeleted, onProcessAsMappingKey);
            if (fullDeleted && existing.IsEmpty)
            {
                LogVerbose($"Full deleted and empty, removing prototype data");
                kindData.RawResults.Remove(id);
            }
            else
            {
                LogVerbose($"Replacing node with combined partial");
                kindData.RawResults[id] = existing;
            }
        }
        else if (partialOnly)
        {
            return existing ?? data;
        }
        else
        {
            kindData.RawResults[id] = data;
        }

        if (kindData.Inheritance is { } inheritance)
        {
            if (parents != null)
                inheritance.Add(id, parents);
            else if (!partial || existing == null) // If this is partial we don't want to mess up the inheritance graph
                inheritance.Add(id);
        }

        if (changed == null)
            return existing ?? data;

        var set = changed.GetOrNew(kind);
        set.Add(id);
        return existing ?? data;
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
    public void PartialFile(ResPath file, int index)
    {
        _partialFiles.Add((file, index));
    }

    /// <inheritdoc/>
    public void PartialDirectory(ResPath path, int index)
    {
        _partialDirectories.Add((path, index));
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
            foreach (var partialDirectory in _partialDirectories)
            {
                if (!file.TryRelativeTo(partialDirectory.Path, out _))
                    continue;

                index = partialDirectory.Index;
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

    /// <summary>
    /// Turns a node used to note the parents of a prototype into a string array
    /// of those parent ids.
    /// </summary>
    /// <param name="node">The node to read.</param>
    /// <returns>A string array of the parent id or parent ids.</returns>
    /// <exception cref="ArgumentException">
    /// raised if the given <see cref="node"/> is not of type
    /// <see cref="SequenceDataNode"/> or <see cref="ValueDataNode"/>
    /// </exception>
    private string[] NodeToParentArray(DataNode node)
    {
        switch (node)
        {
            case SequenceDataNode sequence:
            {
                var parents = new string[sequence.Count];
                for (var i = 0; i < sequence.Count; i++)
                {
                    parents[i] = ((ValueDataNode) sequence[i]).Value;
                }

                return parents;
            }
            case ValueDataNode value:
                return [value.Value];
        }

        throw new ArgumentException(
            $"Node of type {node.GetType()} cannot be used as a single parent or list of parents! Expected {typeof(SequenceDataNode)} or {typeof(ValueDataNode)}. Node string:\n{node}");
    }

    private void CombineMapNode(
        MappingDataNode existing,
        MappingDataNode data,
        out bool fullDeleted,
        Func<string, string?>? onProcessAsMappingKey = null)
    {
        fullDeleted = false;
        foreach (var (key, dataNode) in data)
        {
            LogVerbose($"Mapping processing key {key}");
            if (IsTag(data.GetKeyTag(key), PartialClearTag) ||
                IsTag(dataNode, PartialClearTag))
            {
                LogVerbose($"Mapping found {PartialClearTag}");

                if (existing.TryGet(key, out MappingDataNode? existingMapping))
                {
                    LogVerbose($"Mapping clearing mapping with key {key}");
                    existingMapping.Clear();
                    existingMapping.Tag = PartialModifiedTag;
                }
                else if (existing.TryGet(key, out SequenceDataNode? existingSequence))
                {
                    LogVerbose($"Mapping clearing sequence with key {key}");
                    existingSequence.Clear();
                    existingSequence.Tag = PartialModifiedTag;
                }

                // We might want to add after clearing
            }

            if (IsTag(data.GetKeyTag(key), PartialRemoveTag) ||
                IsTag(dataNode, PartialRemoveTag))
            {
                LogVerbose($"Found {PartialRemoveTag}");

                // "a": !Remove -> Remove regardless of value
                // "a": !Remove 1 -> Remove only if value is 1
                if (dataNode.IsEmpty ||
                    (existing.TryGetValue(key, out var existingValue) &&
                     existingValue.Equals(dataNode)))
                {
                    LogVerbose($"Mapping removing key {key}");
                    existing.Remove(key);
                    existing.Tag = PartialModifiedTag;

                    if (existing.IsEmpty)
                    {
                        LogVerbose($"Mapping node is empty after removal, full deleting");
                        fullDeleted = true;
                    }
                }

                continue;
            }

            if (existing.TryGetValue(key, out var existingNode))
            {
                var processAsMappingKey = onProcessAsMappingKey?.Invoke(key);
                if (Combine(existingNode, dataNode, out _, processAsMappingKey))
                    continue;
            }

            if (!dataNode.IsEmpty)
            {
                LogVerbose(
                    $"Mapping Node{GetValueNodeValueToLog(dataNode)} with key {key} is not empty, adding it to the mapping");
                existing[key] = dataNode;
            }
        }
    }

    private void CombineSeqNode(SequenceDataNode existing, SequenceDataNode data, string? processAsMappingKey)
    {
        for (var i = data.Count - 1; i >= 0; i--)
        {
            LogVerbose($"Sequence processing index {i}");
            var dataNode = data[i];

            // - !CombineIndex:0 "a": 1
            if (StartsWithTagOrMappingKeyTag(dataNode, PartialCombineIndexTag, out var actualTag))
            {
                SequenceCombineIndex(existing, dataNode, actualTag, i);
                continue;
            }

            if (IsTag(dataNode, PartialRemoveTag))
            {
                LogVerbose($"Sequence index {i} found {PartialRemoveTag}, removing node{GetValueNodeValueToLog(dataNode)} from sequence");
                if (!existing.Remove(dataNode))
                    LogVerbose($"Could not find node{GetValueNodeValueToLog(dataNode)} by equality to remove from sequence {i}");

                continue;
            }

            if (dataNode.Tag?.StartsWith(PartialIndexTag) ?? false)
            {
                LogVerbose($"Sequence index {i} found {PartialIndexTag}");
                var indexStr = dataNode.Tag.AsSpan(PartialIndexTag.Length);
                var index = ProcessPartialSeqNodeIndex(existing, indexStr, dataNode);
                LogVerbose($"Sequence index {i} inserting node{GetValueNodeValueToLog(dataNode)} at index {index}");
                existing.Insert(index, dataNode);
                continue;
            }

            if (processAsMappingKey != null)
            {
                LogVerbose($"Sequence index {i} being processed as a mapping using key {processAsMappingKey}");
                if (dataNode is not MappingDataNode dataNodeMap)
                {
                    LogVerbose($"Sequence index {i} is not a {nameof(MappingDataNode)}, cannot process as a mapping, skipping");
                    continue;
                }

                if (dataNodeMap.Count == 0)
                {
                    LogVerbose($"Sequence index {i} mapping has no nodes, cannot process as a mapping, skipping");
                    continue;
                }

                if (!dataNodeMap.TryGet(processAsMappingKey, out ValueDataNode? key))
                {
                    LogVerbose($"Sequence index {i} mapping has no {nameof(ValueDataNode)} key {key}, cannot process as a mapping, skipping");
                    continue;
                }

                var found = false;
                for (var j = 0; j < existing.Count; j++)
                {
                    if (!existing.TryGetValue(i, out var existingNode) ||
                        existingNode is not MappingDataNode existingMapping)
                    {
                        continue;
                    }

                    if (!existingMapping.TryGet(processAsMappingKey, out ValueDataNode? existingKey) ||
                        existingKey.Value != key.Value)
                    {
                        continue;
                    }

                    LogVerbose($"Sequence index {i} found mapping with key {key}, combining mapping nodes");
                    CombineMapNode(existingMapping, dataNodeMap, out var fullDeleted);
                    if (fullDeleted && existingMapping.IsEmpty)
                    {
                        LogVerbose($"Sequence index {i} found mapping with key {key} that full deleted, removing from sequence");
                        existing.RemoveAt(j);
                    }

                    found = true;
                    break;
                }

                if (found)
                    continue;

                LogVerbose($"Sequence index {i} could not find mapping with key {key} in original node, adding the full partial node to the original sequence");
            }

            // If this is a mapping with a single !Remove key, we don't want to add it if it doesn't already exist
            if (dataNode is not MappingDataNode mapping ||
                mapping.Count == 0 ||
                mapping.Count > 1 ||
                mapping.GetKeyTag(mapping.Keys.First()) != PartialRemoveTag)
            {
                LogVerbose($"Sequence index {i} adding its data node{GetValueNodeValueToLog(dataNode)} to existing sequence");
                existing.Add(dataNode);
            }
        }
    }

    private void SequenceCombineIndex(SequenceDataNode existing, DataNode dataNode, ReadOnlySpan<char> tag, int i)
    {
        var indexStr = tag[PartialCombineIndexTag.Length..];
        var index = ProcessPartialSeqNodeIndex(existing, indexStr, dataNode);
        LogVerbose($"Sequence index {i} found {PartialCombineIndexTag}");

        if (existing.TryGetValue(index, out var existingNode) &&
            Combine(existingNode, dataNode, out var fullDeleted, null))
        {
            LogVerbose($"Sequence index {i} combined index {index}");
            if (fullDeleted && existingNode.IsEmpty)
            {
                LogVerbose($"Sequence index {i} full deleted, removing it from the sequence");
                existing.RemoveAt(i);
            }
        }
        else
        {
            LogVerbose($"Sequence index {i} could not find index {index} to combine");
        }
    }

    private int ProcessPartialSeqNodeIndex(SequenceDataNode existing, ReadOnlySpan<char> indexStr, DataNode dataNode)
    {
        var fromEnd = false;
        if (indexStr.StartsWith('-'))
        {
            LogVerbose($"Index {indexStr} is negative, searching from the end");
            indexStr = indexStr[1..];
            fromEnd = true;
        }

        if (!int.TryParse(indexStr, out var index))
        {
            throw new PrototypeLoadException(
                $"Found partial prototype node with index tag, but could not parse its index as a number. Expected tag in format !Index:0 or !ReplaceIndex:0, got tag {dataNode.Tag} for data node {dataNode}");
        }

        if (fromEnd)
            index = existing.Count - index;

        if (index < 0 || index > existing.Count)
            LogVerbose($"Index {index} is outside of bounds, clamping to minimum 0 and maximum {existing.Count}");

        index = Math.Clamp(index, 0, existing.Count);
        return index;
    }

    private bool Combine(DataNode existing, DataNode data, out bool fullDeleted, string? processAsMappingKey)
    {
        fullDeleted = false;
        switch (existing, data)
        {
            case (MappingDataNode existingMapping, MappingDataNode dataMapping):
                CombineMapNode(existingMapping, dataMapping, out fullDeleted);
                return true;
            case (SequenceDataNode existingSequence, SequenceDataNode dataSequence):
                CombineSeqNode(existingSequence, dataSequence, processAsMappingKey);
                return true;
            default:
                return false;
        }
    }

    private bool IsTag(string? nodeTag, string tag)
    {
        return nodeTag != null &&
               nodeTag.Equals(tag, StringComparison.OrdinalIgnoreCase);
    }

    private bool IsTag(DataNode node, string tag)
    {
        return IsTag(node.Tag, tag);
    }

    private bool StartsWithTagOrMappingKeyTag(DataNode dataNode, string tag, [NotNullWhen(true)] out string? actualTag)
    {
        if (IsTag(dataNode, tag))
        {
            DebugTools.AssertNotNull(dataNode.Tag);
            actualTag = dataNode.Tag;
            return true;
        }

        if (dataNode is MappingDataNode dataNodeMapping &&
            dataNodeMapping.Count > 0 &&
            dataNodeMapping.GetKeyTag(dataNodeMapping[0].Key) is { } keyTag &&
            keyTag.StartsWith(tag))
        {
            actualTag = keyTag;
            return true;
        }

        actualTag = null;
        return false;
    }

    private void LogVerbose([InterpolatedStringHandlerArgument] ref DefaultInterpolatedStringHandler handler)
    {
        // Check if it's enabled first so we don't allocate for fun
        if (Sawmill.IsLogLevelEnabled(LogLevel.Verbose))
            Sawmill.Verbose(handler.ToStringAndClear());
    }

    private string GetValueNodeValueToLog(DataNode node)
    {
        return node is not ValueDataNode value ? string.Empty : $" {value.Value}";
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

    private sealed class PrototypeLoadContext : ISerializationContext
    {
        public SerializationManager.SerializerProvider SerializerProvider { get; }
        public bool WritingReadingPrototypes { get; }

        public PrototypeLoadContext(ISerializationManager serialization)
        {
            SerializerProvider = new SerializationManager.SerializerProvider(serialization);
            SerializerProvider.RegisterSerializer<ComponentRegistrySerializer>()?.CacheComponents = true;
        }
    }
}
