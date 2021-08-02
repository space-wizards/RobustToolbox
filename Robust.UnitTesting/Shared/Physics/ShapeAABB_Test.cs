using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    public class ShapeAABB_Test : RobustUnitTest
    {
        private Transform _transform;

        [OneTimeSetUp]
        public void Setup()
        {
            _transform = new Transform(Vector2.One, 0f);
        }

        [Test]
        public async Task TestCircleAABB()
        {
            var circle = new PhysShapeCircle {Radius = 0.5f};
            var aabb = circle.ComputeAABB(_transform, 0);
            Assert.That(aabb.Width, Is.EqualTo(1f));
            Assert.That(aabb, Is.EqualTo(new Box2(0.5f, 0.5f, 1.5f, 1.5f)));
        }
    }
}
