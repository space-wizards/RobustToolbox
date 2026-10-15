using System;

namespace Robust.Shared.GameObjects;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ByRefEventAttribute : Attribute;

/// <summary>
/// This attribute enables an event to be raised as a "component event" via <see cref="IDirectedEventBus.RaiseComponentEvent{TEvent}(EntityUid, IComponent, TEvent)"/>.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class ComponentEventAttribute : Attribute
{
    /// <summary>
    /// If true, this event may **only** be raised via as a "component event", as any subscriptions will not be
    /// included in the normal event tables.
    /// </summary>
    public bool Exclusive = true;
}
