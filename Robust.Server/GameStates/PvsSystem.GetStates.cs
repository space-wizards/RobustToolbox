using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

// This partial class contains code for turning a list of visible entities into actual entity states.
internal sealed partial class PvsSystem
{
    public void GetStateList(
        List<EntityState> states,
        List<EntityData> toSend,
        SessionPvsData sessionData,
        GameTick fromTick)
    {
        DebugTools.Assert(states.Count == 0);
        var entData = sessionData.EntityData;
        var session = sessionData.Session;

        if (sessionData.RequestedFull)
        {
            foreach (var data in CollectionsMarshal.AsSpan(toSend))
            {
                DebugTools.AssertNotNull(data.Entity.Comp);
                DebugTools.Assert(data.LastSent == _gameTiming.CurTick);
                DebugTools.Assert(data.Visibility > PvsEntityVisibility.Unsent);
                DebugTools.Assert(ReferenceEquals(data, entData[data.NetEntity]));
                states.Add(GetFullEntityState(session, data.Entity.Owner, data.Entity.Comp));
            }
            return;
        }

        foreach (var data in CollectionsMarshal.AsSpan(toSend))
        {
            DebugTools.AssertNotNull(data.Entity.Comp);
            DebugTools.Assert(data.LastSent == _gameTiming.CurTick);
            DebugTools.Assert(data.Visibility > PvsEntityVisibility.Unsent);
            DebugTools.Assert(ReferenceEquals(data, entData[data.NetEntity]));

            if (data.Visibility == PvsEntityVisibility.Unchanged)
                continue;

            var (uid, meta) = data.Entity;
            var entered = data.Visibility == PvsEntityVisibility.Entered;
            var entFromTick = entered ? data.EntityLastAcked : fromTick;

            // TODO PVS turn into debug assert
            // This is should really be a debug assert, but I want to check for errors on live servers
            // If an entity is not marked as "entering" this tick, then it HAS to have been in the last acked state
            if (!entered && data.EntityLastAcked < fromTick)
            {
                Log.Error($"un-acked entity is not marked as entering. Entity{ToPrettyString(uid)}. FromTick: {fromTick}. CurTick: {_gameTiming.CurTick}. Data: {data}");
            }

            var state = GetEntityState(session, uid, entFromTick, meta);

            if (entered || !state.Empty)
                states.Add(state);
        }
    }

    /// <summary>
    /// Generates a network entity state for the given entity.
    /// </summary>
    /// <param name="player">The player to generate this state for. This may be null if the state is for replay recordings.</param>
    /// <param name="entityUid">Uid of the entity to generate the state from.</param>
    /// <param name="fromTick">Only provide delta changes from this tick.</param>
    /// <param name="meta">The entity's metadata component</param>
    /// <returns>New entity State for the given entity.</returns>
    private EntityState GetEntityState(ICommonSession? player, EntityUid entityUid, GameTick fromTick, MetaDataComponent meta)
    {
        var bus = EntityManager.EventBus;
        var changed = new List<ComponentChange>();

        bool sendCompList = meta.LastComponentRemoved > fromTick;
        HashSet<ushort>? netComps = sendCompList ? new() : null;

        foreach (var (netId, component) in meta.NetComponents)
        {
            DebugTools.Assert(component.NetSyncEnabled);

            if (component.Deleted || !component.Initialized)
            {
                Log.Error("Entity manager returned deleted or uninitialized components while sending entity data");
                continue;
            }

            if (component.SendOnlyToOwner && player != null && player.AttachedEntity != entityUid)
                continue;

            if (component.LastModifiedTick <= fromTick)
            {
                if (sendCompList && (!component.SessionSpecific || player == null || EntityManager.CanGetComponentState(bus, component, player)))
                    netComps!.Add(netId);
                continue;
            }

            if (component.SessionSpecific && player != null && !EntityManager.CanGetComponentState(bus, component, player))
                continue;

            var state = EntityManager.GetComponentState(bus, component, player, fromTick);
            DebugTools.Assert(fromTick > component.CreationTick || state is not IComponentDeltaState delta || delta.FullState);
            changed.Add(new ComponentChange(netId, state, component.LastModifiedTick));

            if (sendCompList)
                netComps!.Add(netId);
        }

        DebugTools.Assert(meta.EntityLastModifiedTick >= meta.LastComponentRemoved);
        DebugTools.Assert(GetEntity(meta.NetEntity) == entityUid);
        var entState = new EntityState(meta.NetEntity, changed, meta.EntityLastModifiedTick, netComps);

        return entState;
    }

    /// <summary>
    /// Variant of <see cref="GetEntityState"/> that includes all entity data, including data that can be inferred implicitly from the entity prototype.
    /// </summary>
    private EntityState GetFullEntityState(ICommonSession player, EntityUid entityUid, MetaDataComponent meta)
    {
        var bus = EntityManager.EventBus;
        var changed = new List<ComponentChange>();

        HashSet<ushort> netComps = new();

        foreach (var (netId, component) in meta.NetComponents)
        {
            DebugTools.Assert(component.NetSyncEnabled);

            if (component.SendOnlyToOwner && player.AttachedEntity != entityUid)
                continue;

            if (component.SessionSpecific && !EntityManager.CanGetComponentState(bus, component, player))
                continue;

            var state = EntityManager.GetComponentState(bus, component, player, GameTick.Zero);
            DebugTools.Assert(state is not IComponentDeltaState delta || delta.FullState);
            changed.Add(new ComponentChange(netId, state, component.LastModifiedTick));
            netComps.Add(netId);
        }

        var entState = new EntityState(meta.NetEntity, changed, meta.EntityLastModifiedTick, netComps);

        return entState;
    }

