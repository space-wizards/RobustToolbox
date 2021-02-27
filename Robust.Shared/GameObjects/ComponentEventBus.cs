using System;
using System.Collections.Generic;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    /// <summary>
    /// A central unified event bus that can raise both entity and component events.
    /// </summary>
    public interface IEventBus : IComponentEventBus, IEntityEventBus { }

    internal class CombinedEventBus : ComponentEventBus, IEventBus
    {
        public CombinedEventBus(IComponentManager compMan) : base(compMan) { }
    }

    public interface IComponentEventBus
    {
        void RaiseCompEvent<TEvent>(EntityUid uid, TEvent args)
            where TEvent:ComponentEvent;

        void SubscribeCompEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : ComponentEvent;

        void UnsubscribeCompEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : ComponentEvent;
    }

    internal class ComponentEventBus : EntityEventBus, IComponentEventBus, IDisposable
    {
        private delegate void EventHandler(EntityUid uid, IComponent comp, ComponentEvent args);

        private IComponentManager _compMan;
        private EventTables _eventTables;
        
        /// <summary>
        /// Constructs a new instance of <see cref="ComponentEventBus"/>.
        /// </summary>
        /// <param name="compMan">The component manager to use</param>
        public ComponentEventBus(IComponentManager compMan)
        {
            _compMan = compMan;
            _eventTables = new EventTables(_compMan);
        }

        /// <inheritdoc />
        public void RaiseCompEvent<TEvent>(EntityUid uid, TEvent args)
            where TEvent:ComponentEvent
        {
            _eventTables.Dispatch(uid, typeof(TEvent), args);
        }

        /// <inheritdoc />
        public void SubscribeCompEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : ComponentEvent
        {
            void EventHandler(EntityUid uid, IComponent comp, ComponentEvent args)
                => handler(uid, (TComp) comp, (TEvent) args);

            _eventTables.Subscribe(typeof(TComp), typeof(TEvent), EventHandler);
        }

        /// <inheritdoc />
        public void UnsubscribeCompEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : ComponentEvent
        {
            _eventTables.Unsubscribe(typeof(TComp), typeof(TEvent));
        }
        
        private class EventTables : IDisposable
        {
            private IComponentManager _compMan;

            // eUid -> EventType -> { CompType1, ... CompTypeN }
            private Dictionary<EntityUid, Dictionary<Type, HashSet<Type>>> _eventTables;

            // EventType -> CompType -> Handler
            private Dictionary<Type, Dictionary<Type, EventHandler>> _subscriptions;

            // prevents shitcode, get your subscriptions figured out before you start spawning entities
            private bool _subscriptionLock;

            public EventTables(IComponentManager compMan)
            {
                _compMan = compMan;

                _compMan.ComponentAdded += OnComponentAdded;
                _compMan.ComponentRemoved += OnComponentRemoved;
                
                _eventTables = new();
                _subscriptions = new();
                _subscriptionLock = false;
            }
            
            private void OnComponentAdded(object? sender, ComponentEventArgs e)
            {
                _subscriptionLock = true;

                var comp = e.Component;
                var euid = e.OwnerUid;

                if(comp is MetaDataComponent)
                    AddEntity(euid);

                AddComponent(euid, comp.GetType());
            }

            private void OnComponentRemoved(object? sender, ComponentEventArgs e)
            {
                var comp = e.Component;
                var euid = e.OwnerUid;

                if (e.Component is MetaDataComponent)
                    RemoveEntity(euid);
                else
                    RemoveComponent(euid, comp.GetType());
            }

            public void Subscribe(Type compType, Type eventType, EventHandler handler)
            {
                if (_subscriptionLock)
                    throw new InvalidOperationException("Subscription locked.");

                if (!_subscriptions.TryGetValue(compType, out var compSubs))
                {
                    compSubs = new Dictionary<Type, EventHandler>();
                    _subscriptions.Add(compType, compSubs);

                    compSubs.Add(eventType, handler);
                }
                else
                {
                    if (compSubs.ContainsKey(eventType))
                        throw new InvalidOperationException($"Duplicate Subscriptions for comp={compType.Name}, event={eventType.Name}");

                    compSubs.Add(eventType, handler);
                }
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

                if (!_subscriptions.TryGetValue(compType, out var compSubs))
                    return;

                foreach (var kvSub in compSubs)
                {
                    if(!eventTable.TryGetValue(kvSub.Key, out var subscribedComps))
                    {
                        subscribedComps = new HashSet<Type>();
                        eventTable.Add(kvSub.Key, subscribedComps);
                    }

                    subscribedComps.Add(compType);
                }
            }

            private void RemoveComponent(EntityUid euid, Type compType)
            {
                var eventTable = _eventTables[euid];

                if (!_subscriptions.TryGetValue(compType, out var compSubs))
                    return;

                foreach (var kvSub in compSubs)
                {
                    if (!eventTable.TryGetValue(kvSub.Key, out var subscribedComps))
                        return;

                    subscribedComps.Remove(compType);
                }
            }

            public void Dispatch(EntityUid euid, Type eventType, ComponentEvent args)
            {
                var eventTable = _eventTables[euid];
                
                if(!eventTable.TryGetValue(eventType, out var subscribedComps))
                    return;

                foreach (var compType in subscribedComps)
                {
                    if(!_subscriptions.TryGetValue(compType, out var compSubs))
                        return;

                    if(!compSubs.TryGetValue(eventType, out var handler))
                        return;

                    var component = _compMan.GetComponent(euid, compType);
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
                _compMan.ComponentAdded -= OnComponentAdded;
                _compMan.ComponentRemoved -= OnComponentRemoved;

                // punishment for use-after-free
                _compMan = null!;
                _eventTables = null!;
                _subscriptions = null!;
            }
        }

        public void Dispose()
        {
            _eventTables.Dispose();
            _eventTables = null!;
            _compMan = null!;
        }
    }

    [Serializable, NetSerializable]
    public abstract class ComponentEvent { }

    public delegate void ComponentEventHandler<in TComp, in TEvent>(EntityUid uid, TComp component, TEvent args)
        where TComp : IComponent
        where TEvent : ComponentEvent;

}
