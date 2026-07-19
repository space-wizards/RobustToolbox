using System;
using System.Collections.Generic;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedUserInterfaceSystem
{
    [Dependency] private EntityTimerSystem _entityTimers = default!;

    private readonly Dictionary<(BoundUserInterface Bui, EntityTimerId LocalId), EntityTimerId> _boundTimerIds = new();
    private readonly Dictionary<(EntityUid Owner, EntityTimerId TimerId), BoundTimerRegistration> _boundTimers = new();
    private ulong _nextBoundTimerId;

    private readonly record struct BoundTimerRegistration(
        BoundUserInterface Bui,
        EntityTimerId LocalId,
        bool Repeating);

    internal TimeSpan SetTimer(
        BoundUserInterface bui,
        EntityTimerId id,
        TimeSpan delay,
        TimeSpan? interval,
        EntityTimerFlags flags)
    {
        if (!UIQuery.TryComp(bui.Owner, out var component))
            throw new InvalidOperationException($"BUI owner {ToPrettyString(bui.Owner)} has no UserInterfaceComponent.");

        var timerId = RegisterBoundTimer(bui, id, interval != null);
        return _entityTimers.SetTimer<UserInterfaceComponent>(
            (bui.Owner, component),
            timerId,
            delay,
            interval,
            flags | EntityTimerFlags.UpdatesOutsidePrediction);
    }

    internal void SetTimerAt(
        BoundUserInterface bui,
        EntityTimerId id,
        TimeSpan deadline,
        TimeSpan? interval,
        EntityTimerFlags flags)
    {
        if (!UIQuery.TryComp(bui.Owner, out var component))
            throw new InvalidOperationException($"BUI owner {ToPrettyString(bui.Owner)} has no UserInterfaceComponent.");

        var timerId = RegisterBoundTimer(bui, id, interval != null);
        _entityTimers.SetTimerAt<UserInterfaceComponent>(
            (bui.Owner, component),
            timerId,
            deadline,
            interval,
            flags | EntityTimerFlags.UpdatesOutsidePrediction);
    }

    internal bool CancelTimer(BoundUserInterface bui, EntityTimerId id)
    {
        if (!_boundTimerIds.Remove((bui, id), out var timerId))
            return false;

        _boundTimers.Remove((bui.Owner, timerId));
        return _entityTimers.CancelTimer<UserInterfaceComponent>(bui.Owner, timerId);
    }

    internal bool TryGetTimer(BoundUserInterface bui, EntityTimerId id, out EntityTimerInfo timer)
    {
        if (_boundTimerIds.TryGetValue((bui, id), out var timerId))
            return _entityTimers.TryGetTimer<UserInterfaceComponent>(bui.Owner, timerId, out timer);

        timer = default;
        return false;
    }

    internal void CancelTimers(BoundUserInterface bui)
    {
        List<EntityTimerId>? timers = null;
        foreach (var ((registeredBui, localId), _) in _boundTimerIds)
        {
            if (ReferenceEquals(registeredBui, bui))
                (timers ??= new()).Add(localId);
        }

        if (timers == null)
            return;

        foreach (var id in timers)
        {
            CancelTimer(bui, id);
        }
    }

    private EntityTimerId RegisterBoundTimer(BoundUserInterface bui, EntityTimerId localId, bool repeating)
    {
        CancelTimer(bui, localId);

        var timerId = new EntityTimerId($"bui:{_nextBoundTimerId++}");
        _boundTimerIds.Add((bui, localId), timerId);
        _boundTimers.Add((bui.Owner, timerId), new BoundTimerRegistration(bui, localId, repeating));
        return timerId;
    }

    private void OnBoundUserInterfaceTimer(Entity<UserInterfaceComponent> ent, ref EntityTimerEvent args)
    {
        if (!_boundTimers.TryGetValue((ent.Owner, args.Id), out var registration))
            return;

        if (!registration.Bui.IsOpened)
        {
            CancelTimer(registration.Bui, registration.LocalId);
            return;
        }

        if (!registration.Repeating)
        {
            _boundTimers.Remove((ent.Owner, args.Id));
            _boundTimerIds.Remove((registration.Bui, registration.LocalId));
        }

        var timer = args with { Id = registration.LocalId };
        registration.Bui.OnTimer(timer);
    }
}
