using Robust.Shared.GameObjects;
using Robust.Shared.Player;

namespace Robust.Server.GameObjects;

/// <summary>
///     Entity System that handles subscribing and unsubscribing to PVS views.
/// </summary>
public sealed class ViewSubscriberSystem : SharedViewSubscriberSystem
{
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ViewSubscriberComponent, ComponentShutdown>(OnViewSubscriberShutdown);
    }

    /// <summary>
    ///     Subscribes the session to get PVS updates from the point of view of the specified entity.
    /// </summary>
    public override void AddViewSubscriber(EntityUid uid, ICommonSession session)
    {
        // If the entity doesn't have the component, it will be added.
        var viewSubscriber = EntityManager.EnsureComponent<Shared.GameObjects.ViewSubscriberComponent>(uid);

        if (viewSubscriber.SubscribedSessions.Contains(session))
            return; // Already subscribed, do nothing else.

        viewSubscriber.SubscribedSessions.Add(session);
        session.ViewSubscriptions.Add(uid);

        RaiseLocalEvent(uid, new ViewSubscriberAddedEvent(uid, session), true);
    }

    /// <summary>
    ///     Unsubscribes the session from getting PVS updates from the point of view of the specified entity.
    /// </summary>
    public override void RemoveViewSubscriber(EntityUid uid, ICommonSession session)
    {
        if(!EntityManager.TryGetComponent(uid, out Shared.GameObjects.ViewSubscriberComponent? viewSubscriber))
            return; // Entity didn't have any subscriptions, do nothing.

        if (!viewSubscriber.SubscribedSessions.Remove(session))
            return; // Session wasn't subscribed, do nothing.

        session.ViewSubscriptions.Remove(uid);
        RaiseLocalEvent(uid, new ViewSubscriberRemovedEvent(uid, session), true);
    }

    private void OnViewSubscriberShutdown(EntityUid uid, ViewSubscriberComponent component, ComponentShutdown _)
    {
        foreach (var session in component.SubscribedSessions)
        {
            session.ViewSubscriptions.Remove(uid);
        }
    }
}

/// <summary>
///     Raised when a session subscribes to an entity's PVS view.
/// </summary>
public sealed class ViewSubscriberAddedEvent : EntityEventArgs
{
    public EntityUid View { get; }
    public ICommonSession Subscriber { get; }

    public ViewSubscriberAddedEvent(EntityUid view, ICommonSession subscriber)
    {
        View = view;
        Subscriber = subscriber;
    }
}

/// <summary>
///     Raised when a session is unsubscribed from an entity's PVS view.
///     Not raised when sessions are unsubscribed due to the component being removed.
/// </summary>
public sealed class ViewSubscriberRemovedEvent : EntityEventArgs
{
    public EntityUid View { get; }
    public ICommonSession Subscriber { get; }

    public ViewSubscriberRemovedEvent(EntityUid view, ICommonSession subscriber)
    {
        View = view;
        Subscriber = subscriber;
    }
}
