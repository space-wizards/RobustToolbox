using System.Globalization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Primitive
{
    [TypeSerializer]
    public sealed class ShortSerializer : ITypeSerializer<short, ValueDataNode>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return short.TryParse(node.Value, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"Failed parsing short value: {node.Value}");
        }

        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null, short value = default)
        {
            return new DeserializedValue<short>(short.Parse(node.Value, CultureInfo.InvariantCulture));
        }

        public DataNode Write(ISerializationManager serializationManager, short value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public short Copy(ISerializationManager serializationManager, short source, short target, bool skipHook,
            ISerializationContext? context = null)
        {
            return source;
        }
    }
}
