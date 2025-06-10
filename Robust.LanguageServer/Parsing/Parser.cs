using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Utility;
using YamlDotNet.RepresentationModel;

namespace Robust.LanguageServer.Parsing;

public sealed class Parser
{
    [Dependency] private readonly IPrototypeManager _protoMan = null!;

    public void ParseDocument(string text)
    {
        using var sr = new StringReader(text);

        var yaml = new YamlStream();
        yaml.Load(sr);

        var errors = new List<string>();

        foreach (var document in yaml.Documents)
        {
            var root = document.RootNode;

            if (root is not YamlSequenceNode seq)
            {
                errors.Add("Expected top-level sequence of prototypes");
                continue;
            }

            var rootNode = (YamlSequenceNode)document.RootNode;
            foreach (YamlMappingNode node in rootNode.Cast<YamlMappingNode>())
            {
                var typeId = node.GetNode("type").AsString();
                // if (_ignoredPrototypeTypes.Contains(typeId))
                //     continue;


                if (!_protoMan.TryGetKindType(typeId, out var type))
                {
                    errors.Add($"Unknown prototype type: '{typeId}'");
                    continue;
                }

                var mapping = node.ToDataNodeCast<MappingDataNode>();
                mapping.Remove("type");

                var id = mapping.Get<ValueDataNode>("id").Value;

                Console.Error.WriteLine($"Parsed prototype ID: '{id}' of type '{type}'");
            }


        }
    }
}
