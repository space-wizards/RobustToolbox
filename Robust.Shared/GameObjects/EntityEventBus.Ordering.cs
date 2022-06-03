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

            foreach (var (handler, _) in found)
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

            var groups = regs.Where(r => r.Ordering != null).GroupBy(b => b.Ordering!.OrderType).ToArray();

            var orderGroups = new List<OrderedRegistration[]>();
            foreach (var group in groups)
            {
                var firstOrder = group.First().Ordering;
                if (!group.All(e => e.Ordering!.Equals(firstOrder)))
                {
                    throw new InvalidOperationException(
                        $"{group.Key} uses different ordering constraints for different subscriptions to the same event {eventType}. " +
                        "All subscriptions to the same event from the same registrar must use the same ordering.");
                }

                orderGroups.Add(group.ToArray());
            }

            var nodes = TopologicalSort.FromBeforeAfter(
                orderGroups,
                n => n[0].Ordering!.OrderType,
                n => n,
                n => n[0].Ordering!.Before,
                n => n[0].Ordering!.After,
                allowMissing: true);

            var i = 1;
            foreach (var group in TopologicalSort.Sort(nodes))
            {
                foreach (var registration in group)
                {
                    registration.Order = i++;
                }
            }

            sub.OrderingUpToDate = true;

            sub.Registrations.Sort(RegistrationOrderComparer.Instance);
        }

        private sealed record OrderingData(Type OrderType, Type[] Before, Type[] After)
        {
            public bool Equals(OrderingData? other)
            {
                if (other == null)
                    return false;

                return other.OrderType == OrderType
                       && Before.AsSpan().SequenceEqual(other.Before)
                       && After.AsSpan().SequenceEqual(other.After);
            }

            public override int GetHashCode()
            {
                var hc = new HashCode();
                hc.Add(OrderType);
                hc.AddArray(Before);
                hc.AddArray(After);
                return hc.ToHashCode();
            }
        }

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
