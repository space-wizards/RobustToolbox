using System.Collections.Generic;
using System.Numerics;
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
public sealed class MapCoordinatesSerializer : ITypeSerializer<MapCoordinates, MappingDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        var mapping = new Dictionary<ValidationNode, ValidationNode>();

        if (!node.TryGetValue("mapId", out var uidNode))
        {
            mapping.Add(
                new ErrorNode(new ValueDataNode(""), $"Failed to find mapId key in {nameof(MapCoordinates)}!"),
                new ErrorNode(new ValueDataNode(""), "Value must be a non-null scalar."));
            return new ValidatedMappingNode(mapping);
        }

        if (!node.TryGetValue("pos", out var posNode))
        {
            mapping.Add(
                new ErrorNode(new ValueDataNode(""), $"Failed to find position key in {nameof(MapCoordinates)}!"),
                new ErrorNode(new ValueDataNode(""), "Value must be a non-null scalar."));
            return new ValidatedMappingNode(mapping);
        }

        var validatedUid = serializationManager.ValidateNode<MapId>(uidNode, context);
        var validatedPos = serializationManager.ValidateNode<Vector2>(posNode, context);

        mapping.Add(new ValidatedValueNode(node.GetKeyNode("mapId")), validatedUid);
        mapping.Add(new ValidatedValueNode(node.GetKeyNode("pos")), validatedPos);

        return new ValidatedMappingNode(mapping);
    }

    public MapCoordinates Read(
        ISerializationManager serializationManager,
        MappingDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<MapCoordinates>? instanceProvider = null)
    {
        var mapId = MapId.Nullspace;
        var pos = Vector2.Zero;

        if (node.TryGetValue("mapId", out var mapIdNode))
            mapId = serializationManager.Read<MapId>(mapIdNode, context);

        if (node.TryGetValue("pos", out var positionNode))
            pos = serializationManager.Read<Vector2>(positionNode, hookCtx, context);

        return new MapCoordinates(pos, mapId);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        MapCoordinates value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var seq = new MappingDataNode();

        seq.Add("mapId", serializationManager.WriteValue(value.MapId, alwaysWrite, context));
        seq.Add("pos", serializationManager.WriteValue(value.Position, alwaysWrite, context));

        return seq;
    }
}
