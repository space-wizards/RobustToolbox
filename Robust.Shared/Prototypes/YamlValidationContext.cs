using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Prototypes;

internal sealed class YamlValidationContext :
    ISerializationContext,
    ITypeSerializer<EntityUid, ValueDataNode>,
    ITypeSerializer<NetEntity, ValueDataNode>
{
    public SerializationManager.SerializerProvider SerializerProvider { get; } = new();
    public bool WritingReadingPrototypes => true;

    public YamlValidationContext()
    {
        SerializerProvider.RegisterSerializer(this);
    }

    ValidationNode ITypeValidator<EntityUid, ValueDataNode>.Validate(ISerializationManager serializationManager,
        ValueDataNode node, IDependencyCollection dependencies, ISerializationContext? context)
    {
        if (node.Value == "invalid")
            return new ValidatedValueNode(node);

        return new ErrorNode(node, "Prototypes should not contain EntityUids", true);
    }

    public DataNode Write(ISerializationManager serializationManager, EntityUid value,
        IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        if (!value.Valid)
            return new ValueDataNode("invalid");

        return new ValueDataNode(value.Id.ToString(CultureInfo.InvariantCulture));
    }

    EntityUid ITypeReader<EntityUid, ValueDataNode>.Read(ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context, ISerializationManager.InstantiationDelegate<EntityUid>? _)
    {
        if (node.Value == "invalid")
            return EntityUid.Invalid;

        return EntityUid.Parse(node.Value);
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (node.Value == "invalid")
            return new ValidatedValueNode(node);

        return new ErrorNode(node, "Prototypes should not contain NetEntities");
    }

    public NetEntity Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<NetEntity>? instanceProvider = null)
    {
        if (node.Value == "invalid")
            return NetEntity.Invalid;

        return NetEntity.Parse(node.Value);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        NetEntity value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        if (!value.Valid)
            return new ValueDataNode("invalid");

        return new ValueDataNode(value.Id.ToString(CultureInfo.InvariantCulture));
    }
}
