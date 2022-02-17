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
    public sealed class UIntSerializer : ITypeSerializer<uint, ValueDataNode>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return uint.TryParse(node.Value, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"Failed parsing unsigned int value: {node.Value}");
        }

        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null, uint value = default)
        {
            return new DeserializedValue<uint>(uint.Parse(node.Value, CultureInfo.InvariantCulture));
        }

        public DataNode Write(ISerializationManager serializationManager, uint value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public uint Copy(ISerializationManager serializationManager, uint source, uint target, bool skipHook,
            ISerializationContext? context = null)
        {
            return source;
        }
    }
}
