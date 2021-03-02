using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class ColorSerializer : ITypeSerializer<Color, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            bool skipHook,
            ISerializationContext? context = null)
        {
            var deserializedColor = Color.TryFromName(node.Value, out var color)
                ? color :
                Color.FromHex(node.Value);

            return new DeserializedValue<Color>(deserializedColor);
        }

        public ValidatedNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            return Color.TryFromName(node.Value, out _) || Color.TryFromHex(node.Value) != null ? new ValidatedValueNode(node) : new ErrorNode(node);
        }

        public DataNode Write(ISerializationManager serializationManager, Color value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToHex());
        }

        [MustUseReturnValue]
        public Color Copy(ISerializationManager serializationManager, Color source, Color target,
            bool skipHook,
            ISerializationContext? context = null)
        {
            return new(source.R, source.G, source.B, source.A);
        }
    }
}