    /// <summary>
    /// Gets all entity states that have been modified after and including the provided tick.
    /// </summary>
    public (List<EntityState>?, List<NetEntity>?, GameTick fromTick) GetAllEntityStates(ICommonSession? player, GameTick fromTick, GameTick toTick)
    {
        List<EntityState>? stateEntities;
        var toSend = _uidSetPool.Get();
        DebugTools.Assert(toSend.Count == 0);
        bool enumerateAll = false;
        DebugTools.AssertEqual(toTick, _gameTiming.CurTick);
        DebugTools.Assert(toTick > fromTick);

        if (player == null)
        {
            enumerateAll = fromTick == GameTick.Zero;
        }
        else if (!_seenAllEnts.Contains(player))
        {
            enumerateAll = true;
            fromTick = GameTick.Zero;
        }

        if (toTick.Value - fromTick.Value > DirtyBufferSize)
        {
            // Fall back to enumerating over all entities.
            enumerateAll = true;
        }

        if (enumerateAll)
        {
            stateEntities = new List<EntityState>(EntityManager.EntityCount);
            var query = EntityManager.AllEntityQueryEnumerator<MetaDataComponent>();
            while (query.MoveNext(out var uid, out var md))
            {
                DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized, $"Entity {ToPrettyString(uid)} has not been initialized");
                DebugTools.Assert(md.EntityLifeStage < EntityLifeStage.Terminating, $"Entity {ToPrettyString(uid)} is/has been terminated");
                if (md.EntityLastModifiedTick <= fromTick)
                    continue;

                var state = GetEntityState(player, uid, fromTick, md);

                if (state.Empty)
                {
                    Log.Error($@"{nameof(GetEntityState)} returned an empty state while enumerating entities.
Tick: {fromTick}--{toTick}
Entity: {ToPrettyString(uid)}
Last modified: {md.EntityLastModifiedTick}
Metadata last modified: {md.LastModifiedTick}
Transform last modified: {Transform(uid).LastModifiedTick}");
                }

                stateEntities.Add(state);
            }
        }
        else
        {
            stateEntities = new();
            for (var i = fromTick.Value + 1; i <= toTick.Value; i++)
            {
                if (!TryGetDirtyEntities(new GameTick(i), out var add, out var dirty))
                {
                    // This should be unreachable if `enumerateAll` is false.
                    throw new Exception($"Failed to get tick dirty data. tick: {i}, from: {fromTick}, to {toTick}, buffer: {DirtyBufferSize}");
                }

                foreach (var uid in add)
                {
                    if (!toSend.Add(uid) || !_metaQuery.TryGetComponent(uid, out var md))
                        continue;

                    DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized, $"Entity {ToPrettyString(uid)} has not been initialized");
                    DebugTools.Assert(md.EntityLifeStage < EntityLifeStage.Terminating, $"Entity {ToPrettyString(uid)} is/has been terminated");
                    DebugTools.Assert(md.EntityLastModifiedTick >= md.CreationTick, $"Entity {ToPrettyString(uid)} last modified tick is less than creation tick");
                    DebugTools.Assert(md.EntityLastModifiedTick > fromTick, $"Entity {ToPrettyString(uid)} last modified tick is less than from tick");

                    var state = GetEntityState(player, uid, fromTick, md);

                    if (state.Empty)
                    {
                        Log.Error($@"{nameof(GetEntityState)} returned an empty state for a new entity.
Tick: {fromTick}--{toTick}
Entity: {ToPrettyString(uid)}
Last modified: {md.EntityLastModifiedTick}
Metadata last modified: {md.LastModifiedTick}
Transform last modified: {Transform(uid).LastModifiedTick}");
                        continue;
                    }

                    stateEntities.Add(state);
                }

                foreach (var uid in dirty)
                {
                    DebugTools.Assert(!add.Contains(uid));
                    if (!toSend.Add(uid) || !_metaQuery.TryGetComponent(uid, out var md))
                        continue;

                    DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized, $"Entity {ToPrettyString(uid)} has not been initialized");
                    DebugTools.Assert(md.EntityLifeStage < EntityLifeStage.Terminating, $"Entity {ToPrettyString(uid)} is/has been terminated");
                    DebugTools.Assert(md.EntityLastModifiedTick >= md.CreationTick, $"Entity {ToPrettyString(uid)} last modified tick is less than creation tick");
                    DebugTools.Assert(md.EntityLastModifiedTick > fromTick, $"Entity {ToPrettyString(uid)} last modified tick is less than from tick");

                    var state = GetEntityState(player, uid, fromTick, md);
                    if (!state.Empty)
                        stateEntities.Add(state);
                }
            }
        }

        _uidSetPool.Return(toSend);
        var deletions = _entityPvsCollection.GetDeletedIndices(fromTick);

        if (stateEntities.Count == 0)
            stateEntities = null;

        return (stateEntities, deletions, fromTick);
    }
}
