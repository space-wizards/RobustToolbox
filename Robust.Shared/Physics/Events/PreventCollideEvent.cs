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
    /// One of the bodies to prevent the collision of.
    /// </summary>
    public PhysicsComponent BodyA;

    /// <summary>
    /// The other body to prevent the collision of.
    /// </summary>
    public PhysicsComponent BodyB;

    /// <summary>
    /// The fixture on the first body to prevent the collision of if specified.
    /// </summary>
    public Fixture FixtureA;

    /// <summary>
    /// The fixture on the other body to prevent the collision of if specified.
    /// </summary>
    public Fixture FixtureB;

    /// <summary>
    /// Whether or not to prevent the collision between the physics bodies.
    /// </summary>
    public bool Cancelled = false;

    public PreventCollideEvent(PhysicsComponent ourBody, PhysicsComponent otherBody, Fixture ourFixture, Fixture otherFixture)
    {
        BodyA = ourBody;
        BodyB = otherBody;
        FixtureA = ourFixture;
        FixtureB = otherFixture;
    }
}
