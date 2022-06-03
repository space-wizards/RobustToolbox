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
    private readonly Dictionary<Type, EventSubscriptions> _eventSubscriptions = new();

    private readonly Dictionary<IEntityEventSubscriber, Dictionary<Type, Registration>> _inverseEventSubscriptions
        = new();

    private readonly Queue<(EventSource source, object args)> _eventQueue = new();

    private IEntityManager _entMan;
    private IComponentFactory _comFac;

    // eUid -> EventType -> { CompType1, ... CompTypeN }
    private Dictionary<EntityUid, Dictionary<Type, HashSet<CompIdx>>> _entEventTables = new();

    // CompType -> EventType -> Handler
    private Dictionary<Type, DirectedRegistration>?[] _entSubscriptions =
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

    private void RegisterCommon(Type eventType, OrderingData? data, out EventSubscriptions subs)
    {
        subs = _eventSubscriptions.GetOrNew(eventType);

        if (data == null)
            return;

        if (data.Before != null || data.After != null)
        {
            subs.IsOrdered = true;
            subs.OrderingUpToDate = false;
        }
    }

    private sealed class Registration : OrderedRegistration, IEquatable<Registration>
    {
        public readonly object EqualityToken;
        public readonly RefEventHandler Handler;
        public readonly EventSource Mask;
        public readonly bool ReferenceEvent;

        public Registration(
            EventSource mask,
            RefEventHandler handler,
            object equalityToken,
            OrderingData? ordering,
            bool referenceEvent) : base(ordering)
        {
            Mask = mask;
            Handler = handler;
            EqualityToken = equalityToken;
            ReferenceEvent = referenceEvent;
        }

        public bool Equals(Registration? other)
        {
            return other != null && Mask == other.Mask && Equals(EqualityToken, other.EqualityToken);
        }

        public override bool Equals(object? obj)
        {
            return obj is Registration other && Equals(other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return ((int)Mask * 397) ^ EqualityToken.GetHashCode();
            }
        }
    }

    private sealed class EventSubscriptions
    {
        public bool IsOrdered;
        public bool OrderingUpToDate;
        public ValueList<Registration> Registrations;
    }

    // This is not a real type. Whenever you see a "ref Unit" it means it's a ref to *some* kind of other type.

    // It should always be cast to/from with Unsafe.As<,>

    private readonly struct Unit
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    private sealed class UnitBox
    {
        [UsedImplicitly] public Unit Value;
    }
}
