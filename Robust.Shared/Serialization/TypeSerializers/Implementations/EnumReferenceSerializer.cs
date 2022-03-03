using System;
using System.Runtime.InteropServices.ComTypes;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Manager.Result;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

[TypeSerializer]
public sealed class EnumReferenceSerializer : ITypeReader<Enum, ValueDataNode>
{
    public DeserializationResult Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, bool skipHook, ISerializationContext? context = null)
    {
        var deserializedEnumRef = dependencies.Resolve<IReflectionManager>()
            .TryParseEnumReference(node.Value, out var enumRef) ? enumRef : null;
        return new DeserializedValue<Enum>(deserializedEnumRef!);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        return dependencies.Resolve<IReflectionManager>().TryParseEnumReference(node.Value, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Failed parsing EnumRef.");
    }
}
