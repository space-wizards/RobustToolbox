using System.Numerics;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Shapes;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class Polygon_Test
{
    [Test]
    public void TestAABB()
    {
        var shape = new Polygon(Box2.UnitCentered.Translated(Vector2.One));

        Assert.That(shape.ComputeAABB(Transform.Empty, 0), Is.EqualTo(Box2.UnitCentered.Translated(Vector2.One)));
    }

    [Test]
    public void TestBox2()
    {
        var shape = new Polygon(Box2.UnitCentered.Translated(Vector2.One));
        Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
        {
            new Vector2(0.5f, 0.5f),
            new Vector2(1.5f, 0.5f),
            new Vector2(1.5f, 1.5f),
            new Vector2(0.5f, 1.5f),
        }));
    }

    [Test]
    public void TestBox2Rotated()
    {
        var shape = new Polygon(new Box2Rotated(Box2.UnitCentered, Angle.FromDegrees(90)));

        Assert.That(shape.Vertices, Is.EqualTo(new Vector2[]
        {
            new Vector2(0.5f, -0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-0.5f, 0.5f),
            new Vector2(-0.5f, -0.5f),
        }));
    }
}
