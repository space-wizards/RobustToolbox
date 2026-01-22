using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Collections;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects
{
    [NotContentImplementable]
    public interface IEventBus : IDirectedEventBus, IBroadcastEventBus
    {
    }

    [NotContentImplementable]
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
        /// is because of the component network source generator.<br/>
        /// This may be removed, modified, or pulled back internal at ANY TIME.
        /// </remarks>
        public void RaiseComponentEvent<TEvent, TComponent>(EntityUid uid, TComponent component, TEvent args)
            where TEvent : notnull
            where TComponent : IComponent;

        /// <inheritdoc cref="RaiseComponentEvent{TEvent,TComponent}(Robust.Shared.GameObjects.EntityUid,TComponent,TEvent)"/>
        public void RaiseComponentEvent<TEvent>(EntityUid uid, IComponent component, TEvent args)
            where TEvent : notnull;

        /// <inheritdoc cref="RaiseComponentEvent{TEvent,TComponent}(Robust.Shared.GameObjects.EntityUid,TComponent,TEvent)"/>
        public void RaiseComponentEvent<TEvent>(EntityUid uid, IComponent component, CompIdx idx, TEvent args)
            where TEvent : notnull;

        /// <inheritdoc cref="RaiseComponentEvent{TEvent,TComponent}(Robust.Shared.GameObjects.EntityUid,TComponent,TEvent)"/>
        public void RaiseComponentEvent<TEvent>(EntityUid uid, IComponent component, ref TEvent args)
            where TEvent : notnull;

        /// <inheritdoc cref="RaiseComponentEvent{TEvent,TComponent}(Robust.Shared.GameObjects.EntityUid,TComponent,TEvent)"/>
        public void RaiseComponentEvent<TEvent, TComponent>(EntityUid uid, TComponent component, ref TEvent args)
            where TEvent : notnull
            where TComponent : IComponent;

        /// <inheritdoc cref="RaiseComponentEvent{TEvent,TComponent}(Robust.Shared.GameObjects.EntityUid,TComponent,TEvent)"/>
        public void RaiseComponentEvent<TEvent>(EntityUid uid, IComponent component, CompIdx idx, ref TEvent args)
            where TEvent : notnull;

        public void OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare();
    }

    internal partial class EntityEventBus : IDisposable
    {
        internal delegate void DirectedEventHandler(EntityUid uid, IComponent comp, ref Unit args);

        /// <summary>
        /// Max size of a components event subscription linked list.
        /// Used to limit the stackalloc in <see cref="EntDispatch"/>
        /// </summary>
        /// <remarks>
        /// SS14 currently requires only 18, I doubt it will ever need to exceed 256.
        /// </remarks>
        private const int MaxEventLinkedListSize = 256;

        /// <summary>
        /// Constructs a new instance of <see cref="EntityEventBus"/>.
        /// </summary>
        /// <param name="entMan">The entity manager to watch for entity/component events.</param>
        /// <param name="reflection">The reflection manager to use when finding derived types.</param>
        public EntityEventBus(EntityManager entMan, IReflectionManager reflection)
        {
            _entMan = entMan;
            _comFac = entMan.ComponentFactory;
            _reflection = reflection;

            // Dynamic handling of components is only for RobustUnitTest compatibility spaghetti.
            _comFac.ComponentsAdded += ComFacOnComponentsAdded;
            ComFacOnComponentsAdded(_comFac.GetAllRegistrations().ToArray());
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RaiseComponentEvent<TEvent>(EntityUid uid, IComponent component, TEvent args)
            where TEvent : notnull
        {
            RaiseComponentEvent(uid, component, _comFac.GetIndex(component.GetType()), ref args);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RaiseComponentEvent<TEvent, TComponent>(EntityUid uid, TComponent component, TEvent args)
            where TEvent : notnull
            where TComponent : IComponent
        {
            RaiseComponentEvent(uid, component, CompIdx.Index<TComponent>(), ref args);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RaiseComponentEvent<TEvent>(EntityUid uid, IComponent component, CompIdx type, TEvent args)
            where TEvent : notnull
        {
            RaiseComponentEvent(uid, component, type, ref args);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RaiseComponentEvent<TEvent>(EntityUid uid, IComponent component, ref TEvent args)
            where TEvent : notnull
        {
            RaiseComponentEvent(uid, component, _comFac.GetIndex(component.GetType()), ref args);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RaiseComponentEvent<TEvent, TComponent>(EntityUid uid, TComponent component, ref TEvent args)
            where TEvent : notnull
            where TComponent : IComponent
        {
            RaiseComponentEvent(uid, component, CompIdx.Index<TComponent>(), ref args);
        }

        /// <inheritdoc />
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void RaiseComponentEvent<TEvent>(EntityUid uid, IComponent component, CompIdx type, ref TEvent args)
            where TEvent : notnull
        {
            if (_compEventSubs[type.Value].TryGetValue(typeof(TEvent), out var handler))
                handler(uid, component, ref Unsafe.As<TEvent, Unit>(ref args));
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
            void EventHandler(EntityUid uid, IComponent comp, ref Unit ev)
            {
                ref var tev = ref Unsafe.As<Unit, TEvent>(ref ev);
                handler(uid, (TComp) comp, tev);
            }

            EntAddSubscription(CompIdx.Index<TComp>(), typeof(TComp), typeof(TEvent), EventHandler);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(
            ComponentEventHandler<TComp, TEvent> handler,
            Type orderType,
            Type[]? before = null,
            Type[]? after = null)
            where TComp : IComponent
            where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref Unit ev)
            {
                ref var tev = ref Unsafe.As<Unit, TEvent>(ref ev);
                handler(uid, (TComp) comp, tev);
            }

            EntAddSubscription(CompIdx.Index<TComp>(), typeof(TComp), typeof(TEvent), EventHandler, orderType, before, after);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler)
            where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref Unit ev)
            {
                ref var tev = ref Unsafe.As<Unit, TEvent>(ref ev);
                handler(uid, (TComp) comp, ref tev);
            }

            EntAddSubscription(CompIdx.Index<TComp>(), typeof(TComp), typeof(TEvent), EventHandler);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler, Type orderType,
            Type[]? before = null,
            Type[]? after = null) where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref Unit ev)
            {
                ref var tev = ref Unsafe.As<Unit, TEvent>(ref ev);
                handler(uid, (TComp) comp, ref tev);
            }

            EntAddSubscription(CompIdx.Index<TComp>(), typeof(TComp), typeof(TEvent), EventHandler, orderType, before, after);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(EntityEventRefHandler<TComp, TEvent> handler, Type orderType,
            Type[]? before = null,
            Type[]? after = null) where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref Unit ev)
            {
                ref var tev = ref Unsafe.As<Unit, TEvent>(ref ev);
                handler(new Entity<TComp>(uid, (TComp) comp), ref tev);
            }

            EntAddSubscription(CompIdx.Index<TComp>(), typeof(TComp), typeof(TEvent), EventHandler, orderType, before, after);
        }

        /// <inheritdoc />
        public void UnsubscribeLocalEvent<TComp, TEvent>()
            where TComp : IComponent
            where TEvent : notnull
        {
            if (!_comFac.TryGetRegistration(typeof(TComp), out _))
            {
                if (!IgnoreUnregisteredComponents)
                    throw new InvalidOperationException($"Component is not a valid reference type: {typeof(TComp).Name}");

                return;
            }

            if (_subscriptionLock)
                throw new InvalidOperationException("Subscription locked.");

            var i = CompIdx.ArrayIndex<TComp>();

            _eventSubsUnfrozen[i]!.Remove(typeof(TEvent));
            _compEventSubsUnfrozen[i]!.Remove(typeof(TEvent));

            if (_eventSubsInv.TryGetValue(typeof(TEvent), out var t))
                t.Remove(CompIdx.Index<TComp>());
        }

        private void ComFacOnComponentsAdded(ComponentRegistration[] regs)
        {
            if (_subscriptionLock)
                throw new InvalidOperationException("Subscription locked.");

            foreach (var reg in regs)
            {
                CompIdx.RefArray(ref _eventSubsUnfrozen, reg.Idx) ??= new();
                CompIdx.RefArray(ref _compEventSubsUnfrozen, reg.Idx) ??= new();
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

            _eventSubs = TrimNull(_eventSubsUnfrozen)
                .Select(dict => dict?.ToFrozenDictionary()!)
                .ToArray();

            _compEventSubs = TrimNull(_compEventSubsUnfrozen)
                .Select(dict => dict?.ToFrozenDictionary()!)
                .ToArray();

            CalcOrdering();
        }

        public void OnComponentRemoved(in RemovedComponentEventArgs e)
        {
            EntRemoveComponent(e.BaseArgs.Owner, e.Idx);
        }

        private void EntAddSubscription(
            CompIdx compType,
            Type compTypeObj,
            Type eventType,
            DirectedEventHandler handler,
            Type? orderType = null,
            Type[]? before = null,
            Type[]? after = null)
        {
            if (_subscriptionLock)
                throw new InvalidOperationException("Subscription locked.");

            if (!_comFac.TryGetRegistration(compTypeObj, out _))
            {
                if (IgnoreUnregisteredComponents)
                    return;

                throw new InvalidOperationException($"Component is not a valid reference type: {compTypeObj.Name}");
            }

            if (eventType.GetCustomAttribute<ComponentEventAttribute>() is { } attr)
            {
                if (!_compEventSubsUnfrozen[compType.Value]!.TryAdd(eventType, handler))
                    throw new InvalidOperationException($"Duplicate Subscriptions for comp={compTypeObj}, event={eventType.Name}");

                // An exclusive component-event is only raised via RaiseComponentEvent, hence it don't need a normal
                // directed event subscription
                if (attr.Exclusive)
                    return;
            }

            var orderData = orderType == null ? null : CreateOrderingData(orderType, before, after);
            var reg = new DirectedRegistration(orderData, handler);

            if (!_eventSubsUnfrozen[compType.Value]!.TryAdd(eventType, reg))
                throw new InvalidOperationException($"Duplicate Subscriptions for comp={compTypeObj}, event={eventType.Name}");

            RegisterCommon(eventType, reg.Ordering, out _);
            _eventSubsInv.GetOrNew(eventType).Add(compType);
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
            var compSubs = _eventSubs[compType.Value];

            foreach (var evType in compSubs.Keys)
            {
                if (eventTable.Free < 0)
                    GrowEventTable(eventTable);

                DebugTools.Assert(eventTable.Free >= 0);

                ref var indices = ref CollectionsMarshal.GetValueRefOrAddDefault(
                    eventTable.EventIndices,
                    evType,
                    out var exists);

                // Allocate linked list entry by popping free list.
                var entryIdx = eventTable.Free;
                ref var entry = ref eventTable.ComponentLists[entryIdx];
                eventTable.Free = entry.Next;

                // Set it up
                entry.Component = compType;
                entry.Next = exists ? indices.Start : -1;

                // Assign new list entry to EventIndices dictionary.
                indices.Start = entryIdx;
                indices.Count++;
                if (indices.Count > MaxEventLinkedListSize)
                    throw new NotSupportedException($"Exceeded maximum event linked list size. Need to implement stackalloc fallback.");
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
            var compSubs = _eventSubs[compType.Value];

            foreach (var evType in compSubs.Keys)
            {
                ref var indices = ref CollectionsMarshal.GetValueRefOrNullRef(eventTable.EventIndices, evType);
                if (Unsafe.IsNullRef(ref indices))
                {
                    DebugTools.Assert("This should not be possible. Were the events for this component never added?");
                    continue;
                }

                var entryIdx = indices.Start;
                ref var entry = ref eventTable.ComponentLists[entryIdx];

                if (indices.Count == 1)
                {
                    // Last entry for this event type, remove from dict.
                    DebugTools.AssertEqual(entry.Next, -1);
                    eventTable.EventIndices.Remove(evType);
                }
                else
                {
                    ref var updateNext = ref indices.Start;

                    // Go over linked list to find index of component.
                    while (entry.Component != compType)
                    {
                        updateNext = ref entry.Next;
                        entryIdx = entry.Next;
                        entry = ref eventTable.ComponentLists[entryIdx];
                    }

                    // Rewrite previous index to point to next in chain.
                    updateNext = entry.Next;
                    indices.Count--;
                }

                // Push entry back onto free list.
                entry.Next = eventTable.Free;
                eventTable.Free = entryIdx;
            }
        }

        private void EntDispatch(EntityUid euid, Type eventType, ref Unit args)
        {
            if (!_entEventTables.TryGetValue(euid, out var eventTable))
                return;

            if (!eventTable.EventIndices.TryGetValue(eventType, out var indices))
                return;

            DebugTools.Assert(indices.Count > 0);
            DebugTools.Assert(indices.Start >= 0);

            // First, collect all subscribing components.
            // This is to avoid infinite loops over the linked list if subscription handlers add or remove components.
            Span<CompIdx> compIds = stackalloc CompIdx[indices.Count];
            var idx = indices.Start;
            for (var index = 0; index < compIds.Length; index++)
            {
                DebugTools.Assert(idx >= 0);
                ref var entry = ref eventTable.ComponentLists[idx];
                idx = entry.Next;
                compIds[index] = entry.Component;
            }

            foreach (var compIdx in compIds)
            {
                if (!_entMan.TryGetComponent(euid, compIdx, out var comp))
                    continue;
                var compSubs = _eventSubs[compIdx.Value];
                compSubs[eventType].Handler(euid, comp, ref args);
            }
        }

        private void EntCollectOrdered(
            EntityUid euid,
            Type eventType,
            ref ValueList<OrderedEventDispatch> found)
        {
            if (!_entEventTables.TryGetValue(euid, out var eventTable))
                return;

            if (!eventTable.EventIndices.TryGetValue(eventType, out var indices))
                return;

            DebugTools.Assert(indices.Count > 0);
            DebugTools.Assert(indices.Start >= 0);
            var idx = indices.Start;
            while (idx != -1)
            {
                ref var entry = ref eventTable.ComponentLists[idx];
                idx = entry.Next;
                var comp = _entMan.GetComponentInternal(euid, entry.Component);
                var compSubs = _eventSubs[entry.Component.Value];
                var reg = compSubs[eventType];

                found.Add(new OrderedEventDispatch(
                    (ref Unit ev) =>
                    {
                        if (!comp.Deleted)
                            reg.Handler(euid, comp, ref ev);
                    },
                    reg.Order));
            }
        }

        public void ClearSubscriptions()
        {
            _subscriptionLock = false;
            _eventDataUnfrozen.Clear();
            _entEventTables.Clear();
            _inverseEventSubscriptions.Clear();
            _compEventSubs = default!;
            _eventSubs = default!;
            _eventData = FrozenDictionary<Type, EventData>.Empty;
            foreach (var sub in _eventSubsUnfrozen)
            {
                sub?.Clear();
            }
            foreach (var sub in _compEventSubsUnfrozen)
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
            _reflection = null!;
            _entEventTables = null!;
            _compEventSubs = null!;
            _eventSubs = null!;
            _eventSubsUnfrozen = null!;
            _compEventSubsUnfrozen = null!;
            _eventSubsInv = null!;
        }

        internal sealed class DirectedRegistration(OrderingData? ordering, DirectedEventHandler handler)
            : OrderedRegistration(ordering)
        {
            public readonly DirectedEventHandler Handler = handler;

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
            public readonly Dictionary<Type, (int Start, int Count)> EventIndices = new();
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

        /// <summary>
        /// Return a new array with any trailing null entries removed.
        /// </summary>
        public static T[] TrimNull<T>(T[] input)
        {
            // Find last non-null entry.
            var last = 0;
            for (var i = 0; i < input.Length; i++)
            {
                var entry = input[i];
                if (entry != null)
                    last = i;
            }

            return input[..(last + 1)];
        }

        /// <summary>
        /// Get an array of event handlers for a given component event, indexed by the component's net-id.
        /// </summary>
        /// <remarks>
        /// For most events, this will generally be a pretty sparse array, with most entries being null.  However, for
        /// the get and handle state events, this array will be relatively dense and helps save PVS a lot of save a
        /// FrozenDictionary lookups.
        /// </remarks>
        internal DirectedEventHandler?[] GetNetCompEventHandlers<TEvent>()
        {
            DebugTools.Assert(_subscriptionLock);
            DebugTools.Assert(typeof(TEvent).HasCustomAttribute<ComponentEventAttribute>());

            var netComps = _comFac.NetworkedComponents!;
            var result = new DirectedEventHandler?[netComps.Count];

            for (var i = 0; i < netComps.Count; i++)
            {
                var reg = netComps[i];
                result[i] = _compEventSubs[reg.Idx.Value].GetValueOrDefault(typeof(TEvent));
            }

            return result;
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
