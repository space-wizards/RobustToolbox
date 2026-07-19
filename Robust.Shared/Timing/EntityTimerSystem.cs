using System;
using System.Collections.Generic;
#if EXCEPTION_TOLERANCE
using Robust.Shared.Exceptions;
#endif
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;

namespace Robust.Shared.Timing;

/// <summary>
/// Maintains a runtime index of timers owned by components on entities.
/// </summary>
/// <remarks>
/// This system does not serialize timer state. Components that need persistence or prediction rollback must retain
/// their deadline and call <see cref="SetTimerAt{TComponent}"/> after initialization, loading, or state application.
/// All methods must be called from the main simulation thread.
/// </remarks>
public abstract partial class EntityTimerSystem : EntitySystem
{
    [Dependency] private IGameTiming _timing = default!;
#if EXCEPTION_TOLERANCE
    [Dependency] private IRuntimeLog _runtimeLog = default!;
#endif

    private readonly PriorityQueue<QueueEntry, (long Deadline, ulong Sequence)> _predictedQueue = new();
    private readonly PriorityQueue<QueueEntry, (long Deadline, ulong Sequence)> _outsidePredictionQueue = new();
    private readonly Dictionary<EntityTimerKey, TimerData> _timers = new();
    private readonly Dictionary<EntityUid, HashSet<EntityTimerKey>> _entityTimers = new();

    private ulong _nextGeneration;
    private ulong _nextSequence;
    private bool _updating;

    public override void Initialize()
    {
        base.Initialize();
        EntityManager.ComponentRemoved += OnComponentRemoved;
        EntityManager.EntityDeleted += OnEntityDeleted;
        EntityManager.BeforeEntityFlush += OnBeforeEntityFlush;
        SubscribeLocalEvent<MetaDataComponent, EntityPausedEvent>(OnEntityPaused);
        SubscribeLocalEvent<MetaDataComponent, EntityUnpausedEvent>(OnEntityUnpaused);
    }

    public override void Shutdown()
    {
        EntityManager.ComponentRemoved -= OnComponentRemoved;
        EntityManager.EntityDeleted -= OnEntityDeleted;
        EntityManager.BeforeEntityFlush -= OnBeforeEntityFlush;
        Clear();
        base.Shutdown();
    }

    /// <summary>
    /// Schedule or replace a timer relative to the current simulation time.
    /// </summary>
    /// <returns>The absolute simulation-time deadline.</returns>

    public TimeSpan SetTimer<TComponent>(
        Entity<TComponent> owner,
        EntityTimerId id,
        TimeSpan delay,
        TimeSpan? interval = null,
        EntityTimerFlags flags = EntityTimerFlags.None)
        where TComponent : IComponent
    {
        if (delay < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(delay), "Entity timer delay cannot be negative.");

        var deadline = SafeAdd(_timing.CurTime, delay);
        SetTimerAt(owner, id, deadline, interval, flags);
        return deadline;
    }

    public void SetTimerAt<TComponent>(
        Entity<TComponent> owner,
        EntityTimerId id,
        TimeSpan deadline,
        TimeSpan? interval = null,
        EntityTimerFlags flags = EntityTimerFlags.None)
        where TComponent : IComponent
    {
        ValidateTimer(owner, id, deadline, interval);
        var now = _timing.CurTime;

        var key = new EntityTimerKey(owner.Owner, CompIdx.Index<TComponent>(), id);
        var data = new TimerData(
            key,
            deadline,
            interval,
            flags,
            ++_nextGeneration,
            ++_nextSequence);
        if (_updating && deadline <= now)
            data.NotBeforeTick = NextTick(_timing.CurTick);

        if ((flags & EntityTimerFlags.IgnoreEntityPause) == 0 &&
            TryComp(owner.Owner, out MetaDataComponent? metadata) &&
            metadata.EntityPaused)
        {
            data.Suspended = true;
            data.Remaining = Max(deadline - now, TimeSpan.Zero);
        }

        _timers[key] = data;
        if (!_entityTimers.TryGetValue(owner.Owner, out var keys))
        {
            keys = new HashSet<EntityTimerKey>();
            _entityTimers.Add(owner.Owner, keys);
        }

        keys.Add(key);
        if (!data.Suspended)
            Enqueue(data);
    }

