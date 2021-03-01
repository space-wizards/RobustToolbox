using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class ColorSerializer : ITypeSerializer<Color, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            var deserializedColor = Color.TryFromName(node.Value, out var color)
                ? color :
                Color.FromHex(node.Value);

            return new DeserializedValue<Color>(deserializedColor);
        }

        public bool Validate(ISerializationManager serializationManager, ValueDataNode node)
        {
            return Color.TryFromName(node.Value, out _) || Color.TryFromHex(node.Value) != null;
        }

        public DataNode Write(ISerializationManager serializationManager, Color value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToHex());
        }

        [MustUseReturnValue]
        public Color Copy(ISerializationManager serializationManager, Color source, Color target, ISerializationContext? context = null)
        {
            return new(source.R, source.G, source.B, source.A);
        }
    }
}
