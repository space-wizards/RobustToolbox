using Robust.Shared.Serialization.Manager.Attributes;

namespace Robust.Shared.GameObjects;

/// <summary>
///     Base method for all events that should be relayed through an entity timer.
///     Required to use <see cref="EntityTimerEvent{TEvent}"/>.
///
///     Since this event will be stored for some amount of time and may need to be serialized,
///     its fields should be DataFields.
/// </summary>
[ImplicitDataDefinitionForInheritors]
public abstract partial class BaseEntityTimerEvent
{
}

[ImplicitDataDefinitionForInheritors]
public partial interface IEntityTimerEvent
{
}

/// <summary>
///     Wrapper class event for a <see cref="TEvent"/> event to be raised on timer fire.
/// </summary>
public sealed partial class EntityTimerEvent<TEvent> : IEntityTimerEvent
    where TEvent: BaseEntityTimerEvent
{
    [DataField]
    public TEvent Data;

    [DataField]
    public EntityUid Timer;

    public EntityTimerEvent(TEvent data, EntityUid timer)
    {
        Data = data;
        Timer = timer;
    }
}

/// <summary>
///     Wrapper class event for a repeating <see cref="TEvent"/> event. Contains fields to modify
///     next repeated timer behavior when handled.
/// </summary>
public sealed partial class RepeatingEntityTimerEvent<TEvent> : IEntityTimerEvent
    where TEvent: BaseEntityTimerEvent
{
    [DataField]
    public TEvent Data;

    [DataField]
    public EntityUid Timer;

    public RepeatingEntityTimerEvent(TEvent data, EntityUid timer)
    {
        Data = data;
        Timer = timer;
    }
}
