using Robust.Shared.IoC;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using static Robust.Shared.Serialization.Manager.ISerializationManager;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Generic;

/// <summary>
///     Serializer used automatically for <see cref="ProtoId{T}"/> types.
/// </summary>
/// <typeparam name="T">The type of the prototype for which the id is stored.</typeparam>
[TypeSerializer]
public sealed class ProtoIdSerializer<T> : ITypeSerializer<ProtoId<T>, ValueDataNode>, ITypeCopyCreator<ProtoId<T>> where T : class, IPrototype
{
    public ValidationNode Validate(ISerializationManager serialization, ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        var prototypes = dependencies.Resolve<IPrototypeManager>();
        if (prototypes.TryGetKindFrom<T>(out _) && prototypes.HasMapping<T>(node.Value))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, $"No {typeof(T)} found with id {node.Value}");
    }

    public ProtoId<T> Read(ISerializationManager serialization, ValueDataNode node, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null, InstantiationDelegate<ProtoId<T>>? instanceProvider = null)
    {
        return new ProtoId<T>(node.Value);
    }

    public DataNode Write(ISerializationManager serialization, ProtoId<T> value, IDependencyCollection dependencies, bool alwaysWrite = false, ISerializationContext? context = null)
    {
        return new ValueDataNode(value.Id);
    }

    public ProtoId<T> CreateCopy(ISerializationManager serializationManager, ProtoId<T> source, IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        return source;
    }
}
