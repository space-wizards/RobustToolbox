using System.Globalization;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class MapIdSerializer : ITypeSerializer<MapId>
    {
        public MapId NodeToType(IDataNode node, ISerializationContext? context = null)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            var val = int.Parse(valueDataNode.GetValue(), CultureInfo.InvariantCulture);
            return new MapId(val);
        }

        public IDataNode TypeToNode(MapId value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var val = (int)value;
            return nodeFactory.GetValueNode(val.ToString());
        }
    }
}
