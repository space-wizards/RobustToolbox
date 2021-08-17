using System;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.Shared.Utility;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects
{
    public partial class EntityEventBusTests
    {

        [Test]
        public void SubscribeCompRefEvent()
        {
            // Arrange.
            var simulation = RobustServerSimulation
                .NewSimulation()
                .RegisterComponents(factory => factory.RegisterClass<DummyComponent>())
                .RegisterEntitySystems(factory => factory.LoadExtraSystemType<SubscribeCompRefEventSystem>())
                .InitializeInstance();

            var map = new MapId(1);
            simulation.AddMap(map);

            var entity = simulation.SpawnEntity(null, new MapCoordinates(0, 0, map));
            entity.AddComponent<DummyComponent>();

            // Act.
            var testEvent = new TestStructEvent {TestNumber = 5};
            var eventBus = simulation.Resolve<IEntityManager>().EventBus;
            eventBus.RaiseLocalEvent(entity.Uid, ref testEvent);

            // Check that the entity system changed the value correctly
            Assert.That(testEvent.TestNumber, Is.EqualTo(10));
        }

        [Reflect(false)]
        private class SubscribeCompRefEventSystem : EntitySystem
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
        public void SubscriptionNoMixedRefValueEvent()
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
                    factory.LoadExtraSystemType<SubscriptionNoMixedRefValueEventSystem>());

            // Act. No mixed ref and value subscriptions are allowed.
            Assert.Throws(typeof(InvalidOperationException), () => simulation.InitializeInstance());
        }

        [Reflect(false)]
        private class SubscriptionNoMixedRefValueEventSystem : EntitySystem
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
