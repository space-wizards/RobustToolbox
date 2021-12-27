using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using JetBrains.Annotations;
using Robust.Shared.Utility;

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
        /// Subscribes an event handler for an event type.
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
            Type[]? before = null,
            Type[]? after = null)
            where T : notnull;

        void SubscribeEvent<T>(EventSource source, IEntityEventSubscriber subscriber,
            EntityEventRefHandler<T> eventHandler) where T : notnull;

        void SubscribeEvent<T>(
            EventSource source,
            IEntityEventSubscriber subscriber,
            EntityEventRefHandler<T> eventHandler,
            Type orderType,
            Type[]? before = null,
            Type[]? after = null)
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

        void RaiseEvent<T>(EventSource source, ref T toRaise) where T : notnull;

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
        None = 0b0000,
        Local = 0b0001,
        Network = 0b0010,

        All = Local | Network,
    }

    /// <summary>
    /// Implements the event broadcast functions.
    /// </summary>
    internal partial class EntityEventBus : IBroadcastEventBusInternal
    {
        // Inside this class we pass a lot of things around as "ref Unit unitRef".
        // The idea behind this is to avoid using type arguments in core dispatch that only needs to pass around a ref*
        // Type arguments require the JIT to compile a new method implementation for every event type,
        // which would start to weigh a LOT.

        private delegate void RefEventHandler(ref Unit ev);

        private readonly Dictionary<Type, List<Registration>> _eventSubscriptions = new();

        private readonly Dictionary<IEntityEventSubscriber, Dictionary<Type, Registration>> _inverseEventSubscriptions
            = new();

        private readonly Queue<(EventSource source, object args)> _eventQueue = new();

        private readonly Dictionary<Type, (EventSource, CancellationTokenRegistration, RefEventHandler)>
            _awaitingMessages = new();

        /// <inheritdoc />
        public void UnsubscribeEvents(IEntityEventSubscriber subscriber)
        {
            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            if (!_inverseEventSubscriptions.TryGetValue(subscriber, out var val))
                return;

            // UnsubscribeEvent modifies _inverseEventSubscriptions, requires val to be cached
            foreach (var (type, tuple) in val.ToList())
            {
                UnsubscribeEvent(type, tuple, subscriber);
            }
        }

        /// <inheritdoc />
        public void ProcessEventQueue()
        {
            while (_eventQueue.Count != 0)
            {
                var (source, args) = _eventQueue.Dequeue();
                var type = args.GetType();
                ref var unitRef = ref ExtractUnitRef(ref args, type);

                ProcessSingleEvent(source, ref unitRef, type, false);
            }
        }

        /// <inheritdoc />
        public void SubscribeEvent<T>(
            EventSource source,
            IEntityEventSubscriber subscriber,
            EntityEventHandler<T> eventHandler)
            where T : notnull
        {
            if (eventHandler == null)
                throw new ArgumentNullException(nameof(eventHandler));

            SubscribeEventCommon<T>(source, subscriber,
                (ref Unit ev) => eventHandler(Unsafe.As<Unit, T>(ref ev)), eventHandler, null, false);
        }

        public void SubscribeEvent<T>(
            EventSource source,
            IEntityEventSubscriber subscriber,
            EntityEventHandler<T> eventHandler,
            Type orderType,
            Type[]? before = null,
            Type[]? after = null)
            where T : notnull
        {
            if (eventHandler == null)
                throw new ArgumentNullException(nameof(eventHandler));

            var order = new OrderingData(orderType, before, after);

            SubscribeEventCommon<T>(source, subscriber,
                (ref Unit ev) => eventHandler(Unsafe.As<Unit, T>(ref ev)), eventHandler, order, false);

            HandleOrderRegistration(typeof(T), order);
        }

        public void SubscribeEvent<T>(EventSource source, IEntityEventSubscriber subscriber,
            EntityEventRefHandler<T> eventHandler) where T : notnull
        {
            SubscribeEventCommon<T>(source, subscriber, (ref Unit ev) =>
            {
                ref var tev = ref Unsafe.As<Unit, T>(ref ev);
                eventHandler(ref tev);
            }, eventHandler, null, true);
        }

        public void SubscribeEvent<T>(EventSource source, IEntityEventSubscriber subscriber,
            EntityEventRefHandler<T> eventHandler,
            Type orderType, Type[]? before = null, Type[]? after = null) where T : notnull
        {
            var order = new OrderingData(orderType, before, after);

            SubscribeEventCommon<T>(source, subscriber, (ref Unit ev) =>
            {
                ref var tev = ref Unsafe.As<Unit, T>(ref ev);
                eventHandler(ref tev);
            }, eventHandler, order, true);
            HandleOrderRegistration(typeof(T), order);
        }

        private void SubscribeEventCommon<T>(
            EventSource source,
            IEntityEventSubscriber subscriber,
            RefEventHandler handler,
            object equalityToken,
            OrderingData? order,
            bool byRef)
            where T : notnull
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            if (subscriber == null)
                throw new ArgumentNullException(nameof(subscriber));

            var eventType = typeof(T);

            var eventReference = eventType.HasCustomAttribute<ByRefEventAttribute>();

            if (eventReference != byRef)
                throw new InvalidOperationException(
                    $"Attempted to subscribe by-ref and by-value to the same broadcast event! event={eventType} eventIsByRef={eventReference} subscriptionIsByRef={byRef}");

            var subscriptionTuple = new Registration(source, handler, equalityToken, order, byRef);

            var subscriptions = _eventSubscriptions.GetOrNew(eventType);
            if (!subscriptions.Any(p => p.Equals(subscriptionTuple)))
                subscriptions.Add(subscriptionTuple);

            var inverseSubscription = _inverseEventSubscriptions.GetOrNew(subscriber);
            if (!inverseSubscription.ContainsKey(eventType))
                inverseSubscription.Add(eventType, subscriptionTuple);
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
                UnsubscribeEvent(eventType, tuple, subscriber);
        }

        /// <inheritdoc />
        public void RaiseEvent(EventSource source, object toRaise)
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            var eventType = toRaise.GetType();
            ref var unitRef = ref ExtractUnitRef(ref toRaise, eventType);

            ProcessSingleEvent(source, ref unitRef, eventType, false);
        }

        public void RaiseEvent<T>(EventSource source, T toRaise) where T : notnull
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            ProcessSingleEvent(source, ref Unsafe.As<T, Unit>(ref toRaise), typeof(T), false);
        }

        public void RaiseEvent<T>(EventSource source, ref T toRaise) where T : notnull
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            ProcessSingleEvent(source, ref Unsafe.As<T, Unit>(ref toRaise), typeof(T), true);
        }

        /// <inheritdoc />
        public void QueueEvent(EventSource source, EntityEventArgs toRaise)
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            if (toRaise == null)
                throw new ArgumentNullException(nameof(toRaise));

            _eventQueue.Enqueue((source, toRaise));
        }

        /// <inheritdoc />
        public Task<T> AwaitEvent<T>(EventSource source) where T : notnull => AwaitEvent<T>(source, default);

        /// <inheritdoc />
        public Task<T> AwaitEvent<T>(EventSource source, CancellationToken cancellationToken) where T : notnull
        {
            if (source == EventSource.None)
                throw new ArgumentOutOfRangeException(nameof(source));

            var type = typeof(T);

            if (_awaitingMessages.ContainsKey(type))
                throw new InvalidOperationException("Cannot await the same message type twice at once.");

            var tcs = new TaskCompletionSource<T>();
            CancellationTokenRegistration reg = default;
            if (cancellationToken != default)
            {
                reg = cancellationToken.Register(() =>
                {
                    _awaitingMessages.Remove(type);
                    tcs.TrySetCanceled();
                });
            }

            _awaitingMessages.Add(type, (source, reg, (ref Unit unCast) =>
            {
                var msg = Unsafe.As<Unit, T>(ref unCast);
                tcs.SetResult(msg);
            }));

            return tcs.Task;
        }

        private void UnsubscribeEvent(Type eventType, Registration tuple, IEntityEventSubscriber subscriber)
        {
            if (_eventSubscriptions.TryGetValue(eventType, out var subscriptions) && subscriptions.Contains(tuple))
                subscriptions.Remove(tuple);

            if (_inverseEventSubscriptions.TryGetValue(subscriber, out var inverse) && inverse.ContainsKey(eventType))
                inverse.Remove(eventType);
        }

        private void ProcessSingleEvent(EventSource source, ref Unit unitRef, Type eventType, bool byRef)
        {
            if (_orderedEvents.Contains(eventType))
            {
                ProcessSingleEventOrdered(source, ref unitRef, eventType, byRef);
            }
            else if (_eventSubscriptions.TryGetValue(eventType, out var subs))
            {
                foreach (var handler in subs)
                {
                    if (handler.ReferenceEvent != byRef)
                        ThrowByRefMisMatch();

                    if ((handler.Mask & source) != 0)
                        handler.Handler(ref unitRef);
                }
            }

            ProcessAwaitingMessages(source, ref unitRef, eventType);
        }

        // Generic here so we can avoid boxing alloc unless actually awaiting.
        private void ProcessAwaitingMessages(EventSource source, ref Unit untypedArgs, Type eventType)
        {
            if (_awaitingMessages.TryGetValue(eventType, out var awaiting))
            {
                var (mask1, _, tcs) = awaiting;

                if ((source & mask1) != 0)
                {
                    tcs(ref untypedArgs);
                    _awaitingMessages.Remove(eventType);
                }
            }
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void ThrowByRefMisMatch() =>
            throw new InvalidOperationException("Mismatching by-ref ness on event!");

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static ref Unit ExtractUnitRef(ref object obj, Type objType)
        {
            // If it's a boxed value type we have to do some trickery to return the INTERIOR reference,
            // not the reference to the boxed object.
            // Otherwise the unit points to the reference to the reference type.
            return ref objType.IsValueType
                ? ref Unsafe.As<object, UnitBox>(ref obj).Value
                : ref Unsafe.As<object, Unit>(ref obj);
        }

        private readonly struct Registration : IEquatable<Registration>
        {
            public readonly EventSource Mask;
            public readonly object EqualityToken;

            public readonly RefEventHandler Handler;
            public readonly OrderingData? Ordering;
            public readonly bool ReferenceEvent;

            public Registration(
                EventSource mask,
                RefEventHandler handler,
                object equalityToken,
                OrderingData? ordering,
                bool referenceEvent)
            {
                Mask = mask;
                Handler = handler;
                EqualityToken = equalityToken;
                Ordering = ordering;
                ReferenceEvent = referenceEvent;
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
                    return ((int)Mask * 397) ^ EqualityToken.GetHashCode();
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
        }

        // This is not a real type. Whenever you see a "ref Unit" it means it's a ref to *some* kind of other type.
        // It should always be cast to/from with Unsafe.As<,>
        private readonly struct Unit
        {
        }

        [StructLayout(LayoutKind.Sequential)]
        private class UnitBox
        {
            [UsedImplicitly] public Unit Value;
        }
    }
}
