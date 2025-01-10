using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Map.Components;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

// This partial class contains functions for adding entities to a the collections of entities that are getting sent to
// a player this tick..
internal sealed partial class PvsSystem
{
    /// <summary>
    /// Iterate over chunks that are visible to a player and add entities to the game-state.
    /// </summary>
    private void AddPvsChunks(PvsSession pvsSession)
    {
        foreach (var (chunk, distance) in CollectionsMarshal.AsSpan(pvsSession.Chunks))
        {
            AddPvsChunk(chunk, distance, pvsSession);
        }
    }

    /// <summary>
    /// Add all entities on a given PVS chunk to a clients game-state.
    /// </summary>
    private void AddPvsChunk(PvsChunk chunk, float distance, PvsSession session)
    {
        // Each root nodes should simply be a map or a grid entity.
        DebugTools.Assert(Exists(chunk.Root), $"Chunk root does not exist!");
        DebugTools.Assert(Exists(chunk.Map), $"Map does not exist!.");
        DebugTools.Assert(HasComp<MapComponent>(chunk.Root) || HasComp<MapGridComponent>(chunk.Root));

        var fromTick = session.FromTick;
        var mask = session.VisMask;

        // Send the map.
        if (!AddEntity(session, chunk.Map, fromTick))
            return;

        // Send the grid
        if (chunk.Map.Owner != chunk.Root.Owner && !AddEntity(session, chunk.Root, fromTick))
            return;

        // Get the number of entities to send (i.e., basic LOD restrictions)
        // We add chunk-size here so that its consistent with the normal PVS range setting.
        // I.e., distance here is the Chebyshev distance to the centre of each chunk, but the normal pvs range only
        // required that the chunk be touching the box, not the centre.
        var count = distance <=  (_viewSize + ChunkSize) / 2
            ? chunk.Contents.Count
            : chunk.LodCounts[0];

        // Send entities on the chunk.
        var span = CollectionsMarshal.AsSpan(chunk.Contents)[..count];
        foreach (ref var ent in span)
        {
            ref var meta = ref _metadataMemory.GetRef(ent.Ptr.Index);
            meta.Validate(ent.Meta);
            if ((mask & meta.VisMask) == meta.VisMask)
                AddEntity(session, ref ent, ref meta, fromTick);
        }
    }

