using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.GameObjects
{
    public interface IEventBus : IDirectedEventBus, IBroadcastEventBus { }

    public interface IDirectedEventBus
    {
        void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = true)
            where TEvent:EntityEventArgs;

        public void RaiseLocalEvent(EntityUid uid, EntityEventArgs args, bool broadcast = true);

        void SubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : EntityEventArgs;

        void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventHandler<TComp, TEvent> handler,
            Type orderType, Type[]? before=null, Type[]? after=null)
            where TComp : IComponent
            where TEvent : EntityEventArgs;

        [Obsolete("Use the overload without the handler argument.")]
        void UnsubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : EntityEventArgs;

        void UnsubscribeLocalEvent<TComp, TEvent>()
            where TComp : IComponent
            where TEvent : EntityEventArgs;
    }

    internal partial class EntityEventBus : IDirectedEventBus, IEventBus, IDisposable
    {
        private delegate void DirectedEventHandler(EntityUid uid, IComponent comp, EntityEventArgs args);

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
            _eventTables.DispatchComponent(component.Owner.Uid, component, typeof(TEvent), args);
        }

        /// <inheritdoc />
        public void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = true)
            where TEvent : EntityEventArgs
        {
            if (_orderedEvents.Contains(typeof(TEvent)))
            {
                RaiseLocalOrdered(uid, args, broadcast);
                return;
            }

            _eventTables.Dispatch(uid, typeof(TEvent), args);

            // we also broadcast it so the call site does not have to.
            if(broadcast)
                RaiseEvent(EventSource.Local, args);
        }

        /// <inheritdoc />
        public void RaiseLocalEvent(EntityUid uid, EntityEventArgs args, bool broadcast = true)
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

        /// <inheritdoc />
        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : EntityEventArgs
        {
            void EventHandler(EntityUid uid, IComponent comp, EntityEventArgs args)
                => handler(uid, (TComp) comp, (TEvent) args);

            _eventTables.Subscribe(typeof(TComp), typeof(TEvent), EventHandler, null);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventHandler<TComp, TEvent> handler,
            Type orderType,
            Type[]? before=null,
            Type[]? after=null)
            where TComp : IComponent
            where TEvent : EntityEventArgs
        {
            void EventHandler(EntityUid uid, IComponent comp, EntityEventArgs args)
                => handler(uid, (TComp) comp, (TEvent) args);

            var orderData = new OrderingData(orderType, before, after);

            _eventTables.Subscribe(typeof(TComp), typeof(TEvent), EventHandler, orderData);
            HandleOrderRegistration(typeof(TEvent), orderData);
        }

        /// <inheritdoc />
        [Obsolete("Use the overload without the handler argument.")]
        public void UnsubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : EntityEventArgs
        {
            _eventTables.Unsubscribe(typeof(TComp), typeof(TEvent));
        }

        /// <inheritdoc />
        public void UnsubscribeLocalEvent<TComp, TEvent>()
            where TComp : IComponent
            where TEvent : EntityEventArgs
        {
            _eventTables.Unsubscribe(typeof(TComp), typeof(TEvent));
        }

        private class EventTables : IDisposable
        {
            private IEntityManager _entMan;
            private IComponentFactory _comFac;

            // eUid -> EventType -> { CompType1, ... CompTypeN }
            private Dictionary<EntityUid, Dictionary<Type, HashSet<Type>>> _eventTables;

            // EventType -> CompType -> Handler
            private Dictionary<Type, Dictionary<Type, (DirectedEventHandler handler, OrderingData? ordering)>> _subscriptions;

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

            public void Subscribe(Type compType, Type eventType, DirectedEventHandler handler, OrderingData? order)
            {
                if (_subscriptionLock)
                    throw new InvalidOperationException("Subscription locked.");

                if (!_subscriptions.TryGetValue(compType, out var compSubs))
                {
                    compSubs = new Dictionary<Type, (DirectedEventHandler, OrderingData?)>();
                    _subscriptions.Add(compType, compSubs);
                }

                if (compSubs.ContainsKey(eventType))
                    throw new InvalidOperationException($"Duplicate Subscriptions for comp={compType.Name}, event={eventType.Name}");

                compSubs.Add(eventType, (handler, order));
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

            public void Dispatch(EntityUid euid, Type eventType, EntityEventArgs args)
            {
                var eventTable = _eventTables[euid];

                if(!eventTable.TryGetValue(eventType, out var subscribedComps))
                    return;

                foreach (var compType in subscribedComps)
                {
                    if(!_subscriptions.TryGetValue(compType, out var compSubs))
                        return;

                    if(!compSubs.TryGetValue(eventType, out var sub))
                        return;

                    var (handler, _) = sub;
                    var component = _entMan.ComponentManager.GetComponent(euid, compType);

                    handler(euid, component, args);
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

                    if(!compSubs.TryGetValue(eventType, out var sub))
                        return;

                    var (handler, order) = sub;
                    var component = _entMan.ComponentManager.GetComponent(euid, compType);

                    found.Add((ev => handler(euid, component, (EntityEventArgs) ev), order));
                }
            }

            public void DispatchComponent(EntityUid euid, IComponent component, Type eventType, EntityEventArgs args)
            {
                var enumerator = GetReferences(component.GetType());
                while (enumerator.MoveNext(out var type))
                {
                    if (!_subscriptions.TryGetValue(type, out var compSubs))
                        continue;

                    if (!compSubs.TryGetValue(eventType, out var sub))
                        continue;

                    var (handler, _) = sub;
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
    }

    public delegate void ComponentEventHandler<in TComp, in TEvent>(EntityUid uid, TComp component, TEvent args)
        where TComp : IComponent
        where TEvent : EntityEventArgs;

}
