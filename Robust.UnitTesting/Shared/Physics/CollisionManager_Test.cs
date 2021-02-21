using System.Collections.Immutable;
using System.Linq;
using Moq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
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
            mock.Setup(foo => foo.MapID).Returns(new MapId(0));
            mock.Setup(foo => foo.CanCollide).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.TryCollideRect(testBox, new MapId(0));

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
            mock.Setup(foo => foo.MapID).Returns(new MapId(0));
            mock.Setup(foo => foo.CanCollide).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.TryCollideRect(testBox, new MapId(0));

            // Assert
            Assert.That(result, Is.True);
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
            mock.Setup(foo => foo.MapID).Returns(new MapId(0));
            mock.Setup(foo => foo.CanCollide).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.TryCollideRect(testBox, new MapId(0));

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
            mock.Setup(foo => foo.MapID).Returns(new MapId(0));
            mock.Setup(foo => foo.CanCollide).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0); // Collision layer is None
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.TryCollideRect(testBox, new MapId(0));

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
            mock.Setup(foo => foo.MapID).Returns(new MapId(3));
            mock.Setup(foo => foo.CanCollide).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock.Setup(foo => foo.CollisionMask).Returns(0x04);
            manager.AddBody(mock.Object);

            // Act
            var result = manager.TryCollideRect(testBox, new MapId(0));

            // Assert
            Assert.That(result, Is.False);
        }

        [Test]
        public void RayCast()
        {
            // Arrange
            var box = new Box2(5, -5, 10, 6);
            var ray = new CollisionRay(new Vector2(0, 1), Vector2.UnitX, 1);
            var manager = new PhysicsManager();

            var mock = new Mock<IPhysBody>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            mock.Setup(foo => foo.Entity).Returns(new Entity(new ServerEntityManager(), EntityUid.FirstUid)); // requires IPhysBody not have null owner
            mock.Setup(foo => foo.CanCollide).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(1);
            mock.Setup(foo => foo.CollisionMask).Returns(1);
            manager.AddBody(mock.Object);

            // Act
            var results = manager.IntersectRay(new MapId(0), ray).ToList();

            Assert.That(results.Count, Is.EqualTo(1));

            var result = results.First();

            // Assert
            Assert.That(result.Distance, Is.EqualTo(5));
            Assert.That(result.HitPos.X, Is.EqualTo(5));
            Assert.That(result.HitPos.Y, Is.EqualTo(1));
        }

        [Test]
        public void RayCastMissUp()
        {
            // Arrange
            var box = new Box2(5, -5, 10, 6);
            var ray = new CollisionRay(new Vector2(4.99999f, 1), Vector2.UnitY, 1);
            var manager = new PhysicsManager();

            var mock = new Mock<IPhysBody>();
            mock.Setup(foo => foo.WorldAABB).Returns(box);
            mock.Setup(foo => foo.Entity).Returns(new Entity(new ServerEntityManager(), EntityUid.FirstUid)); // requires IPhysBody not have null owner
            mock.Setup(foo => foo.CanCollide).Returns(true);
            mock.Setup(foo => foo.CollisionLayer).Returns(1);
            mock.Setup(foo => foo.CollisionMask).Returns(1);
            manager.AddBody(mock.Object);

            // Act
            var results = manager.IntersectRay(new MapId(0), ray);

            // Assert
            Assert.That(results.Count(), Is.EqualTo(0));
        }

        [Test]
        public void MultiHitRayCast()
        {
            // Arrange
            var b1 = new Box2(5, -5, 10, 6);
            var b2 = new Box2(6, -10, 7, 10);
            var ray = new CollisionRay(Vector2.UnitY, Vector2.UnitX, 1);
            var manager = new PhysicsManager();

            var e1 = new Entity(new ServerEntityManager(), EntityUid.FirstUid);
            var e2 = new Entity(new ServerEntityManager(), EntityUid.FirstUid);

            var m1 = new Mock<IPhysBody>();
            m1.Setup(foo => foo.WorldAABB).Returns(b1);
            m1.Setup(foo => foo.Entity).Returns(e1);
            m1.Setup(foo => foo.CanCollide).Returns(true);
            m1.Setup(foo => foo.CollisionLayer).Returns(1);
            m1.Setup(foo => foo.CollisionMask).Returns(1);
            manager.AddBody(m1.Object);

            var m2 = new Mock<IPhysBody>();
            m2.Setup(foo => foo.WorldAABB).Returns(b2);
            m2.Setup(foo => foo.Entity).Returns(e2);
            m2.Setup(foo => foo.CanCollide).Returns(true);
            m2.Setup(foo => foo.CollisionLayer).Returns(1);
            m2.Setup(foo => foo.CollisionMask).Returns(1);
            manager.AddBody(m2.Object);

            var results = manager.IntersectRay(new MapId(0), ray, returnOnFirstHit: false).ToList();

            Assert.That(results.Count, Is.EqualTo(2));
            Assert.That(results[0].HitEntity.Uid, Is.EqualTo(e1.Uid));
            Assert.That(results[1].HitEntity.Uid, Is.EqualTo(e2.Uid));
            Assert.That(results[0].Distance, Is.EqualTo(5));
            Assert.That(results[0].HitPos.X, Is.EqualTo(5));
            Assert.That(results[0].HitPos.Y, Is.EqualTo(1));
            Assert.That(results[1].Distance, Is.EqualTo(6));
            Assert.That(results[1].HitPos.X, Is.EqualTo(6));
            Assert.That(results[1].HitPos.Y, Is.EqualTo(1));
        }

        [Test]
        public void DoCollisionTestTrue()
        {
            // Arrange
            var manager = new PhysicsManager();

            var mockEntity0 = new Mock<IEntity>().Object;
            var mockEntity1 = new Mock<IEntity>().Object;

            var mock0 = new Mock<IPhysBody>();
            mock0.Setup(foo => foo.WorldAABB).Returns(new Box2(-3, -3, 6, 6));
            mock0.Setup(foo => foo.MapID).Returns(new MapId(1));
            mock0.Setup(foo => foo.CanCollide).Returns(true);
            mock0.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock0.Setup(foo => foo.CollisionMask).Returns(0x04);
            mock0.Setup(foo => foo.Entity).Returns(mockEntity0);
            var staticBody = mock0.Object;
            manager.AddBody(staticBody);

            var mock1 = new Mock<IPhysBody>();
            mock1.Setup(foo => foo.WorldAABB).Returns(new Box2(5, -5, 10, 6));
            mock1.Setup(foo => foo.MapID).Returns(new MapId(1));
            mock1.Setup(foo => foo.CanCollide).Returns(true);
            mock1.Setup(foo => foo.CollisionLayer).Returns(0x4);
            mock1.Setup(foo => foo.CollisionMask).Returns(0x04);
            mock1.Setup(foo => foo.Entity).Returns(mockEntity1);
            var testBody = mock1.Object;
            manager.AddBody(testBody);

            // Act
            var results = manager.GetCollidingEntities(testBody, Vector2.Zero).ToImmutableList();

            // Assert
            Assert.That(results.Count, Is.EqualTo(1));
            Assert.That(results[0], Is.EqualTo(mockEntity0));
        }
    }
}
