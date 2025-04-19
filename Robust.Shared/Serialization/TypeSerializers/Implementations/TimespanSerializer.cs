using System;
using System.Collections.Generic;
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
        return double.TryParse(node.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
               || TryTimeSpan(node, out _)
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
    /// Convert strings from the compatible format into TimeSpan and output it. Returns true if successful.
    /// The string must start with a number and end with a single letter referring to the time unit used.
    /// It can NOT combine multiple types (like "1h30m"), but it CAN use decimals ("1.5h")
    /// </summary>
    private bool TryTimeSpan(ValueDataNode node, [NotNullWhen(true)] out TimeSpan? timeSpan)
    {
        timeSpan = null;
        var units = new List<string>{"h", "m", "s"};

        var input = node.Value.Replace(" ","");
        var unit = input[^1].ToString();
        var number = input.Substring(0, input.Length - 1);
        var valid = false;

        if (units.Contains(unit)
            && double.TryParse(number, out var d))
        {
            switch (unit)
            {
                case "s":
                    timeSpan = TimeSpan.FromSeconds(d);
                    valid = true;
                    break;
                case "m":
                    timeSpan = TimeSpan.FromMinutes(d);
                    valid = true;
                    break;
                case "h":
                    timeSpan = TimeSpan.FromHours(d);
                    valid = true;
                    break;
            }
        }

        return valid;
    }
}
