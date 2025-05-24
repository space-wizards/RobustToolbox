using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Utility;

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

    internal readonly FixedArray2<Vector2> _worldPoints;

    public readonly int PointCount;
    public readonly Vector2 WorldNormal;

    public Vector2[] WorldPoints => _worldPoints.AsSpan[..PointCount].ToArray();


    internal StartCollideEvent(
        EntityUid ourEntity,
        EntityUid otherEntity,
        string ourFixtureId,
        string otherFixtureId,
        Fixture ourFixture,
        Fixture otherFixture,
        PhysicsComponent ourBody,
        PhysicsComponent otherBody,
        FixedArray2<Vector2> worldPoints,
        int pointCount,
        Vector2 worldNormal)
    {
        OurEntity = ourEntity;
        OtherEntity = otherEntity;
        OurFixtureId = ourFixtureId;
        OtherFixtureId = otherFixtureId;
        OurFixture = ourFixture;
        OtherFixture = otherFixture;
        OtherBody = otherBody;
        OurBody = ourBody;
        _worldPoints = worldPoints;
        PointCount = pointCount;
        WorldNormal = worldNormal;
    }
}
