using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

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
        // Cannot Use Resolve(ref toInsert) as the physics component is optional
        if (!Resolve(toRemove.Owner, ref toRemove.Comp1, ref toRemove.Comp2))
            return false;

        // TODO move logic over to the system.
        return container.Remove(toRemove, EntityManager, toRemove, toRemove, reparent, force, destination, localRotation);
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
