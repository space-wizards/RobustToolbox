using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;

namespace Robust.Shared.Physics;

[ByRefEvent]
public readonly record struct PhysicsWakeEvent(EntityUid Entity, PhysicsComponent Body);

[ByRefEvent]
public record struct PhysicsSleepEvent(EntityUid Entity, PhysicsComponent Body)
{
    /// <summary>
    /// Marks the entity as still being awake and cancels sleeping.
    /// This only works if <see cref="PhysicsComponent.CanCollide"/> is still enabled and the object is not a static object..
    /// </summary>
    public bool Cancelled;
};
