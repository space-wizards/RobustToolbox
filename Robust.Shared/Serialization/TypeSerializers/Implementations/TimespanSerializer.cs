using System;
using System.Diagnostics.CodeAnalysis;
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

[TypeSerializer]
public sealed class TimespanSerializer : ITypeSerializer<TimeSpan, ValueDataNode>, ITypeCopyCreator<TimeSpan>
{
    public TimeSpan Read(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TimeSpan>? instanceProvider = null)
    {
        if (TryTimeSpan(node, out var time))
            return time.Value;

        var seconds = double.Parse(node.Value, CultureInfo.InvariantCulture);
        return TimeSpan.FromSeconds(seconds);
    }

    public ValidationNode Validate(
        ISerializationManager serializationManager,
        ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return TryTimeSpan(node, out _)
            || double.TryParse(node.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Failed parsing TimeSpan");
    }

    public DataNode Write(
        ISerializationManager serializationManager,
        TimeSpan value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        return new ValueDataNode(value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
    }

    [MustUseReturnValue]
    public TimeSpan CreateCopy(
        ISerializationManager serializationManager,
        TimeSpan source,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null)
    {
        return source;
    }

    /// <summary>
    /// Convert strings from the compatible format (or just numbers) into TimeSpan and output it. Returns true if successful.
    /// The string must start with a number and end with a single letter referring to the time unit used.
    /// It can NOT combine multiple types (like "1h30m"), but it CAN use decimals ("1.5h")
    /// </summary>
    private bool TryTimeSpan(ValueDataNode node, [NotNullWhen(true)] out TimeSpan? timeSpan)
    {
        timeSpan = null;

        // A lot of the checks will be for plain numbers, so might as well rule them out right away, instead of
        // running all the other checks on them. They will need to get parsed later anyway, if they weren't now.
        if (double.TryParse(node.Value, out var v))
        {
            timeSpan = TimeSpan.FromSeconds(v);
            return true;
        }

        // If there aren't even enough characters for a number and a time unit, exit
        if (node.Value.Length <= 1)
            return false;

        // If the input without the last character is still not a valid number, exit
        if (!double.TryParse(node.Value.AsSpan()[..^1], out var number))
            return false;

        // Check the last character of the input for time unit indicators
        switch (node.Value[^1])
        {
            case 's':
                timeSpan = TimeSpan.FromSeconds(number);
                return true;
            case 'm':
                timeSpan = TimeSpan.FromMinutes(number);
                return true;
            case 'h':
                timeSpan = TimeSpan.FromHours(number);
                return true;
            default:
                return false;
        }
    }
}
