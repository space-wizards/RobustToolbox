using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(PhysicsManager))]
    internal class CollisionManager_Test
    {
        [Test]
        public void IsCollidingFalse()
        {
            // Arrange
            var box = new Box2(5, -5, 10, 6);
            var testBox = new Box2(-3, -3, 4, 6);
            var manager = new PhysicsManager();

            var mock = new Mock<IPhysBody>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            mock.Setup(foo => foo.IsHardCollidable).Returns(true);
            mock.Setup(foo => foo.MapID).Returns(new MapId(0));
            mock.Setup(foo => foo.CollisionEnabled).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.IsColliding(testBox, new MapId(0));

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsCollidingInclusive()
        {
            // Arrange
            var box = new Box2(5, -5, 10, 6);
            var testBox = new Box2(-3, -3, 5, 6);
            var manager = new PhysicsManager();

            var mock = new Mock<IPhysBody>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            mock.Setup(foo => foo.IsHardCollidable).Returns(true);
            mock.Setup(foo => foo.MapID).Returns(new MapId(0));
            mock.Setup(foo => foo.CollisionEnabled).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.IsColliding(testBox, new MapId(0));

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsCollidingNotHard()
        {
            // Arrange
            var box = new Box2(5, -5, 10, 6);
            var testBox = new Box2(-3, -3, 5, 6);
            var manager = new PhysicsManager();

            var mock = new Mock<IPhysBody>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            mock.Setup(foo => foo.IsHardCollidable).Returns(false);
            mock.Setup(foo => foo.MapID).Returns(new MapId(0));
            mock.Setup(foo => foo.CollisionEnabled).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.IsColliding(testBox, new MapId(0));

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsCollidingTrue()
        {
            // Arrange
            var box = new Box2(5, -5, 10, 6);
            var testBox = new Box2(-3, -3, 6, 6);
            var manager = new PhysicsManager();

            var mock = new Mock<IPhysBody>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            mock.Setup(foo => foo.IsHardCollidable).Returns(true);
            mock.Setup(foo => foo.MapID).Returns(new MapId(0));
            mock.Setup(foo => foo.CollisionEnabled).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.IsColliding(testBox, new MapId(0));

            // Assert
            Assert.That(result, Is.True);
        }

        [Test]
        public void IsCollidingFalseNoLayer()
        {
            // Arrange
            var box = new Box2(5, -5, 10, 6);
            var testBox = new Box2(-3, -3, 6, 6);
            var manager = new PhysicsManager();

            var mock = new Mock<IPhysBody>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            mock.Setup(foo => foo.IsHardCollidable).Returns(true);
            mock.Setup(foo => foo.MapID).Returns(new MapId(0));
            mock.Setup(foo => foo.CollisionEnabled).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0); // Collision layer is None
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.IsColliding(testBox, new MapId(0));

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void IsCollidingWrongMap()
        {
            // Arrange
            var box = new Box2(5, -5, 10, 6);
            var testBox = new Box2(-3, -3, 5, 6);
            var manager = new PhysicsManager();

            var mock = new Mock<IPhysBody>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            mock.Setup(foo => foo.IsHardCollidable).Returns(true);
            mock.Setup(foo => foo.MapID).Returns(new MapId(3));
            mock.Setup(foo => foo.CollisionEnabled).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.IsColliding(testBox, new MapId(0));

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void RayCast()
        {
            // Arrange
            var box = new Box2(5, -5, 10, 6);
            var ray = new Ray(new Vector2(0, 1), Vector2.UnitX, 1);
            var manager = new PhysicsManager();

            var mock = new Mock<IPhysBody>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            mock.Setup(foo => foo.Owner).Returns(new Entity()); // requires IPhysBody not have null owner
            mock.Setup(foo => foo.CollisionEnabled).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(1);
            mock.Setup(foo => foo.CollisionMask).Returns(1);
            mock.Setup(foo => foo.IsHardCollidable).Returns(true);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.IntersectRay(new MapId(0), ray);

            // Assert
            Assert.That(result.DidHitObject, Is.True);
            Assert.That(result.Distance, Is.EqualTo(5));
            Assert.That(result.HitPos.X, Is.EqualTo(5));
            Assert.That(result.HitPos.Y, Is.EqualTo(1));
        }

        [Test]
        public void RayCastMissUp()
        {
            // Arrange
            var box = new Box2(5, -5, 10, 6);
            var ray = new Ray(new Vector2(4.99999f, 1), Vector2.UnitY, 1);
            var manager = new PhysicsManager();

            var mock = new Mock<IPhysBody>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            mock.Setup(foo => foo.Owner).Returns(new Entity()); // requires IPhysBody not have null owner
            mock.Setup(foo => foo.CollisionEnabled).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(1);
            mock.Setup(foo => foo.CollisionMask).Returns(1);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.IntersectRay(new MapId(0), ray);

            // Assert
            Assert.That(result.DidHitObject, Is.False);
            Assert.That(result.Distance, Is.EqualTo(0.0f));
            Assert.That(result.HitPos, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void DoCollisionTestTrue()
        {
            // Arrange
            var results = new List<IPhysBody>(1);
            var manager = new PhysicsManager();

            var mockEntity0 = new Mock<IEntity>().Object;
            var mockEntity1 = new Mock<IEntity>().Object;

            var mock0 = new Mock<IPhysBody>();
            mock0.Setup(foo => foo.WorldAABB).Returns(new Box2(-3, -3, 6, 6));
            mock0.Setup(foo => foo.IsHardCollidable).Returns(true);
            mock0.Setup(foo => foo.MapID).Returns(new MapId(1));
            mock0.Setup(foo => foo.CollisionEnabled).Returns(true);
            mock0.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock0.Setup(foo => foo.CollisionMask).Returns(0x04);
            mock0.Setup(foo => foo.Owner).Returns(mockEntity0);
            var staticBody = mock0.Object;
            manager.AddBody(staticBody);

            var mock1 = new Mock<IPhysBody>();
            mock1.Setup(foo => foo.WorldAABB).Returns(new Box2(5, -5, 10, 6));
            mock1.Setup(foo => foo.IsHardCollidable).Returns(true);
            mock1.Setup(foo => foo.MapID).Returns(new MapId(1));
            mock1.Setup(foo => foo.CollisionEnabled).Returns(true);
            mock1.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock1.Setup(foo => foo.CollisionMask).Returns(0x04);
            mock1.Setup(foo => foo.Owner).Returns(mockEntity1);
            var testBody = mock1.Object;
            manager.AddBody(testBody);

            // Act
            manager.DoCollisionTest(testBody, testBody.WorldAABB, results);

            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(staticBody));
        }
    }
}
