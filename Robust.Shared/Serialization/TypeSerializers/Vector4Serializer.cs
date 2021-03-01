using System;
using System.Globalization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class Vector4Serializer : ITypeSerializer<Vector4, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            string raw = node.Value;
            string[] args = raw.Split(',');

            if (args.Length != 4)
            {
                throw new InvalidMappingException($"Could not parse {nameof(Vector4)}: '{raw}'");
            }

            var x = float.Parse(args[0], CultureInfo.InvariantCulture);
            var y = float.Parse(args[1], CultureInfo.InvariantCulture);
            var z = float.Parse(args[2], CultureInfo.InvariantCulture);
            var w = float.Parse(args[3], CultureInfo.InvariantCulture);
            var vector = new Vector4(x, y, z, w);

            return new DeserializedValue<Vector4>(vector);
        }

        public bool Validate(ISerializationManager serializationManager, ValueDataNode node)
        {
            string raw = node.Value;
            string[] args = raw.Split(',');

            if (args.Length != 4)
            {
                return false;
            }

            return float.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[3], NumberStyles.Any, CultureInfo.InvariantCulture, out _);
        }

        public DataNode Write(ISerializationManager serializationManager, Vector4 value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.X.ToString(CultureInfo.InvariantCulture)},{value.Y.ToString(CultureInfo.InvariantCulture)},{value.Z.ToString(CultureInfo.InvariantCulture)},{value.W.ToString(CultureInfo.InvariantCulture)}");
        }

        public Vector4 Copy(ISerializationManager serializationManager, Vector4 source, Vector4 target, ISerializationContext? context = null)
        {
            return new(source);
        }
    }
}
