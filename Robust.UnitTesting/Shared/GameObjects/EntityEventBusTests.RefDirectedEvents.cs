using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
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

            var map = simulation.CreateMap().MapId;
            var entity = simulation.SpawnEntity(null, new MapCoordinates(0, 0, map));
            IoCManager.Resolve<IEntityManager>().AddComponent<DummyComponent>(entity);

            // Act.
            var testEvent = new TestStructEvent {TestNumber = 5};
            var eventBus = simulation.Resolve<IEntityManager>().EventBus;
            eventBus.RaiseLocalEvent(entity, ref testEvent, true);

            // Check that the entity system changed the value correctly
            Assert.That(testEvent.TestNumber, Is.EqualTo(10));
        }

        [Reflect(false)]
        private sealed class SubscribeCompRefDirectedEventSystem : EntitySystem
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

        [Reflect(false)]
        private sealed class SubscriptionNoMixedRefValueDirectedEventSystem : EntitySystem
        {
            public override void Initialize()
            {
                // The below is not allowed, as you're subscribing by-ref and by-value to the same event...
                SubscribeLocalEvent<DummyComponent, TestStructEvent>(MyRefHandler);
#pragma warning disable RA0013
                SubscribeLocalEvent<DummyTwoComponent, TestStructEvent>(MyValueHandler);
#pragma warning restore RA0013
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
                    factory.RegisterClass<OrderAComponent>();
                    factory.RegisterClass<OrderBComponent>();
                    factory.RegisterClass<OrderCComponent>();
                })
                .RegisterEntitySystems(factory =>
                {
                    factory.LoadExtraSystemType<OrderASystem>();
                    factory.LoadExtraSystemType<OrderBSystem>();
                    factory.LoadExtraSystemType<OrderCSystem>();
                })
                .InitializeInstance();

            var map = simulation.CreateMap().MapId;
            var entity = simulation.SpawnEntity(null, new MapCoordinates(0, 0, map));
            IoCManager.Resolve<IEntityManager>().AddComponent<OrderAComponent>(entity);
            IoCManager.Resolve<IEntityManager>().AddComponent<OrderBComponent>(entity);
            IoCManager.Resolve<IEntityManager>().AddComponent<OrderCComponent>(entity);

            // Act.
            var testEvent = new TestStructEvent {TestNumber = 5};
            var eventBus = simulation.Resolve<IEntityManager>().EventBus;
            eventBus.RaiseLocalEvent(entity, ref testEvent, true);

            // Check that the entity systems changed the value correctly
            Assert.That(testEvent.TestNumber, Is.EqualTo(15));
        }

        [Reflect(false)]
        private sealed class OrderASystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<OrderAComponent, TestStructEvent>(OnA, new[]{typeof(OrderBSystem)}, new[]{typeof(OrderCSystem)});
            }

            private void OnA(EntityUid uid, OrderAComponent component, ref TestStructEvent args)
            {
                // Second handler being ran.
                Assert.That(args.TestNumber, Is.EqualTo(0));
                args.TestNumber = 10;
            }
        }

        [Reflect(false)]
        private sealed class OrderBSystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<OrderBComponent, TestStructEvent>(OnB, null, new []{typeof(OrderASystem)});
            }

            private void OnB(EntityUid uid, OrderBComponent component, ref TestStructEvent args)
            {
                // Last handler being ran.
                Assert.That(args.TestNumber, Is.EqualTo(10));
                args.TestNumber = 15;
            }
        }

        [Reflect(false)]
        private sealed class OrderCSystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<OrderCComponent, TestStructEvent>(OnC);
            }

            private void OnC(EntityUid uid, OrderCComponent component, ref TestStructEvent args)
            {
                // First handler being ran.
                Assert.That(args.TestNumber, Is.EqualTo(5));
                args.TestNumber = 0;
            }
        }

        private sealed partial class DummyTwoComponent : Component
        {
        }

        [ByRefEvent]
        private struct TestStructEvent
        {
            public int TestNumber;
        }
    }
}
