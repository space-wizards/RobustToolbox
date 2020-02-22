using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// Provides a central event bus that EntitySystems can subscribe to. This is the main way that
    /// EntitySystems communicate with each other.
    /// </summary>
    [PublicAPI]
    public interface IEventBus
    {
        /// <summary>
        /// Subscribes an event handler for a event type.
        /// </summary>
        /// <typeparam name="T">Event type to subscribe to.</typeparam>
        /// <param name="source"></param>
        /// <param name="subscriber">Subscriber that owns the handler.</param>
        /// <param name="eventHandler">Delegate that handles the event.</param>
        void SubscribeEvent<T>(EventSource source, IEntityEventSubscriber subscriber, EntityEventHandler<T> eventHandler)
            where T : EntityEventArgs;

        /// <summary>
        /// Unsubscribes all event handlers of a given type.
        /// </summary>
        /// <typeparam name="T">Event type being unsubscribed from.</typeparam>
        /// <param name="source"></param>
        /// <param name="subscriber">Subscriber that owns the handlers.</param>
        void UnsubscribeEvent<T>(EventSource source, IEntityEventSubscriber subscriber)
            where T : EntityEventArgs;

        /// <summary>
        /// Immediately raises an event onto the bus.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="toRaise">Event being raised.</param>
        void RaiseEvent(EventSource source, EntityEventArgs toRaise);

        /// <summary>
        /// Queues an event to be raised at a later time.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="toRaise">Event being raised.</param>
        void QueueEvent(EventSource source, EntityEventArgs toRaise);

        /// <summary>
        /// Waits for an event to be raised. You do not have to subscribe to the event.
        /// </summary>
        /// <typeparam name="T">Event type being waited for.</typeparam>
        /// <param name="source"></param>
        /// <returns></returns>
        Task<T> AwaitEvent<T>(EventSource source)
            where T : EntityEventArgs;

        /// <summary>
        /// Waits for an event to be raised. You do not have to subscribe to the event.
        /// </summary>
        /// <typeparam name="T">Event type being waited for.</typeparam>
        /// <param name="source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> AwaitEvent<T>(EventSource source, CancellationToken cancellationToken)
            where T : EntityEventArgs;

        /// <summary>
        /// Unsubscribes all event handlers for a given subscriber.
        /// </summary>
        /// <param name="subscriber">Owner of the handlers being removed.</param>
        void UnsubscribeEvents(IEntityEventSubscriber subscriber);
    }

    /// <inheritdoc />
    internal interface IEntityEventBus : IEventBus
    {
        /// <summary>
        /// Raises all queued events onto the event bus. This needs to be called often.
        /// </summary>
        void ProcessEventQueue();
    }

    [Flags]
    public enum EventSource
    {
        None    = 0b0000,
        Local   = 0b0001,
        Network = 0b0010,

        All = Local | Network,
    }

    /// <inheritdoc />
    internal class EntityEventBus : IEntityEventBus
    {
        private readonly Dictionary<Type, List<(EventSource mask, Delegate callback)>> _eventSubscriptions
            = new Dictionary<Type, List<(EventSource source, Delegate handler)>>();

        private readonly Dictionary<IEntityEventSubscriber, Dictionary<Type, (EventSource source, Delegate handler)>> _inverseEventSubscriptions
            = new Dictionary<IEntityEventSubscriber, Dictionary<Type, (EventSource source, Delegate handler)>>();

        private readonly Queue<(EventSource source, EntityEventArgs args)> _eventQueue = new Queue<(EventSource source, EntityEventArgs args)>();

        private readonly Dictionary<Type, (EventSource, CancellationTokenRegistration, TaskCompletionSource<EntityEventArgs>)>
            _awaitingMessages
                = new Dictionary<Type, (EventSource, CancellationTokenRegistration, TaskCompletionSource<EntityEventArgs>)>();

        /// <inheritdoc />
        public void UnsubscribeEvents(IEntityEventSubscriber subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (!_inverseEventSubscriptions.TryGetValue(subscriber, out var val))
                return;

            // UnsubscribeEvent modifies _inverseEventSubscriptions, requires val to be cached
            foreach (var (type, subscription) in val.ToList())
            {
                UnsubscribeEvent(subscription.source, type, subscription.handler, subscriber);
            }
        }

        /// <inheritdoc />
        public void ProcessEventQueue()
        {
            while (_eventQueue.Count != 0)
            {
                var eventTuple = _eventQueue.Dequeue();
                ProcessSingleEvent(eventTuple.source, eventTuple.args);
            }
        }

        /// <inheritdoc />
        public void SubscribeEvent<T>(EventSource source, IEntityEventSubscriber subscriber, EntityEventHandler<T> eventHandler)
            where T : EntityEventArgs
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            if (eventHandler == null)
                throw new ArgumentNullException(nameof(eventHandler));

            if(subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            var eventType = typeof(T);
            var subscriptionTuple = (source, eventHandler);
            if (!_eventSubscriptions.TryGetValue(eventType, out var subscriptions))
                _eventSubscriptions.Add(eventType, new List<(EventSource, Delegate)> {subscriptionTuple});
            else if (!subscriptions.Contains(subscriptionTuple))
                subscriptions.Add(subscriptionTuple);

            if (!_inverseEventSubscriptions.TryGetValue(subscriber, out var inverseSubscription))
            {
                inverseSubscription = new Dictionary<Type, (EventSource, Delegate)>
                {
                    {eventType, subscriptionTuple}
                };

                _inverseEventSubscriptions.Add(
                    subscriber,
                    inverseSubscription
                );
            }

            else if (!inverseSubscription.ContainsKey(eventType))
            {
                inverseSubscription.Add(eventType, subscriptionTuple);
            }
        }

        /// <inheritdoc />
        public void UnsubscribeEvent<T>(EventSource source, IEntityEventSubscriber subscriber)
            where T : EntityEventArgs
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            var eventType = typeof(T);

            if (_inverseEventSubscriptions.TryGetValue(subscriber, out var inverse)
                && inverse.TryGetValue(eventType, out var tuple))
                UnsubscribeEvent(source, eventType, tuple.handler, subscriber);
        }

        /// <inheritdoc />
        public void RaiseEvent(EventSource source, EntityEventArgs toRaise)
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            if (toRaise == null)
                throw new ArgumentNullException(nameof(toRaise));

            ProcessSingleEvent(source, toRaise);
        }

        /// <inheritdoc />
        public void QueueEvent(EventSource source, EntityEventArgs toRaise)
        {
            if(source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            if(toRaise == null)
                throw new ArgumentNullException(nameof(toRaise));

            _eventQueue.Enqueue((source, toRaise));
        }

        /// <inheritdoc />
        public Task<T> AwaitEvent<T>(EventSource source)
            where T : EntityEventArgs
        {
            return AwaitEvent<T>(source,default);
        }

        /// <inheritdoc />
        public Task<T> AwaitEvent<T>(EventSource source, CancellationToken cancellationToken)
            where T : EntityEventArgs
        {
            if(source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            var type = typeof(T);
            if (_awaitingMessages.ContainsKey(type))
            {
                throw new InvalidOperationException("Cannot await the same message type twice at once.");
            }

            var tcs = new TaskCompletionSource<EntityEventArgs>();
            CancellationTokenRegistration reg = default;
            if (cancellationToken != default)
            {
                reg = cancellationToken.Register(() =>
                {
                    _awaitingMessages.Remove(type);
                    tcs.TrySetCanceled();
                });
            }

            // Tiny trick so we can return T while the tcs is passed an EntitySystemMessage.
            async Task<T> DoCast(Task<EntityEventArgs> task)
            {
                return (T)await task;
            }

            _awaitingMessages.Add(type, (source, reg, tcs));
            return DoCast(tcs.Task);
        }

        private void UnsubscribeEvent(EventSource source, Type eventType, Delegate handler, IEntityEventSubscriber subscriber)
        {
            var tuple = (source, evh: handler);
            if (_eventSubscriptions.TryGetValue(eventType, out var subscriptions) && subscriptions.Contains(tuple))
                subscriptions.Remove(tuple);

            if (_inverseEventSubscriptions.TryGetValue(subscriber, out var inverse) && inverse.ContainsKey(eventType))
                inverse.Remove(eventType);
        }

        private void ProcessSingleEvent(EventSource source, EntityEventArgs eventArgs)
        {
            var eventType = eventArgs.GetType();

            if (_eventSubscriptions.TryGetValue(eventType, out var subs))
            {
                foreach (var handler in subs)
                {
                    if((handler.mask & source) != 0)
                        handler.callback.DynamicInvoke(eventArgs);
                }
            }

            if (_awaitingMessages.TryGetValue(eventType, out var awaiting))
            {
                var (mask, _, tcs) = awaiting;

                if((source & mask) != 0)
                {
                    tcs.TrySetResult(eventArgs);
                    _awaitingMessages.Remove(eventType);
                }
            }
        }
    }
}
