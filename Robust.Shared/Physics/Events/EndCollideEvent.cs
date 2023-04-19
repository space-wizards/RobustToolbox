using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics.Events;

[ByRefEvent]
public readonly struct EndCollideEvent
{
    public readonly EntityUid OurEntity;
    public readonly EntityUid OtherEntity;

    public readonly Fixture OurFixture;
    public readonly Fixture OtherFixture;

    public EndCollideEvent(EntityUid ourEntity, EntityUid otherEntity, Fixture ourFixture, Fixture otherFixture)
    {
        OurFixture = ourFixture;
        OtherFixture = otherFixture;
    }
}
