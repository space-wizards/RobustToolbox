using System;
using System.Globalization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

/// <summary>
/// This serializer offsets a timespan by the game's current time. If the entity is currently paused, the pause time
/// will also be accounted for,
/// </summary>
/// <remarks>
/// Prototypes and pre map-init entities will always serialize this as zero. This is done mainly as a brute force fix
/// to prevent time-offsets from being unintentionally saved to maps while mapping. If an entity must have an initial
/// non-zero time, then that time should just be configured during map-init.
/// </remarks>
public sealed class TimeOffsetSerializer : ITypeSerializer<TimeSpan, ValueDataNode>
{
    public TimeSpan Read(ISerializationManager serializationManager, ValueDataNode node,
        IDependencyCollection dependencies,
        SerializationHookContext hookCtx,
        ISerializationContext? context = null,
        ISerializationManager.InstantiationDelegate<TimeSpan>? instanceProvider = null)
    {
        // Caveat: if non-zero times are to be supported for prototypes, then this method needs to be changed so that
        // the time is added when copying, instead of when reading.
        if (context == null || context.WritingReadingPrototypes)
            return TimeSpan.Zero;

        IGameTiming? timing = null;
        if (context is MapSerializationContext mapContext)
        {
            if (!mapContext.MapInitialized)
                return TimeSpan.Zero;
            timing = mapContext.Timing;
        }

        timing ??= dependencies.Resolve<IGameTiming>();
        var seconds = double.Parse(node.Value, CultureInfo.InvariantCulture);
        return TimeSpan.FromSeconds(seconds) + timing.CurTime;
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
            DebugTools.Assert(value == TimeSpan.Zero,
                "non-zero time offsets in prototypes are not supported. If required, initialize offsets on map-init");

            return new ValueDataNode("0");
        }

        if (context is not MapSerializationContext mapContext)
        {
            value -= dependencies.Resolve<IGameTiming>().CurTime;
            return new ValueDataNode(value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
        }

        if (!mapContext.MapInitialized)
            return new ValueDataNode("0");

        if (mapContext.EntityManager.TryGetComponent(mapContext.CurrentWritingEntity, out MetaDataComponent? meta))
        {
            // Here, PauseTime is a time -- not a duration.
            if (meta.PauseTime != null)
                value -= meta.PauseTime.Value;
        }
        else
        {
            // But here, PauseTime is a duration instead of a time
            // What jolly fun.
            value = value - mapContext.Timing.CurTime + mapContext.PauseTime;
        }

        return new ValueDataNode(value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
    }
}
