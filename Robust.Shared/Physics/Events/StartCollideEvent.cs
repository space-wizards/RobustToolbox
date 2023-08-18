using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics.Events;

[ByRefEvent]
public readonly struct StartCollideEvent
{
    public readonly EntityUid OurEntity;
    public readonly EntityUid OtherEntity;

    public readonly PhysicsComponent OurBody;
    public readonly PhysicsComponent OtherBody;

    public readonly string OurFixtureId;
    public readonly string OtherFixtureId;

    public readonly Fixture OurFixture;
    public readonly Fixture OtherFixture;
    public readonly Vector2 WorldPoint;

    public StartCollideEvent(
        EntityUid ourEntity,
        EntityUid otherEntity,
        string ourFixtureId,
        string otherFixtureId,
        Fixture ourFixture,
        Fixture otherFixture,
        PhysicsComponent ourBody,
        PhysicsComponent otherBody,
        Vector2 worldPoint)
    {
        OurEntity = ourEntity;
        OtherEntity = otherEntity;
        OurFixtureId = ourFixtureId;
        OtherFixtureId = otherFixtureId;
        OurFixture = ourFixture;
        OtherFixture = otherFixture;
        WorldPoint = worldPoint;
        OtherBody = otherBody;
        OurBody = ourBody;
    }
}
