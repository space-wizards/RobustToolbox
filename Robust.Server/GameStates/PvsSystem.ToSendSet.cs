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
        var limit = distance < (_lowLodDistance + ChunkSize) / 2
            ? chunk.Contents.Count
            : chunk.LodCounts[0];

        // If the PVS budget is exceeded, it should still be safe to send all of the chunk's direct children, though
        // after that we have no guarantee that an entity's parent got sent.
        var directChildren = Math.Min(limit, chunk.LodCounts[2]);

        // Send entities on the chunk.
        var span = CollectionsMarshal.AsSpan(chunk.Contents);
        for (var i = 0; i < limit; i++)
        {
            var ent = span[i];
            if ((mask & ent.Comp.VisibilityMask) != ent.Comp.VisibilityMask)
                continue;

            // TODO PVS improve this somehow
            // Having entities "leave" pvs view just because the pvs entry budget was exceeded sucks.
            // This probably requires changing client game state manager to support receiving entities with unknown parents.
            // Probably needs to do something similar to pending net entity states, but for entity spawning.
            if (!AddEntity(session, ent, fromTick))
                limit = directChildren;
        }
    }

    /// <summary>
    /// Attempt to add an entity to the to-send lists, while respecting pvs budgets.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AddEntity(PvsSession session, Entity<MetaDataComponent> entity, GameTick fromTick)
    {
        var nuid = entity.Comp.NetEntity;
        ref var data = ref CollectionsMarshal.GetValueRefOrAddDefault(session.Entities, nuid, out var exists);
        if (!exists)
            data = new(nuid);

        if (entity.Comp.Deleted)
        {
            Log.Error($"Attempted to send deleted entity: {ToPrettyString(entity, entity)}");
            session.Entities.Remove(entity.Comp.NetEntity);
            return false;
        }

        DebugTools.AssertEqual(data!.NetEntity, entity.Comp.NetEntity);
        if (data.LastSeen == _gameTiming.CurTick)
            return true;

        var (entered,budgetExceeded) = IsEnteringPvsRange(data, fromTick, ref session.Budget);

        if (budgetExceeded)
            return false;

        if (!AddToSendList(session, data, entity, fromTick, entered))
            return false;

        DebugTools.AssertNotEqual(data.LastSeen, GameTick.Zero);
        return true;
    }

    /// <summary>
    /// This method adds an entity to the list of visible entities, updates the last-seen tick, and computes any
    /// required game states.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private bool AddToSendList(PvsSession session, PvsData data, Entity<MetaDataComponent> entity, GameTick fromTick,
        bool entered)
    {
        DebugTools.Assert(fromTick < _gameTiming.CurTick);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (data == null)
        {
            Log.Error($"Encountered null EntityData.");
            return false;
        }

        DebugTools.AssertNotEqual(data.LastSeen, _gameTiming.CurTick);
        DebugTools.Assert(data.EntityLastAcked <= fromTick || fromTick == GameTick.Zero);
        var (uid, meta) = entity;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (meta == null)
        {
            Log.Error($"Encountered null metadata in EntityData. Entity: {ToPrettyString(uid)}");
            return false;
        }

        if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            var rep = new EntityStringRepresentation(entity);
            Log.Error($"Attempted to add a deleted entity to PVS send set: '{rep}'. Deletion queued: {EntityManager.IsQueuedForDeletion(uid)}. Trace:\n{Environment.StackTrace}");

            // This can happen if some entity was some removed from it's parent while that parent was being deleted.
            // As a result the entity was marked for deletion but was never actually properly deleted.
            EntityManager.QueueDeleteEntity(uid);
            return false;
        }

        data.LastSeen = _gameTiming.CurTick;
        session.ToSend!.Add(data);
        EntityState state;

        if (session.RequestedFull)
        {
            state = GetFullEntityState(session.Session, uid, meta);
            session.States.Add(state);
            return true;
        }

        if (entered)
        {
            state = GetEntityState(session.Session, uid, data.EntityLastAcked, meta);
            session.States.Add(state);
            return true;
        }

        if (meta.EntityLastModifiedTick <= fromTick)
            return true;

        state = GetEntityState(session.Session, uid, fromTick , meta);

        if (!state.Empty)
            session.States.Add(state);

        return true;
    }

    /// <summary>
    /// This method figures out whether a given entity is currently entering a player's PVS range.
    /// This method will also check that the player's PVS entry budget is not being exceeded.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private (bool Entering, bool BudgetExceeded) IsEnteringPvsRange(
        PvsData data,
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
