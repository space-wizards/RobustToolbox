using System;
using JetBrains.Annotations;
using Robust.Shared.Collections;

namespace Robust.Shared.GameObjects
{
    public abstract partial class EntitySystem
    {
        private ValueList<SubBase> _subscriptions;

        /// <summary>
        /// A handle to allow subscription on this entity system's behalf.
        /// </summary>
        protected Subscriptions Subs { get; }

        // NOTE: EntityEventHandler<T> and EntitySessionEventHandler<T> CANNOT BE ORDERED BETWEEN EACH OTHER.
        // EntityEventRefHandler<T> and EntityEventHandler<T> can be, however. They're essentially the same.

        protected void SubscribeNetworkEvent<T>(
            EntityEventHandler<T> handler,
            Type[]? before = null, Type[]? after = null)
            where T : notnull
        {
            SubEvent(EventSource.Network, handler, before, after);
        }

        /// <seealso cref="SubscribeLocalEvent{T}(EntityEventRefHandler{T}, Type[], Type[])"/>
        // [Obsolete("Subscribe to the event by ref instead (EntityEventRefHandler)")]
        protected void SubscribeLocalEvent<T>(
            EntityEventHandler<T> handler,
            Type[]? before = null, Type[]? after = null)
            where T : notnull
        {
            SubEvent(EventSource.Local, handler, before, after);
        }

        protected void SubscribeLocalEvent<T>(
            EntityEventRefHandler<T> handler,
            Type[]? before = null, Type[]? after = null)
            where T : notnull
        {
            SubEvent(EventSource.Local, handler, before, after);
        }

        protected void SubscribeAllEvent<T>(
            EntityEventHandler<T> handler,
            Type[]? before = null, Type[]? after = null)
            where T : notnull
        {
            SubEvent(EventSource.All, handler, before, after);
        }

        protected void SubscribeNetworkEvent<T>(
            EntitySessionEventHandler<T> handler,
            Type[]? before = null, Type[]? after = null)
            where T : notnull
        {
            SubSessionEvent(EventSource.Network, handler, before, after);
        }

        protected void SubscribeLocalEvent<T>(
            EntitySessionEventHandler<T> handler,
            Type[]? before = null, Type[]? after = null)
            where T : notnull
        {
            SubSessionEvent(EventSource.Local, handler, before, after);
        }

        protected void SubscribeAllEvent<T>(
            EntitySessionEventHandler<T> handler,
            Type[]? before = null, Type[]? after = null)
            where T : notnull
        {
            SubSessionEvent(EventSource.All, handler, before, after);
        }

        /// <seealso cref="SubEvent{T}(EventSource, EntityEventRefHandler{T}, Type[], Type[])"/>
        // [Obsolete("Subscribe to the event by ref instead (EntityEventRefHandler)")]
        private void SubEvent<T>(
            EventSource src,
            EntityEventHandler<T> handler,
            Type[]? before, Type[]? after)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeEvent(src, this, handler, GetType(), before, after);

            _subscriptions.Add(new SubBroadcast<T>(src));
        }

        private void SubEvent<T>(
            EventSource src,
            EntityEventRefHandler<T> handler,
            Type[]? before, Type[]? after)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeEvent(src, this, handler, GetType(), before, after);

