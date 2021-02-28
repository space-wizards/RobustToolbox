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
    public class Vector2iSerializer : ITypeSerializer<Vector2i, ValueDataNode>
    {
        public DeserializationResult<Vector2i> Read(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            string raw = node.Value;
            string[] args = raw.Split(',');

            if (args.Length != 2)
            {
                throw new ArgumentException($"Could not parse {nameof(Vector2)}: '{raw}'");
            }

            var x = int.Parse(args[0], CultureInfo.InvariantCulture);
            var y = int.Parse(args[1], CultureInfo.InvariantCulture);
            var vector = new Vector2i(x, y);

            return new DeserializedValue<Vector2i>(vector);
        }

        public DataNode Write(ISerializationManager serializationManager, Vector2i value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.X.ToString(CultureInfo.InvariantCulture)},{value.Y.ToString(CultureInfo.InvariantCulture)}");
        }

        public Vector2i Copy(ISerializationManager serializationManager, Vector2i source, Vector2i target)
        {
            return new(source.X, source.Y);
        }
    }
}
