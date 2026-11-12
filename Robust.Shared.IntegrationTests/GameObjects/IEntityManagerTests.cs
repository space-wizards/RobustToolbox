using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.GameObjects
{
    [TestFixture, Parallelizable]
    sealed partial class EntityManagerTests
    {
        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .InitializeInstance();

            return sim;
        }

        /// <summary>
        /// The entity prototype can define field on the TransformComponent, just like any other component.
        /// </summary>
        [Test]
        public void Spawn_PrototypeTransform_Works()
        {
            var sim = SimulationFactory();

            var entMan = sim.Resolve<IEntityManager>();
            entMan.System<SharedMapSystem>().CreateMap(out var map);

            var newEnt = entMan.Spawn(null, new MapCoordinates(0, 0, map));
            Assert.That(newEnt, Is.Not.EqualTo(EntityUid.Invalid));
        }

        [Test]
        public void ComponentCount_Works()
        {
            var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

            var entManager = sim.Resolve<IEntityManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            Assert.That(entManager.Count<TransformComponent>(), Is.EqualTo(0));

            mapSystem.CreateMap(out var mapId);
            Assert.That(entManager.Count<TransformComponent>(), Is.EqualTo(1));
            mapSystem.DeleteMap(mapId);
            Assert.That(entManager.Count<TransformComponent>(), Is.EqualTo(0));
        }
    }
}
