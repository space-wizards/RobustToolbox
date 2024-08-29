using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, TestOf(typeof(MapManager))]
    public sealed class MapManagerTests
    {
        private static ISimulation SimulationFactory()
        {
            var sim = RobustServerSimulation
                .NewSimulation()
                .InitializeInstance();

            return sim;
        }

        /// <summary>
        /// When the map manager is restarted, the maps are deleted.
        /// </summary>
        [Test]
        public void Restart_ExistingMap_IsRemoved()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();

            var mapID = sim.CreateMap().MapId;

            mapMan.Restart();

            Assert.That(mapMan.MapExists(mapID), Is.False);
        }

        /// <summary>
        /// When the map manager is restarted, the grids are removed.
        /// </summary>
        [Test]
        public void Restart_ExistingGrid_IsRemoved()
        {
            var sim = SimulationFactory();
            var mapMan = sim.Resolve<IMapManager>();
            var entMan = sim.Resolve<IEntityManager>();

            var mapID = sim.CreateMap().MapId;
            var grid = mapMan.CreateGridEntity(mapID);

            mapMan.Restart();

            Assert.That(entMan.HasComponent<MapGridComponent>(grid), Is.False);
        }

        /// <summary>
        /// When entities are flushed check nullsapce is also culled.
        /// </summary>
        [Test]
        public void Restart_NullspaceMap_IsEmptied()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var oldEntity = entMan.CreateEntityUninitialized(null, MapCoordinates.Nullspace);
            entMan.InitializeComponents(oldEntity);
            entMan.FlushEntities();
            Assert.That(entMan.Deleted(oldEntity), Is.True);
        }

        [Test]
        public void Restart_MapEntity_IsRemoved()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();
            var entity = entMan.System<SharedMapSystem>().CreateMap();
            mapMan.Restart();
            Assert.That((!entMan.EntityExists(entity) ? EntityLifeStage.Deleted : entMan.GetComponent<MetaDataComponent>(entity).EntityLifeStage) >= EntityLifeStage.Deleted, Is.True);
        }
    }
}
