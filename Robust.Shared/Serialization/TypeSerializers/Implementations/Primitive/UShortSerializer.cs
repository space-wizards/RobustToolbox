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
    public sealed class UShortSerializer : ITypeSerializer<ushort, ValueDataNode>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return ushort.TryParse(node.Value, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"Failed parsing unsigned short value: {node.Value}");
        }

        public ushort Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
        {
            return ushort.Parse(node.Value, CultureInfo.InvariantCulture);
        }

        public DataNode Write(ISerializationManager serializationManager, ushort value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public ushort Copy(ISerializationManager serializationManager, ushort source, ushort target, bool skipHook,
            ISerializationContext? context = null)
        {
            return source;
        }
    }
}
