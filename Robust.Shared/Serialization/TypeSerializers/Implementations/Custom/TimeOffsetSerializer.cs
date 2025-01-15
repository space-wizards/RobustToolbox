using System;
using System.Globalization;
using Robust.Shared.EntitySerialization;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Serialization.Manager;
using Robust.Shared.Serialization.Markdown;
using Robust.Shared.Serialization.Markdown.Validation;
using Robust.Shared.Serialization.Markdown.Value;
using Robust.Shared.Serialization.TypeSerializers.Interfaces;
using Robust.Shared.Utility;

namespace Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

/// <summary>
/// This serializer offsets a timespan by the game's current time. If the entity is currently paused, the the offset will
/// instead be the time at which the entity was paused.
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
        if (context is {WritingReadingPrototypes: true})
            return TimeSpan.Zero;

        if (context is not EntityDeserializer {CurrentReadingEntity.PostInit: true} ctx)
            return TimeSpan.Zero;

        var timing = ctx.Timing;
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

    public DataNode Write(
        ISerializationManager serializationManager,
        TimeSpan value,
        IDependencyCollection dependencies,
        bool alwaysWrite = false,
        ISerializationContext? context = null)
    {
        if (context is not EntitySerializer serializer
            || serializer.WritingReadingPrototypes
            || !serializer.EntMan.TryGetComponent(serializer.CurrentEntity, out MetaDataComponent? meta)
            || meta.EntityLifeStage < EntityLifeStage.MapInitialized)
        {
            DebugTools.Assert(value == TimeSpan.Zero || context?.WritingReadingPrototypes != true,
                "non-zero time offsets in prototypes are not supported. If required, initialize offsets on map-init");
            return new ValueDataNode("0");
        }

        // We subtract the current time, unless the entity is paused, in which case we subtract the time at which
        // it was paused.
        if (meta.PauseTime != null)
            value -= meta.PauseTime.Value;
        else
            value -= serializer.Timing.CurTime;

        return new ValueDataNode(value.TotalSeconds.ToString(CultureInfo.InvariantCulture));
    }
}
