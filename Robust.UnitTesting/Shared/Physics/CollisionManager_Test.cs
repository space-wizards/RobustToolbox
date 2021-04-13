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

        [OneTimeSetUp]
        public void Setup()
        {
            _collisionManager = IoCManager.Resolve<ICollisionManager>();
        }

        // I dumped the manifold tests under whatever's listed first in the method name.

        [Test]
        public void TestCircleCollision()
        {
            var circleOne = new PhysShapeCircle {Radius = 0.5f};
            var circleTwo = new PhysShapeCircle {Radius = 0.5f};
            var transformA = new Transform(new Vector2(-1, -1), 0f);
            var transformB = new Transform(Vector2.One, 0f);

            // No overlap
            Assert.That(_collisionManager.TestOverlap(circleOne, 0, circleTwo, 0, transformA, transformB), Is.EqualTo(false));

            // Overlap directly
            transformA = new Transform(transformB.Position, 0f);
            Assert.That(_collisionManager.TestOverlap(circleOne, 0, circleTwo, 0, transformA, transformB), Is.EqualTo(true));

            // Overlap on edge
            transformA = new Transform(Vector2.One + circleOne.Radius - float.Epsilon, 0f);
            Assert.That(_collisionManager.TestOverlap(circleOne, 0, circleTwo, 0, transformA, transformB), Is.EqualTo(true));
        }

        [Test]
        public void TestPolyCollisions()
        {
            var polyOne = new PolygonShape();
            var polyTwo = new PolygonShape();
            polyOne.SetAsBox(-0.5f, 0.5f);
            polyTwo.SetAsBox(-0.5f, 0.5f);
            var transformA = new Transform(new Vector2(-1, -1), 0f);
            var transformB = new Transform(Vector2.One, 0f);

            // No overlap
            Assert.That(_collisionManager.TestOverlap(polyOne, 0, polyTwo, 0, transformA, transformB), Is.EqualTo(false));

            // Overlap directly
            transformA = new Transform(transformB.Position, 0f);
            Assert.That(_collisionManager.TestOverlap(polyOne, 0, polyTwo, 0, transformA, transformB), Is.EqualTo(true));

            // Overlap on edge
            transformA = new Transform(Vector2.One + 0.5f - float.Epsilon, 0f);
            Assert.That(_collisionManager.TestOverlap(polyOne, 0, polyTwo, 0, transformA, transformB), Is.EqualTo(true));

            transformA.Position = transformB.Position + 0.5f;
            var manifold = new Manifold();

            _collisionManager.CollidePolygons(ref manifold, polyOne, transformA, polyTwo, transformB);
            Assert.That(manifold.LocalNormal, Is.EqualTo(new Vector2(-1, 0)));
            Assert.That(manifold.LocalPoint, Is.EqualTo(new Vector2(-0.5f, 0.0f)));
            // TODO: More

            // TODO: 45 degree angle as well
        }

        // TODO: Need TestOverlap between disparate shape types.
    }
}
