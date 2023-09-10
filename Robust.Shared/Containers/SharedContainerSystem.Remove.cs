using Robust.Shared.GameObjects;

namespace Robust.Shared.Containers;

public abstract partial class SharedContainerSystem
{
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
