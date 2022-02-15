using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
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

            var mapID = new MapId(11);
            mapMan.CreateMap(mapID);

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

            var mapID = new MapId(11);
            var gridId = new GridId(7);
            mapMan.CreateMap(mapID);
            mapMan.CreateGrid(mapID, gridId);

            mapMan.Restart();

            Assert.That(mapMan.GridExists(gridId), Is.False);
        }

        /// <summary>
        /// When the map manager is restarted, Nullspace is recreated.
        /// </summary>
        [Test]
        public void Restart_NullspaceMap_IsEmptied()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            mapMan.CreateNewMapEntity(MapId.Nullspace);

            var oldEntity = entMan.CreateEntityUninitialized(null, MapCoordinates.Nullspace);
            entMan.InitializeComponents(oldEntity);

            mapMan.Restart();

            Assert.That(mapMan.MapExists(MapId.Nullspace), Is.True);
            Assert.That(mapMan.GridExists(GridId.Invalid), Is.False);
            Assert.That(entMan.Deleted(oldEntity), Is.True);

        }

        /// <summary>
        /// When using SetMapEntity, the existing entities on the map are removed, and the new map entity gets a IMapComponent.
        /// </summary>
        [Test]
        public void SetMapEntity_WithExistingEntity_ExistingEntityDeleted()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var mapID = new MapId(11);

            mapMan.CreateMap(new MapId(7));
            mapMan.CreateMap(mapID);
            var oldMapEntity = mapMan.GetMapEntityId(mapID);
            var newMapEntity = entMan.CreateEntityUninitialized(null, new MapCoordinates(Vector2.Zero, new MapId(7)));

            // Act
            mapMan.SetMapEntity(mapID, newMapEntity);

            // Assert
            Assert.That(entMan.Deleted(oldMapEntity));
            Assert.That(entMan.HasComponent<IMapComponent>(newMapEntity));

            var mapComp = entMan.GetComponent<IMapComponent>(newMapEntity);
            Assert.That(mapComp.WorldMap == mapID);
        }

        /// <summary>
        /// After creating a new map entity for nullspace, you can spawn entities into nullspace like any other map.
        /// </summary>
        [Test]
        public void SpawnEntityAt_IntoNullspace_Success()
        {
            // Arrange
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            mapMan.CreateNewMapEntity(MapId.Nullspace);

            // Act
            var newEntity = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

            // Assert
            Assert.That(entMan.GetComponent<TransformComponent>(newEntity).MapID, Is.EqualTo(MapId.Nullspace));
        }

        [Test]
        public void Restart_MapEntity_IsRemoved()
        {
            var sim = SimulationFactory();
            var entMan = sim.Resolve<IEntityManager>();
            var mapMan = sim.Resolve<IMapManager>();

            var entity = mapMan.CreateNewMapEntity(MapId.Nullspace);

            mapMan.Restart();

            Assert.That(mapMan.MapExists(MapId.Nullspace), Is.True);
            Assert.That((!entMan.EntityExists(entity) ? EntityLifeStage.Deleted : entMan.GetComponent<MetaDataComponent>(entity).EntityLifeStage) >= EntityLifeStage.Deleted, Is.True);
            Assert.That(mapMan.GetMapEntityId(MapId.Nullspace), Is.EqualTo(EntityUid.Invalid));
        }
    }
}
