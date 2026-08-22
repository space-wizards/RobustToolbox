using NUnit.Framework;

namespace Robust.Shared.Maths.Tests;

[TestFixture, Parallelizable, TestOf(typeof(Box2i))]
internal sealed class Box2i_Test
{
    [Test]
    public void Box2iUnion()
    {
        var boxOne = new Box2i(-1, -1, 1, 1);
        var boxTwo = new Box2i(0, 0, 2, 2);

        var result = boxOne.Union(boxTwo);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(result.Left, Is.EqualTo(-1));
            Assert.That(result.Bottom, Is.EqualTo(-1));
            Assert.That(result.Right, Is.EqualTo(2));
            Assert.That(result.Top, Is.EqualTo(2));
        }
    }

    [Test]
    public void Box2iVector2iUnion()
    {
        var box = new Box2i();
        Assert.That(box, Is.EqualTo(Box2i.Empty));

        box = box.UnionTile(Vector2i.Zero);
        Assert.That(box.Right, Is.EqualTo(1));

        box = box.UnionTile(Vector2i.One);
        Assert.That(box.Top, Is.EqualTo(2));

        box = box.Union(new Vector2i(2, 0));
        Assert.That(box.Right, Is.EqualTo(2));
    }

    [Test]
    public void Box2iUsesDirectDimensions()
    {
        var valid = new Box2i(-1, -2, 3, 4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(valid.Width, Is.EqualTo(4));
            Assert.That(valid.Height, Is.EqualTo(6));
            Assert.That(valid.Size, Is.EqualTo(new Vector2i(4, 6)));
            Assert.That(valid.IsValid(), Is.True);
        }
    }

    [Test]
    public void Box2iInvalidConstruction()
    {
#if DEBUG
        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => new Box2i(3, 4, -1, -2), Throws.Exception);
            Assert.That(() => new Box2i(new Vector2i(3, 4), new Vector2i(-1, -2)), Throws.Exception);
        }
#else
        var expected = new Box2i(3, 4, 3, 4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(new Box2i(3, 4, -1, -2), Is.EqualTo(expected));
            Assert.That(new Box2i(new Vector2i(3, 4), new Vector2i(-1, -2)), Is.EqualTo(expected));
        }
#endif
    }

    [Test]
    public void Box2iInvalidProperties()
    {
#if DEBUG
        var box = new Box2i(-1, -2, 3, 4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(() => box.Left = 4, Throws.Exception);
            Assert.That(() => box.Bottom = 5, Throws.Exception);
            Assert.That(() => box.Right = -2, Throws.Exception);
            Assert.That(() => box.Top = -3, Throws.Exception);
            Assert.That(() => box.BottomLeft = new Vector2i(4, 0), Throws.Exception);
            Assert.That(() => box.TopRight = new Vector2i(0, -3), Throws.Exception);
        }
#else
        var expected = new Box2i(-1, -2, 3, 4);

        var left = expected;
        left.Left = 4;
        Assert.That(left, Is.EqualTo(new Box2i(3, -2, 3, 4)));

        var bottom = expected;
        bottom.Bottom = 5;
        Assert.That(bottom, Is.EqualTo(new Box2i(-1, 4, 3, 4)));

        var right = expected;
        right.Right = -2;
        Assert.That(right, Is.EqualTo(new Box2i(-1, -2, -1, 4)));

        var top = expected;
        top.Top = -3;
        Assert.That(top, Is.EqualTo(new Box2i(-1, -2, 3, -2)));

        var bottomLeft = expected;
        bottomLeft.BottomLeft = new Vector2i(4, 0);
        Assert.That(bottomLeft, Is.EqualTo(new Box2i(3, 0, 3, 4)));

        var topRight = expected;
        topRight.TopRight = new Vector2i(0, -3);
        Assert.That(topRight, Is.EqualTo(new Box2i(-1, -2, 0, -2)));
#endif
    }

    [Test]
    public void Box2iFromTwoPointsNormalizes()
    {
        var box = Box2i.FromTwoPoints(new Vector2i(3, -2), new Vector2i(-1, 4));

        Assert.That(box, Is.EqualTo(new Box2i(-1, -2, 3, 4)));
        Assert.That(box.IsValid(), Is.True);
    }

    [Test]
    public void Box2iContainsUsesValidBounds()
    {
        var box = new Box2i(-1, -1, 1, 1);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(box.Contains(Vector2i.Zero), Is.True);
            Assert.That(box.Contains(new Vector2i(1, 1)), Is.True);
            Assert.That(box.Contains(new Vector2i(1, 1), false), Is.False);
            Assert.That(box.Contains(new Box2i(0, 0, 1, 1)), Is.True);
            Assert.That(box.Encloses(new Box2i(0, 0, 1, 1)), Is.False);
        }
    }

    [Test]
    public void Box2iIntersect()
    {
        var boxOne = new Box2i(-1, -1, 2, 2);
        var boxTwo = new Box2i(0, 1, 3, 4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(boxOne.Intersect(boxTwo), Is.EqualTo(new Box2i(0, 1, 2, 2)));
            Assert.That(boxOne.Intersect(new Box2i(3, 3, 4, 4)), Is.EqualTo(Box2i.Empty));
        }
    }

    [Test]
    public void Box2iClosestPoint()
    {
        var box = new Box2i(-1, -2, 3, 4);

        using (Assert.EnterMultipleScope())
        {
            Assert.That(box.ClosestPoint(new Vector2i(10, -10)), Is.EqualTo(new Vector2i(3, -2)));
            Assert.That(box.ClosestPoint(Vector2i.Zero), Is.EqualTo(Vector2i.Zero));
        }
    }
}
