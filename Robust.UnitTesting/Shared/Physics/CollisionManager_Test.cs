using System.Collections.Generic;
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

        private PhysShapeAabb _aabbA = default!;
        private PhysShapeAabb _aabbB = default!;

        [OneTimeSetUp]
        public void Setup()
        {
            _collisionManager = IoCManager.Resolve<ICollisionManager>();
            _circleA = new PhysShapeCircle {Radius = 0.5f};
            _circleB = new PhysShapeCircle {Radius = 0.5f};
            _polyA = new PolygonShape();
            _polyB = new PolygonShape();

            var bounds = new Box2(-0.5f, 0.5f);

            _aabbA = new PhysShapeAabb
            {
                LocalBounds = bounds
            };
            _aabbB = new PhysShapeAabb
            {
                LocalBounds = bounds
            };
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
            var manifold = new Manifold();

            var expectedManifold = new Manifold
            {
                Type = ManifoldType.FaceA,
                LocalNormal = new Vector2(-1, 0),
                LocalPoint = new Vector2(-0.5f, 0),
                PointCount = 2,
                Points = new FixedArray2<ManifoldPoint>
                {
                    [0] = new() {LocalPoint = new Vector2(0.5f, -0.5f), Id = new ContactID {Key = 65538}},
                    [1] = new() {LocalPoint = new Vector2(0.5f, 0.5f), Id = new ContactID {Key = 65794}}
                }
            };
            _collisionManager.CollidePolygons(ref manifold, _polyA, transformA, _polyB, transformB);
            Assert.That(manifold, Is.EqualTo(expectedManifold));
        }

        public class BoxTestCase
        {
            public Transform TransformA { get; }
            public Transform TransformB { get; }

            internal BoxTestCase(Transform transformA, Transform transformB)
            {
                TransformA = transformA;
                TransformB = transformB;
            }
        }

        private static IEnumerable<BoxTestCase> BoxTestCases
        {
            get
            {
                yield return new BoxTestCase(
                    new Transform(Vector2.One + new Vector2(0.25f, 0.25f), 0f),
                    new Transform(Vector2.One, 0f));

                yield return new BoxTestCase(
                    new Transform(Vector2.Zero, 0f),
                    new Transform(new Vector2(0.5f, 0.0f), 0f));

                yield return new BoxTestCase(
                    new Transform(Vector2.Zero, 0f),
                    new Transform(new Vector2(-0.5f, 0.0f), 0f));

                yield return new BoxTestCase(
                    new Transform(Vector2.Zero, 0f),
                    new Transform(new Vector2(0.0f, 0.5f), 0f));

                yield return new BoxTestCase(
                    new Transform(Vector2.Zero, 0f),
                    new Transform(new Vector2(0.0f, -0.5f), 0f));
            }
        }

        /// <summary>
        /// 2 box polygons and 2 aabbs / boxes should generate the same manifolds.
        /// </summary>
        [Test, TestCaseSource(nameof(BoxTestCases))]
        public void TestBoxCollisions(BoxTestCase testCase)
        {
            var transformA = testCase.TransformA;
            var transformB = testCase.TransformB;
            var manifoldA = new Manifold();
            var manifoldB = new Manifold();

            _collisionManager.CollidePolygons(ref manifoldA, _polyA, transformA, _polyB, transformB);
            _collisionManager.CollideAabbs(ref manifoldB, _aabbA, transformA, _aabbB, transformB);

            Assert.That(manifoldA.PointCount, Is.EqualTo(manifoldB.PointCount));
            Assert.That(manifoldA.Type, Is.EqualTo(manifoldB.Type));
            Assert.That(manifoldA.LocalPoint, Is.EqualTo(manifoldB.LocalPoint));
            Assert.That(manifoldA.LocalNormal, Is.EqualTo(manifoldB.LocalNormal));

            for (var i = 0; i < 2; i++)
            {
                Assert.That(manifoldA.Points[i].Id, Is.EqualTo(manifoldB.Points[i].Id));
                Assert.That(manifoldA.Points[i].LocalPoint, Is.EqualTo(manifoldB.Points[i].LocalPoint));
            }
        }
    }
}
