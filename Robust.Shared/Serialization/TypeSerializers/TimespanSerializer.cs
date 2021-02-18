using System;
using System.Globalization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class TimespanSerializer : ITypeSerializer<TimeSpan>
    {
        public TimeSpan NodeToType(DataNode node, ISerializationContext? context = null)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            var seconds = double.Parse(valueDataNode.GetValue(), CultureInfo.InvariantCulture);
            return TimeSpan.FromSeconds(seconds);
        }

        public DataNode TypeToNode(TimeSpan value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }
    }
}
