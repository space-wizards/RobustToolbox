using System;
using System.Collections.Generic;

namespace Robust.Shared.GameObjects
{
    public abstract partial class EntitySystem
    {
        private List<SubBase>? _subscriptions;

        // NOTE: EntityEventHandler<T> and EntitySessionEventHandler<T> CANNOT BE ORDERED BETWEEN EACH OTHER.

        protected void SubscribeNetworkEvent<T>(
            EntityEventHandler<T> handler,
            Type[]? before = null, Type[]? after = null)
            where T : notnull
        {
            SubEvent(EventSource.Network, handler, before, after);
        }

        protected void SubscribeLocalEvent<T>(
            EntityEventHandler<T> handler,
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

        private void SubEvent<T>(
            EventSource src,
            EntityEventHandler<T> handler,
            Type[]? before, Type[]? after)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeEvent(src, this, handler, GetType(), before, after);

            _subscriptions ??= new();
            _subscriptions.Add(new SubBroadcast<T>(src));
        }

        private void SubSessionEvent<T>(
            EventSource src,
            EntitySessionEventHandler<T> handler,
            Type[]? before, Type[]? after)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeSessionEvent(src, this, handler, GetType(), before, after);

            _subscriptions ??= new();
            _subscriptions.Add(new SubBroadcast<EntitySessionMessage<T>>(src));
        }

        [Obsolete("Unsubscribing of entity system events is now automatic")]
        protected void UnsubscribeNetworkEvent<T>()
            where T : notnull
        {
            EntityManager.EventBus.UnsubscribeEvent<T>(EventSource.Network, this);
        }

        [Obsolete("Unsubscribing of entity system events is now automatic")]
        protected void UnsubscribeLocalEvent<T>()
            where T : notnull
        {
            EntityManager.EventBus.UnsubscribeEvent<T>(EventSource.Local, this);
        }

        protected void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventHandler<TComp, TEvent> handler,
            Type[]? before = null, Type[]? after = null)
            where TComp : IComponent
            where TEvent : EntityEventArgs
        {
            EntityManager.EventBus.SubscribeLocalEvent(handler);

            _subscriptions ??= new();
            _subscriptions.Add(new SubLocal<TComp, TEvent>());
        }

        [Obsolete("Unsubscribing of entity system events is now automatic")]
        protected void UnsubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : EntityEventArgs
        {
            EntityManager.EventBus.UnsubscribeLocalEvent<TComp, TEvent>();
        }

        [Obsolete("Unsubscribing of entity system events is now automatic")]
        protected void UnsubscribeLocalEvent<TComp, TEvent>()
            where TComp : IComponent
            where TEvent : EntityEventArgs
        {
            EntityManager.EventBus.UnsubscribeLocalEvent<TComp, TEvent>();
        }

        private void ShutdownSubscriptions()
        {
            if (_subscriptions == null)
                return;

            foreach (var sub in _subscriptions)
            {
                sub.Unsubscribe(this, EntityManager.EventBus);
            }

            _subscriptions = null;
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

        private sealed class SubLocal<TComp, TBase> : SubBase where TComp : IComponent where TBase : EntityEventArgs
        {
            public override void Unsubscribe(EntitySystem sys, IEventBus bus)
            {
                bus.UnsubscribeLocalEvent<TComp, TBase>();
            }
        }
    }
}
