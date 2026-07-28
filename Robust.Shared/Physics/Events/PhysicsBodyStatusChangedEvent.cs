using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics.Events;

// This is called PhysicsBody and not just Body to differentiate from a potential literal living body
/// <summary>
///     Raised on an entity when it's <see cref="PhysicsComponent"/>'s <see cref="BodyStatus"/> is <i>being</i> changed.
///         Raised after <see cref="PhysicsComponent.BodyStatus"/> is set to the new status.
/// </summary>
/// <param name="PhysicsComponent"><see cref="PhysicsComponent"/> of the entity which this event was directed at.</param>
[ByRefEvent]
public readonly record struct PhysicsBodyStatusChangedEvent(PhysicsComponent PhysicsComponent, BodyStatus OldStatus, BodyStatus NewStatus);
