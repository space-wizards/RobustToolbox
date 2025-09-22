using System.Collections.Generic;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Shapes;
using Robust.UnitTesting.Shared.Maths;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
[TestOf(typeof(SlimPolygon))]
public sealed class SlimPolygonTest
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

    public static IEnumerable<(Box2 baseBox, Vector2 origin, Angle rotation, Box2 expected)> CalcBoundingBoxData
        => Box2Rotated_Test.CalcBoundingBoxData;

    [Test]
    public void TestBox2Rotated([ValueSource(nameof(CalcBoundingBoxData))] (Box2 baseBox, Vector2 origin, Angle rotation, Box2 expected) dat)
    {
        var box = new Box2Rotated(dat.baseBox, dat.rotation, dat.origin);
        var shape = new SlimPolygon(box);

        Assert.That(shape._vertices._00, Is.Approximately(box.BottomLeft, 0.0001f));
        Assert.That(shape._vertices._01, Is.Approximately(box.BottomRight, 0.0001f));
        Assert.That(shape._vertices._02, Is.Approximately(box.TopRight, 0.0001f));
        Assert.That(shape._vertices._03, Is.Approximately(box.TopLeft, 0.0001f));
    }

    [Test]
    public void TestBox2RotatedBounds([ValueSource(nameof(CalcBoundingBoxData))](Box2 baseBox, Vector2 origin, Angle rotation, Box2 expected) dat)
    {
        var box = new Box2Rotated(dat.baseBox, dat.rotation, dat.origin);
        var shape = new SlimPolygon(box);
        var aabb = shape.ComputeAABB(Transform.Empty, 0);
        Assert.That(aabb, Is.Approximately(dat.expected));
    }

    [Test]
    public void TestTransformConstructor([ValueSource(nameof(CalcBoundingBoxData))] (Box2 baseBox, Vector2 origin, Angle rotation, Box2 expected) dat)
    {
        var box = new Box2Rotated(dat.baseBox, dat.rotation, dat.origin);
        var shape = new SlimPolygon(box.Box, box.Transform, out var bounds);

        Assert.That(shape._vertices._00, Is.Approximately(box.BottomLeft, 0.0001f));
        Assert.That(shape._vertices._01, Is.Approximately(box.BottomRight, 0.0001f));
        Assert.That(shape._vertices._02, Is.Approximately(box.TopRight, 0.0001f));
        Assert.That(shape._vertices._03, Is.Approximately(box.TopLeft, 0.0001f));
        Assert.That(box.CalcBoundingBox(), Is.Approximately(bounds, 0.0001f));
    }

    [Test]
    public void TestTransformRotatedConstructor([ValueSource(nameof(CalcBoundingBoxData))](Box2 baseBox, Vector2 origin, Angle rotation, Box2 expected) dat)
    {
        var box = new Box2Rotated(dat.baseBox, dat.rotation, dat.origin);
        Matrix3x2.Invert(box.Transform, out var inverse);
        var shape = new SlimPolygon(box, inverse, out var bounds);

        Assert.That(shape._vertices._00, Is.Approximately(dat.baseBox.BottomLeft, 0.0001f));
        Assert.That(shape._vertices._01, Is.Approximately(dat.baseBox.BottomRight, 0.0001f));
        Assert.That(shape._vertices._02, Is.Approximately(dat.baseBox.TopRight, 0.0001f));
        Assert.That(shape._vertices._03, Is.Approximately(dat.baseBox.TopLeft, 0.0001f));
        Assert.That(dat.baseBox, Is.Approximately(bounds, 0.0001f));
    }
}
