using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public interface IEventBus : IDirectedEventBus, IBroadcastEventBus { }

    public interface IDirectedEventBus
    {
        void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = true)
            where TEvent : notnull;

        void RaiseLocalEvent(EntityUid uid, object args, bool broadcast = true);

        void SubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : notnull;

        void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventHandler<TComp, TEvent> handler,
            Type orderType, Type[]? before=null, Type[]? after=null)
            where TComp : IComponent
            where TEvent : notnull;

        #region Ref Subscriptions

        void RaiseLocalEvent<TEvent>(EntityUid uid, ref TEvent args, bool broadcast = true)
            where TEvent : notnull;

        void RaiseLocalEvent(EntityUid uid, ref object args, bool broadcast = true);

        void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : notnull;

        void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventRefHandler<TComp, TEvent> handler,
            Type orderType, Type[]? before=null, Type[]? after=null)
            where TComp : IComponent
            where TEvent : notnull;

        #endregion

        void UnsubscribeLocalEvent<TComp, TEvent>()
            where TComp : IComponent
            where TEvent : notnull;
    }

    internal partial class EntityEventBus : IDirectedEventBus, IEventBus, IDisposable
    {
        private delegate void DirectedEventHandler(EntityUid uid, IComponent comp, object args);
        private delegate void DirectedEventHandler<in TEvent>(EntityUid uid, IComponent comp, TEvent args) where TEvent : notnull;

        private delegate void DirectedEventRefHandler(EntityUid uid, IComponent comp, ref object args);
        private delegate void DirectedEventRefHandler<TEvent>(EntityUid uid, IComponent comp, ref TEvent args) where TEvent : notnull;

        private IEntityManager _entMan;
        private EventTables _eventTables;

        /// <summary>
        /// Constructs a new instance of <see cref="EntityEventBus"/>.
        /// </summary>
        /// <param name="entMan">The entity manager to watch for entity/component events.</param>
        public EntityEventBus(IEntityManager entMan)
        {
            _entMan = entMan;
            _eventTables = new EventTables(_entMan);
        }

        /// <summary>
        /// Dispatches an event directly to a specific component.
        /// </summary>
        /// <remarks>
        /// This has a very specific purpose, and has massive potential to be abused.
        /// DO NOT EXPOSE THIS TO CONTENT.
        /// </remarks>
        /// <typeparam name="TEvent">Event to dispatch.</typeparam>
        /// <param name="component">Component receiving the event.</param>
        /// <param name="args">Event arguments for the event.</param>
        internal void RaiseComponentEvent<TEvent>(IComponent component, TEvent args)
            where TEvent : EntityEventArgs
        {
            _eventTables.DispatchComponent(component.Owner.Uid, component, args);
        }

        /// <inheritdoc />
        public void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = true)
            where TEvent : notnull
        {
            if (_orderedEvents.Contains(typeof(TEvent)))
            {
                RaiseLocalOrdered(uid, args, broadcast);
                return;
            }

            _eventTables.Dispatch(uid, args);

            // we also broadcast it so the call site does not have to.
            if(broadcast)
                RaiseEvent(EventSource.Local, args);
        }

        /// <inheritdoc />
        public void RaiseLocalEvent(EntityUid uid, object args, bool broadcast = true)
        {
            var type = args.GetType();

            if (_orderedEvents.Contains(type))
            {
                RaiseLocalOrdered(uid, args, broadcast);
                return;
            }

            _eventTables.Dispatch(uid, type, args);

            // we also broadcast it so the call site does not have to.
            if(broadcast)
                RaiseEvent(EventSource.Local, args);
        }

        public void RaiseLocalEvent<TEvent>(EntityUid uid, ref TEvent args, bool broadcast = true) where TEvent : notnull
        {
            if (_orderedEvents.Contains(typeof(TEvent)))
            {
                RaiseLocalOrdered(uid, args, broadcast);
                return;
            }

            _eventTables.Dispatch(uid, ref args);

            // we also broadcast it so the call site does not have to.
            if(broadcast)
                RaiseEvent(EventSource.Local, args);
        }

        public void RaiseLocalEvent(EntityUid uid, ref object args, bool broadcast = true)
        {
            var type = args.GetType();

            if (_orderedEvents.Contains(type))
            {
                RaiseLocalOrdered(uid, args, broadcast);
                return;
            }

            _eventTables.Dispatch(uid, type, ref args);

            // we also broadcast it so the call site does not have to.
            if(broadcast)
                RaiseEvent(EventSource.Local, args);
        }

        /// <inheritdoc />
        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, TEvent args)
                => handler(uid, (TComp) comp, args);

            _eventTables.Subscribe<TEvent>(typeof(TComp), typeof(TEvent), EventHandler, null);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventHandler<TComp, TEvent> handler,
            Type orderType,
            Type[]? before=null,
            Type[]? after=null)
            where TComp : IComponent
            where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, TEvent args)
                => handler(uid, (TComp) comp, args);

            var orderData = new OrderingData(orderType, before, after);

            _eventTables.Subscribe<TEvent>(typeof(TComp), typeof(TEvent), EventHandler, orderData);
            HandleOrderRegistration(typeof(TEvent), orderData);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler) where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref TEvent args)
                => handler(uid, (TComp) comp, ref args);

            _eventTables.Subscribe<TEvent>(typeof(TComp), typeof(TEvent), EventHandler, null);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler, Type orderType, Type[]? before = null,
            Type[]? after = null) where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref TEvent args)
                => handler(uid, (TComp) comp, ref args);

            var orderData = new OrderingData(orderType, before, after);

            _eventTables.Subscribe<TEvent>(typeof(TComp), typeof(TEvent), EventHandler, orderData);
            HandleOrderRegistration(typeof(TEvent), orderData);
        }

        /// <inheritdoc />
        public void UnsubscribeLocalEvent<TComp, TEvent>()
            where TComp : IComponent
            where TEvent : notnull
        {
            _eventTables.Unsubscribe(typeof(TComp), typeof(TEvent));
        }

        private class EventTables : IDisposable
        {
            private const string ValueDispatchError = "Tried to dispatch a value event to a by-reference subscription.";
            private const string RefDispatchError = "Tried to dispatch a ref event to a by-value subscription.";

            private IEntityManager _entMan;
            private IComponentFactory _comFac;

            // eUid -> EventType -> { CompType1, ... CompTypeN }
            private Dictionary<EntityUid, Dictionary<Type, HashSet<Type>>> _eventTables;

            // EventType -> CompType -> Handler
            private Dictionary<Type, Dictionary<Type, DirectedRegistration>> _subscriptions;

            // EventType -> Passed by Ref or not
            private Dictionary<Type, bool> _refEvents;

            // prevents shitcode, get your subscriptions figured out before you start spawning entities
            private bool _subscriptionLock;

            public EventTables(IEntityManager entMan)
            {
                _entMan = entMan;
                _comFac = entMan.ComponentManager.ComponentFactory;

                _entMan.EntityAdded += OnEntityAdded;
                _entMan.EntityDeleted += OnEntityDeleted;

                _entMan.ComponentManager.ComponentAdded += OnComponentAdded;
                _entMan.ComponentManager.ComponentRemoved += OnComponentRemoved;

                _eventTables = new();
                _subscriptions = new();
                _refEvents = new();
                _subscriptionLock = false;
            }

            private void OnEntityAdded(object? sender, EntityUid e)
            {
                AddEntity(e);
            }

            private void OnEntityDeleted(object? sender, EntityUid e)
            {
                RemoveEntity(e);
            }

            private void OnComponentAdded(object? sender, ComponentEventArgs e)
            {
                _subscriptionLock = true;

                AddComponent(e.OwnerUid, e.Component.GetType());
            }

            private void OnComponentRemoved(object? sender, ComponentEventArgs e)
            {
                RemoveComponent(e.OwnerUid, e.Component.GetType());
            }

            private void AddSubscription(Type compType, Type eventType, DirectedRegistration registration)
            {
                if (_subscriptionLock)
                    throw new InvalidOperationException("Subscription locked.");

                if (!_refEvents.TryGetValue(eventType, out var referenceEvent))
                {
                    _refEvents.Add(eventType, registration.ReferenceEvent);
                }

                if (referenceEvent != registration.ReferenceEvent)
                    throw new InvalidOperationException($"Attempted to subscribe by-ref and by-value to the same event!");

                if (!_subscriptions.TryGetValue(compType, out var compSubs))
                {
                    compSubs = new Dictionary<Type, DirectedRegistration>();
                    _subscriptions.Add(compType, compSubs);
                }

                if (compSubs.ContainsKey(eventType))
                    throw new InvalidOperationException($"Duplicate Subscriptions for comp={compType.Name}, event={eventType.Name}");

                compSubs.Add(eventType, registration);
            }

            public void Subscribe<TEvent>(Type compType, Type eventType, DirectedEventHandler<TEvent> handler, OrderingData? order)
                where TEvent : notnull
            {
                AddSubscription(compType, eventType, new DirectedRegistration(handler, order,
                    (DirectedEventHandler) ((uid, comp, ev) => handler(uid, comp, (TEvent) ev)), false));
            }

            public void Subscribe<TEvent>(Type compType, Type eventType, DirectedEventRefHandler<TEvent> handler, OrderingData? order)
                where TEvent : notnull
            {
                AddSubscription(compType, eventType, new DirectedRegistration(handler, order,
                    (DirectedEventRefHandler) ((EntityUid uid, IComponent comp, ref object ev) =>
                        {
                            var tev = (TEvent) ev;
                            handler(uid, comp, ref tev);

                        }), true));
            }

            public void Unsubscribe(Type compType, Type eventType)
            {
                if (_subscriptionLock)
                    throw new InvalidOperationException("Subscription locked.");

                if (!_subscriptions.TryGetValue(compType, out var compSubs))
                    return;

                compSubs.Remove(eventType);
            }

            private void AddEntity(EntityUid euid)
            {
                // odds are at least 1 component will subscribe to an event on the entity, so just
                // preallocate the table now. Dispatch does not need to check this later.
                _eventTables.Add(euid, new Dictionary<Type, HashSet<Type>>());
            }

            private void RemoveEntity(EntityUid euid)
            {
                _eventTables.Remove(euid);
            }

            private void AddComponent(EntityUid euid, Type compType)
            {
                var eventTable = _eventTables[euid];

                var enumerator = GetReferences(compType);
                while (enumerator.MoveNext(out var type))
                {
                    if (!_subscriptions.TryGetValue(type, out var compSubs))
                        continue;

                    foreach (var kvSub in compSubs)
                    {
                        if(!eventTable.TryGetValue(kvSub.Key, out var subscribedComps))
                        {
                            subscribedComps = new HashSet<Type>();
                            eventTable.Add(kvSub.Key, subscribedComps);
                        }

                        subscribedComps.Add(type);
                    }
                }
            }

            private void RemoveComponent(EntityUid euid, Type compType)
            {
                var eventTable = _eventTables[euid];

                var enumerator = GetReferences(compType);
                while (enumerator.MoveNext(out var type))
                {
                    if (!_subscriptions.TryGetValue(type, out var compSubs))
                        continue;

                    foreach (var kvSub in compSubs)
                    {
                        if (!eventTable.TryGetValue(kvSub.Key, out var subscribedComps))
                            return;

                        subscribedComps.Remove(type);
                    }
                }
            }

            public void Dispatch<TEvent>(EntityUid euid, TEvent args) where TEvent : notnull
            {
                var eventType = typeof(TEvent);

                if (!TryGetSubscriptions(eventType, euid, out var enumerator))
                    return;

                while (enumerator.MoveNext(out var tuple))
                {
                    var component = tuple.Value.Component;
                    var reg = tuple.Value.Registration;
                    DebugTools.Assert(!reg.ReferenceEvent, ValueDispatchError);
                    var handler = (DirectedEventHandler<TEvent>) reg.Original;
                    handler(euid, component, args);
                }
            }

            public void Dispatch(EntityUid euid, Type eventType, object args)
            {
                if (!TryGetSubscriptions(eventType, euid, out var enumerator))
                    return;

                while (enumerator.MoveNext(out var tuple))
                {
                    var component = tuple.Value.Component;
                    var reg = tuple.Value.Registration;
                    DebugTools.Assert(!reg.ReferenceEvent, ValueDispatchError);
                    var handler = (DirectedEventHandler) reg.Handler;
                    handler(euid, component, args);
                }
            }

            public void Dispatch<TEvent>(EntityUid euid, ref TEvent args) where TEvent : notnull
            {
                var eventType = typeof(TEvent);

                if (!TryGetSubscriptions(eventType, euid, out var enumerator))
                    return;

                while (enumerator.MoveNext(out var tuple))
                {
                    var component = tuple.Value.Component;
                    var reg = tuple.Value.Registration;
                    DebugTools.Assert(reg.ReferenceEvent, RefDispatchError);
                    var handler = (DirectedEventRefHandler<TEvent>) reg.Original;
                    handler(euid, component, ref args);
                }
            }

            public void Dispatch(EntityUid euid, Type eventType, ref object args)
            {
                if (!TryGetSubscriptions(eventType, euid, out var enumerator))
                    return;

                while (enumerator.MoveNext(out var tuple))
                {
                    var component = tuple.Value.Component;
                    var reg = tuple.Value.Registration;
                    DebugTools.Assert(reg.ReferenceEvent, RefDispatchError);
                    var handler = (DirectedEventRefHandler) reg.Handler;
                    handler(euid, component, ref args);
                }
            }

            public void CollectOrdered(
                EntityUid euid,
                Type eventType,
                List<(EventHandler, OrderingData?)> found)
            {
                var eventTable = _eventTables[euid];

                if(!eventTable.TryGetValue(eventType, out var subscribedComps))
                    return;

                foreach (var compType in subscribedComps)
                {
                    if(!_subscriptions.TryGetValue(compType, out var compSubs))
                        return;

                    if(!compSubs.TryGetValue(eventType, out var reg))
                        return;

                    var component = _entMan.ComponentManager.GetComponent(euid, compType);

                    if(reg.ReferenceEvent)
                        found.Add((ev => ((DirectedEventRefHandler)reg.Handler)(euid, component, ref ev), reg.Ordering));
                    else
                        found.Add((ev => ((DirectedEventHandler)reg.Handler)(euid, component, ev), reg.Ordering));
                }
            }

            public void DispatchComponent<TEvent>(EntityUid euid, IComponent component, TEvent args) where TEvent : notnull
            {
                var enumerator = GetReferences(component.GetType());
                while (enumerator.MoveNext(out var type))
                {
                    if (!_subscriptions.TryGetValue(type, out var compSubs))
                        continue;

                    if (!compSubs.TryGetValue(typeof(TEvent), out var reg))
                        continue;

                    var handler = (DirectedEventHandler<TEvent>)reg.Original;
                    handler(euid, component, args);
                }
            }

            public void ClearEntities()
            {
                _eventTables = new();
                _subscriptionLock = false;
            }

            public void Clear()
            {
                ClearEntities();
                _subscriptions = new();
            }

            public void Dispose()
            {
                _entMan.EntityAdded -= OnEntityAdded;
                _entMan.EntityDeleted -= OnEntityDeleted;

                _entMan.ComponentManager.ComponentAdded -= OnComponentAdded;
                _entMan.ComponentManager.ComponentRemoved -= OnComponentRemoved;

                // punishment for use-after-free
                _entMan = null!;
                _eventTables = null!;
                _subscriptions = null!;
            }

            /// <summary>
            ///     Enumerates the type's component references, returning the type itself last.
            /// </summary>
            private ReferencesEnumerator GetReferences(Type type)
            {
                return new(type, _comFac.GetRegistration(type).References);
            }

            /// <summary>
            ///     Enumerates all subscriptions for an event on a specific entity, returning the component instances and registrations.
            /// </summary>
            private bool TryGetSubscriptions(Type eventType, EntityUid euid, [NotNullWhen(true)] out SubscriptionsEnumerator enumerator)
            {
                var eventTable = _eventTables[euid];

                // No subscriptions to this event type, return null.
                if (!eventTable.TryGetValue(eventType, out var subscribedComps))
                {
                    enumerator = default;
                    return false;
                }

                enumerator = new(eventType, subscribedComps.GetEnumerator(), _subscriptions, euid, _entMan.ComponentManager);
                return true;
            }

            private struct ReferencesEnumerator
            {
                private readonly Type _baseType;
                private readonly IReadOnlyList<Type> _list;
                private readonly int _totalLength;
                private int _idx;

                public ReferencesEnumerator(Type baseType, IReadOnlyList<Type> list)
                {
                    _baseType = baseType;
                    _list = list;
                    _totalLength = list.Count;
                    _idx = 0;
                }

                public bool MoveNext([NotNullWhen(true)] out Type? type)
                {
                    if (_idx >= _totalLength)
                    {
                        if (_idx++ == _totalLength)
                        {
                            type = _baseType;
                            return true;
                        }

                        type = null;
                        return false;
                    }

                    type = _list[_idx++];
                    if (type == _baseType)
                        return MoveNext(out type);

                    return true;
                }
            }

            private struct SubscriptionsEnumerator : IDisposable
            {
                private readonly Type _eventType;
                private HashSet<Type>.Enumerator _enumerator;
                private readonly IReadOnlyDictionary<Type, Dictionary<Type, DirectedRegistration>> _subscriptions;
                private readonly EntityUid _uid;
                private readonly IComponentManager _componentManager;

                public SubscriptionsEnumerator(Type eventType, HashSet<Type>.Enumerator enumerator, IReadOnlyDictionary<Type, Dictionary<Type, DirectedRegistration>> subscriptions, EntityUid uid, IComponentManager componentManager)
                {
                    _eventType = eventType;
                    _enumerator = enumerator;
                    _subscriptions = subscriptions;
                    _componentManager = componentManager;
                    _uid = uid;
                }

                public bool MoveNext([NotNullWhen(true)] out (IComponent Component, DirectedRegistration Registration)? tuple)
                {
                    _enumerator.MoveNext();

                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (_enumerator.Current == null)
                    {
                        tuple = null;
                        return false;
                    }

                    var compType = _enumerator.Current;

                    if (!_subscriptions.TryGetValue(compType, out var compSubs))
                    {
                        tuple = null;
                        return false;
                    }

                    if (!compSubs.TryGetValue(_eventType, out var registration))
                    {
                        tuple = null;
                        return false;
                    }

                    tuple = (_componentManager.GetComponent(_uid, compType), registration);
                    return true;
                }

                public void Dispose()
                {
                    _enumerator.Dispose();
                }
            }
        }

        /// <inheritdoc />
        public void ClearEventTables()
        {
            _eventTables.Clear();
        }

        public void Dispose()
        {
            _eventTables.Dispose();
            _eventTables = null!;
            _entMan = null!;
        }

        private readonly struct DirectedRegistration
        {
            public readonly Delegate Original;
            public readonly OrderingData? Ordering;
            public readonly Delegate Handler;
            public readonly bool ReferenceEvent;

            public DirectedRegistration(Delegate original, OrderingData? ordering, Delegate handler, bool referenceEvent)
            {
                Original = original;
                Ordering = ordering;
                Handler = handler;
                ReferenceEvent = referenceEvent;
            }
        }
    }

    public delegate void ComponentEventHandler<in TComp, in TEvent>(EntityUid uid, TComp component, TEvent args)
        where TComp : IComponent
        where TEvent : notnull;

    public delegate void ComponentEventRefHandler<in TComp, TEvent>(EntityUid uid, TComp component, ref TEvent args)
        where TComp : IComponent
        where TEvent : notnull;
}
