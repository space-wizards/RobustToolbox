using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects.Systems
{
    [TestFixture, Parallelizable]
    class TransformSystemTests
    {
        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .InitializeInstance();

            // Adds the map with id 1, and spawns entity 1 as the map entity.
            sim.AddMap(1);

            return sim;
        }

        /// <summary>
        /// When the local position of the transform changes, a MoveEvent is raised.
        /// </summary>
        [Test]
        public void OnMove_LocalPosChanged_RaiseMoveEvent()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();

            var subscriber = new Subscriber();
            int calledCount = 0;
            entMan.EventBus.SubscribeEvent<MoveEvent>(EventSource.Local, subscriber, MoveEventHandler);
            var ent1 = entMan.SpawnEntity(null, new MapCoordinates(Vector2.Zero, new MapId(1)));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1.Uid).LocalPosition = Vector2.One;

            Assert.That(calledCount, Is.EqualTo(1));
            void MoveEventHandler(ref MoveEvent ev)
            {
                calledCount++;
                Assert.That(ev.OldPosition, Is.EqualTo(new EntityCoordinates(new EntityUid(1), Vector2.Zero)));
                Assert.That(ev.NewPosition, Is.EqualTo(new EntityCoordinates(new EntityUid(1), Vector2.One)));
            }
        }

        private class Subscriber : IEntityEventSubscriber { }
    }
}
