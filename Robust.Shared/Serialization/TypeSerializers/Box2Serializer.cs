using System.Globalization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    public class Box2Serializer : ITypeSerializer<Box2>
    {
        public Box2 NodeToType(DataNode node, ISerializationContext? context = null)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            var args = valueDataNode.GetValue().Split(',');

            var b = float.Parse(args[0], CultureInfo.InvariantCulture);
            var l = float.Parse(args[1], CultureInfo.InvariantCulture);
            var t = float.Parse(args[2], CultureInfo.InvariantCulture);
            var r = float.Parse(args[3], CultureInfo.InvariantCulture);

            return new Box2(l, b, r, t);
        }

        public DataNode TypeToNode(Box2 value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.Bottom.ToString(CultureInfo.InvariantCulture)},{value.Left.ToString(CultureInfo.InvariantCulture)},{value.Top.ToString(CultureInfo.InvariantCulture)},{value.Right.ToString(CultureInfo.InvariantCulture)}");
        }
    }
}
