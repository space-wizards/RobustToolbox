using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics.Events;

[ByRefEvent]
public readonly struct EndCollideEvent
{
    public readonly Fixture OurFixture;
    public readonly Fixture OtherFixture;

    public EndCollideEvent(Fixture ourFixture, Fixture otherFixture)
    {
        OurFixture = ourFixture;
        OtherFixture = otherFixture;
    }
}
