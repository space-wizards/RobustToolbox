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
    }
}
