using System;
using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Timing;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

/// <summary>
/// Offsets the timespan by the CurTime.
/// </summary>
public sealed class TimeOffsetSerializer : ITypeSerializer<TimeSpan, ValueDataNode>
{
    public TimeSpan Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        bool skipHook,
        ISerializationContext? context = null, TimeSpan value = default)
    {
        var seconds = double.Parse(node.Value, CultureInfo.InvariantCulture);
        var curTime = dependencies.Resolve<IGameTiming>().CurTime;

        return curTime + TimeSpan.FromSeconds(seconds);
    }

    public ValidationNode Validate(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        ISerializationContext? context = null)
    {
        return double.TryParse(node.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out _)
            ? new ValidatedValueNode(node)
            : new ErrorNode(node, "Failed parsing TimeSpan");
    }

    public DataNode Write(ISerializationManager serializationManager, TimeSpan value, IDependencyCollection dependencies, bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        var curTime = dependencies.Resolve<IGameTiming>().CurTime;
        return new ValueDataNode((value - curTime).TotalSeconds.ToString(CultureInfo.InvariantCulture));
    }

    [MustUseReturnValue]
    public TimeSpan Copy(ISerializationManager serializationManager, TimeSpan source, TimeSpan target,
        bool skipHook,
        ISerializationContext? context = null)
    {
        return source;
    }
}
