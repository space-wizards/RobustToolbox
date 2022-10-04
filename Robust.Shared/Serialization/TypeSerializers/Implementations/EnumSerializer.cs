using System;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

/// <summary>
///     Attempts to resolve a string into an enum. If it fails, it simply reads the string. Useful for both both sprite
///     layer and appearance data keys, which both simultaneously support enums and strings. 
/// </summary>
[TypeSerializer]
public sealed class EnumSerializer : ITypeSerializer<Enum, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        if (serializationManager.ReflectionManager.TryParseEnumReference(node.Value, out var _, false))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, $"Failed to parse enum {node.Value}");
    }

    public Enum Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null, Enum? value = null)
    {
        if (serializationManager.ReflectionManager.TryParseEnumReference(node.Value, out var @enum))
            return @enum;

        throw new ArgumentException($"Failed to parse enum {node.Value}");
    }

    public DataNode Write(ISerializationManager serializationManager, Enum value, IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(serializationManager.ReflectionManager.GetEnumReference(value));
    }

    public Enum Copy(ISerializationManager serializationManager, Enum source, Enum target, bool skipHook,
        ISerializationContext? context = null)
    {
        return source;
    }
}

