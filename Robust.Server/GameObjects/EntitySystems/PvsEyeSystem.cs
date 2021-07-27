using Robust.Server.Player;
using Robust.Shared.GameObjects;

namespace Robust.Server.GameObjects
{
    /// <summary>
    ///     Entity System that handles subscribing and unsubscribing regarding PVS Eyes.
    /// </summary>
    public class PvsEyeSystem : EntitySystem
    {
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<PvsEyeComponent, ComponentShutdown>(OnPvsEyeShutdown);
        }

        /// <summary>
        ///     Subscribes the session to get PVS updates from the point of view of the specified entity.
        /// </summary>
        public void AddPvsEyeSubscriber(EntityUid uid, IPlayerSession session)
        {
            // This will throw if you pass in an invalid uid.
            var entity = EntityManager.GetEntity(uid);

            // If the entity doesn't have the component, it will be added.
            var pvsEye = entity.EnsureComponent<PvsEyeComponent>();

            if (pvsEye.SubscribedSessions.Contains(session))
                return; // Already subscribed, do nothing else.

            pvsEye.SubscribedSessions.Add(session);
            session.AddPvsEyeSubscription(uid);

            RaiseLocalEvent(uid, new PvsEyeSubscriberAddedEvent(entity, session));
        }

        /// <summary>
        ///     Unsubscribes the session from getting PVS updates from the point of view of the specified entity.
        /// </summary>
        public void RemovePvsEyeSubscriber(EntityUid uid, IPlayerSession session)
        {
            if(!ComponentManager.TryGetComponent(uid, out PvsEyeComponent? pvsEye))
                return; // Entity didn't have any subscriptions, do nothing.

            if (!pvsEye.SubscribedSessions.Remove(session))
                return; // Session wasn't subscribed, do nothing.

            session.RemovePvsEyeSubscription(uid);
            RaiseLocalEvent(uid, new PvsEyeSubscriberRemovedEvent(EntityManager.GetEntity(uid), session));
        }

        private void OnPvsEyeShutdown(EntityUid uid, PvsEyeComponent component, ComponentShutdown _)
        {
            foreach (var session in component.SubscribedSessions)
            {
                session.RemovePvsEyeSubscription(uid);
            }
        }
    }

    /// <summary>
    ///     Raised when a session subscribes to a PVS eye.
    /// </summary>
    public class PvsEyeSubscriberAddedEvent : EntityEventArgs
    {
        public IEntity PvsEye { get; }
        public IPlayerSession Subscriber { get; }

        public PvsEyeSubscriberAddedEvent(IEntity pvsEye, IPlayerSession subscriber)
        {
            PvsEye = pvsEye;
            Subscriber = subscriber;
        }
    }

    /// <summary>
    ///     Raised when a session is unsubscribed from a PVS eye.
    ///     Not raised when sessions are unsubscribed due to the component being removed.
    /// </summary>
    public class PvsEyeSubscriberRemovedEvent : EntityEventArgs
    {
        public IEntity PvsEye { get; }
        public IPlayerSession Subscriber { get; }

        public PvsEyeSubscriberRemovedEvent(IEntity pvsEye, IPlayerSession subscriber)
        {
            PvsEye = pvsEye;
            Subscriber = subscriber;
        }
    }
}
