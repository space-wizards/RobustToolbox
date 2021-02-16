using System;
using System.Globalization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class Vector2Serializer : ITypeSerializer<Vector2>
    {
        public Vector2 NodeToType(IDataNode node, ISerializationContext? context = null)
        {
            if (node is not IValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            string raw = valueDataNode.GetValue();
            string[] args = raw.Split(',');
            if (args.Length != 2)
            {
                throw new ArgumentException(string.Format("Could not parse {0}: '{1}'", nameof(Vector2), raw));
            }

            return new Vector2(float.Parse(args[0], CultureInfo.InvariantCulture),
                float.Parse(args[1], CultureInfo.InvariantCulture));
        }

        public IDataNode TypeToNode(Vector2 value, IDataNodeFactory nodeFactory, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return nodeFactory.GetValueNode($"{value.X.ToString(CultureInfo.InvariantCulture)},{value.Y.ToString(CultureInfo.InvariantCulture)}");
        }
    }
}