            _subscriptions.Add(new SubBroadcast<T>(src));
        }

        private void SubSessionEvent<T>(
            EventSource src,
            EntitySessionEventHandler<T> handler,
            Type[]? before, Type[]? after)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeSessionEvent(src, this, handler, GetType(), before, after);

            _subscriptions.Add(new SubBroadcast<EntitySessionMessage<T>>(src));
        }

        protected void SubscribeLocalEvent<TComp, TEvent>(
            EntityEventRefHandler<TComp, TEvent> handler,
            Type[]? before = null, Type[]? after = null)
            where TComp : IComponent
            where TEvent : notnull
        {
            EntityManager.EventBus.SubscribeLocalEvent(handler, GetType(), before, after);

            _subscriptions.Add(new SubLocal<TComp, TEvent>());
        }

        /// <seealso cref="SubscribeLocalEvent{TComp, TEvent}(ComponentEventRefHandler{TComp, TEvent}, Type[], Type[])"/>
        // [Obsolete("Subscribe to the event by ref instead (ComponentEventRefHandler)")]
        protected void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventHandler<TComp, TEvent> handler,
            Type[]? before = null, Type[]? after = null)
            where TComp : IComponent
            where TEvent : notnull
        {
            EntityManager.EventBus.SubscribeLocalEvent(handler, GetType(), before, after);

            _subscriptions.Add(new SubLocal<TComp, TEvent>());
        }

        protected void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventRefHandler<TComp, TEvent> handler,
            Type[]? before = null, Type[]? after = null)
            where TComp : IComponent
            where TEvent : notnull
        {
            EntityManager.EventBus.SubscribeLocalEvent(handler, GetType(), before, after);

            _subscriptions.Add(new SubLocal<TComp, TEvent>());
        }

        private void ShutdownSubscriptions()
        {
            foreach (var sub in _subscriptions)
            {
                sub.Unsubscribe(this, EntityManager.EventBus);
            }

            _subscriptions = default;
        }

        /// <summary>
        /// API class to allow registering on an EntitySystem's behalf.
        /// Intended to support creation of boilerplate-reduction-methods
        /// that need to subscribe stuff on an entity system.
        /// </summary>
        [PublicAPI]
        public sealed class Subscriptions
        {
            public EntitySystem System { get; }

            internal Subscriptions(EntitySystem system)
            {
                System = system;
            }

            // Intended for helper methods, so minimal API.

            public void SubEvent<T>(
                EventSource src,
                EntityEventHandler<T> handler,
                Type[]? before = null, Type[]? after = null)
                where T : notnull
            {
                System.SubEvent(src, handler, before, after);
            }

            public void SubSessionEvent<T>(
                EventSource src,
                EntitySessionEventHandler<T> handler,
                Type[]? before = null, Type[]? after = null)
                where T : notnull
            {
                System.SubSessionEvent(src, handler, before, after);
            }

            public void SubscribeLocalEvent<TComp, TEvent>(
                ComponentEventHandler<TComp, TEvent> handler,
                Type[]? before = null, Type[]? after = null)
                where TComp : IComponent
                where TEvent : EntityEventArgs
            {
                System.SubscribeLocalEvent(handler, before, after);
            }

            /// <summary>
            /// Proxy to <see cref="M:Robust.Shared.GameObjects.EntitySystem.SubscribeLocalEvent``2(Robust.Shared.GameObjects.ComponentEventRefHandler{``0,``1},System.Type[],System.Type[])" />
            /// on the owning system.
            /// </summary>
            public void SubscribeLocalEvent<TComp, TEvent>(
                ComponentEventRefHandler<TComp, TEvent> handler,
                Type[]? before = null, Type[]? after = null)
                where TComp : IComponent
                where TEvent : EntityEventArgs
            {
                System.SubscribeLocalEvent(handler, before, after);
            }

            /// <summary>
            /// Proxy to <see cref="M:Robust.Shared.GameObjects.EntitySystem.SubscribeLocalEvent``2(Robust.Shared.GameObjects.EntityEventRefHandler{``0,``1},System.Type[],System.Type[])" />
            /// on the owning system.
            /// </summary>
            public void SubscribeLocalEvent<TComp, TEvent>(
                EntityEventRefHandler<TComp, TEvent> handler,
                Type[]? before = null, Type[]? after = null)
                where TComp : IComponent
                where TEvent : EntityEventArgs
            {
                System.SubscribeLocalEvent(handler, before, after);
            }

            /// <summary>
            /// Register an action to be ran when this entity system is shut down.
            /// </summary>
            /// <remarks>
            /// This can be used by extension methods for <see cref="Subscriptions"/>
            /// to unsubscribe from from external sources such as CVars.
            /// </remarks>
            /// <param name="action">An action to be ran when the entity system is shut down.</param>
            public void RegisterUnsubscription(Action action)
            {
                System._subscriptions.Add(new SubAction(action));
            }
        }

        private abstract class SubBase
        {
            public abstract void Unsubscribe(EntitySystem sys, IEventBus bus);
        }

        private sealed class SubBroadcast<T> : SubBase where T : notnull
        {
            private readonly EventSource _source;

            public SubBroadcast(EventSource source)
            {
                _source = source;
            }

            public override void Unsubscribe(EntitySystem sys, IEventBus bus)
            {
                bus.UnsubscribeEvent<T>(_source, sys);
            }
        }

        private sealed class SubLocal<TComp, TBase> : SubBase where TComp : IComponent where TBase : notnull
        {
            public override void Unsubscribe(EntitySystem sys, IEventBus bus)
            {
                bus.UnsubscribeLocalEvent<TComp, TBase>();
            }
        }

        private sealed class SubAction(Action action) : SubBase
        {
            public override void Unsubscribe(EntitySystem sys, IEventBus bus)
            {
                action();
            }
        }
    }
}
