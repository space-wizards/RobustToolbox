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

        private void ProcessSingleEventOrdered(EventSource source, object eventArgs, Type eventType)
        {
            var found = new List<(EventHandler, OrderingData?)>();

            CollectBroadcastOrdered(source, eventType, found);

            DispatchOrderedEvents(eventArgs, found);
        }

        private void CollectBroadcastOrdered(
            EventSource source,
            Type eventType,
            List<(EventHandler, OrderingData?)> found)
        {
            if (!_eventSubscriptions.TryGetValue(eventType, out var subs))
                return;

            foreach (var handler in subs)
            {
                if ((handler.Mask & source) != 0)
                    found.Add((handler.Handler, handler.Ordering));
            }
        }

        private void RaiseLocalOrdered<TEvent>(
            EntityUid uid,
            TEvent args,
            bool broadcast)
            where TEvent : EntityEventArgs
        {
            var found = new List<(EventHandler, OrderingData?)>();

            if (broadcast)
                CollectBroadcastOrdered(EventSource.Local, typeof(TEvent), found);

            _eventTables.CollectOrdered(uid, typeof(TEvent), found);

            DispatchOrderedEvents(args, found);

            if (broadcast)
                ProcessAwaitingMessages(EventSource.Local, args, typeof(TEvent));
        }

        private static void DispatchOrderedEvents(object eventArgs, List<(EventHandler, OrderingData?)> found)
        {
            var dict = new Dictionary<Type, (EventHandler handler, TopologicalSort.GraphNode<Type> node)>();

            foreach (var (handler, orderData) in found)
            {
                if (orderData != null)
                {
                    var node = new TopologicalSort.GraphNode<Type>(orderData.OrderType);
                    dict.Add(orderData.OrderType, (handler, node));
                }
            }

            foreach (var (_, orderData) in found)
            {
                if (orderData == null)
                    continue;

                var (type, before, after) = orderData;

                var thisNode = dict[type];

                if (before != null)
                {
                    foreach (var bef in before)
                    {
                        if (dict.TryGetValue(bef, out var befNode))
                            thisNode.node.Dependant.Add(befNode.node);
                    }
                }

                if (after != null)
                {
                    foreach (var aft in after)
                    {
                        if (dict.TryGetValue(aft, out var aftNode))
                            aftNode.node.Dependant.Add(thisNode.node);
                    }
                }
            }

            foreach (var type in TopologicalSort.Sort(dict.Values.Select(c => c.node)))
            {
                var handler = dict[type].handler;

                handler(eventArgs);
            }

            // Go over all handlers that don't have ordering so weren't included in the sort.
            foreach (var (handler, orderData) in found)
            {
                if (orderData == null)
                    handler(eventArgs);
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
