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
        public TimeSpan NodeToType(IDataNode node, ISerializationContext? context = null)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            var seconds = double.Parse(valueDataNode.GetValue(), CultureInfo.InvariantCulture);
            return TimeSpan.FromSeconds(seconds);
        }

        public IDataNode TypeToNode(TimeSpan value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }
    }
}
