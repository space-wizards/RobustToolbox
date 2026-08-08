using System;
using System.Collections.Generic;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Reflection;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects
{
    internal sealed partial class EntityEventBusTests
    {
        private static (EntityEventBus Bus, EntityUid Uid, Action Attach) MultiRegistrarFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterComponents(f => f.RegisterClass<OrderAComponent>())
                .InitializeInstance();

            var entMan = sim.Resolve<EntityManager>();
            var uid = entMan.Spawn();
            var comp = entMan.AddComponent<OrderAComponent>(uid);
            var bus = entMan.EventBusInternal;
            bus.ClearSubscriptions();

            var reg = sim.Resolve<IComponentFactory>().GetRegistration(CompIdx.Index<OrderAComponent>());

            void Attach()
            {
                bus.LockSubscriptions();
                bus.OnEntityAdded(uid);
                bus.OnComponentAdded(new AddedComponentEventArgs(new ComponentEventArgs(comp, uid), reg));
            }

            return (bus, uid, Attach);
        }

        [Test]
        public void MultipleRegistrarsSubscribeToSameCompEvent()
        {
            var (bus, uid, attach) = MultiRegistrarFactory();

            var calls = new List<string>();
            void HandlerA(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("A");
            void HandlerB(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("B");

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerA, typeof(RegistrarA));
            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerB, typeof(RegistrarB), after: new[] { typeof(RegistrarA) });
            attach();

            bus.RaiseLocalEvent(uid, new TestEvent(0));

            Assert.That(calls, Is.EqualTo(new[] { "A", "B" }));
        }

        [Test]
        public void SecondRegistrarCanRunBeforeTheFirst()
        {
            var (bus, uid, attach) = MultiRegistrarFactory();

            var calls = new List<string>();
            void HandlerA(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("A");
            void HandlerB(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("B");

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerA, typeof(RegistrarA));
            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerB, typeof(RegistrarB), before: new[] { typeof(RegistrarA) });
            attach();

            bus.RaiseLocalEvent(uid, new TestEvent(0));

            Assert.That(calls, Is.EqualTo(new[] { "B", "A" }));
        }

        [Test]
        public void FirstRegistrarMayDeclareTheOrderInstead()
        {
            var (bus, uid, attach) = MultiRegistrarFactory();

            var calls = new List<string>();
            void HandlerA(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("A");
            void HandlerB(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("B");

            // Either side of the pair may be the one to declare the order
            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerA, typeof(RegistrarA), before: new[] { typeof(RegistrarB) });
            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerB, typeof(RegistrarB));
            attach();

            bus.RaiseLocalEvent(uid, new TestEvent(0));

            Assert.That(calls, Is.EqualTo(new[] { "A", "B" }));
        }

        [Test]
        public void StackedRegistrarsMustDeclareAnOrder()
        {
            var (bus, _, _) = MultiRegistrarFactory();

            void Handler(EntityUid _, OrderAComponent _1, TestEvent _2) { }

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarA));

            // No order relative to RegistrarA at all
            Assert.Throws<InvalidOperationException>(
                () => bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarB)));

            // Ordered but against an unrelated registrar
            Assert.Throws<InvalidOperationException>(
                () => bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarC), after: new[] { typeof(RegistrarD) }));
        }

        [Test]
        public void ThirdRegistrarMustOrderAgainstBothOthers()
        {
            var (bus, _, _) = MultiRegistrarFactory();

            void Handler(EntityUid _, OrderAComponent _1, TestEvent _2) { }

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarA));
            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarB), after: new[] { typeof(RegistrarA) });

            // Ordering against B alone leaves C's order relative to A undeclared
            Assert.Throws<InvalidOperationException>(
                () => bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarC), after: new[] { typeof(RegistrarB) }));

            Assert.DoesNotThrow(
                () => bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarC), after: new[] { typeof(RegistrarA), typeof(RegistrarB) }));
        }

        [Test]
        public void SameRegistrarCannotSubscribeTwiceToSameCompEvent()
        {
            var (bus, _, _) = MultiRegistrarFactory();

            void Handler(EntityUid _, OrderAComponent _1, TestEvent _2) { }

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarA));

            Assert.Throws<InvalidOperationException>(
                () => bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarA)));
        }

        [Test]
        public void UnownedSubscriptionsCannotStack()
        {
            var (bus, _, _) = MultiRegistrarFactory();

            void Handler(EntityUid _, OrderAComponent _1, TestEvent _2) { }

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler);

            Assert.Throws<InvalidOperationException>(
                () => bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarA)));

            var (otherBus, _, _) = MultiRegistrarFactory();
            otherBus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarA));

            Assert.Throws<InvalidOperationException>(
                () => otherBus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler));
        }

        [Test]
        public void UnsubscribeByOwnerKeepsOtherRegistrars()
        {
            var (bus, uid, attach) = MultiRegistrarFactory();

            var calls = new List<string>();
            void HandlerA(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("A");
            void HandlerB(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("B");
            void HandlerC(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("C");

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerA, typeof(RegistrarA));
            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerB, typeof(RegistrarB), after: new[] { typeof(RegistrarA) });
            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerC, typeof(RegistrarC), after: new[] { typeof(RegistrarA), typeof(RegistrarB) });

            bus.UnsubscribeLocalEvent<OrderAComponent, TestEvent>(typeof(RegistrarB));
            bus.UnsubscribeLocalEvent<OrderAComponent, TestEvent>(typeof(RegistrarA));

            bus.UnsubscribeLocalEvent<OrderAComponent, TestEvent>(typeof(RegistrarD));

            attach();
            bus.RaiseLocalEvent(uid, new TestEvent(0));

            Assert.That(calls, Is.EqualTo(new[] { "C" }));
        }

        [Test]
        public void UnsubscribeByOwnerRemovesLastRegistrar()
        {
            var (bus, uid, attach) = MultiRegistrarFactory();

            var called = false;
            void Handler(EntityUid _, OrderAComponent _1, TestEvent _2) => called = true;

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(Handler, typeof(RegistrarA));
            bus.UnsubscribeLocalEvent<OrderAComponent, TestEvent>(typeof(RegistrarA));

            attach();
            bus.RaiseLocalEvent(uid, new TestEvent(0));

            Assert.That(called, Is.False);
        }

        [Test]
        public void UnsubscribeWithoutOwnerRemovesEveryRegistrar()
        {
            var (bus, uid, attach) = MultiRegistrarFactory();

            var calls = new List<string>();
            void HandlerA(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("A");
            void HandlerB(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("B");

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerA, typeof(RegistrarA));
            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerB, typeof(RegistrarB), after: new[] { typeof(RegistrarA) });
            bus.UnsubscribeLocalEvent<OrderAComponent, TestEvent>();

            attach();
            bus.RaiseLocalEvent(uid, new TestEvent(0));

            Assert.That(calls, Is.Empty);
        }

        [Test]
        public void StackedHandlerSkippedWhenComponentRemoved()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .RegisterComponents(f => f.RegisterClass<OrderAComponent>())
                .InitializeInstance();

            var entMan = sim.Resolve<EntityManager>();
            var uid = entMan.Spawn();
            var comp = entMan.AddComponent<OrderAComponent>(uid);
            var bus = entMan.EventBusInternal;
            bus.ClearSubscriptions();

            var reg = sim.Resolve<IComponentFactory>().GetRegistration(CompIdx.Index<OrderAComponent>());

            var calls = new List<string>();

            void HandlerA(EntityUid _, OrderAComponent _1, TestEvent _2)
            {
                calls.Add("A");
                entMan.RemoveComponent<OrderAComponent>(uid);
            }

            void HandlerB(EntityUid _, OrderAComponent _1, TestEvent _2) => calls.Add("B");

            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerA, typeof(RegistrarA));
            bus.SubscribeLocalEvent<OrderAComponent, TestEvent>(HandlerB, typeof(RegistrarB), after: new[] { typeof(RegistrarA) });

            bus.LockSubscriptions();
            bus.OnEntityAdded(uid);
            bus.OnComponentAdded(new AddedComponentEventArgs(new ComponentEventArgs(comp, uid), reg));

            bus.RaiseLocalEvent(uid, new TestEvent(0));

            Assert.That(calls, Is.EqualTo(new[] { "A" }));
        }

        [Test]
        public void MultipleSystemsSubscribeToSameCompEvent()
        {
            var simulation = RobustServerSimulation
                .NewSimulation()
                .RegisterComponents(factory => factory.RegisterClass<DummyComponent>())
                .RegisterEntitySystems(factory =>
                {
                    factory.LoadExtraSystemType<MultiSubOneSystem>();
                    factory.LoadExtraSystemType<MultiSubTwoSystem>();
                })
                .InitializeInstance();

            var map = simulation.CreateMap().MapId;
            var entity = simulation.SpawnEntity(null, new MapCoordinates(0, 0, map));
            IoCManager.Resolve<IEntityManager>().AddComponent<DummyComponent>(entity);

            var testEvent = new TestStructEvent { TestNumber = 5 };
            simulation.Resolve<IEntityManager>().EventBus.RaiseLocalEvent(entity, ref testEvent);

            Assert.That(testEvent.TestNumber, Is.EqualTo(11));
        }

        [Reflect(false)]
        private sealed class MultiSubOneSystem : EntitySystem
        {
            public override void Initialize()
            {
                SubscribeLocalEvent<DummyComponent, TestStructEvent>(OnEvent);
            }

            private void OnEvent(EntityUid uid, DummyComponent component, ref TestStructEvent args)
            {
                args.TestNumber *= 2;
            }
        }

        [Reflect(false)]
        private sealed class MultiSubTwoSystem : EntitySystem
        {
            public override void Initialize()
            {
                SubscribeLocalEvent<DummyComponent, TestStructEvent>(OnEvent, after: new[] { typeof(MultiSubOneSystem) });
            }

            private void OnEvent(EntityUid uid, DummyComponent component, ref TestStructEvent args)
            {
                args.TestNumber++;
            }
        }

        private sealed class RegistrarA;

        private sealed class RegistrarB;

        private sealed class RegistrarC;

        private sealed class RegistrarD;
    }
}
