using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics.Events;

[ByRefEvent]
public readonly struct EndCollideEvent
{
    public readonly EntityUid OurEntity;
    public readonly EntityUid OtherEntity;

    public readonly PhysicsComponent OurBody;
    public readonly PhysicsComponent OtherBody;

    public readonly string OurFixtureId;
    public readonly string OtherFixtureId;

    public readonly Fixture OurFixture;
    public readonly Fixture OtherFixture;

    public EndCollideEvent(
        EntityUid ourEntity,
        EntityUid otherEntity,
        string ourFixtureId,
        string otherFixtureId,
        Fixture ourFixture,
        Fixture otherFixture,
        PhysicsComponent ourBody,
        PhysicsComponent otherBody)
    {
        OurEntity = ourEntity;
        OtherEntity = otherEntity;
        OurFixtureId = ourFixtureId;
        OtherFixtureId = otherFixtureId;
        OurFixture = ourFixture;
        OtherFixture = otherFixture;
        OtherBody = otherBody;
        OurBody = ourBody;
    }
}
