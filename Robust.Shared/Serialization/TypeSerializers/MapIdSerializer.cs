using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class MapIdSerializer : ITypeSerializer<MapId, ValueDataNode>
    {
        public DeserializationResult<MapId> Read(ValueDataNode node, ISerializationContext? context = null)
        {
            var val = int.Parse(node.Value, CultureInfo.InvariantCulture);
            return DeserializationResult.Value(new MapId(val));
        }

        public DataNode Write(MapId value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var val = (int)value;
            return new ValueDataNode(val.ToString());
        }

        [MustUseReturnValue]
        public MapId Copy(MapId source, MapId target)
        {
            return new(source.Value);
        }
    }
}