    public bool CancelTimer<TComponent>(EntityUid owner, EntityTimerId id)
        where TComponent : IComponent
    {
        return RemoveTimer(new EntityTimerKey(owner, CompIdx.Index<TComponent>(), id));
    }

    public int CancelTimers<TComponent>(EntityUid owner)
        where TComponent : IComponent
    {
        if (!_entityTimers.TryGetValue(owner, out var keys))
            return 0;

        var component = CompIdx.Index<TComponent>();
        var count = 0;
        foreach (var key in keys)
        {
            if (key.Component != component)
                continue;

            if (_timers.Remove(key))
                count++;
        }

        keys.RemoveWhere(key => key.Component == component);
        if (keys.Count == 0)
            _entityTimers.Remove(owner);

        return count;
    }

    public int CancelTimers(EntityUid owner)
    {
        if (!_entityTimers.Remove(owner, out var keys))
            return 0;

        var count = 0;
        foreach (var key in keys)
        {
            if (_timers.Remove(key))
                count++;
        }

        return count;
    }

    public bool TryGetTimer<TComponent>(
        EntityUid owner,
        EntityTimerId id,
        out EntityTimerInfo timer)
        where TComponent : IComponent
    {
        var key = new EntityTimerKey(owner, CompIdx.Index<TComponent>(), id);
        if (!_timers.TryGetValue(key, out var data))
        {
            timer = default;
            return false;
        }

        var remaining = data.Suspended
            ? data.Remaining
            : Max(data.Deadline - _timing.CurTime, TimeSpan.Zero);
        timer = new EntityTimerInfo(data.Deadline, remaining, data.Interval, data.Suspended);
        return true;
    }

    public void UpdateTimers(bool noPredictions)
    {
        var (processPredicted, processOutsidePrediction) = GetQueuesToProcess(noPredictions);
        if (!processPredicted && !processOutsidePrediction)
            return;

        if (_updating)
            throw new InvalidOperationException("Entity timers cannot be updated recursively.");

        _updating = true;
        try
        {
            ProcessQueues(processPredicted, processOutsidePrediction);
        }
        finally
        {
            _updating = false;
        }
    }

    /// <summary>
    /// Select which queues should be processed for this platform and tick.
    /// </summary>
    protected virtual (bool Predicted, bool OutsidePrediction) GetQueuesToProcess(bool noPredictions)
    {
        return (true, true);
    }

    private void ProcessQueues(bool processPredicted, bool processOutsidePrediction)
    {
        var now = _timing.CurTime;
        var tick = _timing.CurTick;
        var sequenceWatermark = _nextSequence;
        List<DeferredEntry>? deferred = null;

        while (TryPeekNext(processPredicted, processOutsidePrediction, out var outsidePrediction, out var priority) &&
               priority.Deadline <= now.Ticks)
        {
            var queue = outsidePrediction ? _outsidePredictionQueue : _predictedQueue;
            queue.TryDequeue(out var entry, out priority);

            if (!_timers.TryGetValue(entry.Key, out var data) ||
                data.Generation != entry.Generation ||
                data.Suspended)
            {
                continue;
            }

            // Timers installed by a callback are never dispatched recursively or again in the same tick.
            if (priority.Sequence > sequenceWatermark || data.NotBeforeTick > tick)
            {
                deferred ??= new List<DeferredEntry>();
                deferred.Add(new DeferredEntry(entry, priority, outsidePrediction));
                continue;
            }

            Dispatch(data, now);
        }

        if (deferred == null)
            return;

        foreach (var entry in deferred)
        {
            var queue = entry.OutsidePrediction ? _outsidePredictionQueue : _predictedQueue;
            queue.Enqueue(entry.Entry, entry.Priority);
        }
    }

