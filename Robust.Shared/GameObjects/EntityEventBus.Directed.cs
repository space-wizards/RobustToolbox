using System;
using Arch.LowLevel.Jagged;
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

            if (subs.IsOrdered && !subs.OrderingUpToDate)
            {
                UpdateOrderSeq(type, subs);
                _archetypeEventTables = new(EventBusDefaultCapacity, EventBusDefaultCapacity);
            }

            if (broadcast && subs.BroadcastRegistrations.Count > 0)
                EntDispatchMerged(uid, type, subs.BroadcastRegistrations.Span, ref unitRef);
            else
                EntDispatch(uid, type, ref unitRef);
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

            EntAddSubscription(CompIdx.Index<TComp>(), typeof(TComp), typeof(TEvent), handler.Method.DeclaringType, EventHandler);
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

            EntAddSubscription(CompIdx.Index<TComp>(), typeof(TComp), typeof(TEvent), handler.Method.DeclaringType, EventHandler, orderType, before, after);
        }

        public void SubscribeLocalEvent<TComp, TEvent>(ComponentEventRefHandler<TComp, TEvent> handler)
            where TComp : IComponent where TEvent : notnull
        {
            void EventHandler(EntityUid uid, IComponent comp, ref Unit ev)
            {
                ref var tev = ref Unsafe.As<Unit, TEvent>(ref ev);
                handler(uid, (TComp) comp, ref tev);
            }

            EntAddSubscription(CompIdx.Index<TComp>(), typeof(TComp), typeof(TEvent), handler.Method.DeclaringType, EventHandler);
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

            EntAddSubscription(CompIdx.Index<TComp>(), typeof(TComp), typeof(TEvent), handler.Method.DeclaringType, EventHandler, orderType, before, after);
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

            EntAddSubscription(CompIdx.Index<TComp>(), typeof(TComp), typeof(TEvent), handler.Method.DeclaringType, EventHandler, orderType, before, after);
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
                CompIdx.RefArray(ref _compEventSubsDeclaringTypes, reg.Idx) ??= new();
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
            EntAddComponent(e.BaseArgs.Owner, e.ComponentType.Idx, e.BaseArgs.Component);
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

            // Subscriptions changed, so any tables built before this are stale.
            _archetypeEventTables = new(EventBusDefaultCapacity, EventBusDefaultCapacity);
        }

        public void OnComponentRemoved(in RemovedComponentEventArgs e)
        {
            EntRemoveComponent(e.BaseArgs.Owner, e.Idx);
        }

        private void EntAddSubscription(
            CompIdx compType,
            Type compTypeObj,
            Type eventType,
            Type? declaringType,
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
                {
                    var original = _compEventSubsDeclaringTypes[compType.Value]![(eventType, true)]?.Name;
                    throw new InvalidOperationException($"Duplicate Subscriptions for comp={compTypeObj}, event={eventType.Name}, original={original}, new={declaringType?.Name}");
                }

                _compEventSubsDeclaringTypes[compType.Value]![(eventType, true)] = declaringType;

                // An exclusive component-event is only raised via RaiseComponentEvent, hence it don't need a normal
                // directed event subscription
                if (attr.Exclusive)
                    return;
            }

            var orderData = orderType == null ? null : CreateOrderingData(orderType, before, after);
            var reg = new DirectedRegistration(orderData, handler);

            if (!_eventSubsUnfrozen[compType.Value]!.TryAdd(eventType, reg))
            {
                var original = _compEventSubsDeclaringTypes[compType.Value]![(eventType, false)]?.Name;
                throw new InvalidOperationException($"Duplicate Subscriptions for comp={compTypeObj}, event={eventType.Name}, original={original}, new={declaringType?.Name}");
            }

            _compEventSubsDeclaringTypes[compType.Value]![(eventType, false)] = declaringType;

            RegisterCommon(eventType, reg.Ordering, out _);
            _eventSubsInv.GetOrNew(eventType).Add(compType);
        }

        private void EntAddEntity(EntityUid euid)
        {
            _entMan.ArchetypeAddEntity(euid);
        }

        private void EntRemoveEntity(EntityUid euid)
        {
            _entMan.ArchetypeRemoveEntity(euid);
        }

        private void EntAddComponent(EntityUid euid, CompIdx compType, IComponent component)
        {
            DebugTools.Assert(_subscriptionLock);
            _entMan.ArchetypeAddComponent(euid, compType);
        }

        private void EntRemoveComponent(EntityUid euid, CompIdx compType)
        {
            _entMan.ArchetypeRemoveComponent(euid, compType);
        }

        /// <param name="DenseIndex">Dense storage index of the component, or -1.</param>
        internal readonly record struct ArchetypeEventEntry(CompIdx Component, int DenseIndex, DirectedRegistration Registration);

        /// <summary>
        /// Gets the event table for an archetype, building it on first use.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal FrozenDictionary<Type, ArchetypeEventEntry[]> GetEventTable(int archetype)
        {
            var tables = _archetypeEventTables;
            return (archetype < tables.Capacity ? tables[archetype] : null) ?? BuildEventTable(archetype);
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private FrozenDictionary<Type, ArchetypeEventEntry[]> BuildEventTable(int archetype)
        {
            var events = new Dictionary<Type, List<ArchetypeEventEntry>>();
            foreach (var comp in _entMan.GetArchetype(archetype).Components)
            {
                if (comp.Value >= _eventSubs.Length || _eventSubs[comp.Value] is not { } compSubs)
                    continue;

                foreach (var (evType, reg) in compSubs)
                {
                    events.GetOrNew(evType).Add(new ArchetypeEventEntry(comp, _entMan.GetDenseIndex(comp), reg));
                }
            }

            // Sorted by subscription order (stable, so equal orders keep component order).
            var table = events.ToFrozenDictionary(x => x.Key, x => x.Value.OrderBy(e => e.Registration.Order).ToArray());
            _archetypeEventTables.EnsureCapacity(archetype + 1);
            _archetypeEventTables[archetype] = table;
            return table;
        }

        private void EntDispatch(EntityUid euid, Type eventType, ref Unit args)
        {
            var archetype = _entMan.GetArchetypeIndex(euid);
            if (archetype == EntityManager.NoArchetype || !GetEventTable(archetype).TryGetValue(eventType, out var entries))
                return;

            // The table is shared and never modified, so handlers adding / removing components can't break this loop.
            // Components are fetched per handler: removed ones are skipped, added ones aren't raised on.
            foreach (var entry in entries)
            {
                if (_entMan.TryGetComponentForEvent(euid, entry.Component, entry.DenseIndex, out var comp))
                    entry.Registration.Handler(euid, comp, ref args);
            }
        }

        /// <summary>
        /// Raises directed and broadcast handlers together, both already sorted by order. On equal order directed
        /// handlers run first.
        /// </summary>
        private void EntDispatchMerged(EntityUid euid, Type eventType, ReadOnlySpan<BroadcastRegistration> broadcast, ref Unit args)
        {
            ReadOnlySpan<ArchetypeEventEntry> directed = default;
            var archetype = _entMan.GetArchetypeIndex(euid);
            if (archetype != EntityManager.NoArchetype && GetEventTable(archetype).TryGetValue(eventType, out var entries))
                directed = entries;

            var i = 0;
            var j = 0;
            while (i < directed.Length || j < broadcast.Length)
            {
                if (j >= broadcast.Length || (i < directed.Length && directed[i].Registration.Order <= broadcast[j].Order))
                {
                    var entry = directed[i++];
                    if (_entMan.TryGetComponentForEvent(euid, entry.Component, entry.DenseIndex, out var comp))
                        entry.Registration.Handler(euid, comp, ref args);
                    continue;
                }

                var handler = broadcast[j++];
                if ((handler.Mask & EventSource.Local) != 0)
                    handler.Handler(ref args);
            }
        }

        public void ClearSubscriptions()
        {
            _subscriptionLock = false;
            _eventDataUnfrozen.Clear();
            _archetypeEventTables = new(EventBusDefaultCapacity, EventBusDefaultCapacity);
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
            _compEventSubs = null!;
            _eventSubs = null!;
            _eventSubsUnfrozen = null!;
            _compEventSubsUnfrozen = null!;
            _compEventSubsDeclaringTypes = null!;
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
