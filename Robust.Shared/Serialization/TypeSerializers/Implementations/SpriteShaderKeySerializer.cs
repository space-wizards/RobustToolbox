using System;
using System.Globalization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

[TypeSerializer]
public sealed class SpriteShaderKeySerializer :
    ITypeSerializer<SpriteShaderKey, ValueDataNode>,
    ITypeCopyCreator<SpriteShaderKey>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return Parse.TryInt32(node.Value, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, $"Failed parsing sprite shader key render order (int) value: {node.Value}");
    }

    public SpriteShaderKey Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<SpriteShaderKey>? instanceProvider = null)
    {
        return new SpriteShaderKey(GenerateAutoId(), Parse.Int32(node.Value));
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        SpriteShaderKey value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value.RenderOrder.ToString(CultureInfo.InvariantCulture));
    }

    public SpriteShaderKey CreateCopy(
        ISerializationManager serializationManager,
        SpriteShaderKey source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        return new SpriteShaderKey(source.Id, source.RenderOrder);
    }

    private static string GenerateAutoId()
    {
        // Generate new guid and take 8 chars
        return $"{Guid.NewGuid():N}"[..8];
    }
}
