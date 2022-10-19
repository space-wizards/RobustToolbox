using System;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

public sealed class EntityLocDataSerializer : ITypeSerializer<EntityLocData, MappingDataNode>
{
    private PrototypeIdSerializer<EntityPrototype> _prototype = new();
    public ValidationNode Validate(ISerializationManager serializationManager, MappingDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return node.TryGet<ValueDataNode>("id", out var idNode)
            ? _prototype.Validate(serializationManager, idNode, dependencies, context)
            : new ErrorNode(node, "No id found.");
    }

    public EntityLocData Read(ISerializationManager serializationManager, MappingDataNode node, IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, EntityLocData value = default)
    {
        var loc = dependencies.Resolve<ILocalizationManager>();
        var id = node.Get<ValueDataNode>("id");
        return loc.GetEntityData(id.Value);
    }

    public DataNode Write(ISerializationManager serializationManager, EntityLocData value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        throw new NotSupportedException();
    }

    public EntityLocData Copy(ISerializationManager serializationManager, EntityLocData source, EntityLocData target,
        bool skipHook, ISerializationContext? context = null)
    {
        return source;
    }
}
