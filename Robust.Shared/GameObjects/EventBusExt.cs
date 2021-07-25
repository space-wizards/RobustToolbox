using System;

namespace Robust.Shared.GameObjects
{
    public static class EventBusExt
    {
        public static void SubscribeSessionEvent<T>(
            this IEventBus eventBus,
            EventSource source,
            IEntityEventSubscriber subscriber,
            EntitySessionEventHandler<T> handler)
        {
            var wrapper = new HandlerWrapper<T>(handler);
            eventBus.SubscribeEvent<EntitySessionMessage<T>>(source, subscriber, wrapper.Invoke);
        }

        public static void SubscribeSessionEvent<T>(
            this IEventBus eventBus,
            EventSource source,
            IEntityEventSubscriber subscriber,
            EntitySessionEventHandler<T> handler,
            Type orderType,
            Type[]? before=null,
            Type[]? after=null)
        {
            var wrapper = new HandlerWrapper<T>(handler);
            eventBus.SubscribeEvent<EntitySessionMessage<T>>(
                source,
                subscriber,
                wrapper.Invoke,
                orderType,
                before, after);
        }

        private sealed class HandlerWrapper<T>
        {
            public HandlerWrapper(EntitySessionEventHandler<T> handler)
            {
                Handler = handler;
            }

            public EntitySessionEventHandler<T> Handler { get; }

            public void Invoke(EntitySessionMessage<T> msg)
            {
                Handler(msg.Message, msg.EventArgs);
            }

            private bool Equals(HandlerWrapper<T> other)
            {
                return Handler.Equals(other.Handler);
            }

            public override bool Equals(object? obj)
            {
                return ReferenceEquals(this, obj) || obj is HandlerWrapper<T> other && Equals(other);
            }

            public override int GetHashCode()
            {
                return Handler.GetHashCode();
            }
        }
    }
}
