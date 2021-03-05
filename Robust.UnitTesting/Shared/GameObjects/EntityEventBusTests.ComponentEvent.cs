using System;
using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;

namespace Robust.UnitTesting.Shared.GameObjects
{
    public partial class EntityEventBusTests
    {
        [Test]
        public void SubscribeCompEvent()
        {
            // Arrange
            var entUid = new EntityUid(7);
            var compInstance = new MetaDataComponent();

            var entManMock = new Mock<IEntityManager>();
            

            var compManMock = new Mock<IComponentManager>();

            IComponent? outIComponent = compInstance;
            compManMock.Setup(m => m.TryGetComponent(entUid, typeof(MetaDataComponent), out outIComponent))
                .Returns(true);

            compManMock.Setup(m => m.GetComponent(entUid, typeof(MetaDataComponent)))
                .Returns(compInstance);

            entManMock.Setup(m => m.ComponentManager).Returns(compManMock.Object);
            var bus = new ComponentEventBus(entManMock.Object);

            // Subscribe
            int calledCount = 0;
            bus.SubscribeLocalEvent<MetaDataComponent, TestEvent>(HandleTestEvent);

            // add a component to the system
            entManMock.Raise(m=>m.EntityAdded += null, entManMock.Object, entUid);
            compManMock.Raise(m => m.ComponentAdded += null, new AddedComponentEventArgs(compInstance, entUid));

            // Raise
            var evntArgs = new TestEvent(5);
            bus.RaiseLocalEvent(entUid, evntArgs);

            // Assert
            Assert.That(calledCount, Is.EqualTo(1));
            void HandleTestEvent(EntityUid uid, MetaDataComponent component, TestEvent args)
            {
                calledCount++;
                Assert.That(uid, Is.EqualTo(entUid));
                Assert.That(component, Is.EqualTo(compInstance));
                Assert.That(args.TestNumber, Is.EqualTo(5));
            }

        }

        [Test]
        public void UnsubscribeCompEvent()
        {
            // Arrange
            var entUid = new EntityUid(7);
            var compInstance = new MetaDataComponent();

            var entManMock = new Mock<IEntityManager>();


            var compManMock = new Mock<IComponentManager>();

            IComponent? outIComponent = compInstance;
            compManMock.Setup(m => m.TryGetComponent(entUid, typeof(MetaDataComponent), out outIComponent))
                .Returns(true);

            compManMock.Setup(m => m.GetComponent(entUid, typeof(MetaDataComponent)))
                .Returns(compInstance);

            entManMock.Setup(m => m.ComponentManager).Returns(compManMock.Object);
            var bus = new ComponentEventBus(entManMock.Object);

            // Subscribe
            int calledCount = 0;
            bus.SubscribeLocalEvent<MetaDataComponent, TestEvent>(HandleTestEvent);
            bus.UnsubscribeLocalEvent<MetaDataComponent, TestEvent>(HandleTestEvent);

            // add a component to the system
            entManMock.Raise(m => m.EntityAdded += null, entManMock.Object, entUid);
            compManMock.Raise(m => m.ComponentAdded += null, new AddedComponentEventArgs(compInstance, entUid));

            // Raise
            var evntArgs = new TestEvent(5);
            bus.RaiseLocalEvent(entUid, evntArgs);

            // Assert
            Assert.That(calledCount, Is.EqualTo(0));
            void HandleTestEvent(EntityUid uid, MetaDataComponent component, TestEvent args)
            {
                calledCount++;
            }

        }

        private class DummyComponent : Component
        {
            public override string Name => "Dummy";
        }

        private class TestEvent : EntitySystemMessage
        {
            public int TestNumber { get; }

            public TestEvent(int testNumber)
            {
                TestNumber = testNumber;
            }
        }
    }
}
