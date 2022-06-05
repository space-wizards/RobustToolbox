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
            EventData sub,
            ref ValueList<OrderedEventDispatch> found,
            bool byRef)
        {
            foreach (var handler in sub.BroadcastRegistrations)
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
            EventData subs,
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

        /// <summary>
        /// Calculate sequence order for all event subscriptions, broadcast and directed.
        /// </summary>
        private void UpdateOrderSeq(Type eventType, EventData sub)
        {
            DebugTools.Assert(sub.IsOrdered);

            // Collect all subscriptions, broadcast and ordered.
            IEnumerable<OrderedRegistration> regs = sub.BroadcastRegistrations;
            if (_entSubscriptionsInv.TryGetValue(eventType, out var comps))
            {
                regs = regs.Concat(comps
                    .Select(c => _entSubscriptions[c.Value])
                    .Where(c => c != null)
                    .Select(c => c![eventType]));
            }

            // A hard problem in EventBus' design is that ordering is keyed on a single Type instance
            // (probably just the type of the EntitySystem).
            // This is problematic if a system listens to the same directed event multiple times for the same component,
            // as there are now two distinct subscriptions with the same key.
            // To solve this, I decided that *this is allowed*, but all ordering on the same key must be the same.
            // So you can't have different Before/After on two subscriptions to an event on the same system.
            //
            // Group by ordering types, also filter out un-ordered ones.

            var groups = regs.Where(r => r.Ordering != null).GroupBy(b => b.Ordering!.OrderType).ToArray();

            // Verify that all groups of order types have the same ordering info for Before/After.
            foreach (var group in groups)
            {
                var firstOrder = group.First().Ordering;
                if (!group.All(e => e.Ordering!.Equals(firstOrder)))
                {
                    throw new InvalidOperationException(
                        $"{group.Key} uses different ordering constraints for different subscriptions to the same event {eventType}. " +
                        "All subscriptions to the same event from the same registrar must use the same ordering.");
                }
            }

            // Set up topological sort.
            var nodes = TopologicalSort.FromBeforeAfter(
                groups.Select(g => g.ToArray()),
                n => n[0].Ordering!.OrderType,
                n => n,
                n => n[0].Ordering!.Before,
                n => n[0].Ordering!.After,
                allowMissing: true);

            // Start at 1, if only so events with no Ordering data at all have a distinct position.
            // Doesn't really matter.
            var i = 1;
            foreach (var group in TopologicalSort.Sort(nodes))
            {
                // Assign indices to all registrations in order of the topological sort.
                foreach (var registration in group)
                {
                    registration.Order = i++;
                }
            }

            sub.OrderingUpToDate = true;

            // Sort the broadcast registrations ahead of time.
            // This means ordered broadcast events have no overhead from unordered ones.
            sub.BroadcastRegistrations.Sort(RegistrationOrderComparer.Instance);

            // We still need Order indices on the OrderedRegistrations for directed ordered events.
            // Since we sort those at submission time.
            // Still, it's a single .Sort() now for those instead of the whole topological short shebang.
        }

        internal sealed record OrderingData(Type OrderType, Type[] Before, Type[] After)
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

        private sealed class RegistrationOrderComparer : IComparer<OrderedRegistration>
        {
            public static readonly RegistrationOrderComparer Instance = new();

            public int Compare(OrderedRegistration? x, OrderedRegistration? y)
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

        /// <summary>
        /// Base type for directed and broadcast subscriptions. Contains ordering data.
        /// </summary>
        internal abstract class OrderedRegistration
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
            foreach (var (type, sub) in _eventData)
            {
                if (sub.IsOrdered && !sub.OrderingUpToDate)
                {
                    UpdateOrderSeq(type, sub);
                }
            }
        }
    }
}
