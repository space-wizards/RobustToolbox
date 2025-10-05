using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

/// <summary>
/// Stores a reference to a map using its EntityUid.
/// </summary>
[TypeSerializer]
public sealed class MapIdSerializer : ITypeSerializer<MapId, ValueDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return serializationManager.ValidateNode<EntityUid>(node, context);
    }

    public MapId Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<MapId>? instanceProvider = null)
    {
        if (context is not EntityDeserializer entSerializer)
            return MapId.Nullspace;

        var uid = serializationManager.Read<EntityUid>(node, context);
        if (!entSerializer.EntMan.TryGetComponent(uid, out MapComponent? mapComp))
            return MapId.Nullspace;

        return mapComp.MapId;
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        MapId value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var uid = dependencies.Resolve<IEntityManager>().System<SharedMapSystem>().GetMap(value);
        return serializationManager.WriteValue(uid, alwaysWrite, context);
    }
}
