using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

[TypeSerializer]
public sealed class EntityRelationSerializer : ITypeSerializer<EntityRelation, ValueDataNode>
{
    ValidationNode ITypeValidator<EntityRelation, ValueDataNode>.Validate(
        ISerializationManager seri,
        ValueDataNode node,
        IDependencyCollection deps,
        ISerializationContext? context)
    {
        return seri.ValidateNode<EntityUid?>(node, context);
    }

    EntityRelation ITypeReader<EntityRelation, ValueDataNode>.Read(
        ISerializationManager seri,
        ValueDataNode node,
        IDependencyCollection deps,
        SerializationHookContext hookCtx,
        ISerializationContext? ctx,
        ISerializationManager.InstantiationDelegate<EntityRelation>? instanceProvider)
    {
        return new EntityRelation(seri.Read<EntityUid?>(node, ctx));
    }

    DataNode ITypeWriter<EntityRelation>.Write(
        ISerializationManager seri,
        EntityRelation value,
        IDependencyCollection deps,
        bool alwaysWrite,
        ISerializationContext? ctx)
    {
        return seri.WriteValue(value.Entity, alwaysWrite, ctx);
    }
}
