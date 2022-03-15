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
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.Manager;

// ReSharper disable InconsistentNaming
// ReSharper disable AccessToStaticMemberViaDerivedType
namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture, Parallelizable, TestOf(typeof(EntityCoordinates))]
    public sealed class EntityCoordinates_Tests : RobustUnitTest
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
            prototypeManager.ResolveResults();

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

            var coords = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates;

            var result = coords.IsValid(entityManager);

            Assert.That(result, Is.True);

            IoCManager.Resolve<IEntityManager>().DeleteEntity(mapEntity);

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
            var coords = IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates;

            Assert.That(coords.IsValid(entityManager), Is.False);
        }

        [Test]
        public void EntityCoordinates_Map()
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEntity = mapManager.CreateNewMapEntity(mapId);

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEntity).ParentUid.IsValid(), Is.False);
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEntity).Coordinates.EntityId, Is.EqualTo(mapEntity));
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
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEntity).Coordinates.Position, Is.EqualTo(Vector2.Zero));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEntity).LocalPosition = Vector2.One;
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEntity).Coordinates.Position, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void GetGridId_Map()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEnt = mapManager.CreateNewMapEntity(mapId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new MapCoordinates(Vector2.Zero, mapId));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEnt).Coordinates.GetGridId(entityManager), Is.EqualTo(GridId.Invalid));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.GetGridId(entityManager), Is.EqualTo(GridId.Invalid));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.EntityId, Is.EqualTo(mapEnt));
        }

        [Test]
        public void GetGridId_Grid()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var gridEnt = grid.GridEntityId;
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(gridEnt, Vector2.Zero));

            // Grids aren't parented to other grids.
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt).Coordinates.GetGridId(entityManager), Is.EqualTo(GridId.Invalid));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.GetGridId(entityManager), Is.EqualTo(grid.Index));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.EntityId, Is.EqualTo(gridEnt));
        }

        [Test]
        public void GetMapId_Map()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEnt = mapManager.CreateNewMapEntity(mapId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new MapCoordinates(Vector2.Zero, mapId));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(mapEnt).Coordinates.GetMapId(entityManager), Is.EqualTo(mapId));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.GetMapId(entityManager), Is.EqualTo(mapId));
        }

        [Test]
        public void GetMapId_Grid()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var gridEnt = grid.GridEntityId;
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(gridEnt, Vector2.Zero));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt).Coordinates.GetMapId(entityManager), Is.EqualTo(mapId));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.GetMapId(entityManager), Is.EqualTo(mapId));
        }

        [Test]
        public void GetParent()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var mapEnt = mapManager.GetMapEntityId(mapId);
            var gridEnt = grid.GridEntityId;
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(grid.GridEntityId, Vector2.Zero));

            Assert.That(entityManager.GetComponent<TransformComponent>(mapEnt).Coordinates.EntityId, Is.EqualTo(mapEnt));
            Assert.That(entityManager.GetComponent<TransformComponent>(gridEnt).Coordinates.EntityId, Is.EqualTo(mapEnt));
            Assert.That(entityManager.GetComponent<TransformComponent>(newEnt).Coordinates.EntityId, Is.EqualTo(gridEnt));

            // Reparenting the entity should return correct results.
            entityManager.GetComponent<TransformComponent>(newEnt).AttachParent(mapEnt);

            Assert.That(entityManager.GetComponent<TransformComponent>(newEnt).Coordinates.EntityId, Is.EqualTo(mapEnt));
        }

        [Test]
        public void TryGetParent()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var mapEnt = mapManager.GetMapEntityId(mapId);
            var gridEnt = grid.GridEntityId;
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(grid.GridEntityId, Vector2.Zero));

            var mapCoords = entityManager.GetComponent<TransformComponent>(mapEnt).Coordinates;
            Assert.That(mapCoords.IsValid(entityManager), Is.EqualTo(true));
            Assert.That(mapCoords.EntityId, Is.EqualTo(mapEnt));

            var gridCoords = entityManager.GetComponent<TransformComponent>(mapEnt).Coordinates;
            Assert.That(gridCoords.IsValid(entityManager), Is.EqualTo(true));
            Assert.That(gridCoords.EntityId, Is.EqualTo(mapEnt));

            var newEntTransform = entityManager.GetComponent<TransformComponent>(newEnt);
            var newEntCoords = newEntTransform.Coordinates;
            Assert.That(newEntCoords.IsValid(entityManager), Is.EqualTo(true));
            Assert.That(newEntCoords.EntityId, Is.EqualTo(gridEnt));

            // Reparenting the entity should return correct results.
            newEntTransform.AttachParent(mapEnt);
            var newEntCoords2 = newEntTransform.Coordinates;

            Assert.That(newEntCoords2.IsValid(entityManager), Is.EqualTo(true));
            Assert.That(newEntCoords2.EntityId, Is.EqualTo(mapEnt));

            // Deleting the parent should make TryGetParent return false.
            entityManager.DeleteEntity(mapEnt);

            // These shouldn't be valid anymore.
            Assert.That(entityManager.Deleted(newEnt), Is.True);
            Assert.That(entityManager.Deleted(gridEnt), Is.True);

            Assert.That(entityManager.Deleted(newEntCoords.EntityId), Is.True);
            Assert.That(entityManager.Deleted(gridCoords.EntityId), Is.True);
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
            var gridEnt = grid.GridEntityId;
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(grid.GridEntityId, entPos));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.ToMap(entityManager), Is.EqualTo(new MapCoordinates(entPos, mapId)));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt).LocalPosition += gridPos;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.ToMap(entityManager), Is.EqualTo(new MapCoordinates(entPos + gridPos, mapId)));
        }

        [Test]
        public void WithEntityId()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEnt = mapManager.GetMapEntityId(mapId);
            var grid = mapManager.CreateGrid(mapId);
            var gridEnt = grid.GridEntityId;
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(grid.GridEntityId, Vector2.Zero));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.WithEntityId(mapEnt).Position, Is.EqualTo(Vector2.Zero));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).LocalPosition = Vector2.One;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.Position, Is.EqualTo(Vector2.One));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.WithEntityId(mapEnt).Position, Is.EqualTo(Vector2.One));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(gridEnt).LocalPosition = Vector2.One;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.Position, Is.EqualTo(Vector2.One));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.WithEntityId(mapEnt).Position, Is.EqualTo(new Vector2(2, 2)));

            var newEntTwo = entityManager.CreateEntityUninitialized("dummy", new EntityCoordinates(newEnt, Vector2.Zero));

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo).Coordinates.Position, Is.EqualTo(Vector2.Zero));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo).Coordinates.WithEntityId(mapEnt).Position, Is.EqualTo(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.WithEntityId(mapEnt).Position));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo).Coordinates.WithEntityId(gridEnt).Position, Is.EqualTo(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEnt).Coordinates.Position));

            IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo).LocalPosition = -Vector2.One;

            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo).Coordinates.Position, Is.EqualTo(-Vector2.One));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo).Coordinates.WithEntityId(mapEnt).Position, Is.EqualTo(Vector2.One));
            Assert.That(IoCManager.Resolve<IEntityManager>().GetComponent<TransformComponent>(newEntTwo).Coordinates.WithEntityId(gridEnt).Position, Is.EqualTo(Vector2.Zero));
        }
    }
}
