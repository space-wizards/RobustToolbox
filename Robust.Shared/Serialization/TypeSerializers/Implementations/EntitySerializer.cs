using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations
{
    [TypeSerializer]
    public class EntitySerializer : ITypeReaderWriter<IEntity, ValueDataNode>
    {
        public DeserializationResult Read(
            ISerializationManager serializationManager,
            ValueDataNode node,
            IDependencyCollection dependencies, bool skipHook,
            ISerializationContext? context = null)
        {
            if (!EntityUid.TryParse(node.Value, out var uid) ||
                !uid.IsValid())
            {
                throw new InvalidMappingException($"{node.Value} is not a valid entity uid.");
            }

            var entity = dependencies.Resolve<IEntityManager>().GetEntity(uid);
            return new DeserializedValue<IEntity>(entity);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            IDependencyCollection dependencies,
            ISerializationContext? context = null)
        {
            return EntityUid.TryParse(node.Value, out var uid) &&
                   uid.IsValid() &&
                   dependencies.Resolve<IEntityManager>().EntityExists(uid)
                ? new ValidatedValueNode(node)
                : new ErrorNode(node, "Failed parsing EntityUid");
        }

        public DataNode Write(ISerializationManager serializationManager, IEntity value, bool alwaysWrite = false,
            ISerializationContext? context = null)
        {
            return new ValueDataNode(value.Uid.ToString());
        }
    }
}
