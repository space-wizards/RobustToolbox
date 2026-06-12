using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, TestOf(typeof(MapManager))]
    internal sealed class MapManagerTests
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
            var entMan = sim.Resolve<IEntityManager>();
            var mapSys = entMan.System<SharedMapSystem>();

            var mapID = sim.CreateMap().MapId;

            mapMan.Restart();

            Assert.That(mapSys.MapExists(mapID), Is.False);
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
            entMan.InitializeEntity(oldEntity);
            entMan.FlushEntities();
            Assert.That(entMan.Deleted(oldEntity), Is.True);
        }

        [Test]
        public void MapEntity_HasMapFlag()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapSys = entMan.System<SharedMapSystem>();

            var entity = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            var xform = entMan.GetComponent<TransformComponent>(entity);

            Assert.Multiple(() =>
            {
                Assert.That(xform.IsMap, Is.False);
                Assert.That(xform.IsGrid, Is.False);
                Assert.That(mapSys.IsMap(entity, xform), Is.False);
                Assert.That(mapSys.IsGrid(entity, xform), Is.False);
            });

            var map = mapSys.CreateMap();
            var mapXform = entMan.GetComponent<TransformComponent>(map);

            Assert.Multiple(() =>
            {
                Assert.That(mapXform.IsMap, Is.True);
                Assert.That(mapXform.IsGrid, Is.False);
                Assert.That(mapSys.IsMap(map, mapXform), Is.True);
                Assert.That(mapSys.IsGrid(map, mapXform), Is.False);
            });
        }

        [Test]
        public void GridEntity_HasGridFlag()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();
            var mapSys = entMan.System<SharedMapSystem>();

            var map = sim.CreateMap();
            var grid = mapMan.CreateGridEntity(map.MapId);
            var gridXform = entMan.GetComponent<TransformComponent>(grid);

            Assert.Multiple(() =>
            {
                Assert.That(gridXform.IsMap, Is.False);
                Assert.That(gridXform.IsGrid, Is.True);
                Assert.That(mapSys.IsMap(grid.Owner, gridXform), Is.False);
                Assert.That(mapSys.IsGrid(grid.Owner, gridXform), Is.True);
            });
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
