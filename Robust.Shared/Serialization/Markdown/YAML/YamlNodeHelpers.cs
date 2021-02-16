using System;
using YamlDotNet.RepresentationModel;

namespace Robust.Shared.Serialization.Markdown.YAML
{
    public static class YamlNodeHelpers
    {
        public static IDataNode ToDataNode(this YamlNode node)
        {
            switch (node)
            {
                case YamlScalarNode scalarNode:
                    return new YamlValueDataNode(scalarNode);
                case YamlMappingNode mappingNode:
                    return new YamlMappingDataNode(mappingNode);
                case YamlSequenceNode sequenceNode:
                    return new YamlSequenceDataNode(sequenceNode);
                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
        }

        public static YamlNode ToYamlNode(this IDataNode node)
        {
            switch (node)
            {
                case YamlValueDataNode valueDataNode:
                    return new YamlScalarNode(valueDataNode.Value);
                case YamlMappingDataNode mappingDataNode:
                    return mappingDataNode.ToMappingNode();
                case YamlSequenceDataNode sequenceNode:
                    return sequenceNode.ToSequenceNode();
                default:
                    throw new ArgumentOutOfRangeException(nameof(node));
            }
        }
    }
}
