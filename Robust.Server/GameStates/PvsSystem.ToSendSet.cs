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
        NetEntity ent,
        ref EntityData data,
        List<NetEntity> list,
        GameTick curTick,
        GameTick fromTick,
        bool entered,
        ref int dirtyEntityCount)
    {
        var meta = data.Entity.Comp;
        DebugTools.AssertEqual(meta.NetEntity, ent);
        DebugTools.Assert(fromTick < curTick);
        DebugTools.AssertNotEqual(data.LastSent, curTick);

        if (meta.EntityLifeStage >= EntityLifeStage.Terminating)
        {
            var rep = new EntityStringRepresentation(data.Entity);
            Log.Error($"Attempted to add a deleted entity to PVS send set: '{rep}'. Deletion queued: {EntityManager.IsQueuedForDeletion(data.Entity)}. Trace:\n{Environment.StackTrace}");

            // This can happen if some entity was some removed from it's parent while that parent was being deleted.
            // As a result the entity was marked for deletion but was never actually properly deleted.
            EntityManager.QueueDeleteEntity(data.Entity);
            return;
        }

        data.LastSent = curTick;
        list.Add(ent);

        if (entered)
        {
            data.Visibility = PvsEntityVisibility.Entered;
            dirtyEntityCount++;
            return;
        }

        if (meta.EntityLastModifiedTick <= fromTick)
        {
            //entity has been sent before and hasn't been updated since
            data.Visibility = PvsEntityVisibility.StayedUnchanged;
            return;
        }

        //add us
        data.Visibility = PvsEntityVisibility.StayedChanged;
        dirtyEntityCount++;
    }

    /// <summary>
    /// This method figures out whether a given entity is currently entering a player's PVS range.
    /// This method will also check that the player's PVS entry budget is not being exceeded.
    /// </summary>
    private (bool Entered, bool UnderBudget) GetPvsEntryData(
        ref EntityData entity,
        GameTick fromTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        int newEntityBudget,
        int enteredEntityBudget)
    {
        var enteredSinceLastSent = fromTick == GameTick.Zero
                                   || entity.LastSent == GameTick.Zero
                                   || entity.LastSent.Value == fromTick.Value - 1;

        var entered = enteredSinceLastSent
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
                return (entered, false);

            enteredEntityCount++;

            if (entity.EntityLastAcked == GameTick.Zero)
                newEntityCount++;
        }

        return (entered, true);
    }

    /// <summary>
    /// Recursively add an entity and all of its children to the to-send set.
    /// </summary>
    private void RecursivelyAddTreeNode(in NetEntity nodeIndex,
        RobustTree<NetEntity> tree,
        List<NetEntity> toSend,
        Dictionary<NetEntity, EntityData> entityData,
        Stack<NetEntity> stack,
        GameTick fromTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        ref int dirtyEntityCount,
        int newEntityBudget,
        int enteredEntityBudget)
    {
        stack.Push(nodeIndex);

        var curTick = _gameTiming.CurTick;

        while (stack.TryPop(out var currentNodeIndex))
        {
            DebugTools.Assert(currentNodeIndex.IsValid());

            // As every map is parented to uid 0 in the tree we still need to get their children, plus because we go top-down
            // we may find duplicate parents with children we haven't encountered before
            // on different chunks (this is especially common with direct grid children)

            ref var data = ref GetOrNewEntityData(entityData, currentNodeIndex);
            if (data.LastSent != curTick)
            {
                var (entered, underBudget) = GetPvsEntryData(ref data, fromTick,
                    ref newEntityCount, ref enteredEntityCount, newEntityBudget, enteredEntityBudget);

                if (!underBudget)
                    continue;

                AddToSendList(currentNodeIndex, ref data, toSend, curTick, fromTick, entered, ref dirtyEntityCount);
            }

            var node = tree[currentNodeIndex];
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
        List<NetEntity> toSend,
        Dictionary<NetEntity, EntityData> entityData,
        GameTick fromTick,
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
        if (parent.IsValid() && !RecursivelyAddOverride(in parent, toSend, entityData, fromTick,
                ref newEntityCount, ref enteredEntityCount, ref dirtyEntityCount, newEntityBudget, enteredEntityBudget))
        {
            return false;
        }

        var netEntity = _metaQuery.GetComponent(uid).NetEntity;

        // Note that we check this AFTER adding parents. This is because while this entity may already have been added
        // to the toSend set, it doesn't guarantee that its parents have been. E.g., if a player ghost just teleported
        // to follow a far away entity, the player's own entity is still being sent, but we need to ensure that we also
        // send the new parents, which may otherwise be delayed because of the PVS budget.

        var curTick = _gameTiming.CurTick;
        ref var data = ref GetOrNewEntityData(entityData, netEntity);
        if (data.LastSent != curTick)
        {
            var (entered, _) = GetPvsEntryData(ref data, fromTick, ref newEntityCount, ref enteredEntityCount, newEntityBudget, enteredEntityBudget);
            AddToSendList(netEntity, ref data, toSend, curTick, fromTick, entered, ref dirtyEntityCount);
        }

        if (addChildren)
        {
            RecursivelyAddChildren(xform, toSend, entityData, fromTick, ref newEntityCount,
                ref enteredEntityCount, ref dirtyEntityCount, in newEntityBudget, in enteredEntityBudget);
        }

        return true;
    }

    /// <summary>
    /// Recursively add an entity and all of its children to the to-send set.
    /// </summary>
    private void RecursivelyAddChildren(TransformComponent xform,
        List<NetEntity> toSend,
        Dictionary<NetEntity, EntityData> entityData,
        in GameTick fromTick,
        ref int newEntityCount,
        ref int enteredEntityCount,
        ref int dirtyEntityCount,
        in int newEntityBudget,
        in int enteredEntityBudget)
    {
        var curTick = _gameTiming.CurTick;
        foreach (var child in xform._children)
        {
            if (!_xformQuery.TryGetComponent(child, out var childXform))
                continue;

            var metadata = _metaQuery.GetComponent(child);
            var netChild = metadata.NetEntity;
            ref var data = ref GetOrNewEntityData(entityData, netChild);
            if (data.LastSent != curTick)
            {
                var (entered, _) = GetPvsEntryData(ref data, fromTick, ref newEntityCount,
                    ref enteredEntityCount, newEntityBudget, enteredEntityBudget);
                AddToSendList(netChild, ref data, toSend, curTick, fromTick, entered, ref dirtyEntityCount);
            }

            RecursivelyAddChildren(childXform, toSend, entityData, fromTick, ref newEntityCount,
                ref enteredEntityCount, ref dirtyEntityCount, in newEntityBudget, in enteredEntityBudget);
        }
    }
}
