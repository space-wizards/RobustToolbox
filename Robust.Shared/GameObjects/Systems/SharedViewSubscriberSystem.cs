using Robust.Shared.Player;

namespace Robust.Shared.GameObjects;

public abstract class SharedViewSubscriberSystem : EntitySystem
{
    // NOOP on client

    /// <summary>
    ///     Subscribes the session to get PVS updates from the point of view of the specified entity.
    /// </summary>
    public virtual void AddViewSubscriber(EntityUid uid, ICommonSession session) {}

    /// <summary>
    ///     Unsubscribes the session from getting PVS updates from the point of view of the specified entity.
    /// </summary>
    public virtual void RemoveViewSubscriber(EntityUid uid, ICommonSession session) {}
}