    private bool TryPeekNext(
        bool processPredicted,
        bool processOutsidePrediction,
        out bool outsidePrediction,
        out (long Deadline, ulong Sequence) priority)
    {
        (long Deadline, ulong Sequence) predictedPriority = default;
        (long Deadline, ulong Sequence) outsidePriority = default;
        var hasPredicted = processPredicted && _predictedQueue.TryPeek(out _, out predictedPriority);
        var hasOutside = processOutsidePrediction &&
                         _outsidePredictionQueue.TryPeek(out _, out outsidePriority);

        if (!hasPredicted && !hasOutside)
        {
            outsidePrediction = false;
            priority = default;
            return false;
        }

        if (!hasOutside || hasPredicted && predictedPriority.CompareTo(outsidePriority) <= 0)
        {
            outsidePrediction = false;
            priority = predictedPriority;
            return true;
        }

        outsidePrediction = true;
        priority = outsidePriority;
        return true;
    }

    private void Dispatch(TimerData data, TimeSpan now)
    {
        var scheduledTime = data.Deadline;
        TimeSpan? nextDeadline = null;
        uint elapsedCount = 1;

        if (data.Interval is { } interval)
        {
            var elapsed = (now.Ticks - scheduledTime.Ticks) / interval.Ticks + 1;
            elapsedCount = elapsed > uint.MaxValue ? uint.MaxValue : (uint) elapsed;

            var remainder = (now.Ticks - scheduledTime.Ticks) % interval.Ticks;
            nextDeadline = SafeAdd(now, TimeSpan.FromTicks(interval.Ticks - remainder));
            data.Deadline = nextDeadline.Value;
            data.Generation = ++_nextGeneration;
            data.Sequence = ++_nextSequence;
            Enqueue(data);
        }
        else
        {
            RemoveTimer(data.Key);
        }

        if (!EntityManager.TryGetComponent(data.Key.Owner, data.Key.Component, out var component))
        {
            RemoveTimer(data.Key);
            return;
        }

        var ev = new EntityTimerEvent(
            data.Key.Id,
            scheduledTime,
            now,
            nextDeadline,
            elapsedCount);

#if EXCEPTION_TOLERANCE
        try
        {
#endif
            EntityManager.EventBus.RaiseComponentEvent(data.Key.Owner, component, data.Key.Component, ref ev);
#if EXCEPTION_TOLERANCE
        }
        catch (Exception e)
        {
            _runtimeLog.LogException(e, "Entity timer callback");
        }
#endif
    }

    private void OnEntityPaused(EntityUid uid, MetaDataComponent component, ref EntityPausedEvent args)
    {
        if (!_entityTimers.TryGetValue(uid, out var keys))
            return;

        var now = _timing.CurTime;
        foreach (var key in keys)
        {
            var data = _timers[key];
            if (data.Suspended || (data.Flags & EntityTimerFlags.IgnoreEntityPause) != 0)
                continue;

            data.Remaining = Max(data.Deadline - now, TimeSpan.Zero);
            data.Suspended = true;
            data.Generation = ++_nextGeneration;
        }
    }

    private void OnEntityUnpaused(EntityUid uid, MetaDataComponent component, ref EntityUnpausedEvent args)
    {
        if (!_entityTimers.TryGetValue(uid, out var keys))
            return;

        var now = _timing.CurTime;
        foreach (var key in keys)
        {
            var data = _timers[key];
            if (!data.Suspended)
                continue;

            data.Deadline = SafeAdd(now, data.Remaining);
            data.Suspended = false;
            data.Generation = ++_nextGeneration;
            data.Sequence = ++_nextSequence;
            Enqueue(data);
        }
    }

