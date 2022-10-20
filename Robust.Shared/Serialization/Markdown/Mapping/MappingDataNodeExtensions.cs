using System.Collections.Generic;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.Shared.Serialization.Markdown.Mapping
{
    public static class MappingDataNodeExtensions
    {
        public static MappingDataNode Add(this MappingDataNode mapping, string key, DataNode node)
        {
            mapping.Add(new ValueDataNode(key), node);
            return mapping;
        }

        public static MappingDataNode Add(this MappingDataNode mapping, string key, string value)
        {
            mapping.Add(new ValueDataNode(key), new ValueDataNode(value));
            return mapping;
        }

        public static MappingDataNode Add(this MappingDataNode mapping, string key, List<string> sequence)
        {
            mapping.Add(new ValueDataNode(key), new SequenceDataNode(sequence));
            return mapping;
        }

        public static bool TryGetAndValidate<T>(this MappingDataNode mapping, string tag, ISerializationManager serializationManager, ISerializationContext? context, out ValidationNode validationNode)
        {
            if (!mapping.TryGet(tag, out var idNode))
                validationNode = new ErrorNode(mapping, $"No node with tag '{tag}' found");
            else
                validationNode = serializationManager.ValidateNode<T>(idNode, context);

            return validationNode is not ErrorNode;
        }
    }
}
