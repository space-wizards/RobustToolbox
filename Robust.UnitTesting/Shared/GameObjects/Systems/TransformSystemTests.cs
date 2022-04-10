using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects.Systems
{
    [TestFixture, Parallelizable]
    sealed class TransformSystemTests
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

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(ent1).LocalPosition = Vector2.One;

            Assert.That(calledCount, Is.EqualTo(1));
            void MoveEventHandler(ref MoveEvent ev)
            {
                calledCount++;
                Assert.That(ev.OldPosition, Is.EqualTo(new EntityCoordinates(new EntityUid(1), Vector2.Zero)));
                Assert.That(ev.NewPosition, Is.EqualTo(new EntityCoordinates(new EntityUid(1), Vector2.One)));
            }
        }

        /// <summary>
        /// Checks that the MoverCoordinates between parent and children is correct.
        /// </summary>
        [Test]
        public void MoverCoordinatesCorrect()
        {
            var sim = SimulationFactory();
            var entManager = sim.Resolve<IEntityManager>();
            var xformSystem = sim.Resolve<IEntitySystemManager>().GetEntitySystem<SharedTransformSystem>();
            var mapId = new MapId(1);

            var parent = entManager.SpawnEntity(null, new MapCoordinates(Vector2.One, mapId));
            var xform = entManager.GetComponent<TransformComponent>(parent);
            Assert.That(xform.LocalPosition, Is.EqualTo(Vector2.One));

            var child1 = entManager.SpawnEntity(null, new MapCoordinates(Vector2.One, mapId));
            var child2 = entManager.SpawnEntity(null, new MapCoordinates(new Vector2(10f, 10f), mapId));

            var child1Xform = entManager.GetComponent<TransformComponent>(child1);
            var child2Xform = entManager.GetComponent<TransformComponent>(child2);
            child1Xform.AttachParent(xform);
            child2Xform.AttachParent(xform);

            var mover1 = xformSystem.GetMoverCoordinates(child1Xform);
            var mover2 = xformSystem.GetMoverCoordinates(child2Xform);

            Assert.That(mover1.Position, Is.EqualTo(Vector2.One));
            Assert.That(mover2.Position, Is.EqualTo(new Vector2(10f, 10f)));

            var child3 = entManager.SpawnEntity(null, new MapCoordinates(Vector2.One, mapId));
            var child3Xform = entManager.GetComponent<TransformComponent>(child3);
            child3Xform.AttachParent(child2Xform);

            Assert.That(xformSystem.GetMoverCoordinates(child3Xform).Position, Is.EqualTo(Vector2.One));
        }

        private sealed class Subscriber : IEntityEventSubscriber { }
    }
}
