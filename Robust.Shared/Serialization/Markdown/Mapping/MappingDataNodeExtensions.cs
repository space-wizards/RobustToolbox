using System.Collections.Generic;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Value;

namespace Robust.Shared.Serialization.Markdown.Mapping
{
    public static class MappingDataNodeExtensions
    {
        public static MappingDataNode Add(this MappingDataNode mapping, string key, string value)
        {
            mapping.Add(key, new ValueDataNode(value));
            return mapping;
        }

        public static MappingDataNode Add(this MappingDataNode mapping, string key, List<string> sequence)
        {
            mapping.Add(key, new SequenceDataNode(sequence));
            return mapping;
        }
    }
}
