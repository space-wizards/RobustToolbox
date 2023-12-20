using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Robust.Shared.GameObjects;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Server.GameStates;

// This partial class contains contains methods for adding entities to the set of entities that are about to get sent
// to a player.
internal sealed partial class PvsSystem
{
    /// <summary>
    /// This method adds an entity to the to-send list, updates the last-sent tick, and updates the entity's visibility.
    /// </summary>
    private void AddToSendList(
        EntityData data,
        List<EntityData> list,
        GameTick fromTick,
        GameTick toTick,
        bool entered,
        ref int dirtyEntityCount)
    {
        if (data == null)
        {
            Log.Error($"Encountered null EntityData.");
            return;
        }

        var meta = data.Entity.Comp;
        DebugTools.Assert(fromTick < toTick);
        DebugTools.AssertNotEqual(data.LastSent, toTick);
        DebugTools.AssertEqual(toTick, _gameTiming.CurTick);

        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (meta == null)
        {
            Log.Error($"Encountered null metadata in EntityData. Entity: {ToPrettyString(data?.Entity)}");
            return;
        }

        if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            var rep = new EntityStringRepresentation(data.Entity);
            Log.Error($"Attempted to add a deleted entity to PVS send set: '{rep}'. Deletion queued: {EntityManager.IsQueuedForDeletion(data.Entity)}. Trace:\n{Environment.StackTrace}");

            // This can happen if some entity was some removed from it's parent while that parent was being deleted.
            // As a result the entity was marked for deletion but was never actually properly deleted.
            EntityManager.QueueDeleteEntity(data.Entity);
            return;
        }

        data.LastSent = toTick;
        list.Add(data);

        if (entered)
        {
            data.Visibility = PvsEntityVisibility.Entered;
            dirtyEntityCount++;
            return;
        }

        if (meta.EntityLastModifiedTick <= fromTick)
        {
            //entity has been sent before and hasn't been updated since
            data.Visibility = PvsEntityVisibility.Unchanged;
            return;
        }

