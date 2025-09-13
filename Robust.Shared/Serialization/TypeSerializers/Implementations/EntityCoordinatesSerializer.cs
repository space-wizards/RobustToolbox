using System.Collections.Generic;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

[TypeSerializer]
public sealed class EntityCoordinatesSerializer : ITypeSerializer<EntityCoordinates, MappingDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var mapping = new Dictionary<ValidationNode, ValidationNode>();

        if (!node.TryGetValue("uid", out var uidNode))
        {
            mapping.Add(
                new ErrorNode(new ValueDataNode(""), $"Failed to find uid key in {nameof(EntityCoordinates)}!"),
                new ErrorNode(new ValueDataNode(""), "Value must be a non-null scalar."));
            return new ValidatedMappingNode(mapping);
        }

        if (!node.TryGetValue("pos", out var posNode))
        {
            mapping.Add(
                new ErrorNode(new ValueDataNode(""), $"Failed to find position key in {nameof(EntityCoordinates)}!"),
                new ErrorNode(new ValueDataNode(""), "Value must be a non-null scalar."));
            return new ValidatedMappingNode(mapping);
        }

        var validatedUid = serializationManager.ValidateNode<EntityUid>(uidNode, context);
        var validatedPos = serializationManager.ValidateNode<Vector2>(posNode, context);

        mapping.Add(new ValidatedValueNode(node.GetKeyNode("uid")), validatedUid);
        mapping.Add(new ValidatedValueNode(node.GetKeyNode("pos")), validatedPos);

        return new ValidatedMappingNode(mapping);
    }

    public EntityCoordinates Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<EntityCoordinates>? instanceProvider = null)
    {
        var uid = EntityUid.Invalid;
        var pos = Vector2.Zero;

        if (node.TryGetValue("uid", out var uidNode))
            uid = serializationManager.Read<EntityUid>(uidNode, context);

        if (node.TryGetValue("pos", out var positionNode))
            pos = serializationManager.Read<Vector2>(positionNode, hookCtx, context);

        return new EntityCoordinates(uid, pos);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        EntityCoordinates value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var seq = new MappingDataNode();

        seq.Add("uid", serializationManager.WriteValue(value.EntityId, alwaysWrite, context));
        seq.Add("pos", serializationManager.WriteValue(value.Position, alwaysWrite, context));

        return seq;
    }
}
