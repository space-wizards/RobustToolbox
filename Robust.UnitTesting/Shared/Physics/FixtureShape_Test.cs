using System;
using NUnit.Framework;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(FixtureSystem))]
    public class FixtureShape_Test : RobustUnitTest
    {
        private FixtureSystem _shapeManager = default!;

        [OneTimeSetUp]
        public void Setup()
        {
            _shapeManager = new FixtureSystem();
        }

        [Test]
        public void TestCirclePoint()
        {
            var circle = new PhysShapeCircle {Radius = 0.5f};
            var transform = new Transform(0f);
            var posA = Vector2.One;
            var posB = Vector2.Zero;
            var posC = new Vector2(0.1f, 0.3f);

            Assert.That(_shapeManager.TestPoint(circle, transform, posA), Is.EqualTo(false));
            Assert.That(_shapeManager.TestPoint(circle, transform, posB), Is.EqualTo(true));
            Assert.That(_shapeManager.TestPoint(circle, transform, posC), Is.EqualTo(true));
        }

        [Test]
        public void TestEdgePoint()
        {
            // Edges never collide with a point because they're a damn line
            var edge = new EdgeShape(Vector2.Zero, Vector2.One);
            var transform = new Transform(0f);
            var posA = Vector2.One;
            var posB = Vector2.Zero;
            var posC = new Vector2(0.1f, 0.3f);

            Assert.That(_shapeManager.TestPoint(edge, transform, posA), Is.EqualTo(false));
            Assert.That(_shapeManager.TestPoint(edge, transform, posB), Is.EqualTo(false));
            Assert.That(_shapeManager.TestPoint(edge, transform, posC), Is.EqualTo(false));
        }

        [Test]
        public void TestPolyPoint()
        {
            var poly = new PolygonShape();
            poly.SetAsBox(0.5f, 0.5f);
            var transform = new Transform(0f);
            var posA = Vector2.One;
            var posB = Vector2.Zero;
            var posC = new Vector2(0.6f, 0.0f);

            Assert.That(_shapeManager.TestPoint(poly, transform, posA), Is.EqualTo(false));
            Assert.That(_shapeManager.TestPoint(poly, transform, posB), Is.EqualTo(true));
            Assert.That(_shapeManager.TestPoint(poly, transform, posC), Is.EqualTo(false));

            // Rotations
            transform.Quaternion2D = new Quaternion2D(MathF.PI / 4);
            Assert.That(_shapeManager.TestPoint(poly, transform, posC), Is.EqualTo(true));
        }
    }
}
