using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public sealed class StringSerializer : ITypeSerializer<string, ValueDataNode>
    {
        public string Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            SerializationHookContext hookCtx,
            ISerializationContext? context = null,
            ISerializationManager.InstantiationDelegate<string>? instanceProvider = null)
        {
            return node.Value;
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            return new ValidatedValueNode(node);
        }

        public DataNode Write(ISerializationManager serializationManager, string value,
            IDependencyCollection dependencies, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value);
        }
    }
}
