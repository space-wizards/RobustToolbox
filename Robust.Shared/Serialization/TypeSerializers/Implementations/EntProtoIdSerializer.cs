using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
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
        if (prototypes.HasMapping<EntityPrototype>(node.Value))
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
