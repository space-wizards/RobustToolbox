using Moq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Server.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Broadphase;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, TestOf(typeof(MapManager))]
    class MapManager_Tests : RobustUnitTest
    {
        public override UnitTestProject Project => UnitTestProject.Server;

        protected override void OverrideIoC()
        {
            base.OverrideIoC();
            var mock = new Mock<IEntitySystemManager>();
            var broady = new BroadPhaseSystem();
            var physics = new PhysicsSystem();
            mock.Setup(m => m.GetEntitySystem<SharedBroadphaseSystem>()).Returns(broady);
            mock.Setup(m => m.GetEntitySystem<SharedPhysicsSystem>()).Returns(physics);

            IoCManager.RegisterInstance<IEntitySystemManager>(mock.Object, true);
        }

        [SetUp]
        public void Setup()
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            mapMan.Startup();
        }

        [TearDown]
        public void TearDown()
        {
            var mapMan = IoCManager.Resolve<IMapManager>();
            mapMan.Shutdown();
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
            var entMan = IoCManager.Resolve<IEntityManager>();

            mapMan.CreateNewMapEntity(MapId.Nullspace);

            var oldEntity = entMan.CreateEntityUninitialized(null, MapCoordinates.Nullspace);
            entMan.InitializeComponents(oldEntity.Uid);

            mapMan.Restart();

            Assert.That(mapMan.MapExists(MapId.Nullspace), Is.True);
            Assert.That(mapMan.GridExists(GridId.Invalid), Is.False);
            Assert.That((!IoCManager.Resolve<IEntityManager>().EntityExists(oldEntity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(oldEntity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted, Is.True);

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
            var entMan = IoCManager.Resolve<IEntityManager>();

            mapMan.CreateMap(new MapId(7));
            mapMan.CreateMap(mapID);
            var oldMapEntity = mapMan.GetMapEntity(mapID);
            var newMapEntity = entMan.CreateEntityUninitialized(null, new MapCoordinates(Vector2.Zero, new MapId(7)));

            // Act
            mapMan.SetMapEntity(mapID, newMapEntity);

            // Assert
            Assert.That((!IoCManager.Resolve<IEntityManager>().EntityExists(oldMapEntity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(oldMapEntity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted);
            Assert.That(IoCManager.Resolve<IEntityManager>().HasComponent<IMapComponent>(newMapEntity.Uid));

            var mapComp = IoCManager.Resolve<IEntityManager>().GetComponent<IMapComponent>(newMapEntity.Uid);
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
            var newEntity = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

            // Assert
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntity.Uid).MapID, Is.EqualTo(MapId.Nullspace));
        }

        [Test]
        public void Restart_MapEntity_IsRemoved()
        {
            var mapMan = IoCManager.Resolve<IMapManager>();

            var entity = mapMan.CreateNewMapEntity(MapId.Nullspace);

            mapMan.Restart();

            Assert.That(mapMan.MapExists(MapId.Nullspace), Is.True);
            Assert.That((!IoCManager.Resolve<IEntityManager>().EntityExists(entity.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(entity.Uid).EntityLifeStage) >= EntityLifeStage.Deleted, Is.True);
            Assert.That(mapMan.GetMapEntityId(MapId.Nullspace), Is.EqualTo(EntityUid.Invalid));
        }
    }
}
