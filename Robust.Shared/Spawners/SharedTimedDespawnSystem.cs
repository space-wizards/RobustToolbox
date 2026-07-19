using System;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.Spawners;

public sealed partial class SharedTimedDespawnSystem : EntitySystem
{
    private static readonly EntityTimerId DespawnTimer = new("timed-despawn");

    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private EntityTimerSystem _timers = default!;

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;

        SubscribeLocalEvent<TimedDespawnComponent, ComponentInit>(OnComponentInit);
        SubscribeLocalEvent<TimedDespawnComponent, AfterAutoHandleStateEvent>(OnAfterHandleState);
        SubscribeLocalEvent<TimedDespawnComponent, EntityTimerEvent>(OnTimer);
    }

    private void OnComponentInit(Entity<TimedDespawnComponent> ent, ref ComponentInit args)
    {
        ent.Comp.Deadline ??= _timing.CurTime + TimeSpan.FromSeconds(ent.Comp.Lifetime);
        Schedule(ent);
    }

    private void OnAfterHandleState(Entity<TimedDespawnComponent> ent, ref AfterAutoHandleStateEvent args)
    {
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

        var ev = new TimedDespawnEvent();
        RaiseLocalEvent(ent, ref ev);
        PredictedDel(ent.Owner);
    }

    private void Schedule(Entity<TimedDespawnComponent> ent)
    {
        _timers.SetTimerAt(
            ent,
            DespawnTimer,
            ent.Comp.Deadline ?? _timing.CurTime,
            flags: EntityTimerFlags.UpdatesOutsidePrediction);
    }
}
