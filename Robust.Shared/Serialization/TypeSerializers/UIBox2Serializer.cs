using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class UIBox2Serializer : ITypeSerializer<UIBox2, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            var args = node.Value.Split(',');

            if (args.Length != 4)
            {
                throw new InvalidMappingException($"Could not parse {nameof(UIBox2)}: '{node.Value}'");
            }

            var t = float.Parse(args[0], CultureInfo.InvariantCulture);
            var l = float.Parse(args[1], CultureInfo.InvariantCulture);
            var b = float.Parse(args[2], CultureInfo.InvariantCulture);
            var r = float.Parse(args[3], CultureInfo.InvariantCulture);

            return new DeserializedValue<UIBox2>(new UIBox2(l, t, r, b));
        }

        public ValidatedNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            string raw = node.Value;
            string[] args = raw.Split(',');

            if (args.Length != 4)
            {
                return new ErrorNode(node);
            }

            return float.TryParse(args[0], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[1], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[2], NumberStyles.Any, CultureInfo.InvariantCulture, out _) &&
                   float.TryParse(args[3], NumberStyles.Any, CultureInfo.InvariantCulture, out _) ? new ValidatedValueNode(node) : new ErrorNode(node);
        }

        public DataNode Write(ISerializationManager serializationManager, UIBox2 value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode($"{value.Top.ToString(CultureInfo.InvariantCulture)},{value.Left.ToString(CultureInfo.InvariantCulture)},{value.Bottom.ToString(CultureInfo.InvariantCulture)},{value.Right.ToString(CultureInfo.InvariantCulture)}");
        }

        [MustUseReturnValue]
        public UIBox2 Copy(ISerializationManager serializationManager, UIBox2 source, UIBox2 target, ISerializationContext? context = null)
        {
            return new(source.Left, source.Top, source.Right, source.Bottom);
        }
    }
}
