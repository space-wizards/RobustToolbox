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
    public void Box2iValidatesConstruction()
    {
        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<ArgumentException>(() => new Box2i(3, 4, -1, -2));
            Assert.Throws<ArgumentException>(() => new Box2i(new Vector2i(3, 4), new Vector2i(-1, -2)));
        }
    }

    [Test]
    public void Box2iValidatesProperties()
    {
        var box = new Box2i(-1, -2, 3, 4);

        using (Assert.EnterMultipleScope())
        {
            Assert.Throws<ArgumentOutOfRangeException>(() => box.Left = 4);
            Assert.Throws<ArgumentOutOfRangeException>(() => box.Bottom = 5);
            Assert.Throws<ArgumentOutOfRangeException>(() => box.Right = -2);
            Assert.Throws<ArgumentOutOfRangeException>(() => box.Top = -3);
            Assert.Throws<ArgumentOutOfRangeException>(() => box.BottomLeft = new Vector2i(4, 0));
            Assert.Throws<ArgumentOutOfRangeException>(() => box.TopRight = new Vector2i(0, -3));
        }
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
