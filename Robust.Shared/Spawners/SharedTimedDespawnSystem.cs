using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.Spawners;

public abstract partial class SharedTimedDespawnSystem : EntitySystem
{
    private static readonly EntityTimerId DespawnTimer = new("timed-despawn");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IEntityTimerManager _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<TimedDespawnComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TimedDespawnComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnComponentInit(Entity<TimedDespawnComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Deadline ??= _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Lifetime);
        Schedule(ent);
    }

    private void OnTimer(Entity<TimedDespawnComponent> ent, ref EntityTimerEvent args)
    {
        if (args.Id != DespawnTimer)
            return;

        // Client-side deletion must not occur during past prediction. Keep the timer armed so it can retry.
        if (!_timing.IsFirstTimePredicted)
        {
            Schedule(ent);
            return;
        }

        if (!CanDelete(ent))
            return;

        var ev = new TimedDespawnEvent();
        RaiseLocalEvent(ent, ref ev);
        QueueDel(ent);
    }

    private void Schedule(Entity<TimedDespawnComponent> ent)
    {
        _timers.SetTimerAt(
            ent,
            DespawnTimer,
            ent.Comp.Deadline ?? _timing.CurTime,
            flags: EntityTimerFlags.UpdatesOutsidePrediction);
    }

    protected abstract bool CanDelete(EntityUid uid);
}
