using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Shared.Spawners;

public sealed class SharedTimedDespawnSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly HashSet<EntityUid> _queuedDespawnEntities = new();

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_timing.IsFirstTimePredicted)
            return;

        _queuedDespawnEntities.Clear();

        var query = EntityQueryEnumerator<TimedDespawnComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            comp.Lifetime -= frameTime;

            if (comp.Lifetime <= 0)
                _queuedDespawnEntities.Add(uid);
        }

        foreach (var queued in _queuedDespawnEntities)
        {
            var ev = new TimedDespawnEvent();
            RaiseLocalEvent(queued, ref ev);
            PredictedDel(queued);
        }
    }
}
