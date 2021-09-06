using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    internal partial class EntityEventBus
    {
        // TODO: Topological sort is currently done every time an event is emitted.
        // This should be fine for low-volume stuff like interactions, but definitely not for anything high volume.
        // Not sure if we could pre-cache the topological sort, here.

        // Ordered event raising is slow so if this event has any ordering dependencies we use a slower path.
        private readonly HashSet<Type> _orderedEvents = new();

        private void ProcessSingleEventOrdered(EventSource source, ref Unit eventArgs, Type eventType, bool byRef)
        {
            var found = new List<(RefEventHandler, OrderingData?)>();

            CollectBroadcastOrdered(source, eventType, found, byRef);

            DispatchOrderedEvents(ref eventArgs, found);
        }

        private void CollectBroadcastOrdered(
            EventSource source,
            Type eventType,
            List<(RefEventHandler, OrderingData?)> found,
            bool byRef)
        {
            if (!_eventSubscriptions.TryGetValue(eventType, out var subs))
                return;

            foreach (var handler in subs)
            {
                if (handler.ReferenceEvent != byRef)
                    ThrowByRefMisMatch();

                if ((handler.Mask & source) != 0)
                    found.Add((handler.Handler, handler.Ordering));
            }
        }

        private void RaiseLocalOrdered(EntityUid uid,
            Type eventType,
            ref Unit unitRef,
            bool broadcast, bool byRef)
        {
            var found = new List<(RefEventHandler, OrderingData?)>();

            if (broadcast)
                CollectBroadcastOrdered(EventSource.Local, eventType, found, byRef);

            _eventTables.CollectOrdered(uid, eventType, found, byRef);

            DispatchOrderedEvents(ref unitRef, found);

            if (broadcast)
                ProcessAwaitingMessages(EventSource.Local, ref unitRef, eventType);
        }

        private static void DispatchOrderedEvents(ref Unit eventArgs, List<(RefEventHandler, OrderingData?)> found)
        {
            var nodes = TopologicalSort.FromBeforeAfter(
                found.Where(f => f.Item2 != null),
                n => n.Item2!.OrderType,
                n => n.Item1!,
                n => n.Item2!.Before ?? Array.Empty<Type>(),
                n => n.Item2!.After ?? Array.Empty<Type>(),
                allowMissing: true);

            foreach (var handler in TopologicalSort.Sort(nodes))
            {
                handler(ref eventArgs);
            }

            // Go over all handlers that don't have ordering so weren't included in the sort.
            foreach (var (handler, orderData) in found)
            {
                if (orderData == null)
                    handler(ref eventArgs);
            }
        }

        private void HandleOrderRegistration(Type eventType, OrderingData? data)
        {
            if (data == null)
                return;

            if (data.Before != null || data.After != null)
                _orderedEvents.Add(eventType);
        }

        private sealed record OrderingData(Type OrderType, Type[]? Before, Type[]? After);

    }
}
