using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
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
        public void SpawnEntity_PrototypeTransform_Works()
        {
            var sim = SimulationFactory();
            var map = sim.CreateMap().MapId;

            var entMan = sim.Resolve<IEntityManager>();
            var newEnt = entMan.SpawnEntity(null, new MapCoordinates(0, 0, map));
            Assert.That(newEnt, Is.Not.EqualTo(EntityUid.Invalid));
        }

        /// <summary>
        /// The entity prototype can define field on the TransformComponent, just like any other component.
        /// </summary>
        [Test]
        public void EntityQuery3_Works()
        {
            var sim = SimulationFactory();
            var map = sim.CreateMap();
            var map2 = sim.CreateMap();

            var entMan = sim.Resolve<IEntityManager>();
            // Query all maps.
            var query = entMan.GetEntityQuery<TransformComponent, MetaDataComponent, MapComponent>();

            Assert.That(query.Count(), NUnit.Framework.Is.EqualTo(2));

            Assert.That(query.Matches(map.Uid));

            foreach (var entity in query)
            {
                Assert.That(entity.Owner, NUnit.Framework.Is.EqualTo(map.Uid).Or.EqualTo(map2.Uid));
                Assert.That(entity.Comp1, NUnit.Framework.Is.TypeOf<TransformComponent>());
                Assert.That(entity.Comp2, NUnit.Framework.Is.TypeOf<MetaDataComponent>());
                Assert.That(entity.Comp3, NUnit.Framework.Is.TypeOf<MapComponent>());
#pragma warning disable CS0618 // Type or member is obsolete
                Assert.That(entity.Comp1.Owner, NUnit.Framework.Is.EqualTo(entity.Owner));
                Assert.That(entity.Comp2.Owner, NUnit.Framework.Is.EqualTo(entity.Owner));
                Assert.That(entity.Comp3.Owner, NUnit.Framework.Is.EqualTo(entity.Owner));
#pragma warning restore CS0618 // Type or member is obsolete
            }
        }

        [Test]
        public void ComponentCount_Works()
        {
            var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

            var entManager = sim.Resolve<IEntityManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            Assert.That(entManager.Count<TransformComponent>(), Is.EqualTo(0));

            var mapId = sim.CreateMap().MapId;
            Assert.That(entManager.Count<TransformComponent>(), Is.EqualTo(1));
            mapSystem.DeleteMap(mapId);
            Assert.That(entManager.Count<TransformComponent>(), Is.EqualTo(0));
        }
    }
}
