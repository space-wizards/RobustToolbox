using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates;

/// <summary>
/// Tracks dirty entities on the client for the purposes of gamestatemanager.
/// </summary>
internal sealed class ClientDirtySystem : SharedPVSSystem
{
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

    internal void Reset()
    {
        foreach (var (_, sets) in _dirtyEntities)
        {
            sets.Clear();
            _dirtyPool.Return(sets);
        }

        _dirtyEntities.Clear();
    }

    public IEnumerable<EntityUid> GetDirtyEntities(GameTick currentTick)
    {
        _dirty.Clear();

        // This is just to avoid collection being modified during iteration unfortunately.
        foreach (var (tick, dirty) in _dirtyEntities)
        {
            if (tick < currentTick) continue;
            foreach (var ent in dirty)
            {
                _dirty.Add(ent);
            }
        }

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
