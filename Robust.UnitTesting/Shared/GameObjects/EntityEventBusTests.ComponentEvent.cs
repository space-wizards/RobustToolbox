using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects
{
    public sealed partial class EntityEventBusTests
    {
        [Test]
        public void SubscribeCompEvent()
        {
            // Arrange
            var entUid = new EntityUid(7);
            var compInstance = new MetaDataComponent();

            var compRegistration = new Mock<IComponentRegistration>();

            var entManMock = new Mock<IEntityManager>();

            var compFacMock = new Mock<IComponentFactory>();

            compRegistration.Setup(m => m.References).Returns(new List<Type> {typeof(MetaDataComponent)});
            compFacMock.Setup(m => m.GetRegistration(typeof(MetaDataComponent))).Returns(compRegistration.Object);
            entManMock.Setup(m => m.ComponentFactory).Returns(compFacMock.Object);

            IComponent? outIComponent = compInstance;
            entManMock.Setup(m => m.TryGetComponent(entUid, typeof(MetaDataComponent), out outIComponent))
                .Returns(true);

            entManMock.Setup(m => m.GetComponent(entUid, typeof(MetaDataComponent)))
                .Returns(compInstance);

            var bus = new EntityEventBus(entManMock.Object);

            // Subscribe
            int calledCount = 0;
            bus.SubscribeLocalEvent<MetaDataComponent, TestEvent>(HandleTestEvent);

            // add a component to the system
            entManMock.Raise(m=>m.EntityAdded += null, entManMock.Object, entUid);
            entManMock.Raise(m => m.ComponentAdded += null, new AddedComponentEventArgs(compInstance, entUid));

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

            var compRegistration = new Mock<IComponentRegistration>();

            var compFacMock = new Mock<IComponentFactory>();

            compRegistration.Setup(m => m.References).Returns(new List<Type> {typeof(MetaDataComponent)});
            compFacMock.Setup(m => m.GetRegistration(typeof(MetaDataComponent))).Returns(compRegistration.Object);
            entManMock.Setup(m => m.ComponentFactory).Returns(compFacMock.Object);

            IComponent? outIComponent = compInstance;
            entManMock.Setup(m => m.TryGetComponent(entUid, typeof(MetaDataComponent), out outIComponent))
                .Returns(true);

            entManMock.Setup(m => m.GetComponent(entUid, typeof(MetaDataComponent)))
                .Returns(compInstance);

            var bus = new EntityEventBus(entManMock.Object);

            // Subscribe
            int calledCount = 0;
            bus.SubscribeLocalEvent<MetaDataComponent, TestEvent>(HandleTestEvent);
            bus.UnsubscribeLocalEvent<MetaDataComponent, TestEvent>();

            // add a component to the system
            entManMock.Raise(m => m.EntityAdded += null, entManMock.Object, entUid);
            entManMock.Raise(m => m.ComponentAdded += null, new AddedComponentEventArgs(compInstance, entUid));

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

        [Test]
        public void SubscribeCompLifeEvent()
        {
            // Arrange
            var entUid = new EntityUid(7);
            var compInstance = new MetaDataComponent();

            var entManMock = new Mock<IEntityManager>();

            compInstance.Owner = entUid;

            var compRegistration = new Mock<IComponentRegistration>();

            var compFacMock = new Mock<IComponentFactory>();

            compRegistration.Setup(m => m.References).Returns(new List<Type> {typeof(MetaDataComponent)});
            compFacMock.Setup(m => m.GetRegistration(typeof(MetaDataComponent))).Returns(compRegistration.Object);
            entManMock.Setup(m => m.ComponentFactory).Returns(compFacMock.Object);

            IComponent? outIComponent = compInstance;
            entManMock.Setup(m => m.TryGetComponent(entUid, typeof(MetaDataComponent), out outIComponent))
                .Returns(true);

            entManMock.Setup(m => m.GetComponent(entUid, typeof(MetaDataComponent)))
                .Returns(compInstance);

            var bus = new EntityEventBus(entManMock.Object);

            // Subscribe
            int calledCount = 0;
            bus.SubscribeLocalEvent<MetaDataComponent, ComponentInit>(HandleTestEvent);

            // add a component to the system
            entManMock.Raise(m=>m.EntityAdded += null, entManMock.Object, entUid);
            entManMock.Raise(m => m.ComponentAdded += null, new AddedComponentEventArgs(compInstance, entUid));

            // Raise
            ((IEventBus)bus).RaiseComponentEvent(compInstance, new ComponentInit());

            // Assert
            Assert.That(calledCount, Is.EqualTo(1));
            void HandleTestEvent(EntityUid uid, MetaDataComponent component, ComponentInit args)
            {
                calledCount++;
                Assert.That(uid, Is.EqualTo(entUid));
                Assert.That(component, Is.EqualTo(compInstance));
            }
        }

        [Test]
        public void CompEventOrdered()
        {
            // Arrange
            var entUid = new EntityUid(7);

            var entManMock = new Mock<IEntityManager>();
            var compFacMock = new Mock<IComponentFactory>();

            void Setup<T>(out T instance) where T : IComponent, new()
            {
                IComponent? inst = instance = new T();
                var reg = new Mock<IComponentRegistration>();
                reg.Setup(m => m.References).Returns(new Type[] {typeof(T)});

                compFacMock.Setup(m => m.GetRegistration(typeof(T))).Returns(reg.Object);
                entManMock.Setup(m => m.TryGetComponent(entUid, typeof(T), out inst)).Returns(true);
                entManMock.Setup(m => m.GetComponent(entUid, typeof(T))).Returns(inst);
            }

            Setup<OrderAComponent>(out var instA);
            Setup<OrderBComponent>(out var instB);
            Setup<OrderCComponent>(out var instC);

            entManMock.Setup(m => m.ComponentFactory).Returns(compFacMock.Object);
            var bus = new EntityEventBus(entManMock.Object);

            // Subscribe
            var a = false;
            var b = false;
            var c = false;

            void HandlerA(EntityUid uid, Component comp, TestEvent ev)
            {
                Assert.That(b, Is.False, "A should run before B");
                Assert.That(c, Is.False, "A should run before C");

                a = true;
            }

            void HandlerB(EntityUid uid, Component comp, TestEvent ev)
            {
                Assert.That(c, Is.True, "B should run after C");
                b = true;
            }

            void HandlerC(EntityUid uid, Component comp, TestEvent ev) => c = true;

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerA, typeof(OrderAComponent), before: new []{typeof(OrderBComponent), typeof(OrderCComponent)});
            bus.SubscribeLocalEvent<OrderBComponent, TestEvent>(HandlerB, typeof(OrderBComponent), after: new []{typeof(OrderCComponent)});
            bus.SubscribeLocalEvent<OrderCComponent, TestEvent>(HandlerC, typeof(OrderCComponent));

            // add a component to the system
            entManMock.Raise(m=>m.EntityAdded += null, entManMock.Object, entUid);
            entManMock.Raise(m => m.ComponentAdded += null, new AddedComponentEventArgs(instA, entUid));
            entManMock.Raise(m => m.ComponentAdded += null, new AddedComponentEventArgs(instB, entUid));
            entManMock.Raise(m => m.ComponentAdded += null, new AddedComponentEventArgs(instC, entUid));

            // Raise
            var evntArgs = new TestEvent(5);
            bus.RaiseLocalEvent(entUid, evntArgs);

            // Assert
            Assert.That(a, Is.True, "A did not fire");
            Assert.That(b, Is.True, "B did not fire");
            Assert.That(c, Is.True, "C did not fire");
        }

        private sealed class DummyComponent : Component
        {
        }

        private sealed class OrderAComponent : Component
        {
        }

        private sealed class OrderBComponent : Component
        {
        }

        private sealed class OrderCComponent : Component
        {
        }

        private sealed class TestEvent : EntityEventArgs
        {
            public int TestNumber { get; }

            public TestEvent(int testNumber)
            {
                TestNumber = testNumber;
            }
        }
    }
}
