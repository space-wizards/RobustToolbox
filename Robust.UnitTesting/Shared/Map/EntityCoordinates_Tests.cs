using System.IO;
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
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType
namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, Parallelizable, TestOf(typeof(EntityCoordinates))]
    public class EntityCoordinates_Tests : RobustUnitTest
    {
        private const string PROTOTYPES = @"
- type: entity
  name: dummy
  id: dummy
  components:
  - type: Transform";

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

        [OneTimeSetUp]
        public void Setup()
        {
            IoCManager.Resolve<ISerializationManager>().Initialize();
            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.RegisterType(typeof(EntityPrototype));
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            prototypeManager.Resync();

            var factory = IoCManager.Resolve<IComponentFactory>();
            factory.GenerateNetIds();
        }

        /// <summary>
        ///     Passing an invalid entity ID into the constructor makes it invalid.
        /// </summary>
        [Test]
        public void IsValid_InvalidEntId_False()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();

            // Same as EntityCoordinates.Invalid
            var coords = new EntityCoordinates(EntityUid.Invalid, Vector2.Zero);

            Assert.That(coords.IsValid(entityManager), Is.False);
        }

        /// <summary>
        ///     Deleting the parent entity should make the coordinates invalid.
        /// </summary>
        [Test]
        public void IsValid_EntityDeleted_False()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEntity = mapManager.CreateNewMapEntity(mapId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new MapCoordinates(Vector2.Zero, mapId));

            var coords = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates;

            var result = coords.IsValid(entityManager);

            Assert.That(result, Is.True);

            IoCManager.Resolve<IEntityManager>().DeleteEntity(mapEntity.Uid);

            result = coords.IsValid(entityManager);

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     Passing a valid entity ID into the constructor with infinite numbers makes it invalid.
        /// </summary>
        [TestCase(float.NaN, float.NaN)]
        [TestCase(0, float.NaN)]
        [TestCase(float.NaN, 0)]
        public void IsValid_NonFiniteVector_False(float x, float y)
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEntity = mapManager.CreateNewMapEntity(mapId);

            var newEnt = entityManager.CreateEntityUninitialized("dummy", new MapCoordinates(new Vector2(x, y), mapId));
            var coords = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates;

            Assert.That(coords.IsValid(entityManager), Is.False);
        }

        [Test]
        public void EntityCoordinates_Map()
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEntity = mapManager.CreateNewMapEntity(mapId);

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEntity.Uid).ParentUid.IsValid(), Is.False);
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEntity.Uid).Coordinates.EntityId, Is.EqualTo(mapEntity.Uid));
        }

        /// <summary>
        ///     Even if we change the local position of an entity without a parent,
        ///     EntityCoordinates should still return offset 0.
        /// </summary>
        [Test]
        public void NoParent_OffsetZero()
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEntity = mapManager.CreateNewMapEntity(mapId);
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEntity.Uid).Coordinates.Position, Is.EqualTo(Vector2.Zero));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEntity.Uid).LocalPosition = Vector2.One;
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEntity.Uid).Coordinates.Position, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void GetGridId_Map()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEnt = mapManager.CreateNewMapEntity(mapId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new MapCoordinates(Vector2.Zero, mapId));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEnt.Uid).Coordinates.GetGridId(entityManager), Is.EqualTo(GridId.Invalid));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.GetGridId(entityManager), Is.EqualTo(GridId.Invalid));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.EntityId, Is.EqualTo(mapEnt.Uid));
        }

        [Test]
        public void GetGridId_Grid()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var gridEnt = entityManager.GetEntity(grid.GridEntityId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(grid.GridEntityId, Vector2.Zero));

            // Grids aren't parented to other grids.
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt.Uid).Coordinates.GetGridId(entityManager), Is.EqualTo(GridId.Invalid));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.GetGridId(entityManager), Is.EqualTo(grid.Index));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.EntityId, Is.EqualTo(gridEnt.Uid));
        }

        [Test]
        public void GetMapId_Map()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEnt = mapManager.CreateNewMapEntity(mapId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new MapCoordinates(Vector2.Zero, mapId));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEnt.Uid).Coordinates.GetMapId(entityManager), Is.EqualTo(mapId));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.GetMapId(entityManager), Is.EqualTo(mapId));
        }

        [Test]
        public void GetMapId_Grid()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var gridEnt = entityManager.GetEntity(grid.GridEntityId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(grid.GridEntityId, Vector2.Zero));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt.Uid).Coordinates.GetMapId(entityManager), Is.EqualTo(mapId));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.GetMapId(entityManager), Is.EqualTo(mapId));
        }

        [Test]
        public void GetParent()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var mapEnt = mapManager.GetMapEntity(mapId);
            var gridEnt = entityManager.GetEntity(grid.GridEntityId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(grid.GridEntityId, Vector2.Zero));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEnt.Uid).Coordinates.GetEntity(entityManager), Is.EqualTo(mapEnt));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt.Uid).Coordinates.GetEntity(entityManager), Is.EqualTo(mapEnt));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.GetEntity(entityManager), Is.EqualTo(gridEnt));

            // Reparenting the entity should return correct results.
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).AttachParent(mapEnt);

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.GetEntity(entityManager), Is.EqualTo(mapEnt));
        }

        [Test]
        public void TryGetParent()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var mapEnt = mapManager.GetMapEntity(mapId);
            var gridEnt = entityManager.GetEntity(grid.GridEntityId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(grid.GridEntityId, Vector2.Zero));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEnt.Uid).Coordinates.TryGetEntity(entityManager, out var mapEntParent), Is.EqualTo(true));
            Assert.That(mapEntParent, Is.EqualTo(mapEnt));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt.Uid).Coordinates.TryGetEntity(entityManager, out var gridEntParent), Is.EqualTo(true));
            Assert.That(gridEntParent, Is.EqualTo(mapEnt));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.TryGetEntity(entityManager, out var newEntParent), Is.EqualTo(true));
            Assert.That(newEntParent, Is.EqualTo(gridEnt));

            // Reparenting the entity should return correct results.
            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).AttachParent(mapEnt);

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.TryGetEntity(entityManager, out newEntParent), Is.EqualTo(true));
            Assert.That(newEntParent, Is.EqualTo(mapEnt));

            // Deleting the parent should make TryGetParent return false.
            IoCManager.Resolve<IEntityManager>().DeleteEntity(mapEnt.Uid);

            // These shouldn't be valid anymore.
            Assert.That((!IoCManager.Resolve<IEntityManager>().EntityExists(newEnt.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(newEnt.Uid).EntityLifeStage) >= EntityLifeStage.Deleted, Is.True);
            Assert.That((!IoCManager.Resolve<IEntityManager>().EntityExists(gridEnt.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(gridEnt.Uid).EntityLifeStage) >= EntityLifeStage.Deleted, Is.True);

            Assert.That((!IoCManager.Resolve<IEntityManager>().EntityExists(newEntParent!.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(newEntParent!.Uid).EntityLifeStage) >= EntityLifeStage.Deleted, Is.True);
            Assert.That((!IoCManager.Resolve<IEntityManager>().EntityExists(gridEntParent!.Uid) ? EntityLifeStage.Deleted : IoCManager.Resolve<IEntityManager>().GetComponent<MetaDataComponent>(gridEntParent!.Uid).EntityLifeStage) >= EntityLifeStage.Deleted, Is.True);
        }

        [Test]
        [TestCase(0, 0, 0, 0)]
        [TestCase(5, 0, 0, 0)]
        [TestCase(5, 0, 0, 5)]
        [TestCase(-5, 5, 5, -5)]
        [TestCase(100, -500, -200, -300)]
        public void ToMap_MoveGrid(float x1, float y1, float x2, float y2)
        {
            var gridPos = new Vector2(x1, y1);
            var entPos = new Vector2(x2, y2);

            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var gridEnt = entityManager.GetEntity(grid.GridEntityId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(grid.GridEntityId, entPos));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.ToMap(entityManager), Is.EqualTo(new MapCoordinates(entPos, mapId)));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt.Uid).LocalPosition += gridPos;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.ToMap(entityManager), Is.EqualTo(new MapCoordinates(entPos + gridPos, mapId)));
        }

        [Test]
        public void WithEntityId()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEnt = mapManager.GetMapEntity(mapId);
            var grid = mapManager.CreateGrid(mapId);
            var gridEnt = entityManager.GetEntity(grid.GridEntityId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(grid.GridEntityId, Vector2.Zero));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.WithEntityId(mapEnt).Position, Is.EqualTo(Vector2.Zero));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).LocalPosition = Vector2.One;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.Position, Is.EqualTo(Vector2.One));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.WithEntityId(mapEnt).Position, Is.EqualTo(Vector2.One));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt.Uid).LocalPosition = Vector2.One;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.Position, Is.EqualTo(Vector2.One));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.WithEntityId(mapEnt).Position, Is.EqualTo(new Vector2(2, 2)));

            var newEntTwo = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(newEnt.Uid, Vector2.Zero));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo.Uid).Coordinates.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo.Uid).Coordinates.WithEntityId(mapEnt).Position, Is.EqualTo(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.WithEntityId(mapEnt).Position));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo.Uid).Coordinates.WithEntityId(gridEnt).Position, Is.EqualTo(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt.Uid).Coordinates.Position));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo.Uid).LocalPosition = -Vector2.One;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo.Uid).Coordinates.Position, Is.EqualTo(-Vector2.One));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo.Uid).Coordinates.WithEntityId(mapEnt).Position, Is.EqualTo(Vector2.One));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo.Uid).Coordinates.WithEntityId(gridEnt).Position, Is.EqualTo(Vector2.Zero));
        }
    }
}
