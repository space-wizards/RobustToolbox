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
    public interface IBroadcastEventBus
    {
        /// <summary>
        /// Subscribes an event handler for a event type.
        /// </summary>
        /// <typeparam name="T">Event type to subscribe to.</typeparam>
        /// <param name="source"></param>
        /// <param name="subscriber">Subscriber that owns the handler.</param>
        /// <param name="eventHandler">Delegate that handles the event.</param>
        void SubscribeEvent<T>(EventSource source, IEntityEventSubscriber subscriber,
            EntityEventHandler<T> eventHandler) where T : notnull;

        void SubscribeEvent<T>(
            EventSource source,
            IEntityEventSubscriber subscriber,
            EntityEventHandler<T> eventHandler,
            Type orderType,
            Type[]? before=null,
            Type[]? after=null)
            where T : notnull;

        /// <summary>
        /// Unsubscribes all event handlers of a given type.
        /// </summary>
        /// <typeparam name="T">Event type being unsubscribed from.</typeparam>
        /// <param name="source"></param>
        /// <param name="subscriber">Subscriber that owns the handlers.</param>
        void UnsubscribeEvent<T>(EventSource source, IEntityEventSubscriber subscriber) where T : notnull;

        /// <summary>
        /// Immediately raises an event onto the bus.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="toRaise">Event being raised.</param>
        void RaiseEvent(EventSource source, object toRaise);

        void RaiseEvent<T>(EventSource source, T toRaise) where T : notnull;

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
        Task<T> AwaitEvent<T>(EventSource source) where T : notnull;

        /// <summary>
        /// Waits for an event to be raised. You do not have to subscribe to the event.
        /// </summary>
        /// <typeparam name="T">Event type being waited for.</typeparam>
        /// <param name="source"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<T> AwaitEvent<T>(EventSource source, CancellationToken cancellationToken) where T : notnull;

        /// <summary>
        /// Waits for an event to be raised. You do not have to subscribe to the event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type">Event type being waited for.</param>
        /// <returns></returns>
        Task<object> AwaitEvent(EventSource source, Type type);

        /// <summary>
        /// Waits for an event to be raised. You do not have to subscribe to the event.
        /// </summary>
        /// <param name="source"></param>
        /// <param name="type">Event type being waited for.</param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        Task<object> AwaitEvent(EventSource source, Type type, CancellationToken cancellationToken);

        /// <summary>
        /// Unsubscribes all event handlers for a given subscriber.
        /// </summary>
        /// <param name="subscriber">Owner of the handlers being removed.</param>
        void UnsubscribeEvents(IEntityEventSubscriber subscriber);
    }

    /// <inheritdoc />
    internal interface IBroadcastEventBusInternal : IBroadcastEventBus
    {
        /// <summary>
        /// Raises all queued events onto the event bus. This needs to be called often.
        /// </summary>
        void ProcessEventQueue();
    }

    [Flags]
    public enum EventSource : byte
    {
        None    = 0b0000,
        Local   = 0b0001,
        Network = 0b0010,

        All = Local | Network,
    }

    /// <summary>
    /// Implements the event broadcast functions.
    /// </summary>
    internal partial class EntityEventBus : IBroadcastEventBusInternal
    {
        private delegate void EventHandler(object ev);

        private readonly Dictionary<Type, List<Registration>> _eventSubscriptions
            = new();

        private readonly Dictionary<IEntityEventSubscriber, Dictionary<Type, Registration>> _inverseEventSubscriptions
            = new();

        private readonly Queue<(EventSource source, object args)> _eventQueue = new();

        private readonly Dictionary<Type, (EventSource, CancellationTokenRegistration, TaskCompletionSource<object>)>
            _awaitingMessages
                = new();

        /// <inheritdoc />
        public void UnsubscribeEvents(IEntityEventSubscriber subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (!_inverseEventSubscriptions.TryGetValue(subscriber, out var val))
                return;

            // UnsubscribeEvent modifies _inverseEventSubscriptions, requires val to be cached
            foreach (var (type, (source, originalHandler, handler, _)) in val.ToList())
            {
                UnsubscribeEvent(source, type, originalHandler, handler, subscriber);
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
        public void SubscribeEvent<T>(
            EventSource source,
            IEntityEventSubscriber subscriber,
            EntityEventHandler<T> eventHandler)
            where T : notnull
        {
            SubscribeEventCommon(source, subscriber, eventHandler, null);
        }

        public void SubscribeEvent<T>(
            EventSource source,
            IEntityEventSubscriber subscriber,
            EntityEventHandler<T> eventHandler,
            Type orderType,
            Type[]? before=null,
            Type[]? after=null)
            where T : notnull
        {
            var order = new OrderingData(orderType, before, after);

            SubscribeEventCommon(source, subscriber, eventHandler, order);
            HandleOrderRegistration(typeof(T), order);
        }

        private void SubscribeEventCommon<T>(
            EventSource source,
            IEntityEventSubscriber subscriber,
            EntityEventHandler<T> eventHandler,
            OrderingData? order)
            where T : notnull
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            if (eventHandler == null)
                throw new ArgumentNullException(nameof(eventHandler));

            if(subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            var eventType = typeof(T);
            var subscriptionTuple = new Registration(source, eventHandler, ev => eventHandler((T) ev), eventHandler, order);
            if (!_eventSubscriptions.TryGetValue(eventType, out var subscriptions))
                _eventSubscriptions.Add(eventType, new List<Registration> {subscriptionTuple});
            else if (!subscriptions.Any(p => p.Mask == source && p.Original == (Delegate) eventHandler))
                subscriptions.Add(subscriptionTuple);

            if (!_inverseEventSubscriptions.TryGetValue(subscriber, out var inverseSubscription))
            {
                inverseSubscription = new Dictionary<Type, Registration>
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
        public void UnsubscribeEvent<T>(EventSource source, IEntityEventSubscriber subscriber) where T : notnull
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            var eventType = typeof(T);

            if (_inverseEventSubscriptions.TryGetValue(subscriber, out var inverse)
                && inverse.TryGetValue(eventType, out var tuple))
                UnsubscribeEvent(source, eventType, tuple.Original, tuple.Handler, subscriber);
        }

        /// <inheritdoc />
        public void RaiseEvent(EventSource source, object toRaise)
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            if (toRaise == null)
                throw new ArgumentNullException(nameof(toRaise));

            ProcessSingleEvent(source, toRaise);
        }

        public void RaiseEvent<T>(EventSource source, T toRaise) where T : notnull
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
        public Task<T> AwaitEvent<T>(EventSource source) where T : notnull
        {
            return AwaitEvent<T>(source, default);
        }

        /// <inheritdoc />
        public Task<object> AwaitEvent(EventSource source, Type type)
        {
            return AwaitEvent(source, type, default);
        }

        /// <inheritdoc />
        public Task<T> AwaitEvent<T>(EventSource source, CancellationToken cancellationToken) where T : notnull
        {
            var type = typeof(T);

            // Tiny trick so we can return T while the tcs is passed an EntitySystemMessage.
            static async Task<T> DoCast(Task<object> task)
            {
                return (T)await task;
            }

            return DoCast(AwaitEvent(source, type, cancellationToken));
        }

        /// <inheritdoc />
        public Task<object> AwaitEvent(EventSource source, Type type, CancellationToken cancellationToken)
        {
            if(source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            if (_awaitingMessages.ContainsKey(type))
            {
                throw new InvalidOperationException("Cannot await the same message type twice at once.");
            }

            var tcs = new TaskCompletionSource<object>();
            CancellationTokenRegistration reg = default;
            if (cancellationToken != default)
            {
                reg = cancellationToken.Register(() =>
                {
                    _awaitingMessages.Remove(type);
                    tcs.TrySetCanceled();
                });
            }

            _awaitingMessages.Add(type, (source, reg, tcs));
            return tcs.Task;
        }

        private void UnsubscribeEvent(EventSource source, Type eventType, Delegate originalHandler, EventHandler handler, IEntityEventSubscriber subscriber)
        {
            var tuple = new Registration(source, originalHandler, handler, originalHandler, null);
            if (_eventSubscriptions.TryGetValue(eventType, out var subscriptions) && subscriptions.Contains(tuple))
                subscriptions.Remove(tuple);

            if (_inverseEventSubscriptions.TryGetValue(subscriber, out var inverse) && inverse.ContainsKey(eventType))
                inverse.Remove(eventType);
        }

        private void ProcessSingleEvent(EventSource source, object eventArgs)
        {
            var eventType = eventArgs.GetType();

            if (_orderedEvents.Contains(eventType))
            {
                ProcessSingleEventOrdered(source, eventArgs, eventType);
            }
            else if (_eventSubscriptions.TryGetValue(eventType, out var subs))
            {
                foreach (var handler in subs)
                {
                    if((handler.Mask & source) != 0)
                        handler.Handler(eventArgs);
                }
            }

            ProcessAwaitingMessages(source, eventArgs, eventType);
        }

        private void ProcessSingleEvent<T>(EventSource source, T eventArgs) where T : notnull
        {
            var eventType = typeof(T);

            if (_orderedEvents.Contains(eventType))
            {
                ProcessSingleEventOrdered(source, eventArgs, eventType);
            }
            else if (_eventSubscriptions.TryGetValue(eventType, out var subs))
            {
                foreach (var (mask, originalHandler, _, _) in subs)
                {
                    if ((mask & source) != 0)
                    {
                        var foo = (EntityEventHandler<T>) originalHandler;
                        foo(eventArgs);
                    }
                }
            }

            ProcessAwaitingMessages(source, eventArgs, eventType);
        }

        // Generic here so we can avoid boxing alloc unless actually awaiting.
        private void ProcessAwaitingMessages<T>(EventSource source, T eventArgs, Type eventType)
            where T : notnull
        {
            if (_awaitingMessages.TryGetValue(eventType, out var awaiting))
            {
                var (mask1, _, tcs) = awaiting;

                if ((source & mask1) != 0)
                {
                    tcs.TrySetResult(eventArgs);
                    _awaitingMessages.Remove(eventType);
                }
            }
        }

        private readonly struct Registration : IEquatable<Registration>
        {
            public readonly EventSource Mask;
            public readonly object EqualityToken;

            public readonly Delegate Original;
            public readonly EventHandler Handler;
            public readonly OrderingData? Ordering;

            public Registration(
                EventSource mask,
                Delegate original,
                EventHandler handler,
                object equalityToken,
                OrderingData? ordering)
            {
                Mask = mask;
                Original = original;
                Handler = handler;
                EqualityToken = equalityToken;
                Ordering = ordering;
            }

            public bool Equals(Registration other)
            {
                return Mask == other.Mask && Equals(EqualityToken, other.EqualityToken);
            }

            public override bool Equals(object? obj)
            {
                return obj is Registration other && Equals(other);
            }

            public override int GetHashCode()
            {
                unchecked
                {
                    return ((int) Mask * 397) ^ (EqualityToken != null ? EqualityToken.GetHashCode() : 0);
                }
            }

            public static bool operator ==(Registration left, Registration right)
            {
                return left.Equals(right);
            }

            public static bool operator !=(Registration left, Registration right)
            {
                return !left.Equals(right);
            }

            public void Deconstruct(
                out EventSource mask,
                out Delegate originalHandler,
                out EventHandler handler,
                out OrderingData? order)
            {
                mask = Mask;
                originalHandler = Original;
                handler = Handler;
                order = Ordering;
            }
        }
    }
}
