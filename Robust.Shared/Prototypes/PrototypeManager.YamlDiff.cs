using System.Collections.Generic;
using System.IO;
using System.Linq;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;

namespace Robust.Shared.Prototypes;

public partial class PrototypeManager
{
    private static readonly List<string> FieldOrder =
        ["type", "id", "parent", "categories", "name", "suffix", "description"];

    public void SaveEntityPrototypes(ResPath searchPath)
    {
        var streams = Resources.ContentFindFiles(searchPath)
            .ToList()
            .AsParallel()
            .Where(filePath => filePath.Extension == "yml" && !filePath.Filename.StartsWith('.'));

        Dictionary<string, PrototypeValidationData> entityProtos = [];

        foreach (var (_, mapping, path) in PrototypesForValidation(streams)
                     .Where(x => x.Item1 == typeof(EntityPrototype)))
        {
            var id = mapping.Get<ValueDataNode>("id").Value;
            mapping.Remove("type");
            entityProtos[id] = new PrototypeValidationData(id, mapping, path.ToString());
        }

        Dictionary<string, DataNode> normalizedPrototypes = [];
        foreach (var (id, data) in entityProtos.OrderBy(x => x.Key))
        {
            EnsurePushed(data, entityProtos, typeof(EntityPrototype));

            if (data.Mapping.TryGet("abstract", out ValueDataNode? abstractNode)
                && bool.Parse(abstractNode.Value))
                continue;

            normalizedPrototypes[id] = NormalizeDataNode(data.Mapping);
        }

        using var writer = new StreamWriter("entity-prototypes.yml", false);
        normalizedPrototypes.Values.ToSequenceDataNode().Write(writer);
    }

    private DataNode NormalizeDataNode(DataNode node)
    {
        return node switch
        {
            MappingDataNode map => map
                .Select(x => KeyValuePair.Create(x.Key, NormalizeDataNode(x.Value)))
                // Yes this will sort non-component registry mappings.
                .OrderBy(x => FieldOrder.IndexOf(x.Key) is var index && index >= 0 ? index : 100)
                .ThenBy(x => x.Key)
                .ToMappingDataNode(),
            SequenceDataNode sequence => sequence
                .Select(NormalizeDataNode)
                // Put - type: at the top for components
                .OrderByDescending(x =>
                    x is MappingDataNode map && map.TryGet("type", out ValueDataNode? value) ? value.Value : null)
                .ToSequenceDataNode(),
            _ => node
        };
    }
}
