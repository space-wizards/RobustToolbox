using System;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects
{
    public partial class EntityEventBusTests
    {

        [Test]
        public void SubscribeCompRefDirectedEvent()
        {
            // Arrange.
            var simulation = RobustServerSimulation
                .NewSimulation()
                .RegisterComponents(factory => factory.RegisterClass<DummyComponent>())
                .RegisterEntitySystems(factory => factory.LoadExtraSystemType<SubscribeCompRefDirectedEventSystem>())
                .InitializeInstance();

            var map = new MapId(1);
            simulation.AddMap(map);

            var entity = simulation.SpawnEntity(null, new MapCoordinates(0, 0, map));
            IoCManager.Resolve<IEntityManager>().AddComponent<DummyComponent>(entity);

            // Act.
            var testEvent = new TestStructEvent {TestNumber = 5};
            var eventBus = simulation.Resolve<IEntityManager>().EventBus;
            eventBus.RaiseLocalEvent(entity, ref testEvent);

            // Check that the entity system changed the value correctly
            Assert.That(testEvent.TestNumber, Is.EqualTo(10));
        }

        [Reflect(false)]
        private class SubscribeCompRefDirectedEventSystem : EntitySystem
        {
            public override void Initialize()
            {
                SubscribeLocalEvent<DummyComponent, TestStructEvent>(MyRefHandler);
            }

            private void MyRefHandler(EntityUid uid, DummyComponent component, ref TestStructEvent args)
            {
                Assert.That(args.TestNumber, Is.EqualTo(5));
                args.TestNumber = 10;
            }
        }

        [Test]
        public void SubscriptionNoMixedRefValueDirectedEvent()
        {
            // Arrange.
            var simulation = RobustServerSimulation
                .NewSimulation()
                .RegisterComponents(factory =>
                {
                    factory.RegisterClass<DummyComponent>();
                    factory.RegisterClass<DummyTwoComponent>();
                })
                .RegisterEntitySystems(factory =>
                    factory.LoadExtraSystemType<SubscriptionNoMixedRefValueDirectedEventSystem>());

            // Act. No mixed ref and value subscriptions are allowed.
            Assert.Throws(typeof(InvalidOperationException), () => simulation.InitializeInstance());
        }

        [Reflect(false)]
        private class SubscriptionNoMixedRefValueDirectedEventSystem : EntitySystem
        {
            public override void Initialize()
            {
                // The below is not allowed, as you're subscribing by-ref and by-value to the same event...
                SubscribeLocalEvent<DummyComponent, TestStructEvent>(MyRefHandler);
                SubscribeLocalEvent<DummyTwoComponent, TestStructEvent>(MyValueHandler);
            }

            private void MyValueHandler(EntityUid uid, DummyTwoComponent component, TestStructEvent args) { }
            private void MyRefHandler(EntityUid uid, DummyComponent component, ref TestStructEvent args) { }
        }

        [Test]
        public void SortedDirectedRefEvents()
        {
            // Arrange.
            var simulation = RobustServerSimulation
                .NewSimulation()
                .RegisterComponents(factory =>
                {
                    factory.RegisterClass<OrderComponentA>();
                    factory.RegisterClass<OrderComponentB>();
                    factory.RegisterClass<OrderComponentC>();
                })
                .RegisterEntitySystems(factory =>
                {
                    factory.LoadExtraSystemType<OrderASystem>();
                    factory.LoadExtraSystemType<OrderBSystem>();
                    factory.LoadExtraSystemType<OrderCSystem>();
                })
                .InitializeInstance();

            var map = new MapId(1);
            simulation.AddMap(map);

            var entity = simulation.SpawnEntity(null, new MapCoordinates(0, 0, map));
            IoCManager.Resolve<IEntityManager>().AddComponent<OrderComponentA>(entity);
            IoCManager.Resolve<IEntityManager>().AddComponent<OrderComponentB>(entity);
            IoCManager.Resolve<IEntityManager>().AddComponent<OrderComponentC>(entity);

            // Act.
            var testEvent = new TestStructEvent {TestNumber = 5};
            var eventBus = simulation.Resolve<IEntityManager>().EventBus;
            eventBus.RaiseLocalEvent(entity, ref testEvent);

            // Check that the entity systems changed the value correctly
            Assert.That(testEvent.TestNumber, Is.EqualTo(15));
        }

        [Reflect(false)]
        private class OrderASystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<OrderComponentA, TestStructEvent>(OnA, new[]{typeof(OrderBSystem)}, new[]{typeof(OrderCSystem)});
            }

            private void OnA(EntityUid uid, OrderComponentA component, ref TestStructEvent args)
            {
                // Second handler being ran.
                Assert.That(args.TestNumber, Is.EqualTo(0));
                args.TestNumber = 10;
            }
        }

        [Reflect(false)]
        private class OrderBSystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<OrderComponentB, TestStructEvent>(OnB, null, new []{typeof(OrderASystem)});
            }

            private void OnB(EntityUid uid, OrderComponentB component, ref TestStructEvent args)
            {
                // Last handler being ran.
                Assert.That(args.TestNumber, Is.EqualTo(10));
                args.TestNumber = 15;
            }
        }

        [Reflect(false)]
        private class OrderCSystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<OrderComponentC, TestStructEvent>(OnC);
            }

            private void OnC(EntityUid uid, OrderComponentC component, ref TestStructEvent args)
            {
                // First handler being ran.
                Assert.That(args.TestNumber, Is.EqualTo(5));
                args.TestNumber = 0;
            }
        }

        private class DummyTwoComponent : Component
        {
            public override string Name => "DummyTwo";
        }

        private struct TestStructEvent
        {
            public int TestNumber;
        }
    }
}
