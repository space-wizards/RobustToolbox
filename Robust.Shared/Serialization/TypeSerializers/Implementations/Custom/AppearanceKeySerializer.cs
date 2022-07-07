using System;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

/// <summary>
///     Attempts to resolve a string into an enum. If it fails, it simply reads the string. Useful for both both sprite
///     layer and appearance data keys, which both simultaneously support enums and strings. 
/// </summary>
public sealed class AppearanceKeySerializer : ITypeSerializer<object, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        // TODO PERFORMACNE find a way to have serialization manager pass IReflectionManager into type serializers
        // See also instances where IPrototypeManager gets resolved (e.g., sprite specifier serializers).

        var refMan = dependencies.Resolve<IReflectionManager>();

        // Even though literally any value data node value could be resolved into a string, we assume that if it starts
        // with "enum.", it is intended to be resolved into an enum.

        if (!node.Value.StartsWith("enum.") || refMan.TryParseEnumReference(node.Value, out var _, false))
            return new ValidatedValueNode(node);

        return new ErrorNode(node, $"Failed to parse enum {node.Value}");
    }

    public object Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null, object? value = null)
    {
        if (serializationManager.ReflectionManager.TryParseEnumReference(node.Value, out var @enum))
            return @enum;
        else
            return node.Value;
    }

    public DataNode Write(ISerializationManager serializationManager, object value, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        if (value is string str)
            return new ValueDataNode(str);
        else if (value is Enum @enum)
            return new ValueDataNode(serializationManager.ReflectionManager.GetEnumReference(@enum));

        throw new InvalidOperationException($"enum string serializer objects must be either enums or strings, but object was {value.GetType()}");
    }

    public object Copy(ISerializationManager serializationManager, object source, object target, bool skipHook,
        ISerializationContext? context = null)
    {
        return source;
    }
}

