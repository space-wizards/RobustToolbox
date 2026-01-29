using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Sprite;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

[TypeSerializer]
public sealed class LayerKeySerializer : ITypeSerializer<LayerKey, ValueDataNode>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        if (LayerKey.TryParse(serializationManager.ReflectionManager, node.Value, out _))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, $"Failed to parse enum {node.Value}");
    }

    public LayerKey Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<LayerKey>? instanceProvider = null)
    {
        return LayerKey.Parse(serializationManager.ReflectionManager, node.Value);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        LayerKey value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var raw = value.EnumKey != null
            ? serializationManager.ReflectionManager.GetEnumReference(value.EnumKey)
            : value.StringKey;
        return new ValueDataNode(raw);
    }
}

