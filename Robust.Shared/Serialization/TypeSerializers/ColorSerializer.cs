using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class ColorSerializer : ITypeSerializer<Color, ValueDataNode>
    {
        public Color Read(ValueDataNode node, ISerializationContext? context = null)
        {
            if (Color.TryFromName(node.Value, out var color))
            {
                return color;
            }

            return Color.FromHex(node.Value);
        }

        public DataNode Write(Color value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToHex());
        }

        [MustUseReturnValue]
        public Color Copy(Color source, Color target)
        {
            return new(source.R, source.G, source.B, source.A);
        }
    }
}
