using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class ColorSerializer : ITypeSerializer<Color>
    {
        public Color NodeToType(IDataNode node, ISerializationContext? context = null)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            if (Color.TryFromName(valueDataNode.GetValue(), out var color))
            {
                return color;
            }

            return Color.FromHex(valueDataNode.GetValue());
        }

        public IDataNode TypeToNode(Color value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode(value.ToHex());
        }
    }
}
