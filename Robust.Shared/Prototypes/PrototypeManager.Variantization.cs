using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Robust.Shared.Prototypes;

public partial class PrototypeManager
{
    /// <summary>
    /// Recursively searches all child nodes in the tree for any nodes with the CreateVariants tag.
    /// </summary>
    /// <param name="dataNode">The current data node being searched.</param>
    /// <param name="variantIndex">The current variantization index.</param>
    private void RecursivelySearchForVariantNodes(DataNode dataNode, int variantIndex)
    {
        switch (dataNode)
        {
            case MappingDataNode mappingNode:
                foreach (var (childName, childNode) in mappingNode.Children.ToDictionary())
                {
                    if (childNode is MappingDataNode variantNode
                        && variantNode.Tag?.Equals(CreateVariantsTag) == true)
                    {
                        ReplaceVariantNode(mappingNode, childName, variantNode, variantIndex);
                        continue;
                    }

                    RecursivelySearchForVariantNodes(childNode, variantIndex);
                }
                break;
            case SequenceDataNode sequenceNode:
                foreach (var childNode in sequenceNode.Sequence)
                {
                    RecursivelySearchForVariantNodes(childNode, variantIndex);
                }
                break;
        }
    }

    /// <summary>
    /// Extracts an array of data from a node with the CreateVariants tag, selecting the element at the current variant index.
    /// A new DataNode is created to contain this data, which then replaces the original node on its parent.
    /// </summary>
    /// <param name="parentNode">The parent of the node being replaced.</param>
    /// <param name="variantNodeName">The name of the node being replaced.</param>
    /// <param name="variantNode">The node being replaced.</param>
    /// <param name="variantIndex">The current variant index.</param>
    private void ReplaceVariantNode(MappingDataNode parentNode, string variantNodeName, MappingDataNode variantNode, int variantIndex)
    {
        DataNode? newNode = null;

        if (variantNode.TryGet(VariantSequencesFieldAttribute.Name, out var sequenceNode))
        {
            var data = _serializationManager.Read<string[][]>(sequenceNode, notNullableOverride: true);

            if (variantIndex < data.Length)
                newNode = new SequenceDataNode(data[variantIndex]);
        }
        else if (variantNode.TryGet(VariantValuesFieldAttribute.Name, out var valueNode))
        {
            var data = _serializationManager.Read<string[]>(valueNode, notNullableOverride: true);

            if (variantIndex < data.Length)
                newNode = new ValueDataNode(data[variantIndex]);
        }

        if (newNode == null)
        {
            throw new PrototypeLoadException($"DataNode {variantNodeName} does not contain variantization data for index {variantIndex.ToString()}. " +
                $"Check that all '{CreateVariantsTag}' nodes in the prototype are formatted correctly and their fields all have the same array length.");
        }

        parentNode.Remove(variantNodeName);
        parentNode.Add(variantNodeName, newNode);
    }

    /// <summary>
    /// Registers a collection of prototype variants for later reference.
    /// </summary>
    /// <param name="kind">The prototype kind.</param>
    /// <param name="collectionVariants">A list of prototype variants derived from the same source prototype.</param>
    private void RegisterVariantCollection(KindData kindData, List<string> collectionVariants)
    {
        if (kindData.UnfrozenVariants == null)
        {
            kindData.UnfrozenVariants = kindData.Variants.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.ToList());
        }

        kindData.UnfrozenVariants.EnsureCapacity(kindData.UnfrozenVariants.Count + collectionVariants.Count);

        foreach (var collectionMember in collectionVariants)
        {
            kindData.UnfrozenVariants[collectionMember] = collectionVariants;
        }
    }

    /// <summary>
    /// Tries to get the list of all variants associated with a given prototype. 
    /// </summary>
    /// <param name="collectionMember">The prototype being indexed.</param>
    /// <param name="collectionVariants">The collection of variants this prototype belongs to.</param>
    /// <returns>Returns true if the prototype is part of a variant collection, false otherwise.</returns>
    public bool TryGetVariantCollection<T>(ProtoId<T> collectionMember, [NotNullWhen(true)] out List<ProtoId<T>>? collectionVariants) where T : class, IPrototype
    {
        collectionVariants = null;

        if (!_kinds.TryGetValue(typeof(T), out var collectionKind))
            return false;

        if (!collectionKind.Variants.TryGetValue(collectionMember, out var collectionVariantNames))
            return false;

        collectionVariants = new();

        foreach (var variantName in collectionVariantNames)
        {
            if (!Resolve<T>(variantName, out var varitantPrototype))
                continue;

            collectionVariants.Add(varitantPrototype);
        }

        // Ensure the collection contains the original prototype being indexed
        return collectionVariants.Contains(collectionMember);
    }
}
