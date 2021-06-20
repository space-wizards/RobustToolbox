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
            Type[]? before=null, Type[]? after=null)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeEvent(EventSource.Network, this, handler, GetType(), before, after);

            _subscriptions ??= new();
            _subscriptions.Add(new SubBroadcast<T>(EventSource.Network));
        }

        protected void SubscribeNetworkEvent<T>(
            EntitySessionEventHandler<T> handler,
            Type[]? before=null, Type[]? after=null)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeSessionEvent(EventSource.Network, this, handler);

            _subscriptions ??= new();
            _subscriptions.Add(new SubBroadcast<EntitySessionMessage<T>>(EventSource.Network));
        }

        protected void SubscribeLocalEvent<T>(
            EntityEventHandler<T> handler,
            Type[]? before=null, Type[]? after=null)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeEvent(EventSource.Local, this, handler, GetType(), before, after);

            _subscriptions ??= new();
            _subscriptions.Add(new SubBroadcast<T>(EventSource.Local));
        }

        protected void SubscribeLocalEvent<T>(
            EntitySessionEventHandler<T> handler,
            Type[]? before=null, Type[]? after=null)
            where T : notnull
        {
            EntityManager.EventBus.SubscribeSessionEvent(EventSource.Local, this, handler);

            _subscriptions ??= new();
            _subscriptions.Add(new SubBroadcast<EntitySessionMessage<T>>(EventSource.Local));
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
            Type[]? before=null, Type[]? after=null)
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
