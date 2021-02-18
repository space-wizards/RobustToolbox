using System.Globalization;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class AngleSerializer : ITypeSerializer<Angle>
    {
        public Angle NodeToType(DataNode node, ISerializationContext? context = null)
        {
            if (node is not ValueDataNode valueDataNode) throw new InvalidNodeTypeException();
            var nodeContents = valueDataNode.GetValue();
            if (nodeContents.EndsWith("rad"))
            {
                return new Angle(double.Parse(nodeContents.Substring(0, nodeContents.Length - 3), CultureInfo.InvariantCulture));
            }
            return Angle.FromDegrees(double.Parse(nodeContents, CultureInfo.InvariantCulture));

        }

        public DataNode TypeToNode(Angle value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.Theta.ToString(CultureInfo.InvariantCulture)} rad");
        }
    }
}
