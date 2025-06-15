using System.Globalization;
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
    ITypeSerializer<NetEntity, ValueDataNode>,
    ITypeSerializer<WeakEntityReference, ValueDataNode>
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

    ValidationNode ITypeValidator<NetEntity, ValueDataNode>.Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        return node.Value == "invalid"
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Prototypes should not contain NetEntities");
    }

    NetEntity ITypeReader<NetEntity, ValueDataNode>.Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<NetEntity>? instanceProvider)
    {
        return node.Value == "invalid" ? NetEntity.Invalid : NetEntity.Parse(node.Value);
    }

    DataNode ITypeWriter<NetEntity>.Write(
        ISerializationManager serializationManager,
        NetEntity value,
        IDependencyCollection dependencies,
        bool alwaysWrite,
        ISerializationContext? context)
    {
        return value.Valid
            ? new ValueDataNode(value.Id.ToString(CultureInfo.InvariantCulture))
            : new ValueDataNode("invalid");
    }

    ValidationNode ITypeValidator<WeakEntityReference, ValueDataNode>.Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        return node.Value == "invalid"
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Prototypes should not contain WeakEntityReferences");
    }

    WeakEntityReference ITypeReader<WeakEntityReference, ValueDataNode>.Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context,
        ISerializationManager.InstantiationDelegate<WeakEntityReference>? instanceProvider)
    {
        return node.Value == "invalid" ? WeakEntityReference.Invalid : new(NetEntity.Parse(node.Value));
    }

    DataNode ITypeWriter<WeakEntityReference>.Write(
        ISerializationManager serializationManager,
        WeakEntityReference value,
        IDependencyCollection dependencies,
        bool alwaysWrite,
        ISerializationContext? context)
    {
        return !value.Entity.Valid
            ? new ValueDataNode("invalid")
            : new ValueDataNode(value.Entity.Id.ToString(CultureInfo.InvariantCulture));
    }
}
