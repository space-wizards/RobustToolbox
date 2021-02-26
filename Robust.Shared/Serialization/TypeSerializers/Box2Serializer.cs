using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class Box2Serializer : ITypeSerializer<Box2, ValueDataNode>
    {
        public DeserializationResult<Box2> Read(ValueDataNode node, ISerializationContext? context = null)
        {
            var args = node.Value.Split(',');

            var b = float.Parse(args[0], CultureInfo.InvariantCulture);
            var l = float.Parse(args[1], CultureInfo.InvariantCulture);
            var t = float.Parse(args[2], CultureInfo.InvariantCulture);
            var r = float.Parse(args[3], CultureInfo.InvariantCulture);

            return DeserializationResult.Value(new Box2(l, b, r, t));
        }

        public DataNode Write(Box2 value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.Bottom.ToString(CultureInfo.InvariantCulture)},{value.Left.ToString(CultureInfo.InvariantCulture)},{value.Top.ToString(CultureInfo.InvariantCulture)},{value.Right.ToString(CultureInfo.InvariantCulture)}");
        }

        [MustUseReturnValue]
        public Box2 Copy(Box2 source, Box2 target)
        {
            return new(source.Left, source.Bottom, source.Right, source.Top);
        }
    }
}
