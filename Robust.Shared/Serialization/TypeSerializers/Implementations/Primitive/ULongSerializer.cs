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
    public sealed class ULongSerializer : ITypeSerializer<ulong, ValueDataNode>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return ulong.TryParse(node.Value, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"Failed parsing unsigned long value: {node.Value}");
        }

        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null, ulong value = default)
        {
            return new DeserializedValue<ulong>(ulong.Parse(node.ToString(), CultureInfo.InvariantCulture));
        }

        public DataNode Write(ISerializationManager serializationManager, ulong value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public ulong Copy(ISerializationManager serializationManager, ulong source, ulong target, bool skipHook,
            ISerializationContext? context = null)
        {
            return source;
        }
    }
}
