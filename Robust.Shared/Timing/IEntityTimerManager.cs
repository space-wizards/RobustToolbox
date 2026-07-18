using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

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

/// <summary>
/// Maintains a runtime index of timers owned by components on entities.
/// </summary>
/// <remarks>
/// This manager does not serialize timer state. Components that need persistence or prediction rollback must retain
/// their deadline and call <see cref="SetTimerAt{TComponent}"/> after initialization, loading, or state application.
/// All methods must be called from the main simulation thread.
/// </remarks>
[NotContentImplementable]
public interface IEntityTimerManager
{
    /// <summary>
    /// Schedule or replace a timer relative to the current simulation time.
    /// </summary>
    /// <returns>The absolute simulation-time deadline.</returns>
    TimeSpan SetTimer<TComponent>(
        Entity<TComponent> owner,
        EntityTimerId id,
        TimeSpan delay,
        TimeSpan? interval = null,
        EntityTimerFlags flags = EntityTimerFlags.None)
        where TComponent : IComponent;

    /// <summary>
    /// Schedule or replace a timer at an absolute simulation-time deadline.
    /// </summary>
    void SetTimerAt<TComponent>(
        Entity<TComponent> owner,
        EntityTimerId id,
        TimeSpan deadline,
        TimeSpan? interval = null,
        EntityTimerFlags flags = EntityTimerFlags.None)
        where TComponent : IComponent;

    bool CancelTimer<TComponent>(EntityUid owner, EntityTimerId id)
        where TComponent : IComponent;

    int CancelTimers<TComponent>(EntityUid owner)
        where TComponent : IComponent;

    int CancelTimers(EntityUid owner);

    bool TryGetTimer<TComponent>(
        EntityUid owner,
        EntityTimerId id,
        out EntityTimerInfo timer)
        where TComponent : IComponent;

    /// <summary>
    /// Engine lifecycle hook. Content must not call this method.
    /// </summary>
    void Initialize();

    /// <summary>
    /// Engine lifecycle hook. Content must not call this method.
    /// </summary>
    void Shutdown();

    /// <summary>
    /// Engine tick hook. Content must not call this method.
    /// </summary>
    void UpdateTimers(bool noPredictions);
}
