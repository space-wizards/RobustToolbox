using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates;

internal sealed class PVSSystem : SharedPVSSystem
{
    // Why is tracking dirty entities for prediction on PVS, you ask?
    // Because the server does something identical in PVS sooooo

    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<GameTick, HashSet<EntityUid>> _dirtyEntities = new();

    private ObjectPool<HashSet<EntityUid>> _dirtyPool =
        new DefaultObjectPool<HashSet<EntityUid>>(new DefaultPooledObjectPolicy<HashSet<EntityUid>>(), 64);

    // Keep it out of the pool because it's probably going to be a lot bigger.
    private HashSet<EntityUid> _dirty = new(256);

    public override void Initialize()
    {
        base.Initialize();
        EntityManager.EntityDirtied += OnEntityDirty;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EntityManager.EntityDirtied -= OnEntityDirty;
        _dirtyEntities.Clear();
    }

    internal void Reset(GameTick curTick)
    {
        var toRemove = new RemQueue<GameTick>();

        foreach (var (tick, _) in _dirtyEntities)
        {
            if (tick >= curTick - 1) continue;
            toRemove.Add(tick);
        }

        foreach (var tick in toRemove)
        {
            var ents = _dirtyEntities[tick];
            ents.Clear();
            _dirtyPool.Return(ents);
            _dirtyEntities.Remove(tick);
        }
    }

    public IEnumerable<EntityUid> GetDirtyEntities(GameTick currentTick)
    {
        _dirty.Clear();
        var metaQuery = GetEntityQuery<MetaDataComponent>();

        // This is just to avoid collection being modified during iteration unfortunately.
        foreach (var (tick, dirty) in _dirtyEntities)
        {
            if (tick < currentTick) continue;
            foreach (var ent in dirty)
            {
                // DebugTools.Assert(!ent.IsClientSide());

                if (!metaQuery.TryGetComponent(ent, out var meta) ||
                    meta.EntityLastModifiedTick < currentTick) continue;

                _dirty.Add(ent);
            }
        }

        /*
#if DEBUG
        foreach (var comp in EntityQuery<MetaDataComponent>())
        {
            if (comp.Owner.IsClientSide() || comp.EntityLastModifiedTick < currentTick)
            {
                continue;
            }
            DebugTools.Assert(ents.Contains(comp.Owner));
        }
#endif
        */

        return _dirty;
    }

    private void OnEntityDirty(object? sender, EntityUid e)
    {
        if (e.IsClientSide()) return;

        var tick = _timing.CurTick;
        if (!_dirtyEntities.TryGetValue(tick, out var ents))
        {
            ents = _dirtyPool.Get();
            _dirtyEntities[tick] = ents;
        }

        ents.Add(e);
    }
}
