using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Moq;
using NUnit.Framework;
using Robust.Server.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Interfaces.GameObjects;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Prototypes;
using Is = Robust.UnitTesting.Is;

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

        [OneTimeSetUp]
        public void Setup()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            entityManager.Initialize();

            var mapManager = IoCManager.Resolve<IMapManager>();

            mapManager.Initialize();
            mapManager.Startup();

            var prototypeManager = IoCManager.Resolve<IPrototypeManager>();
            prototypeManager.LoadFromStream(new StringReader(PROTOTYPES));
            prototypeManager.Resync();
        }

        /// <summary>
        ///     Passing an invalid entity ID into the constructor makes it throw.
        /// </summary>
        [Test]
        public void IsValid_InvalidEntId_False()
        {
            Assert.Throws<ArgumentException>(() =>
            {
                var _ = new EntityCoordinates(EntityUid.Invalid, Vector2.Zero);
            });
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

            var coords = newEnt.Transform.Coordinates;

            var result = coords.IsValid(entityManager);

            Assert.That(result, Is.True);

            mapEntity.Delete();

            result = coords.IsValid(entityManager);

            Assert.That(result, Is.False);
        }

        /// <summary>
        ///     Passing a valid entity ID into the constructor with infinite numbers makes it throw.
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

            Assert.Throws<ArgumentOutOfRangeException>(() =>
            {
                var newEnt = entityManager.CreateEntityUninitialized("dummy", new MapCoordinates(new Vector2(x, y), mapId));
                var coords = newEnt.Transform.Coordinates;
            });
        }

        [Test]
        public void EntityCoordinates_Map()
        {
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEntity = mapManager.CreateNewMapEntity(mapId);

            Assert.That(mapEntity.Transform.ParentUid.IsValid(), Is.False);
            Assert.That(mapEntity.Transform.Coordinates.EntityId, Is.EqualTo(mapEntity.Uid));
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
            Assert.That(mapEntity.Transform.Coordinates.Position, Is.EqualTo(Vector2.Zero));

            mapEntity.Transform.LocalPosition = Vector2.One;
            Assert.That(mapEntity.Transform.Coordinates.Position, Is.EqualTo(Vector2.Zero));
        }

        [Test]
        public void GetGridId_Map()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var mapEntity = mapManager.CreateNewMapEntity(mapId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new MapCoordinates(Vector2.Zero, mapId));

            Assert.That(mapEntity.Transform.Coordinates.GetGridId(entityManager), Is.EqualTo(GridId.Invalid));
            Assert.That(newEnt.Transform.Coordinates.GetGridId(entityManager), Is.EqualTo(GridId.Invalid));
            Assert.That(newEnt.Transform.Coordinates.EntityId, Is.EqualTo(mapEntity.Uid));
        }

        [Test]
        public void GetGridId_Grid()
        {
            var entityManager = IoCManager.Resolve<IEntityManager>();
            var mapManager = IoCManager.Resolve<IMapManager>();

            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);
            var gridEnt = entityManager.GetEntity(grid.GridEntityId);
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new GridCoordinates(Vector2.Zero, grid.Index));

            // Grids aren't parented to other grids.
            Assert.That(gridEnt.Transform.Coordinates.GetGridId(entityManager), Is.EqualTo(GridId.Invalid));
            Assert.That(newEnt.Transform.Coordinates.GetGridId(entityManager), Is.EqualTo(grid.Index));
            Assert.That(newEnt.Transform.Coordinates.EntityId, Is.EqualTo(gridEnt.Uid));
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
            var newEnt = entityManager.CreateEntityUninitialized("dummy", new GridCoordinates(entPos, grid.Index));

            Assert.That(newEnt.Transform.Coordinates.ToMap(entityManager), Is.EqualTo(new MapCoordinates(entPos, mapId)));

            gridEnt.Transform.LocalPosition += gridPos;

            Assert.That(newEnt.Transform.Coordinates.ToMap(entityManager), Is.EqualTo(new MapCoordinates(entPos + gridPos, mapId)));
        }
    }
}
