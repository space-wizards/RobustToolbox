using System.Collections.Generic;
using System.IO;
using System.Linq;
using DiffPlex.Renderer;
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

    /// <summary>
    ///     AKA yaml dumper 9000.
    ///     Scans through a provided directory for EntityPrototypes and outputs them all as a single file.
    ///     Includes information on inherited components through parent entities.
    /// </summary>
    /// <param name="searchPath">The directory to save prototypes from.</param>
    public void SaveEntityPrototypes(ResPath searchPath,  out string output, bool includeAbstract = false, bool saveFile = false)
    {
        // mild shitcode.
        var outStreams = GetYamlStreams(searchPath);
        var streams = GetYamlStreams(new("/Prototypes"));

        List<string> outProtos = [];
        Dictionary<string, PrototypeValidationData> entityProtos = [];

        foreach (var data in ValidateStreams(outStreams)
            .Where(x => x.Item1 == typeof(EntityPrototype)))
        {
            outProtos.Add(data.Item2.Id);
        }

        foreach (var data in ValidateStreams(streams)
            .Where(x => x.Item1 == typeof(EntityPrototype)))
        {
            data.Item2.Mapping.Remove("type");
            entityProtos[data.Item2.Id] = data.Item2;
        }

        Dictionary<string, DataNode> normalizedPrototypes = [];
        foreach (var (id, data) in entityProtos.OrderBy(x => x.Key))
        {
            EnsurePushed(data, entityProtos, typeof(EntityPrototype));

            if (!outProtos.Contains(data.Id))
                continue;

            if (data.Mapping.TryGet("abstract", out ValueDataNode? abstractNode)
                && bool.Parse(abstractNode.Value)
                && !includeAbstract)
                continue;

            normalizedPrototypes[id] = NormalizeDataNode(data.Mapping);
        }

        // we save the file if this is being run with the --save command
        // otherwise, we output results to the out
        if (saveFile)
        {
            using var writer = new StreamWriter(new ResPath("entity-prototypes.yml").CanonPath, false);
            normalizedPrototypes.Values.ToSequenceDataNode().Write(writer);
        }

        output = normalizedPrototypes.Values.ToSequenceDataNode().ToString();
    }

    public void GenerateDiff(ResPath beforePath, string after)
    {
        string before = File.ReadAllText(beforePath.ToRelativeSystemPath());
        string diff = UnidiffRenderer.GenerateUnidiff(before, after);

        // TODO: probably dont want to use streamwriter here.
        // instead we should return our output so this can be used in other apps.
        // maybe make this a bool?
        using var writer = new StreamWriter("prototype-diff.yml", false);
        writer.WriteLine(diff);
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
