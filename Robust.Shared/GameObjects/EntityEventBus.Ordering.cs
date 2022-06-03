using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    internal partial class EntityEventBus
    {
        private static void CollectBroadcastOrdered(
            EventSource source,
            EventSubscriptions sub,
            ref ValueList<OrderedEventDispatch> found,
            bool byRef)
        {
            foreach (var handler in sub.Registrations)
            {
                if (handler.ReferenceEvent != byRef)
                    ThrowByRefMisMatch();

                if ((handler.Mask & source) != 0)
                    found.Add(new OrderedEventDispatch(handler.Handler, handler.Order));
            }
        }

        private void RaiseLocalOrdered(
            EntityUid uid,
            Type eventType,
            EventSubscriptions subs,
            ref Unit unitRef,
            bool broadcast,
            bool byRef)
        {
            if (!subs.OrderingUpToDate)
                UpdateOrderSeq(eventType, subs);

            var found = new ValueList<OrderedEventDispatch>();

            if (broadcast)
                CollectBroadcastOrdered(EventSource.Local, subs, ref found, byRef);

            EntCollectOrdered(uid, eventType, ref found, byRef);

            DispatchOrderedEvents(ref unitRef, ref found);
        }

        private static void DispatchOrderedEvents(ref Unit eventArgs, ref ValueList<OrderedEventDispatch> found)
        {
            found.Sort(OrderedEventDispatchComparer.Instance);

            foreach (var (handler, orderData) in found)
            {
                handler(ref eventArgs);
            }
        }

        private void UpdateOrderSeq(Type eventType, EventSubscriptions sub)
        {
            IEnumerable<OrderedRegistration> regs = sub.Registrations;
            if (_entSubscriptionsInv.TryGetValue(eventType, out var comps))
            {
                regs = regs.Concat(comps
                        .Select(c => _entSubscriptions[c.Value])
                        .Where(c => c != null)
                        .Select(c => c![eventType]));
            }

            var nodes = TopologicalSort.FromBeforeAfter(
                regs.Where(f => f.Ordering != null),
                n => n.Ordering!.OrderType,
                n => n,
                n => n.Ordering!.Before ?? Array.Empty<Type>(),
                n => n.Ordering!.After ?? Array.Empty<Type>(),
                allowMissing: true);

            var i = 1;
            foreach (var node in TopologicalSort.Sort(nodes))
            {
                node.Order = i++;
            }

            sub.OrderingUpToDate = true;

            sub.Registrations.Sort(RegistrationOrderComparer.Instance);
        }

        private sealed record OrderingData(Type OrderType, Type[]? Before, Type[]? After);

        private sealed class RegistrationOrderComparer : IComparer<Registration>
        {
            public static readonly RegistrationOrderComparer Instance = new();

            public int Compare(Registration? x, Registration? y)
            {
                return x!.Order.CompareTo(y!.Order);
            }
        }

        private record struct OrderedEventDispatch(RefEventHandler Handler, int Order);

        private sealed class OrderedEventDispatchComparer : IComparer<OrderedEventDispatch>
        {
            public static readonly OrderedEventDispatchComparer Instance = new();

            public int Compare(OrderedEventDispatch x, OrderedEventDispatch y)
            {
                return x.Order.CompareTo(y.Order);
            }
        }

        private abstract class OrderedRegistration
        {
            public int Order;
            public readonly OrderingData? Ordering;

            protected OrderedRegistration(OrderingData? ordering)
            {
                Ordering = ordering;
            }
        }

        /// <summary>
        /// Ensure all ordered events are sorted out and verified.
        /// </summary>
        /// <remarks>
        /// Internal sorting for ordered events is normally deferred until raised
        /// (since broadcast event subscriptions can be edited at any time).
        ///
        /// Calling this gets all the sorting done ahead of time,
        /// and makes sure that there are no problems with cycles and such.
        /// </remarks>
        public void CalcOrdering()
        {
            foreach (var (type, sub) in _eventSubscriptions)
            {
                if (sub.IsOrdered && !sub.OrderingUpToDate)
                {
                    UpdateOrderSeq(type, sub);
                }
            }
        }
    }
}
