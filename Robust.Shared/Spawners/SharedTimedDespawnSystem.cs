using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.Spawners;

public abstract class SharedTimedDespawnSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _queuedDespawnEntities = new();

    public override void Initialize()
    {
        base.Initialize();
        UpdatesOutsidePrediction = true;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        // AAAAAAAAAAAAAAAAAAAAAAAAAAA
        // Client both needs to predict this, but also can't properly handle prediction resetting.
        if (!_timing.IsFirstTimePredicted)
            return;

        _queuedDespawnEntities.Clear();

        var query = EntityQueryEnumerator<TimedDespawnComponent>();

        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Lifetime -= frameTime;

            if (!CanDelete(uid))
                continue;

            if (comp.Lifetime <= 0)
            {
                _queuedDespawnEntities.Add(uid);
            }
        }

        foreach (var queued in _queuedDespawnEntities)
        {
            var ev = new TimedDespawnEvent();
            RaiseLocalEvent(queued, ref ev);
            QueueDel(queued);
        }
    }

    protected abstract bool CanDelete(EntityUid uid);
}
