using System;
using System.Globalization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class Vector2iSerializer : ITypeSerializer<Vector2i, ValueDataNode>
    {
        public Vector2i Read(ValueDataNode node, ISerializationContext? context = null)
        {
            string raw = node.Value;
            string[] args = raw.Split(',');
            if (args.Length != 2)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector2), raw));
            }

            return new Vector2i(int.Parse(args[0], CultureInfo.InvariantCulture),
                int.Parse(args[1], CultureInfo.InvariantCulture));
        }

        public DataNode Write(Vector2i value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.X.ToString(CultureInfo.InvariantCulture)},{value.Y.ToString(CultureInfo.InvariantCulture)}");
        }

        public Vector2i Copy(Vector2i source, Vector2i target)
        {
            return new(source.X, source.Y);
        }
    }
}
