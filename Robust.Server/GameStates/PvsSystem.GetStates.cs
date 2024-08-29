using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

// This partial class contains code for turning a list of visible entities into actual entity states.
internal sealed partial class PvsSystem
{
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
            changed.Add(new ComponentChange(netId, state, component.LastModifiedTick));

            if (state != null)
                DebugTools.Assert(fromTick > component.CreationTick || state is not IComponentDeltaState);

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
            DebugTools.Assert(state is not IComponentDeltaState);
            changed.Add(new ComponentChange(netId, state, component.LastModifiedTick));
            netComps.Add(netId);
        }

        var entState = new EntityState(meta.NetEntity, changed, meta.EntityLastModifiedTick, netComps);

        return entState;
    }

    /// <summary>
    /// Gets all entity states that have been modified after and including the provided tick.
    /// </summary>
    private void GetAllEntityStates(PvsSession pvsSession)
    {
        var session = pvsSession.Session;
        var toTick = _gameTiming.CurTick;
        var fromTick = pvsSession.FromTick;

        var toSend = _uidSetPool.Get();
        DebugTools.Assert(toSend.Count == 0);
        bool enumerateAll = false;
        DebugTools.AssertEqual(toTick, _gameTiming.CurTick);
        DebugTools.Assert(toTick > fromTick);

        // Null sessions imply this is a replay.
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (session == null)
        {
            enumerateAll = fromTick == GameTick.Zero;
        }
        else if (!_seenAllEnts.Contains(session))
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
            var query = EntityManager.AllEntityQueryEnumerator<MetaDataComponent>();
            while (query.MoveNext(out var uid, out var md))
            {
                DebugTools.Assert(md.EntityLifeStage >= EntityLifeStage.Initialized, $"Entity {ToPrettyString(uid)} has not been initialized");
                DebugTools.Assert(md.EntityLifeStage < EntityLifeStage.Terminating, $"Entity {ToPrettyString(uid)} is/has been terminated");
                if (md.EntityLastModifiedTick <= fromTick)
                    continue;

                var state = GetEntityState(session, uid, fromTick, md);

                if (state.Empty)
                {
                    Log.Error($@"{nameof(GetEntityState)} returned an empty state while enumerating entities.
Tick: {fromTick}--{toTick}
Entity: {ToPrettyString(uid)}
Last modified: {md.EntityLastModifiedTick}
Metadata last modified: {md.LastModifiedTick}
Transform last modified: {Transform(uid).LastModifiedTick}");
                }

                pvsSession.States.Add(state);
            }
        }
        else
        {
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

                    var state = GetEntityState(session, uid, fromTick, md);

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

                    pvsSession.States.Add(state);
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

                    var state = GetEntityState(session, uid, fromTick, md);
                    if (!state.Empty)
                        pvsSession.States.Add(state);
                }
            }
        }

        _uidSetPool.Return(toSend);
    }
}
