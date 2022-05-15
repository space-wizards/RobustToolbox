using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Primitive;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

[TypeSerializer]
public sealed class EntityUidSerializer : ITypeSerializer<EntityUid, ValueDataNode>
{
    private IntSerializer _intSerializer = new();

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return _intSerializer.Validate(serializationManager, node, dependencies, context);
    }

    public EntityUid Read(ISerializationManager serializationManager, ValueDataNode node, IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, EntityUid value = default)
    {
        return new EntityUid(_intSerializer.Read(serializationManager, node, dependencies, skipHook, context));
    }

    public DataNode Write(ISerializationManager serializationManager, EntityUid value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return _intSerializer.Write(serializationManager, value.GetHashCode(), alwaysWrite, context);
    }

    public EntityUid Copy(ISerializationManager serializationManager, EntityUid source, EntityUid target, bool skipHook,
        ISerializationContext? context = null)
    {
        return source;
    }
}
