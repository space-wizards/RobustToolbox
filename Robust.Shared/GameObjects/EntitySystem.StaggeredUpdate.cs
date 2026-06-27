using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Collections;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects;

public partial class EntitySystem
{
    [IoC.Dependency] private IRobustRandom _rng = default!;
    [IoC.Dependency] private IGameTiming _gameTiming = default!;

    /// <summary>
    ///     Creates a tracker for updating entities with <typeparamref name="TComp"/> staggered over time.
    /// </summary>
    /// <remarks>
    ///     Entities are added to the tracker when their component receives <see cref="MapInitEvent"/>.
    ///     Each tracked entity is returned by the tracker at <see cref="IStaggeredUpdate.UpdateInterval"/>
    ///     intervals, with the first update randomly offset to spread work across ticks.
    /// </remarks>
    /// <param name="mapInit">Optional MapInit handler to invoke before the component is added to the tracker.</param>
    /// <typeparam name="TComp">The component type to track.</typeparam>
    /// <returns>A tracker that enumerates entities due for their staggered update.</returns>
    protected StaggeredUpdateTracker<TComp> GetStaggeredUpdateTracker<TComp>(
        EntityEventRefHandler<TComp, MapInitEvent>? mapInit) where TComp : IComponent, IStaggeredUpdate
    {
        var tracker = new StaggeredUpdateTracker<TComp>(
            mapInit, GetEntityQuery<TComp>(), GetEntityQuery<MetaDataComponent>(), _rng, _gameTiming);
        SubscribeLocalEvent<TComp, MapInitEvent>(tracker.OnMapInit);
        return tracker;
    }
}

public interface IStaggeredUpdate
{
    static abstract TimeSpan UpdateInterval { get; }
    static abstract TimeSpan MaxInitialDelay { get; }
}

public sealed class StaggeredUpdateTracker<TComp>
    where TComp : IComponent, IStaggeredUpdate
{
    private readonly PriorityQueue<EntityUid, TimeSpan> _insertQueue = new();
    private readonly RingBufferList<(EntityUid entity, TimeSpan when)> _schedule = [];
    private readonly HashSet<EntityUid> _tracked = [];

    private readonly EntityQuery<TComp> _compQuery;
    private readonly EntityQuery<MetaDataComponent> _metaQuery;
    private readonly IRobustRandom _rng;
    private readonly IGameTiming _timing;
    private readonly EntityEventRefHandler<TComp, MapInitEvent>? _mapInit;

    internal StaggeredUpdateTracker(
        EntityEventRefHandler<TComp, MapInitEvent>? mapInit,
        EntityQuery<TComp> compQuery,
        EntityQuery<MetaDataComponent> metaQuery,
        IRobustRandom rng,
        IGameTiming timing)
    {
        var interval = TComp.UpdateInterval;
        if (interval <= TimeSpan.Zero)
        {
            throw new InvalidOperationException(
                $"{typeof(TComp)} has an invalid staggered update interval: {interval}. " +
                "Staggered update interval must be positive and non-zero.");
        }

        _mapInit = mapInit;
        _compQuery = compQuery;
        _metaQuery = metaQuery;
        _rng = rng;
        _timing = timing;
    }

    internal void OnMapInit(Entity<TComp> ent, ref MapInitEvent args)
    {
        _mapInit?.Invoke(ent, ref args); // call a chained event handler if we have one

        if (!_tracked.Add(ent.Owner)) return;

        // randomize an offset from the current tick, up to interval
        // we start from current tick + 1 because updates for the current tick may already have been processed
        var when = _timing.CurTime + _timing.TickPeriod + _rng.Next(TimeSpan.Zero, TComp.MaxInitialDelay);
        _insertQueue.Enqueue(ent.Owner, when);
    }

    public Enumerator GetEnumerator()
    {
        return new Enumerator(this);
    }

    public readonly struct Enumerator(StaggeredUpdateTracker<TComp> tracker)
    {
        private readonly PriorityQueue<EntityUid, TimeSpan> _insertQueue = tracker._insertQueue;
        private readonly RingBufferList<(EntityUid entity, TimeSpan when)> _schedule = tracker._schedule;
        private readonly HashSet<EntityUid> _tracked = tracker._tracked;
        private readonly EntityQuery<TComp> _compQuery = tracker._compQuery;
        private readonly EntityQuery<MetaDataComponent> _metaQuery = tracker._metaQuery;
        private readonly TimeSpan _updateInterval = TComp.UpdateInterval;
        private readonly TimeSpan _until = tracker._timing.CurTime;

        public bool MoveNext(out EntityUid uid, [NotNullWhen(true)] out TComp? comp)
        {
            if (_insertQueue.Count != 0) return MoveNextMixed(out uid, out comp);

            while (_schedule.TryPeekFront(out var sched))
            {
                if (sched.when > _until)
                {
                    uid = default;
                    comp = default;
                    return false;
                }

                _schedule.RemoveAt(0);
                uid = sched.entity;

                // since we only schedule when the component can be resolved, entities where the component has been
                // deleted are dropped from the tracker
                if (_compQuery.TryComp(uid, out comp))
                {
                    _schedule.Add((uid, sched.when + _updateInterval));

                    if (!_metaQuery.TryGetComponentInternal(uid, out var metaComp)
                        || metaComp.EntityPaused)
                    {
                        continue;
                    }

                    return true;
                }

                // if our component is missing, stop tracking this entity
                _tracked.Remove(uid);
            }

            uid = default;
            comp = default;
            return false;
        }

        private bool MoveNextMixed(out EntityUid uid, [NotNullWhen(true)] out TComp? comp)
        {
            while (true)
            {
                TimeSpan when;

                // the next entity may come either from the insertion list or the schedule, whichever is sooner
                var queueWhen = _insertQueue.TryPeek(out var queueEnt, out var w)
                    ? w
                    : TimeSpan.MaxValue;
                var (schedEnt, schedWhen) = _schedule.TryPeekFront(out var sched)
                    ? sched
                    : (default, TimeSpan.MaxValue);

                if (schedWhen > _until && queueWhen > _until)
                {
                    uid = default;
                    comp = default;
                    return false;
                }

                if (queueWhen < schedWhen)
                {
                    _insertQueue.Dequeue();
                    uid = queueEnt;
                    when = queueWhen;
                }
                else
                {
                    _schedule.RemoveAt(0);
                    uid = schedEnt;
                    when = schedWhen;
                }

                // since we only schedule when the component can be resolved, entities where the component has been
                // deleted are dropped from the tracker
                if (_compQuery.TryComp(uid, out comp))
                {
                    _schedule.Add((uid, when + _updateInterval));

                    if (!_metaQuery.TryGetComponentInternal(uid, out var metaComp)
                        || metaComp.EntityPaused)
                    {
                        continue;
                    }

                    return true;
                }

                // if our component is missing, stop tracking this entity
                _tracked.Remove(uid);
            }
        }
    }
}
