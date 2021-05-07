using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public class PrototypeReferenceSerializer<TPrototype> : ITypeSerializer<PrototypeReference<TPrototype>, ValueDataNode> where TPrototype : IPrototype
    {
        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, ISerializationContext? context = null)
        {
            return dependencies.Resolve<IPrototypeManager>().HasIndex<TPrototype>(node.Value)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, $"PrototypeID {node.Value} for type {typeof(TPrototype)} not found");
        }

        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
        {
            return DeserializationResult.Value(PrototypeReference<TPrototype>.Create(node.Value));
        }

        public DataNode Write(ISerializationManager serializationManager, PrototypeReference<TPrototype> value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.ID);
        }

        public PrototypeReference<TPrototype> Copy(ISerializationManager serializationManager, PrototypeReference<TPrototype> source,
            PrototypeReference<TPrototype> target, bool skipHook, ISerializationContext? context = null)
        {
            return PrototypeReference<TPrototype>.Create(source.ID);
        }
    }
}