    private void OnComponentRemoved(RemovedComponentEventArgs args)
    {
        if (!_entityTimers.TryGetValue(args.BaseArgs.Owner, out var keys))
            return;

        var count = keys.RemoveWhere(key =>
        {
            if (key.Component != args.Idx)
                return false;

            _timers.Remove(key);
            return true;
        });

        if (count > 0 && keys.Count == 0)
            _entityTimers.Remove(args.BaseArgs.Owner);
    }

    private void OnEntityDeleted(Entity<MetaDataComponent> entity)
    {
        CancelTimers(entity.Owner);
    }

    private void OnBeforeEntityFlush()
    {
        Clear();
    }

    private void Enqueue(TimerData data)
    {
        var queue = (data.Flags & EntityTimerFlags.UpdatesOutsidePrediction) != 0
            ? _outsidePredictionQueue
            : _predictedQueue;
        queue.Enqueue(
            new QueueEntry(data.Key, data.Generation),
            (data.Deadline.Ticks, data.Sequence));
    }

    private bool RemoveTimer(EntityTimerKey key)
    {
        if (!_timers.Remove(key))
            return false;

        if (_entityTimers.TryGetValue(key.Owner, out var keys))
        {
            keys.Remove(key);
            if (keys.Count == 0)
                _entityTimers.Remove(key.Owner);
        }

        return true;
    }

    private void Clear()
    {
        _predictedQueue.Clear();
        _outsidePredictionQueue.Clear();
        _timers.Clear();
        _entityTimers.Clear();
    }

    private void ValidateTimer<TComponent>(
        Entity<TComponent> owner,
        EntityTimerId id,
        TimeSpan deadline,
        TimeSpan? interval)
        where TComponent : IComponent
    {
        if (owner.Comp is null ||
            owner.Comp.Deleted ||
            !TryComp(owner.Owner, out TComponent? current) ||
            !ReferenceEquals(owner.Comp, current))
        {
            throw new ArgumentException("The entity timer owner is not a live component.", nameof(owner));
        }

        if (string.IsNullOrWhiteSpace(id.Value))
            throw new ArgumentException("Entity timer IDs cannot be empty.", nameof(id));
        if (deadline < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(deadline), "Entity timer deadline cannot be negative.");
        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), "Entity timer interval must be positive.");
    }

    private static TimeSpan SafeAdd(TimeSpan left, TimeSpan right)
    {
        if (right > TimeSpan.Zero && left > TimeSpan.MaxValue - right)
            return TimeSpan.MaxValue;
        if (right < TimeSpan.Zero && left < TimeSpan.MinValue - right)
            return TimeSpan.MinValue;
        return left + right;
    }

    private static TimeSpan Max(TimeSpan left, TimeSpan right)
    {
        return left >= right ? left : right;
    }

    private static GameTick NextTick(GameTick tick)
    {
        return tick == GameTick.MaxValue ? tick : tick + 1;
    }

    private readonly record struct EntityTimerKey(EntityUid Owner, CompIdx Component, EntityTimerId Id);

    private readonly record struct QueueEntry(EntityTimerKey Key, ulong Generation);

    private readonly record struct DeferredEntry(
        QueueEntry Entry,
        (long Deadline, ulong Sequence) Priority,
        bool OutsidePrediction);

    private sealed class TimerData(
        EntityTimerKey key,
        TimeSpan deadline,
        TimeSpan? interval,
        EntityTimerFlags flags,
        ulong generation,
        ulong sequence)
    {
        public readonly EntityTimerKey Key = key;
        public readonly TimeSpan? Interval = interval;
        public readonly EntityTimerFlags Flags = flags;
        public TimeSpan Deadline = deadline;
        public TimeSpan Remaining;
        public ulong Generation = generation;
        public ulong Sequence = sequence;
        public GameTick NotBeforeTick;
        public bool Suspended;
    }
}
