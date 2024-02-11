namespace Robust.Shared.GameObjects;

/// <summary>
/// Helper extension methods for working with Bound User Interface events.
/// </summary>
/// <seealso cref="BoundUserInterfaceMessage"/>
public static class BoundUserInterfaceRegisterExt
{
    /// <summary>
    /// Delegate that subscribes to actual BUI events.
    /// Used as a lambda via <see cref="BoundUserInterfaceRegisterExt.BuiEvents{TComp}"/>.
    /// </summary>
    /// <typeparam name="TComp">The type of component that will receive the events.</typeparam>
    public delegate void BuiEventSubscriber<TComp>(Subscriber<TComp> subscriber) where TComp : IComponent;

    /// <summary>
    /// Extension method to subscribe to one or more Bound User Interface events via a system,
    /// sharing the same UI key and owning component.
    /// </summary>
    /// <param name="subs">
    /// The entity system subscriptions.
    /// Call this with <see cref="EntitySystem.Subscriptions"/>.
    /// </param>
    /// <param name="uiKey">
    /// The UI key to filter these subscriptions. The handler will only receive events targeted for this UI key.
    /// </param>
    /// <param name="subscriber">The delegate that will subscribe to the actual events.</param>
    /// <typeparam name="TComp">The type of component that will receive the events.</typeparam>
    /// <seealso cref="Subscriber{TComp}"/>
    public static void BuiEvents<TComp>(
        this EntitySystem.Subscriptions subs,
        object uiKey,
        BuiEventSubscriber<TComp> subscriber)
        where TComp : IComponent
    {
        subscriber(new Subscriber<TComp>(subs, uiKey));
    }

    /// <summary>
    /// Helper class to register Bound User Interface subscriptions against.
    /// Created by <see cref="BoundUserInterfaceRegisterExt.BuiEvents{TComp}"/>.
    /// </summary>
    /// <typeparam name="TComp">The type of component that will receive the events.</typeparam>
    public sealed class Subscriber<TComp> where TComp : IComponent
    {
        private readonly EntitySystem.Subscriptions _subs;
        private readonly object _uiKey;

        internal Subscriber(EntitySystem.Subscriptions subs, object uiKey)
        {
            _subs = subs;
            _uiKey = uiKey;
        }

        /// <summary>
        /// Subscribe to a local event. This is effectively equivalent to <see cref="M:Robust.Shared.GameObjects.EntitySystem.SubscribeLocalEvent``2(Robust.Shared.GameObjects.ComponentEventHandler{``0,``1},System.Type[],System.Type[])"/>,
        /// but reduces repetition and automatically filters for the appropriate UI key.
        /// </summary>
        /// <param name="handler">The handler that will get executed whenever the appropriate event is raised.</param>
        /// <typeparam name="TEvent">The type of event to handle with this subscription.</typeparam>
        /// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.SubscribeLocalEvent``2(Robust.Shared.GameObjects.ComponentEventHandler{``0,``1},System.Type[],System.Type[])"/>
        public void Event<TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TEvent : BaseBoundUserInterfaceEvent
        {
            _subs.SubscribeLocalEvent<TComp, TEvent>((uid, component, args) =>
            {
                if (!_uiKey.Equals(args.UiKey))
                    return;

                handler(uid, component, args);
            });
        }

        /// <summary>
        /// Subscribe to a local event. This is effectively equivalent to <see cref="M:Robust.Shared.GameObjects.EntitySystem.SubscribeLocalEvent``2(Robust.Shared.GameObjects.ComponentEventHandler{``0,``1},System.Type[],System.Type[])"/>,
        /// but reduces repetition and automatically filters for the appropriate UI key.
        /// </summary>
        /// <param name="handler">The handler that will get executed whenever the appropriate event is raised.</param>
        /// <typeparam name="TEvent">The type of event to handle with this subscription.</typeparam>
        /// <seealso cref="M:Robust.Shared.GameObjects.EntitySystem.SubscribeLocalEvent``2(Robust.Shared.GameObjects.ComponentEventHandler{``0,``1},System.Type[],System.Type[])"/>
        public void Event<TEvent>(EntityEventRefHandler<TComp, TEvent> handler)
            where TEvent : BaseBoundUserInterfaceEvent
        {
            _subs.SubscribeLocalEvent((Entity<TComp> ent, ref TEvent args) =>
            {
                if (!_uiKey.Equals(args.UiKey))
                    return;

                handler(ent, ref args);
            });
        }
    }
}
