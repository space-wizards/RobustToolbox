using Robust.Shared.GameObjects;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;

namespace Robust.Shared.Serialization.TypeSerializers
{
    [TypeSerializer]
    public class EntitySerializer : ITypeReaderWriter<IEntity, ValueDataNode>
    {
        public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node, bool skipHook,
            ISerializationContext? context = null)
        {
            if (!EntityUid.TryParse(node.Value, out var uid) ||
                !uid.IsValid())
            {
                throw new InvalidMappingException($"{node.Value} is not a valid entity uid.");
            }

            var entity = serializationManager.EntityManager.GetEntity(uid);

            // TODO Paul what type to return here
            return new DeserializedValue<IEntity>(entity);
        }

        public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
            ISerializationContext? context = null)
        {
            // TODO Paul should we be checking entity exists here
            return EntityUid.TryParse(node.Value, out var uid) &&
                   uid.IsValid() &&
                   serializationManager.EntityManager.EntityExists(uid)
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
