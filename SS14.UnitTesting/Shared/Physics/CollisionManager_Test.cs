using Moq;
using NUnit.Framework;
using SS14.Shared.Interfaces.Physics;
using SS14.Shared.Maths;
using SS14.Shared.Physics;

namespace SS14.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(CollisionManager))]
    class CollisionManager_Test
    {
        [Test]
        public void RayCastTest()
        {
            // Arrange
            var box = new Box2(new Vector2(5, 5), new Vector2(10, -5));
            var ray = new Ray(new Vector2(0, 1), Vector2.UnitX);
            var manager = new CollisionManager();

            var mock = new Mock<ICollidable>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            manager.AddCollidable(mock.Object);

            // Act
            var result = manager.IntersectRay(ray);

            // Assert
            Assert.That(result.HitObject, Is.True);
            Assert.That(result.Distance, Is.EqualTo(5));
            Assert.That(result.HitPos.X, Is.EqualTo(5));
            Assert.That(result.HitPos.Y, Is.EqualTo(1));
        }
    }
}
