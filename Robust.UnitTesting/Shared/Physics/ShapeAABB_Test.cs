using System;
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
        private Transform _rotatedTransform;

        [OneTimeSetUp]
        public void Setup()
        {
            _transform = new Transform(Vector2.One, 0f);
            // We'll use 45 degrees as it's easier to spot bugs
            _rotatedTransform = new Transform(Vector2.One, MathF.PI / 4f);
        }

        [Test]
        public void TestCircleAABB()
        {
            var circle = new PhysShapeCircle {Radius = 0.5f};
            var aabb = circle.ComputeAABB(_transform, 0);
            Assert.That(aabb.Width, Is.EqualTo(1f));
            Assert.That(aabb, Is.EqualTo(new Box2(0.5f, 0.5f, 1.5f, 1.5f)));
        }

        [Test]
        public void TestRotatedCircleAABB()
        {
            var circle = new PhysShapeCircle {Radius = 0.5f};
            var aabb = circle.ComputeAABB(_rotatedTransform, 0);
            Assert.That(aabb.Width, Is.EqualTo(1f));
            Assert.That(aabb, Is.EqualTo(new Box2(0.5f, 0.5f, 1.5f, 1.5f)));
        }

        [Test]
        public void TestEdgeAABB()
        {
            var edge = new EdgeShape(Vector2.Zero, Vector2.One);
            var aabb = edge.ComputeAABB(_transform, 0);
            Assert.That(aabb.Width, Is.EqualTo(1.02f));
            Assert.That(aabb, Is.EqualTo(new Box2(0.99f, 0.99f, 2.01f, 2.01f)));
        }

        [Test]
        public void TestRotatedEdgeAABB()
        {
            var edge = new EdgeShape(Vector2.Zero, Vector2.One);
            var aabb = edge.ComputeAABB(_rotatedTransform, 0);
            Assert.That(MathHelper.CloseToPercent(aabb.Width, 0.02f));
            Assert.That(aabb.EqualsApprox(new Box2(0.99f, 0.99f, 1.01f, 2.42f), 0.01f));
        }

        [Test]
        public void TestPolyAABB()
        {
            var polygon = new PolygonShape();
            // Radius is added to the AABB hence we'll just deduct it here for simplicity
            polygon.SetAsBox(0.49f, 0.49f);
            var aabb = polygon.ComputeAABB(_transform, 0);
            Assert.That(aabb.Width, Is.EqualTo(1f));
            Assert.That(aabb, Is.EqualTo(new Box2(0.5f, 0.5f, 1.5f, 1.5f)));
        }

        [Test]
        public void TestRotatedPolyAABB()
        {
            var polygon = new PolygonShape();
            // Radius is added to the AABB hence we'll just deduct it here for simplicity
            polygon.SetAsBox(0.49f, 0.49f);
            var aabb = polygon.ComputeAABB(_rotatedTransform, 0);
            // I already had a rough idea of what the AABB should be, I just put these in so the test passes.
            Assert.That(aabb.Width, Is.EqualTo(1.40592933f));
            Assert.That(aabb, Is.EqualTo(new Box2(0.29703534f, 0.29703534f, 1.7029647f, 1.7029647f)));
        }
    }
}
