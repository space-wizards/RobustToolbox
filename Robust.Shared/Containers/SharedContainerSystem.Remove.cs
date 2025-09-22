using System;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Utility;

namespace Robust.Shared.Containers;

public abstract partial class SharedContainerSystem
{
    /// <summary>
    /// Attempts to remove the entity from this container.
    /// </summary>
    /// <remarks>
    /// If the insertion is successful, the inserted entity will end up parented to the
    /// container entity, and the inserted entity's local position will be set to the zero vector.
    /// </remarks>
    /// <param name="toRemove">The entity to remove.</param>
    /// <param name="container">The container to remove from.</param>
    /// <param name="reparent">If false, this operation will not rigger a move or parent change event. Ignored if
    /// destination is not null</param>
    /// <param name="force">If true, this will not perform can-remove checks.</param>
    /// <param name="destination">Where to place the entity after removing. Avoids unnecessary broadphase updates.
    /// If not specified, and reparent option is true, then the entity will either be inserted into a parent
    /// container, the grid, or the map.</param>
    /// <param name="localRotation">Optional final local rotation after removal. Avoids redundant move events.</param>
    public bool Remove(
        Entity<TransformComponent?, MetaDataComponent?> toRemove,
        BaseContainer container,
        bool reparent = true,
        bool force = false,
        EntityCoordinates? destination = null,
        Angle? localRotation = null)
    {
        var (uid, xform, meta) = toRemove;

        // Cannot Use Resolve(ref toInsert) as the physics component is optional
        if (!Resolve(uid, ref xform, ref meta))
            return false;

        DebugTools.AssertNotNull(container.Manager);
        DebugTools.Assert(Exists(toRemove), "toRemove does not exist");

        if (!force && !CanRemove(toRemove, container))
            return false;

        if (force && !container.Contains(toRemove))
        {
            DebugTools.Assert("Attempted to force remove an entity that was never inside of the container.");
            return false;
        }

        // Terminating entities should not get re-parented. However, this removal will still be forced when
        // detaching to null-space just before deletion happens.
        if (meta.EntityLifeStage >= EntityLifeStage.Terminating && (!force || reparent))
        {
            Log.Error($"Attempting to remove an entity from a container while it is terminating. Entity: {ToPrettyString(toRemove, meta)}. Container: {ToPrettyString(container.Owner)}. Trace: {Environment.StackTrace}");
            return false;
        }

        DebugTools.Assert(meta.EntityLifeStage < EntityLifeStage.Terminating || (force && !reparent), "Entity is terminating");
        DebugTools.Assert(xform.Broadphase == null || !xform.Broadphase.Value.IsValid(), "broadphase is invalid");
        DebugTools.Assert(!xform.Anchored || _timing.ApplyingState, "anchor is invalid");
        DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) != 0x0, "metadata is invalid");
        DebugTools.Assert(!TryComp(toRemove, out PhysicsComponent? phys) || (!phys.Awake && !phys.CanCollide), "physics is invalid");

        // Unset flag (before parent change events are raised).
        meta.Flags &= ~MetaDataFlags.InContainer;

        // Implementation specific remove logic
        container.InternalRemove(toRemove, EntityManager);

        DebugTools.Assert((meta.Flags & MetaDataFlags.InContainer) == 0x0);
        var oldParent = xform.ParentUid;

        if (destination != null)
        {
            // Container ECS when.
            _transform.SetCoordinates(toRemove, xform, destination.Value, localRotation);
        }
        else if (reparent)
        {
            // Container ECS when.
            AttachParentToContainerOrGrid((toRemove, xform));
            if (localRotation != null)
                _transform.SetLocalRotation(uid, localRotation.Value, xform);
        }

        // Add to new broadphase
        if (xform.ParentUid == oldParent // move event should already have handled it
            && xform.Broadphase == null) // broadphase explicitly invalid?
        {
            _lookup.FindAndAddToEntityTree(toRemove, xform: xform);
        }

        if (TryComp<JointComponent>(toRemove, out var jointComp))
        {
            _joint.RefreshRelay(toRemove, jointComp);
        }

        // Raise container events (after re-parenting and internal remove).
        RaiseLocalEvent(container.Owner, new EntRemovedFromContainerMessage(toRemove, container), true);
        RaiseLocalEvent(toRemove, new EntGotRemovedFromContainerMessage(toRemove, container), false);

        DebugTools.Assert(destination == null || xform.Coordinates.Equals(destination.Value), "failed to set destination");

        Dirty(container.Owner, container.Manager);
        return true;
    }

    /// <summary>
    /// Checks if the entity can be removed from this container.
    /// </summary>
    /// <returns>True if the entity can be removed, false otherwise.</returns>
    public bool CanRemove(EntityUid toRemove, BaseContainer container)
    {
        if (!container.Contains(toRemove))
            return false;

        //raise events
        var removeAttemptEvent = new ContainerIsRemovingAttemptEvent(container, toRemove);
        RaiseLocalEvent(container.Owner, removeAttemptEvent, true);
        if (removeAttemptEvent.Cancelled)
            return false;

        var gettingRemovedAttemptEvent = new ContainerGettingRemovedAttemptEvent(container, toRemove);
        RaiseLocalEvent(toRemove, gettingRemovedAttemptEvent, true);
        return !gettingRemovedAttemptEvent.Cancelled;
    }
}
