using System;
using System.Linq;
using System.Runtime.CompilerServices;
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
    internal sealed partial class EntityEventBus : IBroadcastEventBusInternal
    {
        // Inside this class we pass a lot of things around as "ref Unit unitRef".
        // The idea behind this is to avoid using type arguments in core dispatch that only needs to pass around a ref*
        // Type arguments require the JIT to compile a new method implementation for every event type,
        // which would start to weigh a LOT.

        private delegate void RefEventHandler(ref Unit ev);

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

            var order = new OrderingData(orderType, before ?? Array.Empty<Type>(), after ?? Array.Empty<Type>());

            SubscribeEventCommon<T>(source, subscriber,
                (ref Unit ev) => eventHandler(Unsafe.As<Unit, T>(ref ev)), eventHandler, order, false);
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
            var order = new OrderingData(orderType, before ?? Array.Empty<Type>(), after ?? Array.Empty<Type>());

            SubscribeEventCommon<T>(source, subscriber, (ref Unit ev) =>
            {
                ref var tev = ref Unsafe.As<Unit, T>(ref ev);
                eventHandler(ref tev);
            }, eventHandler, order, true);
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

            var subscriptionTuple = new BroadcastRegistration(source, handler, equalityToken, order, byRef);

            RegisterCommon(eventType, order, out var subscriptions);

            if (!subscriptions.BroadcastRegistrations.Contains(subscriptionTuple))
                subscriptions.BroadcastRegistrations.Add(subscriptionTuple);

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

        private void UnsubscribeEvent(Type eventType, BroadcastRegistration tuple, IEntityEventSubscriber subscriber)
        {
            if (_eventData.TryGetValue(eventType, out var subscriptions)
                && subscriptions.BroadcastRegistrations.Contains(tuple))
                subscriptions.BroadcastRegistrations.Remove(tuple);

            if (_inverseEventSubscriptions.TryGetValue(subscriber, out var inverse) && inverse.ContainsKey(eventType))
                inverse.Remove(eventType);
        }

        private void ProcessSingleEvent(EventSource source, ref Unit unitRef, Type eventType, bool byRef)
        {
            if (!_eventData.TryGetValue(eventType, out var subs))
                return;

            if (subs.IsOrdered && !subs.OrderingUpToDate)
            {
                UpdateOrderSeq(eventType, subs);

                // For ordered events, the Registrations list in the sub list is already sorted to be the correct order.
                // This means ordered broadcast events have no overhead over non-ordered ones.
            }

            ProcessSingleEventCore(source, ref unitRef, subs, byRef);
        }

        private static void ProcessSingleEventCore(
            EventSource source,
            ref Unit unitRef,
            EventData subs,
            bool byRef)
        {
            foreach (var handler in subs.BroadcastRegistrations)
            {
                if (handler.ReferenceEvent != byRef)
                    ThrowByRefMisMatch();

                if ((handler.Mask & source) != 0)
                    handler.Handler(ref unitRef);
            }
        }

        private sealed class BroadcastRegistration : OrderedRegistration, IEquatable<BroadcastRegistration>
        {
            public readonly object EqualityToken;
            public readonly RefEventHandler Handler;
            public readonly EventSource Mask;
            public readonly bool ReferenceEvent;

            public BroadcastRegistration(
                EventSource mask,
                RefEventHandler handler,
                object equalityToken,
                OrderingData? ordering,
                bool referenceEvent) : base(ordering)
            {
                Mask = mask;
                Handler = handler;
                EqualityToken = equalityToken;
                ReferenceEvent = referenceEvent;
            }

            public bool Equals(BroadcastRegistration? other)
            {
                return other != null && Mask == other.Mask && Equals(EqualityToken, other.EqualityToken);
            }

            public override bool Equals(object? obj)
            {
                return obj is BroadcastRegistration other && Equals(other);
            }

            public override int GetHashCode() => HashCode.Combine(Mask, EqualityToken);
        }
    }
}
