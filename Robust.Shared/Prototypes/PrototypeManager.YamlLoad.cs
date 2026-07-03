using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

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

    public event Action<DataNodeDocument>? LoadedData;

    /// <summary>
    /// DataNodes with this tag will be replaced with a new node using data supplied by <see cref="CreateVariants"/>.
    /// </summary>
    private const string CreateVariantsTag = "!type:CreateVariants";

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
                                            foreach (var (variantId, variantMapping) in data.VariantData)
                                            {
                                                if (variantMapping is null)
                                                    continue;

                                                if (ignored)
                                                    AbstractPrototype(variantMapping);

                                                extractedList.Add(new ExtractedMappingData(data.Kind, variantId, data.Parents, variantMapping));
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

                        MergeMapping(extracted, overwrite, changed);

                        // If the prototype has variants, we need to add each of these to the extracted list as well
                        if (extracted.VariantData is not null)
                        {
                            foreach (var (variantId, variantMapping) in extracted.VariantData)
                            {
                                if (variantMapping is null)
                                    continue;

                                if (ignored)
                                    AbstractPrototype(variantMapping);

                                MergeMapping(new ExtractedMappingData(extracted.Kind, variantId, extracted.Parents, variantMapping), overwrite, changed);
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
        var type = dataNode.Get<ValueDataNode>("type").Value;
        if (_ignoredPrototypeTypes.Contains(type))
            return null;

        if (!_kindNames.TryGetValue(type, out var kind))
        {
            throw new PrototypeLoadException($"Unknown prototype type: '{type}'");
        }

        var kindData = _kinds[kind];
        Dictionary<string, MappingDataNode>? variantData = null;

        if (!dataNode.TryGet<ValueDataNode>(IdDataFieldAttribute.Name, out var idNode))
        {
            // If the ID node is a CreateVariants node, we will need to clone the mapping data
            // and replace all CreateVariants nodes with the appropriate variant data for each one.
            if (dataNode.TryGet<MappingDataNode>(IdDataFieldAttribute.Name, out var mappingNode) &&
                mappingNode.Tag?.Equals(CreateVariantsTag) == true)
            {
                variantData = new Dictionary<string, MappingDataNode>();
                var variantCollection = new List<string>();

                // Extract the variant IDs as a sequence of strings.
                // The number of extracted strings (minus one) is the number of clones to generate.
                if (mappingNode.TryGet<SequenceDataNode>(VariantValuesFieldAttribute.Name, out var sequenceNode))
                {
                    // Recursively search through the original and cloned nodes for any CreateVariants nodes.
                    // Replace these nodes with data appropriate for the current variant index.
                    RecursivelySearchForVariantNodes(dataNode, 0);

                    // Check that the ID node was replaced with a ValueDataNode after variantization.
                    if (!dataNode.TryGet(IdDataFieldAttribute.Name, out idNode))
                    {
                        throw new PrototypeLoadException($"Prototype type {type} is missing an 'id' datafield.");
                    }

                    variantCollection.Add(idNode.Value);

                    for (int i = 1; i < sequenceNode.Count(); i++)
                    {
                        var clonedNode = dataNode.Copy();
                        RecursivelySearchForVariantNodes(clonedNode, i);

                        if (!clonedNode.TryGet<ValueDataNode>(IdDataFieldAttribute.Name, out var clonedIdNode))
                        {
                            throw new PrototypeLoadException($"A prototype variant cloned from {type} is missing an 'id' datafield.");
                        }

                        variantData.Add(clonedIdNode.Value, clonedNode);
                        variantCollection.Add(idNode.Value);
                    }

                    // Register all variants of the source prototype as a collection for later reference.
                    RegisterVariantCollection(variantCollection);
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

        var id = idNode.Value;
        string[]? parents = null;

        if (kindData.Inheritance != null)
        {
            if (dataNode.TryGet(ParentDataFieldAttribute.Name, out var parentNode))
            {
                parents = _serializationManager.Read<string[]>(parentNode, notNullableOverride: true);
            }
        }

        return new ExtractedMappingData(kind, id, parents, dataNode, variantData);
    }

    private void MergeMapping(
        ExtractedMappingData mapping,
        bool overwrite,
        Dictionary<Type, HashSet<string>>? changed)
    {
        var (kind, id, parents, data, _) = mapping;

        var kindData = _kinds[kind];

        if (!overwrite && kindData.RawResults.ContainsKey(id))
            throw new PrototypeLoadException($"Duplicate ID: '{id}' for kind '{kind}");

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

                    MergeMapping(extracted, overwrite, changed);

                    // If the prototype has variants, we need to add each of these to the extracted list as well
                    if (extracted.VariantData is not null)
                    {
                        foreach (var (variantId, variantMapping) in extracted.VariantData)
                        {
                            if (variantMapping is null)
                                continue;

                            MergeMapping(new ExtractedMappingData(extracted.Kind, variantId, extracted.Parents, variantMapping), overwrite, changed);
                        }
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

    // All these fields can be null in case the
    private sealed record ExtractedMappingData(
        Type Kind,
        string Id,
        string[]? Parents,
        MappingDataNode Data,
        Dictionary<string, MappingDataNode>? VariantData = null);
}
