using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Primitive;

/// <summary>
/// Implementation of type serializers for <see cref="UnsafeFloat"/> and <see cref="UnsafeDouble"/>.
/// </summary>
/// <remarks>
/// These don't need to do anything different from <see cref="FloatSerializer"/> and <see cref="DoubleSerializer"/>,
/// because YAML cannot contain NaNs.
/// </remarks>
[TypeSerializer]
internal sealed class UnsafeFloatSerializer :
    ITypeSerializer<UnsafeFloat, ValueDataNode>, ITypeCopyCreator<UnsafeFloat>,
    ITypeSerializer<UnsafeDouble, ValueDataNode>, ITypeCopyCreator<UnsafeDouble>
{
    ValidationNode ITypeValidator<UnsafeFloat, ValueDataNode>.Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        return serializationManager.ValidateNode<float>(node, context);
    }

    public UnsafeFloat Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<UnsafeFloat>? instanceProvider = null)
    {
        return serializationManager.Read<float>(node, hookCtx, context);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        UnsafeFloat value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return serializationManager.WriteValue(value.Value, alwaysWrite, context);
    }

    ValidationNode ITypeValidator<UnsafeDouble, ValueDataNode>.Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context)
    {
        return serializationManager.ValidateNode<double>(node, context);
    }

    public UnsafeDouble Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<UnsafeDouble>? instanceProvider = null)
    {
        return serializationManager.Read<double>(node, hookCtx, context);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        UnsafeDouble value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return serializationManager.WriteValue(value.Value, alwaysWrite, context);
    }

    public UnsafeFloat CreateCopy(
        ISerializationManager serializationManager,
        UnsafeFloat source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        return source;
    }

    public UnsafeDouble CreateCopy(
        ISerializationManager serializationManager,
        UnsafeDouble source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        return source;
    }
}
