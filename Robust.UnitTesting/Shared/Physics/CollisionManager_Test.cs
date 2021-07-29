using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(ICollisionManager))]
    public class CollisionManager_Test : RobustUnitTest
    {
        private ICollisionManager _collisionManager = default!;

        private PhysShapeCircle _circleA = default!;
        private PhysShapeCircle _circleB = default!;

        private PolygonShape _polyA = default!;
        private PolygonShape _polyB = default!;

        [OneTimeSetUp]
        public void Setup()
        {
            _collisionManager = IoCManager.Resolve<ICollisionManager>();
            _circleA = new PhysShapeCircle {Radius = 0.5f};
            _circleB = new PhysShapeCircle {Radius = 0.5f};
            _polyA = new PolygonShape();
            _polyB = new PolygonShape();
            _polyA.SetAsBox(-0.5f, 0.5f);
            _polyB.SetAsBox(-0.5f, 0.5f);
        }

        [Test]
        public void TestCircleCollision()
        {
            var transformA = new Transform(new Vector2(-1, -1), 0f);
            var transformB = new Transform(Vector2.One, 0f);

            // No overlap
            Assert.That(_collisionManager.TestOverlap(_circleA, 0, _circleB, 0, transformA, transformB), Is.EqualTo(false));

            // Overlap directly
            transformA = new Transform(transformB.Position, 0f);
            Assert.That(_collisionManager.TestOverlap(_circleA, 0, _circleB, 0, transformA, transformB), Is.EqualTo(true));

            // Overlap on edge
            transformA.Position = transformB.Position + new Vector2(0.5f, 0.0f);
            Assert.That(_collisionManager.TestOverlap(_circleA, 0, _circleB, 0, transformA, transformB), Is.EqualTo(true));
        }

        [Test]
        public void TestPolyCollisions()
        {
            var transformA = new Transform(new Vector2(-1, -1), 0f);
            var transformB = new Transform(Vector2.One, 0f);

            // No overlap
            Assert.That(_collisionManager.TestOverlap(_polyA, 0, _polyB, 0, transformA, transformB), Is.EqualTo(false));

            // Overlap directly
            transformA = new Transform(transformB.Position, 0f);
            Assert.That(_collisionManager.TestOverlap(_polyA, 0, _polyB, 0, transformA, transformB), Is.EqualTo(true));

            // Overlap on edge
            transformA.Position = transformB.Position + new Vector2(0.5f, 0.0f);
            Assert.That(_collisionManager.TestOverlap(_polyA, 0, _polyB, 0, transformA, transformB), Is.EqualTo(true));

            transformA.Quaternion2D = transformA.Quaternion2D.Set(45f);
            Assert.That(_collisionManager.TestOverlap(_polyA, 0, _polyB, 0, transformA, transformB), Is.EqualTo(true));
        }

        [Test]
        public void TestPolyOnPolyManifolds()
        {
            var transformB = new Transform(Vector2.One, 0f);
            var transformA = new Transform(transformB.Position + new Vector2(0.5f, 0.0f), 0f);
            var manifold = new Manifold()
            {
                Points = new ManifoldPoint[2]
            };

            var expectedManifold = new Manifold
            {
                Type = ManifoldType.FaceA,
                LocalNormal = new Vector2(-1, 0),
                LocalPoint = new Vector2(-0.5f, 0),
                PointCount = 2,
                Points = new ManifoldPoint[]
                {
                    new() {LocalPoint = new Vector2(0.5f, -0.5f), Id = new ContactID {Key = 65538}},
                    new() {LocalPoint = new Vector2(0.5f, 0.5f), Id = new ContactID {Key = 65794}}
                }
            };
            _collisionManager.CollidePolygons(ref manifold, _polyA, transformA, _polyB, transformB);

            for (var i = 0; i < manifold.Points.Length; i++)
            {
                Assert.That(manifold.Points[0], Is.EqualTo(expectedManifold.Points[i]));
            }

            Assert.That(manifold, Is.EqualTo(expectedManifold));
        }
    }
}
