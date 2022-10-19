using System;
using Robust.Shared.IoC;
using Robust.Shared.Localization;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

public sealed class LocStringSerializer : ITypeSerializer<string, ValueDataNode>
{
    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies, ISerializationContext? context = null)
    {
        //todo validate fluent ids
        return new ValidatedValueNode(node);
    }

    public string Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        bool skipHook, ISerializationContext? context = null, string? value = default)
    {
        return dependencies.Resolve<ILocalizationManager>().GetString(node.Value);
    }

    public DataNode Write(ISerializationManager serializationManager, string value, IDependencyCollection dependencies,
        bool alwaysWrite = false, ISerializationContext? context = null)
    {
        throw new NotSupportedException("FluentStrings can currently not be serialized");
    }

    public string Copy(ISerializationManager serializationManager, string source, string target, bool skipHook,
        ISerializationContext? context = null)
    {
        return source;
    }
}
