using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Entity System that handles subscribing and unsubscribing regarding PVS Eyes.
    /// </summary>
    public class ViewSubscriberSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<ViewSubscriberComponent, ComponentShutdown>(OnViewSubscriberShutdown);
        }

        /// <summary>
        ///     Subscribes the session to get PVS updates from the point of view of the specified entity.
        /// </summary>
        public void AddViewSubscriber(EntityUid uid, IPlayerSession session)
        {
            // This will throw if you pass in an invalid uid.
            var entity = EntityManager.GetEntity(uid);

            // If the entity doesn't have the component, it will be added.
            var pvsEye = entity.EnsureComponent<ViewSubscriberComponent>();

            if (pvsEye.SubscribedSessions.Contains(session))
                return; // Already subscribed, do nothing else.

            pvsEye.SubscribedSessions.Add(session);
            session.AddViewSubscription(uid);

            RaiseLocalEvent(uid, new ViewSubscriberAddedEvent(entity, session));
        }

        /// <summary>
        ///     Unsubscribes the session from getting PVS updates from the point of view of the specified entity.
        /// </summary>
        public void RemoveViewSubscriber(EntityUid uid, IPlayerSession session)
        {
            if(!ComponentManager.TryGetComponent(uid, out ViewSubscriberComponent? pvsEye))
                return; // Entity didn't have any subscriptions, do nothing.

            if (!pvsEye.SubscribedSessions.Remove(session))
                return; // Session wasn't subscribed, do nothing.

            session.RemoveViewSubscription(uid);
            RaiseLocalEvent(uid, new ViewSubscriberRemovedEvent(EntityManager.GetEntity(uid), session));
        }

        private void OnViewSubscriberShutdown(EntityUid uid, ViewSubscriberComponent component, ComponentShutdown _)
        {
            foreach (var session in component.SubscribedSessions)
            {
                session.RemoveViewSubscription(uid);
            }
        }
    }

    /// <summary>
    ///     Raised when a session subscribes to a PVS eye.
    /// </summary>
    public class ViewSubscriberAddedEvent : EntityEventArgs
    {
        public IEntity View { get; }
        public IPlayerSession Subscriber { get; }

        public ViewSubscriberAddedEvent(IEntity view, IPlayerSession subscriber)
        {
            View = view;
            Subscriber = subscriber;
        }
    }

    /// <summary>
    ///     Raised when a session is unsubscribed from a PVS eye.
    ///     Not raised when sessions are unsubscribed due to the component being removed.
    /// </summary>
    public class ViewSubscriberRemovedEvent : EntityEventArgs
    {
        public IEntity View { get; }
        public IPlayerSession Subscriber { get; }

        public ViewSubscriberRemovedEvent(IEntity view, IPlayerSession subscriber)
        {
            View = view;
            Subscriber = subscriber;
        }
    }
}
