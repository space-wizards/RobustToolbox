using System.Numerics;
using NUnit.Framework;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(IManifoldManager))]
    public sealed class ManifoldManager_Test : RobustUnitTest
    {
        private IManifoldManager _manifoldManager = default!;

        private PhysShapeCircle _circleA = default!;
        private PhysShapeCircle _circleB = default!;

        private PolygonShape _polyA = default!;
        private PolygonShape _polyB = default!;

        [OneTimeSetUp]
        public void Setup()
        {
            _manifoldManager = new CollisionManager();
            _circleA = new PhysShapeCircle(0.5f);
            _circleB = new PhysShapeCircle(0.5f);
            _polyA = new PolygonShape();
            _polyB = new PolygonShape();
            _polyA.SetAsBox(0.5f, 0.5f);
            _polyB.SetAsBox(0.5f, 0.5f);
        }

        [Test]
        public void TestCircleCollision()
        {
            var transformA = new Transform(new Vector2(-1, -1), 0f);
            var transformB = new Transform(Vector2.One, 0f);

            // No overlap
            Assert.That(_manifoldManager.TestOverlap(_circleA, 0, _circleB, 0, transformA, transformB), Is.EqualTo(false));

            // Overlap directly
            transformA = new Transform(transformB.Position, 0f);
            Assert.That(_manifoldManager.TestOverlap(_circleA, 0, _circleB, 0, transformA, transformB), Is.EqualTo(true));

            // Overlap on edge
            transformA.Position = transformB.Position + new Vector2(0.5f, 0.0f);
            Assert.That(_manifoldManager.TestOverlap(_circleA, 0, _circleB, 0, transformA, transformB), Is.EqualTo(true));
        }

        [Test]
        public void TestPolyCollisions()
        {
            var transformA = new Transform(new Vector2(-1, -1), 0f);
            var transformB = new Transform(Vector2.One, 0f);

            // No overlap
            Assert.That(_manifoldManager.TestOverlap(_polyA, 0, _polyB, 0, transformA, transformB), Is.EqualTo(false));

            // Overlap directly
            transformA = new Transform(transformB.Position, 0f);
            Assert.That(_manifoldManager.TestOverlap(_polyA, 0, _polyB, 0, transformA, transformB), Is.EqualTo(true));

            // Overlap on edge
            transformA.Position = transformB.Position + new Vector2(0.5f, 0.0f);
            Assert.That(_manifoldManager.TestOverlap(_polyA, 0, _polyB, 0, transformA, transformB), Is.EqualTo(true));

            transformA.Quaternion2D = transformA.Quaternion2D.Set(45f);
            Assert.That(_manifoldManager.TestOverlap(_polyA, 0, _polyB, 0, transformA, transformB), Is.EqualTo(true));
        }

        [Test]
        public void TestPolyOnPolyManifolds()
        {
            var transformB = new Transform(Vector2.One, 0f);
            var transformA = new Transform(transformB.Position + new Vector2(0.5f, 0.0f), 0f);
            var manifold = new Manifold();

            var expectedManifold = new Manifold
            {
                Type = ManifoldType.FaceA,
                LocalNormal = new Vector2(-1, 0),
                LocalPoint = new Vector2(-0.5f, 0),
                PointCount = 2,
                Points = new FixedArray2<ManifoldPoint>(
                    new ManifoldPoint
                    {
                        LocalPoint = new Vector2(0.5f, -0.5f),
                        Id = new ContactID {Key = 65795}
                    },
                    new ManifoldPoint
                    {
                        LocalPoint = new Vector2(0.5f, 0.5f),
                        Id = new ContactID {Key = 66051}
                    }
                )
            };
            _manifoldManager.CollidePolygons(ref manifold, _polyA, transformA, _polyB, transformB);

            for (var i = 0; i < manifold.PointCount; i++)
            {
                Assert.That(manifold.Points.AsSpan[i], Is.EqualTo(expectedManifold.Points.AsSpan[i]));
            }

            Assert.That(manifold, Is.EqualTo(expectedManifold));
        }
    }
}
