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
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            var args = node.Value.Split(',');

            if (args.Length != 4)
            {
                throw new InvalidMappingException($"Could not parse {nameof(Box2)}: '{node.Value}'");
            }

            var b = float.Parse(args[0], CultureInfo.InvariantCulture);
            var l = float.Parse(args[1], CultureInfo.InvariantCulture);
            var t = float.Parse(args[2], CultureInfo.InvariantCulture);
            var r = float.Parse(args[3], CultureInfo.InvariantCulture);

            return new DeserializedValue<Box2>(new Box2(l, b, r, t));
        }

        public bool Validate(ISerializationManager serializationManager, ValueDataNode node)
        {
            var args = node.Value.Split(',');

            if (args.Length != 4)
            {
                return false;
            }

            return float.TryParse(args[0], out _) &&
                   float.TryParse(args[1], out _) &&
                   float.TryParse(args[2], out _) &&
                   float.TryParse(args[3], out _);
        }

        public DataNode Write(ISerializationManager serializationManager, Box2 value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.Bottom.ToString(CultureInfo.InvariantCulture)},{value.Left.ToString(CultureInfo.InvariantCulture)},{value.Top.ToString(CultureInfo.InvariantCulture)},{value.Right.ToString(CultureInfo.InvariantCulture)}");
        }

        [MustUseReturnValue]
        public Box2 Copy(ISerializationManager serializationManager, Box2 source, Box2 target, ISerializationContext? context = null)
        {
            return new(source.Left, source.Bottom, source.Right, source.Top);
        }
    }
}
