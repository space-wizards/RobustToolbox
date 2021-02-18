using System;
using System.Globalization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class Vector3Serializer : ITypeSerializer<Vector3, ValueDataNode>
    {
        public Vector3 Read(ValueDataNode node, ISerializationContext? context = null)
        {
            string raw = node.Value;
            string[] args = raw.Split(',');
            if (args.Length != 3)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector3), raw));
            }

            return new Vector3(float.Parse(args[0], CultureInfo.InvariantCulture),
                float.Parse(args[1], CultureInfo.InvariantCulture),
                float.Parse(args[2], CultureInfo.InvariantCulture));
        }

        public DataNode Write(Vector3 value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(
                $"{value.X.ToString(CultureInfo.InvariantCulture)},{value.Y.ToString(CultureInfo.InvariantCulture)},{value.Z.ToString(CultureInfo.InvariantCulture)}");
        }
    }
}
