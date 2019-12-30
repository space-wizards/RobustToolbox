using NUnit.Framework;
using Robust.Server.Interfaces.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Map;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, TestOf(typeof(MapManager))]
    class MapManager_Tests : RobustUnitTest
    {
        [SetUp]
        public void Setup()
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            mapMan.Restart();
        }

        /// <summary>
        /// When the map manager is restarted, the maps are deleted.
        /// </summary>
        [Test]
        public void Restart_ExistingMap_IsRemoved()
        {
            var mapMan = IoCManager.Resolve<IMapManager>();

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
            var mapMan = IoCManager.Resolve<IMapManager>();

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
            var mapMan = IoCManager.Resolve<IMapManager>();
            var entMan = IoCManager.Resolve<IServerEntityManager>();

            mapMan.CreateNewMapEntity(MapId.Nullspace);

            var oldEntity = (Entity)entMan.CreateEntityUninitialized(null, new MapCoordinates(Vector2.Zero, MapId.Nullspace));
            oldEntity.InitializeComponents();

            mapMan.Restart();

            Assert.That(mapMan.MapExists(MapId.Nullspace), Is.True);
            Assert.That(mapMan.GridExists(GridId.Nullspace), Is.True);
            Assert.That(oldEntity.Deleted, Is.True);

        }

        /// <summary>
        /// When using SetMapEntity, the existing entities on the map are removed, and the new map entity gets a IMapComponent.
        /// </summary>
        [Test]
        public void SetMapEntity_WithExistingEntity_ExistingEntityDeleted()
        {
            // Arrange
            var mapID = new MapId(11);
            var mapMan = IoCManager.Resolve<IMapManager>();
            var entMan = IoCManager.Resolve<IServerEntityManager>();

            mapMan.CreateMap(new MapId(7));
            mapMan.CreateMap(mapID);
            var oldMapEntity = mapMan.GetMapEntity(mapID);
            var newMapEntity = entMan.CreateEntityUninitialized(null, new MapCoordinates(Vector2.Zero, new MapId(7)));

            // Act
            mapMan.SetMapEntity(mapID, newMapEntity);

            // Assert
            Assert.That(oldMapEntity.Deleted);
            Assert.That(newMapEntity.HasComponent<IMapComponent>());

            var mapComp = newMapEntity.GetComponent<IMapComponent>();
            Assert.That(mapComp.WorldMap == mapID);
        }

        /// <summary>
        /// After creating a new map entity for nullspace, you can spawn entities into nullspace like any other map.
        /// </summary>
        [Test]
        public void SpawnEntityAt_IntoNullspace_Success()
        {
            // Arrange
            var mapMan = IoCManager.Resolve<IMapManager>();
            var entMan = IoCManager.Resolve<IEntityManager>();

            mapMan.CreateNewMapEntity(MapId.Nullspace);

            // Act
            var newEntity = entMan.SpawnEntityAt(null, new MapCoordinates(Vector2.Zero, MapId.Nullspace));

            // Assert
            Assert.That(newEntity.Transform.MapID, Is.EqualTo(MapId.Nullspace));
        }
    }
}
