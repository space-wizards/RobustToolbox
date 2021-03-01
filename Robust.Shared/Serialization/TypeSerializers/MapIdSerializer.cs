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
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            var val = int.Parse(node.Value, CultureInfo.InvariantCulture);
            return new DeserializedValue<MapId>(new MapId(val));
        }

        public bool Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            return int.TryParse(node.Value, out _);
        }

        public DataNode Write(ISerializationManager serializationManager, MapId value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            var val = (int)value;
            return new ValueDataNode(val.ToString());
        }

        [MustUseReturnValue]
        public MapId Copy(ISerializationManager serializationManager, MapId source, MapId target, ISerializationContext? context = null)
        {
            return new(source.Value);
        }
    }
}
