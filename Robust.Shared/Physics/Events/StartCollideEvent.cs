using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;

namespace Robust.Shared.Physics.Events;

[ByRefEvent]
public readonly struct StartCollideEvent
{
    public readonly Fixture OurFixture;
    public readonly Fixture OtherFixture;
    public readonly Vector2 WorldPoint;

    public StartCollideEvent(Fixture ourFixture, Fixture otherFixture, Vector2 worldPoint)
    {
        OurFixture = ourFixture;
        OtherFixture = otherFixture;
        WorldPoint = worldPoint;
    }
}
