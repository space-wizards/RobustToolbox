using System;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Map;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, Parallelizable, TestOf(typeof(EntityManager))]
    public class EntityManager_Tests
    {
        /// <summary>
        /// Raising a null C# delegate does not generate a NullReferenceException.
        /// </summary>
        [Test]
        public void SubscribeEvent_NullEvent_NoNullException()
        {
            // Arrange
            var manager = new TestEntityManager();
            var subscriber = new TestEventSubscriber();

            manager.SubscribeEvent((EntityEventHandler<TestEventArgs>) null, subscriber);
            
            // Act
            manager.RaiseEvent(null, new TestEventArgs());

            //Assert: this should do nothing
        }
    }

    internal class TestEventSubscriber : IEntityEventSubscriber { }

    internal class TestEntityManager : EntityManager
    {
        public override IEntity SpawnEntity(string protoName)
        {
            throw new NotImplementedException();
        }

        public override IEntity SpawnEntityNoMapInit(string protoName)
        {
            throw new NotImplementedException();
        }

        public override IEntity SpawnEntityAt(string entityType, GridCoordinates coordinates)
        {
            throw new NotImplementedException();
        }
    }

    internal class TestEventArgs : EntityEventArgs { }
}
