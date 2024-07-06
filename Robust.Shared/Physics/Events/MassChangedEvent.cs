using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using System.Numerics;

namespace Robust.Shared.Physics.Events;

/// <summary>
/// By-ref directed event raised when the mass or angular inertia or center of mass of a physics body changes.
/// </summary>
/// <param name="Entity">The physics body that changed.</param>
/// <param name="OldMass">The old mass of the physics body.</param>
/// <param name="OldInertia">The old angular inertia of the physics body.</param>
/// <param name="OldCenter">The old (local) center of mass of the physics body.</param>
[ByRefEvent]
public readonly record struct MassDataChangedEvent(
    Entity<PhysicsComponent, FixturesComponent> Entity,
    float OldMass,
    float OldInertia,
    Vector2 OldCenter
)
{
    public float NewMass => Entity.Comp1._mass;
    public float NewInertia => Entity.Comp1._inertia;
    public Vector2 NewCenter => Entity.Comp1._localCenter;
    public bool MassChanged => NewMass != OldMass;
    public bool InertiaChanged => NewInertia != OldInertia;
    public bool CenterChanged => NewCenter != OldCenter;
}
