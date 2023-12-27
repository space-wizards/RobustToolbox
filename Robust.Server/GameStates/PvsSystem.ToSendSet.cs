using System;
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
    /// A chunks that is visible to a player and add entities to the game-state.
    /// </summary>
    private void AddPvsChunk(PvsChunk chunk, float distance, PvsSession session)
    {
#if DEBUG
        // Each root nodes should simply be a map or a grid entity.
        DebugTools.Assert(Exists(chunk.Root), $"Root node does not exist. Node {chunk.Root.Owner}. Session: {session.Session}");
        DebugTools.Assert(HasComp<MapComponent>(chunk.Root) || HasComp<MapGridComponent>(chunk.Root));
#endif

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
        var count = distance < (_lowLodDistance + ChunkSize)/2
            ? chunk.Contents.Count
            : chunk.LodCounts[0];

        // Send entities on the chunk.
        var span = CollectionsMarshal.AsSpan(chunk.Contents);
        for (var i = 0; i < count; i++)
        {
            var ent = span[i];
            if ((mask & ent.Comp.VisibilityMask) == ent.Comp.VisibilityMask)
                AddEntity(session, ent, fromTick);
        }
    }

    /// <summary>
    /// Attempt to add an entity to the to-send lists, while respecting pvs budgets.
    /// </summary>
    private bool AddEntity(PvsSession session, Entity<MetaDataComponent> entity, GameTick fromTick)
    {
        ref var data = ref CollectionsMarshal.GetValueRefOrAddDefault(session.Entities, entity.Comp.NetEntity, out var exists);
        if (!exists)
            data = new(entity);

        if (entity.Comp.Deleted)
        {
            Log.Error($"Attempted to send deleted entity: {ToPrettyString(entity, entity)}");
            session.Entities.Remove(entity.Comp.NetEntity);
            return false;
        }

        DebugTools.AssertEqual(data!.NetEntity, entity.Comp.NetEntity);
        DebugTools.AssertEqual(data.LastSeen == GameTick.Zero, data.Visibility <= PvsEntityVisibility.Unsent);
        DebugTools.AssertEqual(data.Entity, entity);
        if (data.LastSeen == _gameTiming.CurTick)
            return true;

        var (entered,budgetExceeded) = IsEnteringPvsRange(data, fromTick, ref session.Budget);

        if (!budgetExceeded)
        {
            if (!AddToSendList(session, data, fromTick, entered))
                return false;

            DebugTools.AssertNotEqual(data.LastSeen, GameTick.Zero);
            return true;
        }

        // Sending this entity would go over the player's budget, so we will not add it. However, we  do not
        // stop iterating over this (or other chunks). This is to avoid sending bad pvs-leave messages.
        // I.e., other entities may have just stayed in view, and we can send them without exceeding our
        // budget. E.g., this might be the very first chunk we are iterating over, and it just so happens
        // to be a chunk that just entered their PVS range.

        if (data.Visibility != PvsEntityVisibility.Invalid)
            return false;

        // This entity was never sent to the player, and isn't being sent now.
        // However, the data has already been added to the entityData dictionary.
        // In order for debug asserts and other sanity checks to keep working, we mark the entity as
        // explicitly unsent.
        data.Visibility = PvsEntityVisibility.Unsent;
        return false;
    }

    /// <summary>
    /// This method adds an entity to the list of visible entities, updates the last-seen tick, and computes any
    /// required game states.
    /// </summary>
    private bool AddToSendList(PvsSession session, PvsData data, GameTick fromTick, bool entered)
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
        var (uid, meta) = data.Entity;

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (meta == null)
        {
            Log.Error($"Encountered null metadata in EntityData. Entity: {ToPrettyString(data?.Entity)}");
            return false;
        }

        if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            var rep = new EntityStringRepresentation(data.Entity);
            Log.Error($"Attempted to add a deleted entity to PVS send set: '{rep}'. Deletion queued: {EntityManager.IsQueuedForDeletion(data.Entity)}. Trace:\n{Environment.StackTrace}");

            // This can happen if some entity was some removed from it's parent while that parent was being deleted.
            // As a result the entity was marked for deletion but was never actually properly deleted.
            EntityManager.QueueDeleteEntity(data.Entity);
            return false;
        }

        data.LastSeen = _gameTiming.CurTick;
        session.ToSend!.Add(data);
        EntityState state;

        if (session.RequestedFull)
        {
            state = GetFullEntityState(session.Session, data.Entity.Owner, data.Entity.Comp);
            session.States.Add(state);
            return true;
        }

        if (entered)
        {
            data.Visibility = PvsEntityVisibility.Entered;
            state = GetEntityState(session.Session, uid, data.EntityLastAcked, meta);
            session.States.Add(state);
            return true;
        }

        if (meta.EntityLastModifiedTick <= fromTick)
        {
            //entity has been sent before and hasn't been updated since
            data.Visibility = PvsEntityVisibility.Unchanged;
            return true;
        }

        data.Visibility = PvsEntityVisibility.Dirty;
        state = GetEntityState(session.Session, uid, fromTick , meta);

        if (!state.Empty)
            session.States.Add(state);

        return true;
    }

    /// <summary>
    /// This method figures out whether a given entity is currently entering a player's PVS range.
    /// This method will also check that the player's PVS entry budget is not being exceeded.
    /// </summary>
    private (bool Entering, bool BudgetExceeded) IsEnteringPvsRange(
        PvsData data,
        GameTick fromTick,
        ref PvsBudget budget)
    {
        DebugTools.AssertEqual(data.LastSeen == GameTick.Zero, data.Visibility <= PvsEntityVisibility.Unsent);

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
