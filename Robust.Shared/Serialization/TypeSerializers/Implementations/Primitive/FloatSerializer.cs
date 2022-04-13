using System.Globalization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Primitive
{
    [TypeSerializer]
    public sealed class FloatSerializer : ITypeSerializer<float, ValueDataNode>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return float.TryParse(node.Value,NumberStyles.Float | NumberStyles.AllowThousands, CultureInfo.InvariantCulture, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"Failed parsing float value: {node.Value}");
        }

        public float Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null, float value = default)
        {
            return float.Parse(node.Value, CultureInfo.InvariantCulture);
        }

        public DataNode Write(ISerializationManager serializationManager, float value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public float Copy(ISerializationManager serializationManager, float source, float target, bool skipHook,
            ISerializationContext? context = null)
        {
            return source;
        }
    }
}
