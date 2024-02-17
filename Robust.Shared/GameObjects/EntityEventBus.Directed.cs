using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    public interface IEventBus : IDirectedEventBus, IBroadcastEventBus
    {
    }

    public interface IDirectedEventBus
    {
        void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = false)
            where TEvent : notnull;

        void RaiseLocalEvent(EntityUid uid, object args, bool broadcast = false);

        void SubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : notnull;

        void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventHandler<TComp, TEvent> handler,
            Type orderType, Type[]? before = null, Type[]? after = null)
            where TComp : IComponent
            where TEvent : notnull;

        #region Ref Subscriptions

        void RaiseLocalEvent<TEvent>(EntityUid uid, ref TEvent args, bool broadcast = false)
            where TEvent : notnull;

        void RaiseLocalEvent(EntityUid uid, ref object args, bool broadcast = false);

        void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : notnull;

        void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventRefHandler<TComp, TEvent> handler,
            Type orderType, Type[]? before = null, Type[]? after = null)
            where TComp : IComponent
            where TEvent : notnull;

        void SubscribeLocalEvent<TComp, TEvent>(
            EntityEventRefHandler<TComp, TEvent> handler,
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
        /// DO NOT USE THIS IN CONTENT UNLESS YOU KNOW WHAT YOU'RE DOING, the only reason it's not internal
        /// is because of the component network source generator.
        /// </remarks>
        /// <typeparam name="TEvent">Event to dispatch.</typeparam>
        /// <param name="component">Component receiving the event.</param>
        /// <param name="args">Event arguments for the event.</param>
        public void RaiseComponentEvent<TEvent>(IComponent component, TEvent args)
            where TEvent : notnull;

        /// <summary>
        /// Dispatches an event directly to a specific component.
        /// </summary>
        /// <remarks>
        /// This has a very specific purpose, and has massive potential to be abused.
        /// DO NOT USE THIS IN CONTENT UNLESS YOU KNOW WHAT YOU'RE DOING, the only reason it's not internal
        /// is because of the component network source generator.
        /// </remarks>
        /// <typeparam name="TEvent">Event to dispatch.</typeparam>
        /// <param name="component">Component receiving the event.</param>
        /// <param name="idx">Type of the component, for faster lookups.</param>
        /// <param name="args">Event arguments for the event.</param>
        public void RaiseComponentEvent<TEvent>(IComponent component, CompIdx idx, TEvent args)
            where TEvent : notnull;

        /// <summary>
        /// Dispatches an event directly to a specific component, by-ref.
        /// </summary>
        /// <remarks>
        /// This has a very specific purpose, and has massive potential to be abused.
        /// DO NOT USE THIS IN CONTENT UNLESS YOU KNOW WHAT YOU'RE DOING, the only reason it's not internal
        /// is because of the component network source generator.
        /// </remarks>
        /// <typeparam name="TEvent">Event to dispatch.</typeparam>
        /// <param name="component">Component receiving the event.</param>
        /// <param name="args">Event arguments for the event.</param>
        public void RaiseComponentEvent<TEvent>(IComponent component, ref TEvent args)
            where TEvent : notnull;

        public void OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare();
    }

    internal partial class EntityEventBus : IDisposable
    {
        internal delegate void DirectedEventHandler(EntityUid uid, IComponent comp, ref Unit args);

        private delegate void DirectedEventHandler<TEvent>(EntityUid uid, IComponent comp, ref TEvent args)
            where TEvent : notnull;

        /// <summary>
        /// Constructs a new instance of <see cref="EntityEventBus"/>.
        /// </summary>
        /// <param name="entMan">The entity manager to watch for entity/component events.</param>
        public EntityEventBus(IEntityManager entMan)
        {
            _entMan = entMan;
            _comFac = entMan.ComponentFactory;

            // Dynamic handling of components is only for RobustUnitTest compatibility spaghetti.
            _comFac.ComponentsAdded += ComFacOnComponentsAdded;
            ComFacOnComponentsAdded(_comFac.GetAllRegistrations().ToArray());
        }

        /// <inheritdoc />
        void IDirectedEventBus.RaiseComponentEvent<TEvent>(IComponent component, TEvent args)
        {
            ref var unitRef = ref Unsafe.As<TEvent, Unit>(ref args);

            DispatchComponent<TEvent>(
                component.Owner,
                component,
                CompIdx.Index(component.GetType()),
                ref unitRef);
        }

        void IDirectedEventBus.RaiseComponentEvent<TEvent>(IComponent component, CompIdx type, TEvent args)
        {
            ref var unitRef = ref Unsafe.As<TEvent, Unit>(ref args);

            DispatchComponent<TEvent>(
                component.Owner,
                component,
                type,
                ref unitRef);
        }

        /// <inheritdoc />
        void IDirectedEventBus.RaiseComponentEvent<TEvent>(IComponent component, ref TEvent args)
        {
            ref var unitRef = ref Unsafe.As<TEvent, Unit>(ref args);

            DispatchComponent<TEvent>(
                component.Owner,
                component,
                CompIdx.Index(component.GetType()),
                ref unitRef);
        }

        public void OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare()
        {
            IgnoreUnregisteredComponents = true;
        }

        /// <inheritdoc />
        public void RaiseLocalEvent<TEvent>(EntityUid uid, TEvent args, bool broadcast = false)
            where TEvent : notnull
        {
            var type = typeof(TEvent);
            ref var unitRef = ref Unsafe.As<TEvent, Unit>(ref args);

            RaiseLocalEventCore(uid, ref unitRef, type, broadcast);
        }

        /// <inheritdoc />
        public void RaiseLocalEvent(EntityUid uid, object args, bool broadcast = false)
        {
            var type = args.GetType();
            ref var unitRef = ref Unsafe.As<object, Unit>(ref args);

            RaiseLocalEventCore(uid, ref unitRef, type, broadcast);
        }

        public void RaiseLocalEvent<TEvent>(EntityUid uid, ref TEvent args, bool broadcast = false)
            where TEvent : notnull
        {
            var type = typeof(TEvent);
            ref var unitRef = ref Unsafe.As<TEvent, Unit>(ref args);

            RaiseLocalEventCore(uid, ref unitRef, type, broadcast);
        }

        public void RaiseLocalEvent(EntityUid uid, ref object args, bool broadcast = false)
        {
            var type = args.GetType();
            ref var unitRef = ref Unsafe.As<object, Unit>(ref args);

            RaiseLocalEventCore(uid, ref unitRef, type, broadcast);
        }

        private void RaiseLocalEventCore(EntityUid uid, ref Unit unitRef, Type type, bool broadcast)
        {
            if (!_eventData.TryGetValue(type, out var subs))
                return;

            if (subs.IsOrdered)
            {
                RaiseLocalOrdered(uid, type, subs, ref unitRef, broadcast);
                return;
            }

            EntDispatch(uid, type, ref unitRef);

            // we also broadcast it so the call site does not have to.
            if (broadcast)
                ProcessSingleEventCore(EventSource.Local, ref unitRef, subs);
        }

        /// <inheritdoc />
        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventHandler<TComp, TEvent> handler)
            where TComp : IComponent
            where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref TEvent args)
                => handler(uid, (TComp)comp, args);

            EntSubscribe<TEvent>(
                CompIdx.Index<TComp>(),
                typeof(TComp),
                typeof(TEvent),
                EventHandler,
                null);
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

            var orderData = new OrderingData(orderType, before ?? Array.Empty<Type>(), after ?? Array.Empty<Type>());

            EntSubscribe<TEvent>(
                CompIdx.Index<TComp>(),
                typeof(TComp),
                typeof(TEvent),
                EventHandler,
                orderData);

            RegisterCommon(typeof(TEvent), orderData, out _);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler)
            where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref TEvent args)
                => handler(uid, (TComp)comp, ref args);

            EntSubscribe<TEvent>(
                CompIdx.Index<TComp>(),
                typeof(TComp),
                typeof(TEvent),
                EventHandler,
                null);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler, Type orderType,
            Type[]? before = null,
            Type[]? after = null) where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref TEvent args)
                => handler(uid, (TComp)comp, ref args);

            var orderData = new OrderingData(orderType, before ?? Array.Empty<Type>(), after ?? Array.Empty<Type>());

            EntSubscribe<TEvent>(
                CompIdx.Index<TComp>(),
                typeof(TComp),
                typeof(TEvent),
                EventHandler,
                orderData);

            RegisterCommon(typeof(TEvent), orderData, out _);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(EntityEventRefHandler<TComp, TEvent> handler, Type orderType,
            Type[]? before = null,
            Type[]? after = null) where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref TEvent args)
                => handler(new Entity<TComp>(uid, (TComp) comp), ref args);

            var orderData = new OrderingData(orderType, before ?? Array.Empty<Type>(), after ?? Array.Empty<Type>());

            EntSubscribe<TEvent>(
                CompIdx.Index<TComp>(),
                typeof(TComp),
                typeof(TEvent),
                EventHandler,
                orderData);

            RegisterCommon(typeof(TEvent), orderData, out _);
        }

        /// <inheritdoc />
        public void UnsubscribeLocalEvent<TComp, TEvent>()
            where TComp : IComponent
            where TEvent : notnull
        {
            EntUnsubscribe(CompIdx.Index<TComp>(), typeof(TEvent));
        }

        private void ComFacOnComponentsAdded(ComponentRegistration[] regs)
        {
            if (_subscriptionLock)
                throw new InvalidOperationException("Subscription locked.");

            foreach (var reg in regs)
            {
                CompIdx.RefArray(ref _entSubscriptionsUnfrozen, reg.Idx) ??= new();
            }
        }

        public void OnEntityAdded(EntityUid e)
        {
            EntAddEntity(e);
        }

        public void OnEntityDeleted(EntityUid e)
        {
            EntRemoveEntity(e);
        }

        public void OnComponentAdded(in AddedComponentEventArgs e)
        {
            EntAddComponent(e.BaseArgs.Owner, e.ComponentType.Idx);
        }

        internal void LockSubscriptions()
        {
            _subscriptionLock = true;
            _eventData = _eventDataUnfrozen.ToFrozenDictionary();

            _entSubscriptions = _entSubscriptionsUnfrozen
                .Select(x => x?.ToFrozenDictionary())
                .ToArray();

            _entSubscriptionsNoCompEv = _entSubscriptionsUnfrozen.Select(FreezeWithoutComponentEvent).ToArray();

            CalcOrdering();
        }

        /// <summary>
        /// Freezes a dictionary while committing events with the <see cref="ComponentEventAttribute"/>.
        /// This avoids unnecessarily adding one-off events to the list of subscriptions.
        /// </summary>
        private FrozenDictionary<Type, DirectedRegistration>? FreezeWithoutComponentEvent(
            Dictionary<Type, DirectedRegistration>? input)
        {
            if (input == null)
                return null;

            return input.Where(x => !IsComponentEvent(x.Key))
                .ToFrozenDictionary();
        }

        private bool IsComponentEvent(Type t)
        {
            var isCompEv = _eventData[t].ComponentEvent;
            DebugTools.Assert(isCompEv == t.HasCustomAttribute<ComponentEventAttribute>());
            return isCompEv;
        }

        public void OnComponentRemoved(in RemovedComponentEventArgs e)
        {
            EntRemoveComponent(e.BaseArgs.Owner, CompIdx.Index(e.BaseArgs.Component.GetType()));
        }

        private void EntAddSubscription(
            CompIdx compType,
            Type compTypeObj,
            Type eventType,
            DirectedRegistration registration)
        {
            if (_subscriptionLock)
                throw new InvalidOperationException("Subscription locked.");

            if (compType.Value >= _entSubscriptionsUnfrozen.Length
                || _entSubscriptionsUnfrozen[compType.Value] is not { } compSubs)
            {
                if (IgnoreUnregisteredComponents)
                    return;

                throw new InvalidOperationException($"Component is not a valid reference type: {compTypeObj.Name}");
            }

            if (compSubs.ContainsKey(eventType))
            {
                throw new InvalidOperationException(
                    $"Duplicate Subscriptions for comp={compTypeObj}, event={eventType.Name}");
            }

            compSubs.Add(eventType, registration);
            _entSubscriptionsInv.GetOrNew(eventType).Add(compType);

            RegisterCommon(eventType, registration.Ordering, out var data);
            data.ComponentEvent = eventType.HasCustomAttribute<ComponentEventAttribute>();
        }

        private void EntSubscribe<TEvent>(
            CompIdx compType,
            Type compTypeObj,
            Type eventType,
            DirectedEventHandler<TEvent> handler,
            OrderingData? order)
            where TEvent : notnull
        {
            EntAddSubscription(compType, compTypeObj, eventType, new DirectedRegistration(handler, order,
                (EntityUid uid, IComponent comp, ref Unit ev) =>
                {
                    ref var tev = ref Unsafe.As<Unit, TEvent>(ref ev);
                    handler(uid, comp, ref tev);
                }));
        }

        private void EntUnsubscribe(CompIdx compType, Type eventType)
        {
            if (_subscriptionLock)
                throw new InvalidOperationException("Subscription locked.");

            if (compType.Value >= _entSubscriptionsUnfrozen.Length
                || _entSubscriptionsUnfrozen[compType.Value] is not { } compSubs)
            {
                if (IgnoreUnregisteredComponents)
                    return;

                throw new InvalidOperationException("Trying to unsubscribe from unregistered component!");
            }

            var removed = compSubs.Remove(eventType);
            if (removed)
                _entSubscriptionsInv[eventType].Remove(compType);
        }

        private void EntAddEntity(EntityUid euid)
        {
            // odds are at least 1 component will subscribe to an event on the entity, so just
            // preallocate the table now. Dispatch does not need to check this later.
            _entEventTables.Add(euid, new EventTable());
        }

        private void EntRemoveEntity(EntityUid euid)
        {
            _entEventTables.Remove(euid);
        }

        private void EntAddComponent(EntityUid euid, CompIdx compType)
        {
            DebugTools.Assert(_subscriptionLock);

            var eventTable = _entEventTables[euid];
            var compSubs = _entSubscriptionsNoCompEv[compType.Value]!;

            foreach (var evType in compSubs.Keys)
            {
                DebugTools.Assert(!_eventData[evType].ComponentEvent);

                if (eventTable.Free < 0)
                    GrowEventTable(eventTable);

                DebugTools.Assert(eventTable.Free >= 0);

                ref var eventStartIdx = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    eventTable.EventIndices,
                    evType,
                    out var exists);

                // Allocate linked list entry by popping free list.
                var entryIdx = eventTable.Free;
                ref var entry = ref eventTable.ComponentLists[entryIdx];
                eventTable.Free = entry.Next;

                // Set it up
                entry.Component = compType;
                entry.Next = exists ? eventStartIdx : -1;

                // Assign new list entry to EventIndices dictionary.
                eventStartIdx = entryIdx;
            }
        }

        private static void GrowEventTable(EventTable table)
        {
            var newSize = table.ComponentLists.Length * 2;

            var oldArray = table.ComponentLists;
            var newArray = GC.AllocateUninitializedArray<EventTableListEntry>(newSize);
            Array.Copy(oldArray, newArray, oldArray.Length);

            InitEventTableFreeList(newArray, newArray.Length, oldArray.Length);

            table.Free = oldArray.Length;
            table.ComponentLists = newArray;
        }

        private static void InitEventTableFreeList(Span<EventTableListEntry> entries, int end, int start)
        {
            var lastFree = -1;
            for (var i = end - 1; i >= start; i--)
            {
                ref var entry = ref entries[i];
                entry.Component = default;
                entry.Next = lastFree;
                lastFree = i;
            }
        }

        private void EntRemoveComponent(EntityUid euid, CompIdx compType)
        {
            var eventTable = _entEventTables[euid];
            var compSubs = _entSubscriptions[compType.Value]!;

            foreach (var evType in compSubs.Keys)
            {
                ref var dictIdx = ref CollectionsMarshal.GetValueRefOrNullRef(eventTable.EventIndices, evType);
                if (Unsafe.IsNullRef(ref dictIdx))
                    continue;

                ref var updateNext = ref dictIdx;

                // Go over linked list to find index of component.
                var entryIdx = dictIdx;
                ref var entry = ref Unsafe.NullRef<EventTableListEntry>();
                while (true)
                {
                    entry = ref eventTable.ComponentLists[entryIdx];
                    if (entry.Component == compType)
                    {
                        // Found
                        break;
                    }

                    entryIdx = entry.Next;
                    updateNext = ref entry.Next;
                }

                if (entry.Next == -1 && Unsafe.AreSame(ref dictIdx, ref updateNext))
                {
                    // Last entry for this event type, remove from dict.
                    eventTable.EventIndices.Remove(evType);
                }
                else
                {
                    // Rewrite previous index to point to next in chain.
                    updateNext = entry.Next;
                }

                // Push entry back onto free list.
                entry.Next = eventTable.Free;
                eventTable.Free = entryIdx;
            }
        }

        private void EntDispatch(EntityUid euid, Type eventType, ref Unit args)
        {
            if (!EntTryGetSubscriptions(eventType, euid, out var enumerator))
                return;

            while (enumerator.MoveNext(out var component, out var reg))
            {
                if (component.Deleted)
                    continue;

                reg.Handler(euid, component, ref args);
            }
        }

        private void EntCollectOrdered(
            EntityUid euid,
            Type eventType,
            ref ValueList<OrderedEventDispatch> found)
        {
            if (!EntTryGetSubscriptions(eventType, euid, out var enumerator))
                return;

            while (enumerator.MoveNext(out var component, out var reg))
            {
                found.Add(new OrderedEventDispatch((ref Unit ev) =>
                {
                    if (!component.Deleted)
                        reg.Handler(euid, component, ref ev);
                }, reg.Order));
            }
        }

        private void DispatchComponent<TEvent>(
            EntityUid euid,
            IComponent component,
            CompIdx baseType,
            ref Unit args)
            where TEvent : notnull
        {
            var compSubs = _entSubscriptions[baseType.Value]!;

            if (compSubs.TryGetValue(typeof(TEvent), out var reg))
                reg.Handler(euid, component, ref args);
        }

        /// <summary>
        ///     Enumerates all subscriptions for an event on a specific entity, returning the component instances and registrations.
        /// </summary>
        private bool EntTryGetSubscriptions(Type eventType, EntityUid euid, out SubscriptionsEnumerator enumerator)
        {
            if (!_entEventTables.TryGetValue(euid, out var eventTable))
            {
                enumerator = default!;
                return false;
            }

            // No subscriptions to this event type, return null.
            if (!eventTable.EventIndices.TryGetValue(eventType, out var startEntry))
            {
                enumerator = default;
                return false;
            }

            enumerator = new(eventType, startEntry, eventTable.ComponentLists, _entSubscriptions, euid, _entMan);
            return true;
        }

        public void ClearSubscriptions()
        {
            _subscriptionLock = false;
            _eventDataUnfrozen.Clear();
            _entEventTables.Clear();
            _inverseEventSubscriptions.Clear();
            _entSubscriptions = default!;
            _entSubscriptionsNoCompEv = default!;
            _eventData = FrozenDictionary<Type, EventData>.Empty;
            foreach (var sub in _entSubscriptionsUnfrozen)
            {
                sub?.Clear();
            }
        }

        public void Dispose()
        {
            _comFac.ComponentsAdded -= ComFacOnComponentsAdded;

            // punishment for use-after-free
            _entMan = null!;
            _comFac = null!;
            _entEventTables = null!;
            _entSubscriptions = null!;
            _entSubscriptionsNoCompEv = null!;
            _entSubscriptionsUnfrozen = null!;
            _entSubscriptionsInv = null!;
        }

        private struct SubscriptionsEnumerator
        {
            private readonly Type _eventType;
            private readonly EntityUid _uid;
            private readonly FrozenDictionary<Type, DirectedRegistration>?[] _subscriptions;
            private readonly IEntityManager _entityManager;
            private readonly EventTableListEntry[] _list;
            private int _idx;

            public SubscriptionsEnumerator(
                Type eventType,
                int startEntry,
                EventTableListEntry[] list,
                FrozenDictionary<Type, DirectedRegistration>?[] subscriptions,
                EntityUid uid,
                IEntityManager entityManager)
            {
                _eventType = eventType;
                _list = list;
                _subscriptions = subscriptions;
                _idx = startEntry;
                _entityManager = entityManager;
                _uid = uid;
            }

            public bool MoveNext(
                [NotNullWhen(true)] out IComponent? component,
                [NotNullWhen(true)] out DirectedRegistration? registration)
            {
                if (_idx == -1)
                {
                    component = null;
                    registration = null;
                    return false;
                }

                ref var entry = ref _list[_idx];
                _idx = entry.Next;

                var compType = entry.Component;
                var compSubs = _subscriptions[compType.Value]!;

                if (!compSubs.TryGetValue(_eventType, out registration))
                {
                    component = default;
                    return false;
                }

                component = _entityManager.GetComponentInternal(_uid, compType);
                return true;
            }
        }

        internal sealed class DirectedRegistration : OrderedRegistration
        {
            public readonly Delegate Original;
            public readonly DirectedEventHandler Handler;

            public DirectedRegistration(
                Delegate original,
                OrderingData? ordering,
                DirectedEventHandler handler) : base(ordering)
            {
                Original = original;
                Handler = handler;
            }

            public void SetOrder(int order)
            {
                Order = order;
            }
        }

        internal sealed class EventTable
        {
            private const int InitialListSize = 8;

            // Event -> { Comp, Comp, ... } is stored in a simple linked list.
            // EventIndices contains indices into ComponentLists where linked list nodes start.
            // Free contains the first free linked list node, or -1 if there is none.
            // Free nodes form their own linked list.
            // ComponentList is the actual region of memory containing linked list nodes.
            public readonly Dictionary<Type, int> EventIndices = new();
            public int Free;
            public EventTableListEntry[] ComponentLists = new EventTableListEntry[InitialListSize];

            public EventTable()
            {
                InitEventTableFreeList(ComponentLists, ComponentLists.Length, 0);
                Free = 0;
            }
        }

        internal struct EventTableListEntry
        {
            public int Next;
            public CompIdx Component;
        }
    }

    /// <seealso cref="ComponentEventRefHandler{TComp, TEvent}"/>
    // [Obsolete("Use ComponentEventRefHandler instead")]
    public delegate void ComponentEventHandler<in TComp, in TEvent>(EntityUid uid, TComp component, TEvent args)
        where TComp : IComponent
        where TEvent : notnull;

    public delegate void ComponentEventRefHandler<in TComp, TEvent>(EntityUid uid, TComp component, ref TEvent args)
        where TComp : IComponent
        where TEvent : notnull;

    public delegate void EntityEventRefHandler<TComp, TEvent>(Entity<TComp> ent, ref TEvent args)
        where TComp : IComponent
        where TEvent : notnull;
}
