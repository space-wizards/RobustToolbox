using System;
using System.Collections.Generic;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.Shared.Serialization.Markdown;

public static class DataNodeHelpers
{
    public static IEnumerable<DataNode> GetAllNodes(DataNode node)
    {
        return node switch
        {
            MappingDataNode mapping => GetAllNodes(mapping),
            SequenceDataNode sequence => GetAllNodes(sequence),
            ValueDataNode value => GetAllNodes(value),
            _ => throw new ArgumentOutOfRangeException(nameof(node))
        };
    }

    private static IEnumerable<DataNode> GetAllNodes(MappingDataNode node)
    {
        yield return node;

        foreach (var (k, v) in node)
        {
            yield return node.GetKeyNode(k);

            foreach (var child in GetAllNodes(v))
            {
                yield return child;
            }
        }
    }

    private static IEnumerable<DataNode> GetAllNodes(SequenceDataNode node)
    {
        yield return node;

        foreach (var s in node)
        {
            foreach (var child in GetAllNodes(s))
            {
                yield return child;
            }
        }
    }

    private static IEnumerable<DataNode> GetAllNodes(ValueDataNode node)
    {
        yield return node;
    }
}
