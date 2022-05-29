using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public interface IEventBus : IDirectedEventBus, IBroadcastEventBus
    {
    }

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
            Type orderType, Type[]? before = null, Type[]? after = null)
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
            Type orderType, Type[]? before = null, Type[]? after = null)
            where TComp : IComponent
            where TEvent : notnull;

        #endregion

        void UnsubscribeLocalEvent<TComp, TEvent>()
            where TComp : IComponent
            where TEvent : notnull;

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
            where TEvent : notnull;

        /// <summary>
        /// Dispatches an event directly to a specific component, by-ref.
        /// </summary>
        /// <remarks>
        /// This has a very specific purpose, and has massive potential to be abused.
        /// DO NOT EXPOSE THIS TO CONTENT.
        /// </remarks>
        /// <typeparam name="TEvent">Event to dispatch.</typeparam>
        /// <param name="component">Component receiving the event.</param>
        /// <param name="args">Event arguments for the event.</param>
        internal void RaiseComponentEvent<TEvent>(IComponent component, ref TEvent args)
            where TEvent : notnull;

        public void OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare();
    }

    internal partial class EntityEventBus : IDirectedEventBus, IEventBus, IDisposable
    {
        private delegate void DirectedEventHandler(EntityUid uid, IComponent comp, ref Unit args);

        private delegate void DirectedEventHandler<TEvent>(EntityUid uid, IComponent comp, ref TEvent args)
            where TEvent : notnull;

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

        /// <inheritdoc />
        void IDirectedEventBus.RaiseComponentEvent<TEvent>(IComponent component, TEvent args)
        {
            ref var unitRef = ref Unsafe.As<TEvent, Unit>(ref args);

            _eventTables.DispatchComponent<TEvent>(component.Owner, component, ref unitRef, false);
        }

        /// <inheritdoc />
        void IDirectedEventBus.RaiseComponentEvent<TEvent>(IComponent component, ref TEvent args)
        {
            ref var unitRef = ref Unsafe.As<TEvent, Unit>(ref args);

            _eventTables.DispatchComponent<TEvent>(component.Owner, component, ref unitRef, true);
        }

        public void OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare()
        {
            _eventTables.IgnoreUnregisteredComponents = true;
        }

        /// <inheritdoc />
        public void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = true)
            where TEvent : notnull
        {
            var type = typeof(TEvent);
            ref var unitRef = ref Unsafe.As<TEvent, Unit>(ref args);

            RaiseLocalEventCore(uid, ref unitRef, type, broadcast, false);
        }

        /// <inheritdoc />
        public void RaiseLocalEvent(EntityUid uid, object args, bool broadcast = true)
        {
            var type = args.GetType();
            ref var unitRef = ref Unsafe.As<object, Unit>(ref args);

            RaiseLocalEventCore(uid, ref unitRef, type, broadcast, false);
        }

        public void RaiseLocalEvent<TEvent>(EntityUid uid, ref TEvent args, bool broadcast = true)
            where TEvent : notnull
        {
            var type = typeof(TEvent);
            ref var unitRef = ref Unsafe.As<TEvent, Unit>(ref args);

            RaiseLocalEventCore(uid, ref unitRef, type, broadcast, true);
        }

        public void RaiseLocalEvent(EntityUid uid, ref object args, bool broadcast = true)
        {
            var type = args.GetType();
            ref var unitRef = ref Unsafe.As<object, Unit>(ref args);

            RaiseLocalEventCore(uid, ref unitRef, type, broadcast, true);
        }

        private void RaiseLocalEventCore(EntityUid uid, ref Unit unitRef, Type type, bool broadcast, bool byRef)
        {
            if (_orderedEvents.Contains(type))
            {
                RaiseLocalOrdered(uid, type, ref unitRef, broadcast, byRef);
                return;
            }

            _eventTables.Dispatch(uid, type, ref unitRef, byRef);

            // we also broadcast it so the call site does not have to.
            if (broadcast)
                ProcessSingleEvent(EventSource.Local, ref unitRef, type, byRef);
        }

        /// <inheritdoc />
        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref TEvent args)
                => handler(uid, (TComp)comp, args);

            _eventTables.Subscribe<TEvent>(
                CompIdx.Index<TComp>(),
                typeof(TComp),
                typeof(TEvent),
                EventHandler,
                null,
                false);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventHandler<TComp, TEvent> handler,
            Type orderType,
            Type[]? before = null,
            Type[]? after = null)
            where TComp : IComponent
            where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref TEvent args)
                => handler(uid, (TComp)comp, args);

            var orderData = new OrderingData(orderType, before, after);

            _eventTables.Subscribe<TEvent>(
                CompIdx.Index<TComp>(),
                typeof(TComp),
                typeof(TEvent),
                EventHandler,
                orderData,
                false);
            HandleOrderRegistration(typeof(TEvent), orderData);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler)
            where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref TEvent args)
                => handler(uid, (TComp)comp, ref args);

            _eventTables.Subscribe<TEvent>(
                CompIdx.Index<TComp>(),
                typeof(TComp),
                typeof(TEvent),
                EventHandler,
                null,
                true);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler, Type orderType,
            Type[]? before = null,
            Type[]? after = null) where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref TEvent args)
                => handler(uid, (TComp)comp, ref args);

            var orderData = new OrderingData(orderType, before, after);

            _eventTables.Subscribe<TEvent>(
                CompIdx.Index<TComp>(),
                typeof(TComp),
                typeof(TEvent),
                EventHandler,
                orderData,
                true);

            HandleOrderRegistration(typeof(TEvent), orderData);
        }

        /// <inheritdoc />
        public void UnsubscribeLocalEvent<TComp, TEvent>()
            where TComp : IComponent
            where TEvent : notnull
        {
            _eventTables.Unsubscribe(CompIdx.Index<TComp>(), typeof(TEvent));
        }

        private sealed class EventTables : IDisposable
        {
            private const string ValueDispatchError = "Tried to dispatch a value event to a by-reference subscription.";
            private const string RefDispatchError = "Tried to dispatch a ref event to a by-value subscription.";

            private IEntityManager _entMan;
            private IComponentFactory _comFac;

            // eUid -> EventType -> { CompType1, ... CompTypeN }
            private Dictionary<EntityUid, Dictionary<Type, HashSet<CompIdx>>> _eventTables;

            // CompType -> EventType -> Handler
            private Dictionary<Type, DirectedRegistration>?[] _subscriptions;

            // prevents shitcode, get your subscriptions figured out before you start spawning entities
            private bool _subscriptionLock;

            public EventTables(IEntityManager entMan)
            {
                _entMan = entMan;
                _comFac = entMan.ComponentFactory;

                _entMan.EntityAdded += OnEntityAdded;
                _entMan.EntityDeleted += OnEntityDeleted;

                _entMan.ComponentAdded += OnComponentAdded;
                _entMan.ComponentRemoved += OnComponentRemoved;

                // Dynamic handling of components is only for RobustUnitTest compatibility spaghetti.
                _comFac.ComponentAdded += ComFacOnComponentAdded;
                _comFac.ComponentReferenceAdded += ComFacOnComponentReferenceAdded;

                _eventTables = new();
                _subscriptions = Array.Empty<Dictionary<Type, DirectedRegistration>>();
                _subscriptionLock = false;

                InitSubscriptionsArray();
            }

            public bool IgnoreUnregisteredComponents;

            private void InitSubscriptionsArray()
            {
                foreach (var refType in _comFac.GetAllRefTypes())
                {
                    CompIdx.AssignArray(ref _subscriptions, refType, new Dictionary<Type, DirectedRegistration>());
                }
            }

            private void ComFacOnComponentReferenceAdded(ComponentRegistration arg1, CompIdx arg2)
            {
                CompIdx.RefArray(ref _subscriptions, arg2) ??= new Dictionary<Type, DirectedRegistration>();
            }

            private void ComFacOnComponentAdded(ComponentRegistration obj)
            {
                CompIdx.RefArray(ref _subscriptions, obj.Idx) ??= new Dictionary<Type, DirectedRegistration>();
            }

            private void OnEntityAdded(EntityUid e)
            {
                AddEntity(e);
            }

            private void OnEntityDeleted(EntityUid e)
            {
                RemoveEntity(e);
            }

            private void OnComponentAdded(AddedComponentEventArgs e)
            {
                _subscriptionLock = true;

                AddComponent(e.BaseArgs.Owner, CompIdx.Index(e.BaseArgs.Component.GetType()));
            }

            private void OnComponentRemoved(RemovedComponentEventArgs e)
            {
                RemoveComponent(e.BaseArgs.Owner, CompIdx.Index(e.BaseArgs.Component.GetType()));
            }

            private void AddSubscription(CompIdx compType, Type compTypeObj, Type eventType, DirectedRegistration registration)
            {
                if (_subscriptionLock)
                    throw new InvalidOperationException("Subscription locked.");

                var referenceEvent = eventType.HasCustomAttribute<ByRefEventAttribute>();

                if (referenceEvent != registration.ReferenceEvent)
                    throw new InvalidOperationException(
                        $"Attempted to subscribe by-ref and by-value to the same directed event! comp={compTypeObj.Name}, event={eventType.Name} eventIsByRef={referenceEvent} subscriptionIsByRef={registration.ReferenceEvent}");

                if (compType.Value >= _subscriptions.Length || _subscriptions[compType.Value] is not { } compSubs)
                {
                    if (IgnoreUnregisteredComponents)
                        return;

                    throw new InvalidOperationException($"Component is not a valid reference type: {compTypeObj.Name}");
                }

                if (compSubs.ContainsKey(eventType))
                    throw new InvalidOperationException(
                        $"Duplicate Subscriptions for comp={compTypeObj}, event={eventType.Name}");

                compSubs.Add(eventType, registration);
            }

            public void Subscribe<TEvent>(
                CompIdx compType,
                Type compTypeObj,
                Type eventType,
                DirectedEventHandler<TEvent> handler,
                OrderingData? order, bool byReference)
                where TEvent : notnull
            {
                AddSubscription(compType, compTypeObj, eventType, new DirectedRegistration(handler, order,
                    (EntityUid uid, IComponent comp, ref Unit ev) =>
                    {
                        ref var tev = ref Unsafe.As<Unit, TEvent>(ref ev);
                        handler(uid, comp, ref tev);
                    }, byReference));
            }

            public void Unsubscribe(CompIdx compType, Type eventType)
            {
                if (_subscriptionLock)
                    throw new InvalidOperationException("Subscription locked.");

                if (compType.Value >= _subscriptions.Length || _subscriptions[compType.Value] is not { } compSubs)
                {
                    if (IgnoreUnregisteredComponents)
                        return;

                    throw new InvalidOperationException("Trying to unsubscribe from unregistered component!");
                }

                compSubs.Remove(eventType);
            }

            private void AddEntity(EntityUid euid)
            {
                // odds are at least 1 component will subscribe to an event on the entity, so just
                // preallocate the table now. Dispatch does not need to check this later.
                _eventTables.Add(euid, new Dictionary<Type, HashSet<CompIdx>>());
            }

            private void RemoveEntity(EntityUid euid)
            {
                _eventTables.Remove(euid);
            }

            private void AddComponent(EntityUid euid, CompIdx compType)
            {
                var eventTable = _eventTables[euid];

                var enumerator = GetReferences(compType);
                while (enumerator.MoveNext(out var type))
                {
                    var compSubs = _subscriptions[type.Value]!;

                    foreach (var kvSub in compSubs)
                    {
                        if (!eventTable.TryGetValue(kvSub.Key, out var subscribedComps))
                        {
                            subscribedComps = new HashSet<CompIdx>();
                            eventTable.Add(kvSub.Key, subscribedComps);
                        }

                        subscribedComps.Add(type);
                    }
                }
            }

            private void RemoveComponent(EntityUid euid, CompIdx compType)
            {
                var eventTable = _eventTables[euid];

                var enumerator = GetReferences(compType);
                while (enumerator.MoveNext(out var type))
                {
                    var compSubs = _subscriptions[type.Value]!;

                    foreach (var kvSub in compSubs)
                    {
                        if (!eventTable.TryGetValue(kvSub.Key, out var subscribedComps))
                            return;

                        subscribedComps.Remove(type);
                    }
                }
            }

            public void Dispatch(EntityUid euid, Type eventType, ref Unit args, bool dispatchByReference)
            {
                if (!TryGetSubscriptions(eventType, euid, out var enumerator))
                    return;

                while (enumerator.MoveNext(out var tuple))
                {
                    var (component, reg) = tuple.Value;
                    if (reg.ReferenceEvent != dispatchByReference)
                        ThrowByRefMisMatch();

                    reg.Handler(euid, component, ref args);
                }
            }

            public void CollectOrdered(
                EntityUid euid,
                Type eventType,
                List<(RefEventHandler, OrderingData?)> found,
                bool byRef)
            {
                var eventTable = _eventTables[euid];

                if (!eventTable.TryGetValue(eventType, out var subscribedComps))
                    return;

                foreach (var compType in subscribedComps)
                {
                    var compSubs = _subscriptions[compType.Value]!;

                    if (!compSubs.TryGetValue(eventType, out var reg))
                        return;

                    if (reg.ReferenceEvent != byRef)
                        ThrowByRefMisMatch();

                    var component = _entMan.GetComponent(euid, compType);

                    found.Add(((ref Unit ev) => reg.Handler(euid, component, ref ev), reg.Ordering));
                }
            }

            public void DispatchComponent<TEvent>(EntityUid euid, IComponent component, ref Unit args, bool dispatchByReference)
                where TEvent : notnull
            {
                var enumerator = GetReferences(CompIdx.Index(component.GetType()));
                while (enumerator.MoveNext(out var type))
                {
                    var compSubs = _subscriptions[type.Value]!;

                    if (!compSubs.TryGetValue(typeof(TEvent), out var reg))
                        continue;

                    if (reg.ReferenceEvent != dispatchByReference)
                        ThrowByRefMisMatch();

                    reg.Handler(euid, component, ref args);
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

                foreach (var sub in _subscriptions)
                {
                    sub?.Clear();
                }
            }

            public void Dispose()
            {
                _entMan.EntityAdded -= OnEntityAdded;
                _entMan.EntityDeleted -= OnEntityDeleted;

                _entMan.ComponentAdded -= OnComponentAdded;
                _entMan.ComponentRemoved -= OnComponentRemoved;

                _comFac.ComponentAdded -= ComFacOnComponentAdded;
                _comFac.ComponentReferenceAdded -= ComFacOnComponentReferenceAdded;

                // punishment for use-after-free
                _entMan = null!;
                _eventTables = null!;
                _subscriptions = null!;
            }

            /// <summary>
            ///     Enumerates the type's component references, returning the type itself last.
            /// </summary>
            private ReferencesEnumerator GetReferences(CompIdx type)
            {
                return new(type, _comFac.GetRegistration(type).References);
            }

            /// <summary>
            ///     Enumerates all subscriptions for an event on a specific entity, returning the component instances and registrations.
            /// </summary>
            private bool TryGetSubscriptions(Type eventType, EntityUid euid, [NotNullWhen(true)] out SubscriptionsEnumerator enumerator)
            {
                if (!_eventTables.TryGetValue(euid, out var eventTable))
                {
                    enumerator = default!;
                    return false;
                }

                // No subscriptions to this event type, return null.
                if (!eventTable.TryGetValue(eventType, out var subscribedComps))
                {
                    enumerator = default;
                    return false;
                }

                enumerator = new(eventType, subscribedComps.GetEnumerator(), _subscriptions, euid, _entMan);
                return true;
            }

            private struct ReferencesEnumerator
            {
                private readonly CompIdx _baseType;
                private readonly ValueList<CompIdx> _list;
                private readonly int _totalLength;
                private int _idx;

                public ReferencesEnumerator(CompIdx baseType, ValueList<CompIdx> list)
                {
                    _baseType = baseType;
                    _list = list;
                    _totalLength = list.Count;
                    _idx = 0;
                }

                public bool MoveNext([NotNullWhen(true)] out CompIdx type)
                {
                    if (_idx >= _totalLength)
                    {
                        if (_idx++ == _totalLength)
                        {
                            type = _baseType;
                            return true;
                        }

                        type = default;
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
                private HashSet<CompIdx>.Enumerator _enumerator;
                private readonly Dictionary<Type, DirectedRegistration>?[] _subscriptions;
                private readonly EntityUid _uid;
                private readonly IEntityManager _entityManager;

                public SubscriptionsEnumerator(
                    Type eventType,
                    HashSet<CompIdx>.Enumerator enumerator,
                    Dictionary<Type, DirectedRegistration>?[] subscriptions,
                    EntityUid uid,
                    IEntityManager entityManager)
                {
                    _eventType = eventType;
                    _enumerator = enumerator;
                    _subscriptions = subscriptions;
                    _entityManager = entityManager;
                    _uid = uid;
                }

                public bool MoveNext(
                    [NotNullWhen(true)] out (IComponent Component, DirectedRegistration Registration)? tuple)
                {
                    // ReSharper disable once ConditionIsAlwaysTrueOrFalse
                    if (!_enumerator.MoveNext())
                    {
                        tuple = null;
                        return false;
                    }

                    var compType = _enumerator.Current;
                    var compSubs = _subscriptions[compType.Value]!;

                    if (!compSubs.TryGetValue(_eventType, out var registration))
                    {
                        tuple = null;
                        return false;
                    }

                    tuple = (_entityManager.GetComponent(_uid, compType), registration);
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
            public readonly DirectedEventHandler Handler;
            public readonly bool ReferenceEvent;

            public DirectedRegistration(Delegate original, OrderingData? ordering, DirectedEventHandler handler,
                bool referenceEvent)
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
