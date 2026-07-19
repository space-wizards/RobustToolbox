using System;
using Robust.Shared.GameObjects;

namespace Robust.Shared.Timing;

/// <summary>
/// Identifies a timer owned by a component on an entity.
/// </summary>
public readonly record struct EntityTimerId(string Value);

/// <summary>
/// Controls how an entity timer is updated.
/// </summary>
[Flags]
public enum EntityTimerFlags : byte
{
    None = 0,

    /// <summary>
    /// Continue counting and dispatch the timer while its owning entity is paused.
    /// </summary>
    IgnoreEntityPause = 1 << 0,

    /// <summary>
    /// Process this timer when the owning system would update outside client prediction.
    /// </summary>
    UpdatesOutsidePrediction = 1 << 1,
}

/// <summary>
/// Runtime information about a scheduled entity timer.
/// </summary>
public readonly record struct EntityTimerInfo(
    TimeSpan Deadline,
    TimeSpan Remaining,
    TimeSpan? Interval,
    bool Suspended);

/// <summary>
/// Raised directly on the component that owns an elapsed entity timer.
/// </summary>
/// <remarks>
/// <see cref="ElapsedCount"/> is greater than one if several repetitions elapsed between updates.
/// Repeating timers preserve their original phase and are rescheduled before this event is raised.
/// </remarks>
[ByRefEvent, ComponentEvent]
public readonly record struct EntityTimerEvent(
    EntityTimerId Id,
    TimeSpan ScheduledTime,
    TimeSpan FiredAt,
    TimeSpan? NextDeadline,
    uint ElapsedCount);
