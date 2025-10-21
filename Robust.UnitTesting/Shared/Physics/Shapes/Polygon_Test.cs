using System.Numerics;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Shapes;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class Polygon_Test
{
    /// <summary>
    /// Check that Slim and normal Polygon are equals
    /// </summary>
    [Test]
    public void TestSlim()
    {
        var slim = new SlimPolygon(Box2.UnitCentered.Translated(Vector2.One));

        var poly = new Polygon(Box2.UnitCentered.Translated(Vector2.One));

        Assert.That(slim.Equals(poly));
    }

    [Test]
    public void TestAABB()
    {
        var shape = new SlimPolygon(Box2.UnitCentered.Translated(Vector2.One));

        Assert.That(shape.ComputeAABB(Transform.Empty, 0), Is.EqualTo(Box2.UnitCentered.Translated(Vector2.One)));
    }

    [Test]
    public void TestBox2()
    {
        var shape = new SlimPolygon(Box2.UnitCentered.Translated(Vector2.One));
        Assert.That(shape._vertices.AsSpan.ToArray(), Is.EqualTo(new Vector2[]
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
        var shape = new SlimPolygon(new Box2Rotated(Box2.UnitCentered, Angle.FromDegrees(90)));

        Assert.That(shape._vertices.AsSpan.ToArray(), Is.EqualTo(new Vector2[]
        {
            new Vector2(0.5f, -0.5f),
            new Vector2(0.5f, 0.5f),
            new Vector2(-0.5f, 0.5f),
            new Vector2(-0.5f, -0.5f),
        }));
    }
}
