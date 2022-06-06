using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

internal sealed partial class EntityEventBus : IEventBus
{
    private IEntityManager _entMan;
    private IComponentFactory _comFac;

    // Data on individual events. Used to check ordering info and fire broadcast events.
    private readonly Dictionary<Type, EventData> _eventData = new();

    // Inverse subscriptions to be able to unsubscribe an IEntityEventSubscriber.
    private readonly Dictionary<IEntityEventSubscriber, Dictionary<Type, BroadcastRegistration>> _inverseEventSubscriptions
        = new();

    // For queued message broadcast.
    private readonly Queue<(EventSource source, object args)> _eventQueue = new();

    // eUid -> EventType -> { CompType1, ... CompTypeN }
    // See EventTable declaration for layout details
    internal Dictionary<EntityUid, EventTable> _entEventTables = new();

    // CompType -> EventType -> Handler
    internal Dictionary<Type, DirectedRegistration>?[] _entSubscriptions =
        Array.Empty<Dictionary<Type, DirectedRegistration>?>();

    // EventType -> { CompType1, ... CompType N }
    private Dictionary<Type, HashSet<CompIdx>> _entSubscriptionsInv = new();

    // prevents shitcode, get your subscriptions figured out before you start spawning entities
    private bool _subscriptionLock;

    public bool IgnoreUnregisteredComponents;

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static void ThrowByRefMisMatch() =>
        throw new InvalidOperationException("Mismatching by-ref ness on event!");

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static ref Unit ExtractUnitRef(ref object obj, Type objType)
    {
        // If it's a boxed value type we have to do some trickery to return the INTERIOR reference,
        // not the reference to the boxed object.
        // Otherwise the unit points to the reference to the reference type.
        return ref objType.IsValueType
            ? ref Unsafe.As<object, UnitBox>(ref obj).Value
            : ref Unsafe.As<object, Unit>(ref obj);
    }

    private void RegisterCommon(Type eventType, OrderingData? data, out EventData subs)
    {
        subs = _eventData.GetOrNew(eventType);

        if (data == null)
            return;

        if (data.Before.Length > 0 || data.After.Length > 0)
        {
            subs.IsOrdered = true;
            subs.OrderingUpToDate = false;
        }
    }

    /// <summary>
    /// Information for a single event type handled by EventBus. Not specific to broadcast registrations.
    /// </summary>
    private sealed class EventData
    {
        /// <summary>
        /// <see cref="ComponentEventAttribute"/> set?
        /// </summary>
        public bool ComponentEvent;
        public bool IsOrdered;
        public bool OrderingUpToDate;
        public ValueList<BroadcastRegistration> BroadcastRegistrations;
    }

    // This is not a real type. Whenever you see a "ref Unit" it means it's a ref to *some* kind of other type.

    // It should always be cast to/from with Unsafe.As<,>

    internal readonly struct Unit
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    private sealed class UnitBox
    {
        [UsedImplicitly] public Unit Value;
    }
}
