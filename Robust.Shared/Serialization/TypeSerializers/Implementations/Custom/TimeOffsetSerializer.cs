using System;
using System.Globalization;
using JetBrains.Annotations;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Timing;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

/// <summary>
/// Offsets the timespan by the CurTime.
/// </summary>
public sealed class TimeOffsetSerializer : ITypeSerializer<TimeSpan, ValueDataNode>, ITypeCopier<TimeSpan>
{
    public TimeSpan Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TimeSpan>? instanceProvider = null)
    {
        var seconds = double.Parse(node.Value, CultureInfo.InvariantCulture);
        return TimeSpan.FromSeconds(seconds);
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
        // If we're reading from the prototype (e.g. for diffs) then ignore.
        if (context == null || context.WritingReadingPrototypes)
        {
            return new ValueDataNode(value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }

        var curTime = dependencies.Resolve<IGameTiming>().CurTime;

        // We want to get the offset relative to the current time
        // Because paused entities never update their timeoffsets we'll subtract how long it's been paused.
        if (context is MapSerializationContext map)
        {
            curTime -= map.PauseTime;
        }

        return new ValueDataNode((value - curTime).TotalSeconds.ToString(CultureInfo.InvariantCulture));
    }


    public void CopyTo(ISerializationManager serializationManager, TimeSpan source, ref TimeSpan target,
        IDependencyCollection dependencies, SerializationHookContext hookCtx, ISerializationContext? context = null)
    {
        target = source + dependencies.Resolve<IGameTiming>().CurTime;
    }
}
