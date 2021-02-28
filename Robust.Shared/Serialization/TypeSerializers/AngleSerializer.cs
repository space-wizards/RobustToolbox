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
    public class AngleSerializer : ITypeSerializer<Angle, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            var nodeContents = node.Value;

            var angle = nodeContents.EndsWith("rad")
                ? new Angle(double.Parse(nodeContents.Substring(0, nodeContents.Length - 3),
                    CultureInfo.InvariantCulture))
                : Angle.FromDegrees(double.Parse(nodeContents, CultureInfo.InvariantCulture));

            return new DeserializedValue<Angle>(angle);
        }

        public DataNode Write(ISerializationManager serializationManager, Angle value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.Theta.ToString(CultureInfo.InvariantCulture)} rad");
        }

        [MustUseReturnValue]
        public Angle Copy(ISerializationManager serializationManager, Angle source, Angle target, ISerializationContext? context = null)
        {
            return new(source);
        }
    }
}
