using System;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Reflection;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture]
    public partial class EntityEventBusTests
    {
        [Test]
        public void SubscribeCompRefBroadcastEvent()
        {
            // Arrange.
            var simulation = RobustServerSimulation
                .NewSimulation()
                .RegisterEntitySystems(factory => factory.LoadExtraSystemType<SubscribeCompRefBroadcastSystem>())
                .InitializeInstance();

            var ev = new TestStructEvent() {TestNumber = 5};
            simulation.Resolve<IEntityManager>().EventBus.RaiseEvent(EventSource.Local, ref ev);
            Assert.That(ev.TestNumber, Is.EqualTo(15));
        }

        [Reflect(false)]
        public class SubscribeCompRefBroadcastSystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<TestStructEvent>(OnTestEvent);
            }

            private void OnTestEvent(ref TestStructEvent ev)
            {
                Assert.That(ev.TestNumber, Is.EqualTo(5));
                ev.TestNumber += 10;
            }
        }

        [Test]
        public void SubscriptionNoMixedRefValueBroadcastEvent()
        {
            // Arrange.
            var simulation = RobustServerSimulation
                .NewSimulation()
                .RegisterEntitySystems(factory =>
                    factory.LoadExtraSystemType<SubscriptionNoMixedRefValueBroadcastEventSystem>());

            // Act. No mixed ref and value subscriptions are allowed.
            Assert.Throws(typeof(InvalidOperationException), () => simulation.InitializeInstance());
        }

        [Reflect(false)]
        private class SubscriptionNoMixedRefValueBroadcastEventSystem : EntitySystem
        {
            public override void Initialize()
            {
                // The below is not allowed, as you're subscribing by-ref and by-value to the same event...
                SubscribeLocalEvent<TestStructEvent>(MyRefHandler);
                SubscribeLocalEvent<TestStructEvent>(MyValueHandler);
            }

            private void MyValueHandler(TestStructEvent args) { }
            private void MyRefHandler(ref TestStructEvent args) { }
        }

        [Test]
        public void SortedBroadcastRefEvents()
        {
            // Arrange.
            var simulation = RobustServerSimulation
                .NewSimulation()
                .RegisterEntitySystems(factory =>
                {
                    factory.LoadExtraSystemType<BroadcastOrderASystem>();
                    factory.LoadExtraSystemType<BroadcastOrderBSystem>();
                    factory.LoadExtraSystemType<BroadcastOrderCSystem>();
                })
                .InitializeInstance();

            // Act.
            var testEvent = new TestStructEvent {TestNumber = 5};
            var eventBus = simulation.Resolve<IEntityManager>().EventBus;
            eventBus.RaiseEvent(EventSource.Local, ref testEvent);

            // Check that the entity systems changed the value correctly
            Assert.That(testEvent.TestNumber, Is.EqualTo(15));
        }

        [Reflect(false)]
        private class BroadcastOrderASystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<TestStructEvent>(OnA, new[]{typeof(BroadcastOrderBSystem)}, new[]{typeof(BroadcastOrderCSystem)});
            }

            private void OnA(ref TestStructEvent args)
            {
                // Second handler being ran.
                Assert.That(args.TestNumber, Is.EqualTo(0));
                args.TestNumber = 10;
            }
        }

        [Reflect(false)]
        private class BroadcastOrderBSystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<TestStructEvent>(OnB, null, new []{typeof(BroadcastOrderASystem)});
            }

            private void OnB(ref TestStructEvent args)
            {
                // Last handler being ran.
                Assert.That(args.TestNumber, Is.EqualTo(10));
                args.TestNumber = 15;
            }
        }

        [Reflect(false)]
        private class BroadcastOrderCSystem : EntitySystem
        {
            public override void Initialize()
            {
                base.Initialize();

                SubscribeLocalEvent<TestStructEvent>(OnC);
            }

            private void OnC(ref TestStructEvent args)
            {
                // First handler being ran.
                Assert.That(args.TestNumber, Is.EqualTo(5));
                args.TestNumber = 0;
            }
        }
    }
}
