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
    }
}
