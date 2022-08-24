using System;
using System.Collections.Generic;
using Microsoft.Extensions.ObjectPool;
using Robust.Client.Timing;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Timing;

namespace Robust.Client.GameStates;

/// <summary>
/// Tracks dirty entities on the client for the purposes of gamestatemanager.
/// </summary>
internal sealed class ClientDirtySystem : EntitySystem
{
    [Dependency] private readonly IClientGameTiming _timing = default!;

    private readonly Dictionary<GameTick, HashSet<EntityUid>> _dirtyEntities = new();

    private ObjectPool<HashSet<EntityUid>> _dirtyPool =
        new DefaultObjectPool<HashSet<EntityUid>>(new DefaultPooledObjectPolicy<HashSet<EntityUid>>(), 64);

    // TODO maybe have a pool for this as well? But comp/entity deletion will occur much less frequently than general
    // dirtying. So maybe not required?
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

    private void OnCompRemoved(RemovedComponentEventArgs args)
    {
        // TODO if ever entity deletion gets predicted... add an arg to comp removal that specifies whether removal is
        // occcuring because of entity deletion, to speed this function up, as it will get called once for each
        // component the entity had.

        if (args.BaseArgs.Owner.IsClientSide() || !args.BaseArgs.Component.NetSyncEnabled || !_timing.InPrediction)
            return;

        // Was this component added during prediction? If yes, then there is no need to re-add it when resetting.
        // Note that this means a partial reset to anything other than `_timing.LastRealTick` isn't supported
        if (args.BaseArgs.Component.CreationTick > _timing.LastRealTick)
            return;

        var tick = _timing.CurTick;
        if (!_removedComponents.TryGetValue(args.BaseArgs.Owner, out var ticks))
            _removedComponents[args.BaseArgs.Owner] = ticks = new();

        if (!ticks.TryGetValue(tick, out var comps))
            ticks[tick] = comps = new();

        comps.Add(args.BaseArgs.Component.GetType());
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

    internal IEnumerable<Type> GetRemovedComponents(EntityUid uid)
    {
        if (!_removedComponents.TryGetValue(uid, out var ticks))
            return Array.Empty<Type>();

        var removed = new HashSet<Type>();

        // This is just to avoid collection being modified during iteration unfortunately.
        foreach (var (tick, rem) in ticks)
        {
            if (tick < _timing.LastRealTick) continue;
            foreach (var ent in rem)
            {
                removed.Add(ent);
            }
        }

        return removed;
    }

    public IEnumerable<EntityUid> GetDirtyEntities()
    {
        _dirty.Clear();

        // This is just to avoid collection being modified during iteration unfortunately.
        foreach (var (tick, dirty) in _dirtyEntities)
        {
            if (tick < _timing.LastRealTick) continue;
            foreach (var ent in dirty)
            {
                _dirty.Add(ent);
            }
        }

        return _dirty;
    }

    private void OnEntityDirty(EntityUid e)
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
