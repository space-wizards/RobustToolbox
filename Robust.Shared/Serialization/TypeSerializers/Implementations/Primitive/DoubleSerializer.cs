using System.Globalization;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Primitive
{
    [TypeSerializer]
    public sealed class DoubleSerializer : ITypeSerializer<double, ValueDataNode>
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return Parse.TryDouble(node.Value, NumberStyles.Any, out _)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"Failed parsing double value: {node.Value}");
        }

        public double Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null, double value = default)
        {
            return Parse.Double(node.Value);
        }

        public DataNode Write(ISerializationManager serializationManager, double value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ToString(CultureInfo.InvariantCulture));
        }

        public double Copy(ISerializationManager serializationManager, double source, double target, bool skipHook,
            ISerializationContext? context = null)
        {
            return source;
        }
    }
}
