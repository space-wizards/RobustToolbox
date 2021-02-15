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
    }
}
