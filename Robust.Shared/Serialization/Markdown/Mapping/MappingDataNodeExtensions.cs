using System.Collections.Generic;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.Shared.Serialization.Markdown.Mapping
{
    public static class MappingDataNodeExtensions
    {
        public static MappingDataNode AddNode(this MappingDataNode mapping, string key, DataNode node)
        {
            mapping.Add(new ValueDataNode(key), node);
            return mapping;
        }

        public static MappingDataNode AddNode(this MappingDataNode mapping, string key, string value)
        {
            mapping.Add(new ValueDataNode(key), new ValueDataNode(value));
            return mapping;
        }

        public static MappingDataNode AddNode(this MappingDataNode mapping, string key, List<string> sequence)
        {
            mapping.Add(new ValueDataNode(key), new SequenceDataNode(sequence));
            return mapping;
        }
    }
}