        //add us
        data.Visibility = PvsEntityVisibility.Dirty;
        dirtyEntityCount++;
    }

    /// <summary>
    /// This method figures out whether a given entity is currently entering a player's PVS range.
    /// This method will also check that the player's PVS entry budget is not being exceeded.
    /// </summary>
    private (bool Entering, bool BudgetExceeded) IsEnteringPvsRange(EntityData entity,
        GameTick fromTick,
        GameTick toTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        int newEntityBudget,
        int enteredEntityBudget)
    {
        DebugTools.AssertEqual(toTick, _gameTiming.CurTick);
        DebugTools.AssertEqual(entity.LastSent == GameTick.Zero, entity.Visibility <= PvsEntityVisibility.Unsent);

        var enteredSinceLastSent = fromTick == GameTick.Zero
                                   || entity.LastSent == GameTick.Zero
                                   || entity.LastSent.Value != toTick.Value - 1;

        var entering = enteredSinceLastSent
                      || entity.EntityLastAcked == GameTick.Zero
                      || entity.EntityLastAcked < fromTick // this entity was not in the last acked state.
                      || entity.LastLeftView >= fromTick; // entity left and re-entered sometime after the last acked tick

        // If the entity is entering, but we already sent this entering entity in the last message, we won't add it to
        // the budget. Chances are the packet will arrive in a nice and orderly fashion, and the client will stick to
        // their requested budget. However this can cause issues if a packet gets dropped, because a player may create
        // 2x or more times the normal entity creation budget.
        if (enteredSinceLastSent)
        {
            if (newEntityCount >= newEntityBudget || enteredEntityCount >= enteredEntityBudget)
                return (entering, true);

            enteredEntityCount++;

            if (entity.EntityLastAcked == GameTick.Zero)
                newEntityCount++;
        }

        return (entering, false);
    }

    /// <summary>
    /// Recursively add an entity and all of its children to the to-send set.
    /// </summary>
    private void RecursivelyAddTreeNode(in NetEntity nodeIndex,
        RobustTree<NetEntity> tree,
        List<EntityData> toSend,
        Dictionary<NetEntity, EntityData> entityData,
        Stack<NetEntity> stack,
        GameTick fromTick,
        GameTick toTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        ref int dirtyEntityCount,
        int newEntityBudget,
        int enteredEntityBudget)
    {
        stack.Push(nodeIndex);

        while (stack.TryPop(out var currentNodeIndex))
        {
            DebugTools.Assert(currentNodeIndex.IsValid());

            // As every map is parented to uid 0 in the tree we still need to get their children, plus because we go top-down
            // we may find duplicate parents with children we haven't encountered before
            // on different chunks (this is especially common with direct grid children)

            var data = GetOrNewEntityData(entityData, currentNodeIndex);
            if (data.LastSent != toTick)
            {
                var (entered, budgetExceeded) = IsEnteringPvsRange(data, fromTick, toTick,
                    ref newEntityCount, ref enteredEntityCount, newEntityBudget, enteredEntityBudget);

                if (budgetExceeded)
                {
                    if (data.Visibility == PvsEntityVisibility.Invalid)
                    {
                        // This entity was never sent to the player, and isn't being sent now.
                        // However, the data has already been added to the entityData dictionary.
                        // In order for debug asserts and other sanity checks to keep working, we mark the entity as
                        // explicitly unsent.
                        data.Visibility = PvsEntityVisibility.Unsent;
                    }

                    // Sending this entity would go over the player's budget, so we will not add it. However, we  do not
                    // stop iterating over this (or other chunks). This is to avoid sending bad pvs-leave messages.
                    // I.e., other entities may have just stayed in view, and we can send them without exceeding our
                    // budget. E.g., this might be the very first chunk we are iterating over, and it just so happens
                    // to be a chunk that just entered their PVS range.
                    continue;
                }

                AddToSendList(data, toSend, fromTick, toTick, entered, ref dirtyEntityCount);
                DebugTools.AssertNotEqual(data.LastSent, GameTick.Zero);
            }

            if (!tree.TryGet(currentNodeIndex, out var node))
            {
                Log.Error($"tree is missing the current node! Node: {currentNodeIndex}");
                continue;
            }

            if (node.Children == null)
                continue;

            foreach (var child in node.Children)
            {
                stack.Push(child);
            }
        }
    }

    /// <summary>
    /// Recursively add an entity and all of its parents to the to-send set. This optionally also adds all children.
    /// </summary>
    public bool RecursivelyAddOverride(in EntityUid uid,
        List<EntityData> toSend,
        Dictionary<NetEntity, EntityData> entityData,
        GameTick fromTick,
        GameTick toTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        ref int dirtyEntityCount,
        int newEntityBudget,
        int enteredEntityBudget,
        bool addChildren = false)
    {
        //are we valid?
        //sometimes uids gets added without being valid YET (looking at you mapmanager) (mapcreate & gridcreated fire before the uids becomes valid)
        if (!uid.IsValid())
            return false;

        var xform = _xformQuery.GetComponent(uid);
        var parent = xform.ParentUid;
        if (parent.IsValid() && !RecursivelyAddOverride(in parent, toSend, entityData, fromTick, toTick,
                ref newEntityCount, ref enteredEntityCount, ref dirtyEntityCount, newEntityBudget, enteredEntityBudget))
        {
            return false;
        }

        var netEntity = _metaQuery.GetComponent(uid).NetEntity;

        // Note that we check this AFTER adding parents. This is because while this entity may already have been added
        // to the toSend set, it doesn't guarantee that its parents have been. E.g., if a player ghost just teleported
        // to follow a far away entity, the player's own entity is still being sent, but we need to ensure that we also
        // send the new parents, which may otherwise be delayed because of the PVS budget.

        var data = GetOrNewEntityData(entityData, netEntity);
        if (data.LastSent != toTick)
        {
            var (entered, _) = IsEnteringPvsRange(data, fromTick, toTick, ref newEntityCount, ref enteredEntityCount, newEntityBudget, enteredEntityBudget);
            AddToSendList(data, toSend, fromTick, toTick, entered, ref dirtyEntityCount);
        }

        if (addChildren)
        {
            RecursivelyAddChildren(xform, toSend, entityData, fromTick, toTick, ref newEntityCount,
                ref enteredEntityCount, ref dirtyEntityCount, in newEntityBudget, in enteredEntityBudget);
        }

        return true;
    }

    /// <summary>
    /// Recursively add an entity and all of its children to the to-send set.
    /// </summary>
    private void RecursivelyAddChildren(TransformComponent xform,
        List<EntityData> toSend,
        Dictionary<NetEntity, EntityData> entityData,
        in GameTick fromTick,
        in GameTick toTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        ref int dirtyEntityCount,
        in int newEntityBudget,
        in int enteredEntityBudget)
    {
        foreach (var child in xform._children)
        {
            if (!_xformQuery.TryGetComponent(child, out var childXform))
                continue;

            var metadata = _metaQuery.GetComponent(child);
            var netChild = metadata.NetEntity;
            var data = GetOrNewEntityData(entityData, netChild);
            if (data.LastSent != toTick)
            {
                var (entered, _) = IsEnteringPvsRange(data, fromTick, toTick, ref newEntityCount,
                    ref enteredEntityCount, newEntityBudget, enteredEntityBudget);
                AddToSendList(data, toSend, fromTick, toTick, entered, ref dirtyEntityCount);
            }

            RecursivelyAddChildren(childXform, toSend, entityData, fromTick, toTick, ref newEntityCount,
                ref enteredEntityCount, ref dirtyEntityCount, in newEntityBudget, in enteredEntityBudget);
        }
    }
}
