using NUnit.Framework;
using SS14.Shared.Maths;

namespace SS14.UnitTesting.Shared.Maths
{
    [TestFixture]
    [TestOf(typeof(Ray))]
    class Ray_Test
    {
        [Test]
        public void RayIntersectsBoxTest()
        {
            var box = new Box2(new Vector2(5, 5), new Vector2(10, -5));
            var ray = new Ray(new Vector2(0, 1), Vector2.UnitX);
            
            var result = ray.Intersects(box, out var dist, out var hitPos);

            Assert.That(result, Is.True);
            Assert.That(dist, Is.EqualTo(5));
            Assert.That(hitPos.X, Is.EqualTo(5));
            Assert.That(hitPos.Y, Is.EqualTo(1));
        }
    }
}
