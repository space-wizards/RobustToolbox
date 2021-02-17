using System;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.YAML
{
    public static class YamlNodeHelpers
    {
        public static IDataNode ToDataNode(this YamlNode node)
        {
            return node switch
            {
                YamlScalarNode scalarNode => new YamlValueDataNode(scalarNode),
                YamlMappingNode mappingNode => new YamlMappingDataNode(mappingNode),
                YamlSequenceNode sequenceNode => new YamlSequenceDataNode(sequenceNode),
                _ => throw new ArgumentOutOfRangeException(nameof(node))
            };
        }

        public static YamlNode ToYamlNode(this IDataNode node)
        {
            return node switch
            {
                YamlValueDataNode valueDataNode => new YamlScalarNode(valueDataNode.Value),
                YamlMappingDataNode mappingDataNode => mappingDataNode.ToMappingNode(),
                YamlSequenceDataNode sequenceNode => sequenceNode.ToSequenceNode(),
                _ => throw new ArgumentOutOfRangeException(nameof(node))
            };
        }
    }
}
