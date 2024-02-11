using System;
using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations;

/// <summary>
/// Implements serialization for <see cref="DateTime"/>.
/// </summary>
/// <remarks>
/// Serialization is implemented with <see cref="DateTimeStyles.RoundtripKind"/> and the "o" format specifier.
/// </remarks>
[TypeSerializer]
public sealed class DateTimeSerializer : ITypeSerializer<DateTime, ValueDataNode>, ITypeCopyCreator<DateTime>
{
    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return DateTime.TryParse(node.Value, null, DateTimeStyles.RoundtripKind, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Failed parsing DateTime");
    }

    public DateTime Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<DateTime>? instanceProvider = null)
    {
        return DateTime.Parse(node.Value, null, DateTimeStyles.RoundtripKind);
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        DateTime value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value.ToString("o"));
    }

    [MustUseReturnValue]
    public DateTime CreateCopy(
        ISerializationManager serializationManager,
        DateTime source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        return source;
    }
}
