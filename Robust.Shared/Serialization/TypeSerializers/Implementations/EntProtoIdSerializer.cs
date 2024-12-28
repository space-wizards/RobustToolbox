using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Serialization.Markdown.Sequence;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using static Robust.Shared.Serialization.Manager.ISerializationManager;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

/// <summary>
///     Serializer used automatically for <see cref="EntProtoId"/> types.
/// </summary>
[TypeSerializer]
public sealed class EntProtoIdSerializer : ITypeSerializer<EntProtoId, ValueDataNode>, ITypeCopyCreator<EntProtoId>
{
    public ValidationNode Validate(ISerializationManager serialization, ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var prototypes = dependencies.Resolve<IPrototypeManager>();
        if (prototypes.TryGetKindFrom<EntityPrototype>(out _) && prototypes.HasMapping<EntityPrototype>(node.Value))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, $"No {nameof(EntityPrototype)} found with id {node.Value}");
    }

    public EntProtoId Read(ISerializationManager serialization, ValueDataNode node, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, InstantiationDelegate<EntProtoId>? instanceProvider = null)
    {
        return new EntProtoId(node.Value);
    }

    public DataNode Write(ISerializationManager serialization, EntProtoId value, IDependencyCollection dependencies, bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return new ValueDataNode(value.Id);
    }

    public EntProtoId CreateCopy(ISerializationManager serializationManager, EntProtoId source, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        return source;
    }
}

/// <summary>
///     Serializer used automatically for <see cref="EntProtoId"/> types.
/// </summary>
[TypeSerializer]
public sealed class EntProtoIdSerializer<T> : ITypeSerializer<EntProtoId<T>, ValueDataNode>, ITypeCopyCreator<EntProtoId<T>> where T : IComponent, new()
{
    public ValidationNode Validate(ISerializationManager serialization, ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var prototypes = dependencies.Resolve<IPrototypeManager>();
        if (!prototypes.TryGetKindFrom<EntityPrototype>(out _) || !prototypes.TryGetMapping(typeof(EntityPrototype), node.Value, out var mapping))
            return new ErrorNode(node, $"No {nameof(EntityPrototype)} found with id {node.Value} that has a {typeof(T).Name}");

        if (!mapping.TryGet("components", out SequenceDataNode? components))
            return new ErrorNode(node, $"{nameof(EntityPrototype)} {node.Value} doesn't have a {typeof(T).Name}.");

        var compFactory = dependencies.Resolve<IComponentFactory>();
        var registration = compFactory.GetRegistration<T>();
        foreach (var componentNode in components)
        {
            if (componentNode is MappingDataNode component &&
                component.TryGet("type", out ValueDataNode? compName) &&
                compName.Value == registration.Name)
            {
                return new ValidatedValueNode(node);
            }
        }

        return new ErrorNode(node, $"{nameof(EntityPrototype)} {node.Value} doesn't have a {typeof(T).Name}.");
    }

    public EntProtoId<T> Read(ISerializationManager serialization, ValueDataNode node, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, InstantiationDelegate<EntProtoId<T>>? instanceProvider = null)
    {
        return new EntProtoId<T>(node.Value);
    }

    public DataNode Write(ISerializationManager serialization, EntProtoId<T> value, IDependencyCollection dependencies, bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return new ValueDataNode(value.Id);
    }

    public EntProtoId<T> CreateCopy(ISerializationManager serializationManager, EntProtoId<T> source, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        return source;
    }
}
