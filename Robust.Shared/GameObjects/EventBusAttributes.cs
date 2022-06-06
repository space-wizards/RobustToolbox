using System;

namespace Robust.Shared.GameObjects;

[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
public sealed class ByRefEventAttribute : Attribute
{
}

/// <summary>
/// Indicates that an eventbus event should only ever be raised through <see cref="IDirectedEventBus.RaiseComponentEvent{TEvent}(IComponent, TEvent)"/>.
/// This allows extra optimizations.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Struct)]
internal sealed class ComponentEventAttribute : Attribute
{
}
