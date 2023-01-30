using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.ViewVariables;

internal sealed class ViewVariablesSerializationContext : ISerializationContext, ITypeSerializer<EntityUid, ValueDataNode>
{
    public SerializationManager.SerializerProvider SerializerProvider { get; } = new();

    public ViewVariablesSerializationContext()
    {
        SerializerProvider.RegisterSerializer(this);
    }

    ValidationNode ITypeValidator<EntityUid, ValueDataNode>.Validate(ISerializationManager serializationManager,
        ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
    {
        return new ValidatedValueNode(node);
    }

    public DataNode Write(ISerializationManager serializationManager, EntityUid value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value.ToString());
    }

    EntityUid ITypeReader<EntityUid, ValueDataNode>.Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context, ISerializationManager.InstantiationDelegate<EntityUid>? _)
    {
        if (node.Value == "invalid")
        {
            return EntityUid.Invalid;
        }

        return EntityUid.Parse(node.Value);
    }
}
