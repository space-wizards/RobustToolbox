using NUnit.Framework;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Maths
{
    [TestFixture, Parallelizable, TestOf(typeof(Box2i))]
    sealed class Box2i_Test
    {
        [Test]
        public void Box2iUnion()
        {
            var boxOne = new Box2i(-1, -1, 1, 1);
            var boxTwo = new Box2i(0, 0, 2, 2);

            var result = boxOne.Union(boxTwo);

            Assert.That(result.Left, Is.EqualTo(-1));
            Assert.That(result.Bottom, Is.EqualTo(-1));
            Assert.That(result.Right, Is.EqualTo(2));
            Assert.That(result.Top, Is.EqualTo(2));
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
    }
}
