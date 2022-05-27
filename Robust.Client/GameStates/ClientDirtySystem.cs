using System;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Client.GameStates;

/// <summary>
/// Tracks dirty entities on the client for the purposes of gamestatemanager.
/// </summary>
internal sealed class ClientDirtySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;

    private readonly Dictionary<GameTick, HashSet<EntityUid>> _dirtyEntities = new();

    private ObjectPool<HashSet<EntityUid>> _dirtyPool =
        new DefaultObjectPool<HashSet<EntityUid>>(new DefaultPooledObjectPolicy<HashSet<EntityUid>>(), 64);

    // TODO maybe have a pool for this as well? But comp/entity deletion will occur much less frequently than generic dirying.
    private readonly Dictionary<EntityUid, Dictionary<GameTick, HashSet<Type>>> _removedComponents = new();

    // Keep it out of the pool because it's probably going to be a lot bigger.
    private HashSet<EntityUid> _dirty = new(256);

    public override void Initialize()
    {
        base.Initialize();
        EntityManager.EntityDirtied += OnEntityDirty;
        EntityManager.ComponentRemoved += OnCompRemoved;
    }

    public override void Shutdown()
    {
        base.Shutdown();
        EntityManager.EntityDirtied -= OnEntityDirty;
        EntityManager.ComponentRemoved -= OnCompRemoved;
        _dirtyEntities.Clear();
    }

    private void OnCompRemoved(object? sender, ComponentEventArgs args)
    {
        if (args.Owner.IsClientSide() || !args.Component.NetSyncEnabled)
            return;

        // TODO if ever entity deletion gets predicted... add an arg to comp removal that specifies whether removal is
        // occcuring because of entity deletion.
        if (!_timing.InPrediction)
            return;

        // Was this component added during prediction? If yes, there is no need to re-add it when resetting.
        if (args.Component.CreationTick > _timing.LastRealTick)
            return;

        var tick = _timing.CurTick;
        if (!_removedComponents.TryGetValue(args.Owner, out var ticks))
            _removedComponents[args.Owner] = ticks = new();

        if (!ticks.TryGetValue(tick, out var comps))
            ticks[tick] = comps = new();

        comps.Add(args.Component.GetType());
    }

    internal void Reset()
    {
        foreach (var (_, sets) in _dirtyEntities)
        {
            sets.Clear();
            _dirtyPool.Return(sets);
        }

        _dirtyEntities.Clear();
        _removedComponents.Clear();
    }

    internal IEnumerable<Type> GetRemovedComponents(GameTick currentTick, EntityUid uid)
    {
        if (!_removedComponents.TryGetValue(uid, out var ticks))
            return Array.Empty<Type>();

        var removed = new HashSet<Type>();

        // This is just to avoid collection being modified during iteration unfortunately.
        foreach (var (tick, rem) in ticks)
        {
            if (tick < currentTick) continue;
            foreach (var ent in rem)
            {
                removed.Add(ent);
            }
        }

        return removed;
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
        if (e.IsClientSide() || !_timing.InPrediction) return;

        var tick = _timing.CurTick;
        if (!_dirtyEntities.TryGetValue(tick, out var ents))
        {
            ents = _dirtyPool.Get();
            _dirtyEntities[tick] = ents;
        }

        ents.Add(e);
    }
}