    /// <summary>
    /// Attempt to add an entity to the to-send lists, while respecting pvs budgets.
    /// </summary>
    /// <returns>Returns false if the entity would exceed the client's PVS budget.</returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private void AddEntity(PvsSession session, ref PvsChunk.ChunkEntity ent, ref PvsMetadata meta,
        GameTick fromTick)
    {
        DebugTools.Assert(fromTick < _gameTiming.CurTick);
        ref var data = ref session.DataMemory.GetRef(ent.Ptr.Index);

        if (data.LastSeen == _gameTiming.CurTick)
            return;

        if (meta.LifeStage >= EntityLifeStage.Terminating)
        {
            Log.Error($"Attempted to send deleted entity: {ToPrettyString(ent.Uid)}, Meta lifestage: {ent.Meta.EntityLifeStage}, PVS lifestage: {meta.LifeStage}.\n{Environment.StackTrace}");
            return;
        }

        var (entered,budgetExceeded) = IsEnteringPvsRange(ref data, fromTick, ref session.Budget);

        if (budgetExceeded)
            return;

        data.LastSeen = _gameTiming.CurTick;
        session.ToSend!.Add(ent.Ptr);

        if (session.RequestedFull)
        {
            var state = GetFullEntityState(session.Session, ent.Uid, ent.Meta);
            session.States.Add(state);
            return;
        }

        if (entered)
        {
            var state = GetEntityState(session.Session, ent.Uid, data.EntityLastAcked, ent.Meta);
            session.States.Add(state);
            return;
        }

        if (meta.LastModifiedTick <= fromTick)
            return;

        var entState = GetEntityState(session.Session, ent.Uid, fromTick , ent.Meta);

        if (!entState.Empty)
            session.States.Add(entState);
    }

    /// <summary>
    /// Attempt to add an entity to the to-send lists, while respecting pvs budgets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AddEntity(PvsSession session, Entity<MetaDataComponent> entity, GameTick fromTick)
    {
        DebugTools.Assert(fromTick < _gameTiming.CurTick);
        ref var data = ref session.DataMemory.GetRef(entity.Comp.PvsData.Index);

        if (data.LastSeen == _gameTiming.CurTick)
            return true;

        var (entered,budgetExceeded) = IsEnteringPvsRange(ref data, fromTick, ref session.Budget);

        if (budgetExceeded)
            return false;

        var (uid, meta) = entity;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (meta == null)
        {
            Log.Error($"Encountered null metadata in EntityData. Entity: {ToPrettyString(uid)}");
            return false;
        }

        if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            // This can happen if some entity was some removed from it's parent while that parent was being deleted.
            // As a result the entity was marked for deletion but was never actually properly deleted.

            bool queued;
            lock (_toDelete)
            {
                queued = EntityManager.IsQueuedForDeletion(uid) || _toDelete.Contains(uid);
                if (!queued)
                    _toDelete.Add(uid);
            }

            var rep = new EntityStringRepresentation(entity);
            Log.Error($"Attempted to add a deleted entity to PVS send set: '{rep}'. Deletion queued: {queued}. Trace:\n{Environment.StackTrace}");
            return false;
        }

        data.LastSeen = _gameTiming.CurTick;
        session.ToSend!.Add(entity.Comp.PvsData);

        // TODO PVS PERFORMANCE
        // Investigate whether its better to defer actually creating the entity state & populating session.States here?
        // I.e., should be be constructing the to-send list & to-get-states lists, and then separately getting all states
        // after we have gotten all entities? If the CPU can focus on only processing data in session.DataMemory without
        // having to access miscellaneous component info, maybe it will be faster?
        // Though for that to work I guess it also has to avoid accessing the metadata component's lifestage?

        if (session.RequestedFull)
        {
            var state = GetFullEntityState(session.Session, uid, meta);
            session.States.Add(state);
            return true;
        }

        if (entered)
        {
            var state = GetEntityState(session.Session, uid, data.EntityLastAcked, meta);
            session.States.Add(state);
            return true;
        }

        if (meta.EntityLastModifiedTick <= fromTick)
            return true;

        var entState = GetEntityState(session.Session, uid, fromTick , meta);

        if (!entState.Empty)
            session.States.Add(entState);

        return true;
    }

    /// <summary>
    /// This method figures out whether a given entity is currently entering a player's PVS range.
    /// This method will also check that the player's PVS entry budget is not being exceeded.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (bool Entering, bool BudgetExceeded) IsEnteringPvsRange(
        ref PvsData data,
        GameTick fromTick,
        ref PvsBudget budget)
    {
        var enteredSinceLastSent = fromTick == GameTick.Zero
                                   || data.LastSeen == GameTick.Zero
                                   || data.LastSeen != _gameTiming.CurTick - 1;

        var entering = enteredSinceLastSent
                      || data.EntityLastAcked == GameTick.Zero
                      || data.EntityLastAcked < fromTick // this entity was not in the last acked state.
                      || data.LastLeftView >= fromTick; // entity left and re-entered sometime after the last acked tick

        // If the entity is entering, but we already sent this entering entity in the last message, we won't add it to
        // the budget. Chances are the packet will arrive in a nice and orderly fashion, and the client will stick to
        // their requested budget. However this can cause issues if a packet gets dropped, because a player may create
        // 2x or more times the normal entity creation budget.
        if (enteredSinceLastSent)
        {
            if (budget.NewCount >= budget.NewLimit || budget.EnterCount >= budget.EnterLimit)
                return (entering, true);

            budget.EnterCount++;

            if (data.EntityLastAcked == GameTick.Zero)
                budget.NewCount++;
        }

        return (entering, false);
    }
}
