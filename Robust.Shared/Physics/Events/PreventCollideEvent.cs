using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics.Events;

/// <summary>
/// An event used to prevent a physics collision between two physics bodies.
/// </summary>
[ByRefEvent]
public struct PreventCollideEvent
{
    /// <summary>
    /// The entity that this event was directed at. Owner of <see cref="OurBody"/>
    /// </summary>
    public readonly EntityUid OurEntity;

    /// <summary>
    /// The other colliding entity. Owner of <see cref="OtherBody"/>
    /// </summary>
    public readonly EntityUid OtherEntity;

    /// <summary>
    /// The body of the entity that this event was directed at.
    /// </summary>
    public readonly PhysicsComponent OurBody;

    /// <summary>
    /// The other body..
    /// </summary>
    public readonly PhysicsComponent OtherBody;
    /// <summary>
    /// The fixture on the first body to prevent the collision of if specified.
    /// </summary>
    public readonly Fixture OurFixture;

    /// <summary>
    /// The fixture on the other body to prevent the collision of if specified.
    /// </summary>
    public readonly Fixture OtherFixture;

    /// <summary>
    /// Whether or not to prevent the collision between the physics bodies.
    /// </summary>
    public bool Cancelled = false;

    public PreventCollideEvent(
        EntityUid ourEntity,
        EntityUid otherEntity,
        PhysicsComponent ourBody,
        PhysicsComponent otherBody,
        Fixture ourFixture,
        Fixture otherFixture)
    {
        OurEntity = ourEntity;
        OtherEntity = otherEntity;
        OurBody = ourBody;
        OtherBody = otherBody;
        OurFixture = ourFixture;
        OtherFixture = otherFixture;
    }
}
