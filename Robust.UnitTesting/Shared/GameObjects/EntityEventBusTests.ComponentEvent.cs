using System.Collections.Generic;
using Moq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Reflection;
using Robust.UnitTesting.Shared.Reflection;

namespace Robust.UnitTesting.Shared.GameObjects
{
    public sealed partial class EntityEventBusTests
    {
        [Test]
        public void SubscribeCompEvent()
        {
            var compFactory = new ComponentFactory(new DynamicTypeFactory(), new ReflectionManagerTest());

            // Arrange
            var entUid = new EntityUid(7);
            var compInstance = new MetaDataComponent();

            var entManMock = new Mock<IEntityManager>();

            compFactory.RegisterClass<MetaDataComponent>();
            entManMock.Setup(m => m.ComponentFactory).Returns(compFactory);

            IComponent? outIComponent = compInstance;
            entManMock.Setup(m => m.TryGetComponent(entUid, typeof(MetaDataComponent), out outIComponent))
                .Returns(true);

            entManMock.Setup(m => m.GetComponent(entUid, typeof(MetaDataComponent)))
                .Returns(compInstance);

            entManMock.Setup(m => m.GetComponent(entUid, CompIdx.Index<MetaDataComponent>()))
                .Returns(compInstance);

            var bus = new EntityEventBus(entManMock.Object);
            bus.OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare();

            // Subscribe
            int calledCount = 0;
            bus.SubscribeLocalEvent<MetaDataComponent, TestEvent>(HandleTestEvent);

            // add a component to the system
            bus.OnEntityAdded(entUid);
            bus.OnComponentAdded(new AddedComponentEventArgs(new ComponentEventArgs(compInstance, entUid), CompIdx.Index<MetaDataComponent>()));

            // Raise
            var evntArgs = new TestEvent(5);
            bus.RaiseLocalEvent(entUid, evntArgs, true);

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

            var compRegistration = new ComponentRegistration(
                "MetaData",
                typeof(MetaDataComponent),
                CompIdx.Index<MetaDataComponent>());

            var compFacMock = new Mock<IComponentFactory>();

            compFacMock.Setup(m => m.GetRegistration(CompIdx.Index<MetaDataComponent>())).Returns(compRegistration);
            compFacMock.Setup(m => m.GetAllRefTypes()).Returns(new[] { CompIdx.Index<MetaDataComponent>() });
            entManMock.Setup(m => m.ComponentFactory).Returns(compFacMock.Object);

            IComponent? outIComponent = compInstance;
            entManMock.Setup(m => m.TryGetComponent(entUid, typeof(MetaDataComponent), out outIComponent))
                .Returns(true);

            entManMock.Setup(m => m.GetComponent(entUid, typeof(MetaDataComponent)))
                .Returns(compInstance);

            var bus = new EntityEventBus(entManMock.Object);
            bus.OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare();

            // Subscribe
            int calledCount = 0;
            bus.SubscribeLocalEvent<MetaDataComponent, TestEvent>(HandleTestEvent);
            bus.UnsubscribeLocalEvent<MetaDataComponent, TestEvent>();

            // add a component to the system
            bus.OnEntityAdded(entUid);
            bus.OnComponentAdded(new AddedComponentEventArgs(new ComponentEventArgs(compInstance, entUid), CompIdx.Index<MetaDataComponent>()));

            // Raise
            var evntArgs = new TestEvent(5);
            bus.RaiseLocalEvent(entUid, evntArgs, true);

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

            var compRegistration = new ComponentRegistration(
                "MetaData",
                typeof(MetaDataComponent),
                CompIdx.Index<MetaDataComponent>());

            var compFacMock = new Mock<IComponentFactory>();

            compFacMock.Setup(m => m.GetRegistration(CompIdx.Index<MetaDataComponent>())).Returns(compRegistration);
            compFacMock.Setup(m => m.GetAllRefTypes()).Returns(new[] { CompIdx.Index<MetaDataComponent>() });
            entManMock.Setup(m => m.ComponentFactory).Returns(compFacMock.Object);

            IComponent? outIComponent = compInstance;
            entManMock.Setup(m => m.TryGetComponent(entUid, typeof(MetaDataComponent), out outIComponent))
                .Returns(true);

            entManMock.Setup(m => m.GetComponent(entUid, typeof(MetaDataComponent)))
                .Returns(compInstance);

            var bus = new EntityEventBus(entManMock.Object);
            bus.OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare();

            // Subscribe
            int calledCount = 0;
            bus.SubscribeLocalEvent<MetaDataComponent, ComponentInit>(HandleTestEvent);

            // add a component to the system
            entManMock.Raise(m => m.EntityAdded += null, entUid);
            entManMock.Raise(m => m.ComponentAdded += null, new AddedComponentEventArgs(new ComponentEventArgs(compInstance, entUid), CompIdx.Index<MetaDataComponent>()));

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

            List<CompIdx> allRefTypes = new();
            void Setup<T>(out T instance) where T : IComponent, new()
            {
                IComponent? inst = instance = new T();
                var reg = new ComponentRegistration(
                    typeof(T).Name,
                    typeof(T),
                    CompIdx.Index<T>());

                compFacMock.Setup(m => m.GetRegistration(CompIdx.Index<T>())).Returns(reg);
                entManMock.Setup(m => m.TryGetComponent(entUid, typeof(T), out inst)).Returns(true);
                entManMock.Setup(m => m.GetComponent(entUid, typeof(T))).Returns(inst);
                allRefTypes.Add(CompIdx.Index<T>());
            }

            Setup<OrderAComponent>(out var instA);
            Setup<OrderBComponent>(out var instB);
            Setup<OrderCComponent>(out var instC);

            compFacMock.Setup(m => m.GetAllRefTypes()).Returns(allRefTypes.ToArray());

            entManMock.Setup(m => m.ComponentFactory).Returns(compFacMock.Object);
            var bus = new EntityEventBus(entManMock.Object);
            bus.OnlyCallOnRobustUnitTestISwearToGodPleaseSomebodyKillThisNightmare();

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
            bus.OnEntityAdded(entUid);
            bus.OnComponentAdded(new AddedComponentEventArgs(new ComponentEventArgs(instA, entUid), CompIdx.Index<OrderAComponent>()));
            bus.OnComponentAdded(new AddedComponentEventArgs(new ComponentEventArgs(instB, entUid), CompIdx.Index<OrderBComponent>()));
            bus.OnComponentAdded(new AddedComponentEventArgs(new ComponentEventArgs(instC, entUid), CompIdx.Index<OrderCComponent>()));

            // Raise
            var evntArgs = new TestEvent(5);
            bus.RaiseLocalEvent(entUid, evntArgs, true);

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
