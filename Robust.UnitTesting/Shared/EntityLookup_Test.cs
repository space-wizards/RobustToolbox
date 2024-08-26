using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared
{
    [TestFixture, TestOf(typeof(EntityLookupSystem))]
    public sealed class EntityLookupTest
    {
        private static readonly MapId _mapId = new MapId(1);

        private EntityUid GetPhysicsEntity(IEntityManager entManager, MapCoordinates spawnPos)
        {
            var ent = entManager.SpawnEntity(null, spawnPos);
            var comp = entManager.AddComponent<PhysicsComponent>(ent);
            entManager.System<FixtureSystem>().TryCreateFixture(ent, new PhysShapeCircle(0.35f, Vector2.Zero), "fix1");
            return ent;
        }

        private Entity<MapGridComponent> SetupGrid(MapId mapId, SharedMapSystem mapSystem, IEntityManager entManager, IMapManager mapManager)
        {
            mapSystem.CreateMap(mapId);
            var grid = mapManager.CreateGridEntity(mapId);
            entManager.System<SharedTransformSystem>().SetLocalPosition(grid.Owner, new Vector2(10f, 10f));
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            return grid;
        }

        private static readonly TestCaseData[] MapInRangeCases = new[]
        {
            new TestCaseData(new MapCoordinates(Vector2.One, _mapId), new MapCoordinates(Vector2.Zero, _mapId), 0.5f, false),
            new TestCaseData(new MapCoordinates(Vector2.One, _mapId), new MapCoordinates(Vector2.One, _mapId), 0.5f, true),
        };

        private static readonly TestCaseData[] GridInRangeCases = new[]
        {
            new TestCaseData(new MapCoordinates(Vector2.One, _mapId), new MapCoordinates(Vector2.Zero, _mapId), 0.5f, false),
            new TestCaseData(new MapCoordinates(new Vector2(10f, 10f), _mapId), new MapCoordinates(new Vector2(10f, 10f), _mapId), 0.5f, true),
        };

        // Remember this test data is relative.
        private static readonly TestCaseData[] GridBox2Cases = new[]
        {
            new TestCaseData(new MapCoordinates(Vector2.One, _mapId), Box2.UnitCentered, false),
            new TestCaseData(new MapCoordinates(new Vector2(10f, 10f), _mapId), Box2.UnitCentered, true),
        };

        [Test, TestCaseSource(nameof(MapInRangeCases))]
        public void TestMapInRange(MapCoordinates spawnPos, MapCoordinates queryPos, float range, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();

            entManager.System<SharedMapSystem>().CreateMap(spawnPos.MapId);
            GetPhysicsEntity(entManager, spawnPos);

            Assert.That(lookup.GetEntitiesInRange(queryPos.MapId, queryPos.Position, range).Count > 0, Is.EqualTo(result));
            mapManager.DeleteMap(spawnPos.MapId);
        }

        [Test, TestCaseSource(nameof(GridInRangeCases))]
        public void TestGridInRange(MapCoordinates spawnPos, MapCoordinates queryPos, float range, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            mapSystem.CreateMap(spawnPos.MapId);
            var grid = mapManager.CreateGridEntity(spawnPos.MapId);
            entManager.System<SharedTransformSystem>().SetLocalPosition(grid.Owner, new Vector2(10f, 10f));

            GetPhysicsEntity(entManager, spawnPos);

            _ = entManager.SpawnEntity(null, spawnPos);
            Assert.That(lookup.GetEntitiesInRange(queryPos.MapId, queryPos.Position, range).Count > 0, Is.EqualTo(result));
            mapManager.DeleteMap(spawnPos.MapId);
        }

        [Test, TestCaseSource(nameof(MapInRangeCases))]
        public void TestMapNoFixtureInRange(MapCoordinates spawnPos, MapCoordinates queryPos, float range, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();

            entManager.System<SharedMapSystem>().CreateMap(spawnPos.MapId);

            _ = entManager.SpawnEntity(null, spawnPos);
            Assert.That(lookup.GetEntitiesInRange(queryPos.MapId, queryPos.Position, range).Count > 0, Is.EqualTo(result));
            mapManager.DeleteMap(spawnPos.MapId);
        }

        /// <summary>
        /// Tests Box2 local queries for a particular lookup ID.
        /// </summary>
        [Test, TestCaseSource(nameof(GridBox2Cases))]
        public void TestGridLocalIntersecting(MapCoordinates spawnPos, Box2 queryBounds, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            var grid = SetupGrid(spawnPos.MapId, mapSystem, entManager, mapManager);

            GetPhysicsEntity(entManager, spawnPos);
            var entities = new HashSet<EntityUid>();
            lookup.GetLocalEntitiesIntersecting(grid.Owner, queryBounds, entities);

            Assert.That(entities.Count > 0, Is.EqualTo(result));
            mapManager.DeleteMap(spawnPos.MapId);
        }

        [Test, TestCaseSource(nameof(GridInRangeCases))]
        public void TestGridNoFixtureInRange(MapCoordinates spawnPos, MapCoordinates queryPos, float range, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            var grid = SetupGrid(spawnPos.MapId, mapSystem, entManager, mapManager);

            var ent = entManager.SpawnEntity(null, spawnPos);
            Assert.That(lookup.GetEntitiesInRange(queryPos.MapId, queryPos.Position, range).Count > 0, Is.EqualTo(result));
            mapManager.DeleteMap(spawnPos.MapId);
        }

        [Test]
        public void AnyIntersecting()
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();

            var mapId = server.CreateMap().MapId;

            var theMapSpotBeingUsed = new Box2(Vector2.Zero, Vector2.One);

            _ = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
            Assert.That(lookup.AnyEntitiesIntersecting(mapId, theMapSpotBeingUsed));
            mapManager.DeleteMap(mapId);
        }

        /// <summary>
        /// Is the entity correctly removed / added to EntityLookup when anchored
        /// </summary>
        [Test]
        public void TestAnchoring()
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();

            var mapId = server.CreateMap().MapId;
            var grid = mapManager.CreateGridEntity(mapId);

            var theMapSpotBeingUsed = new Box2(Vector2.Zero, Vector2.One);
            grid.Comp.SetTile(new Vector2i(), new Tile(1));

            Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList().Count, Is.EqualTo(0));

            // Setup and check it actually worked
            var dummy = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
            Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList().Count, Is.EqualTo(1));

            var xform = entManager.GetComponent<TransformComponent>(dummy);

            // When anchoring it should still get returned.
            xform.Anchored = true;
            Assert.That(xform.Anchored);
            Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList(), Has.Count.EqualTo(1));

            xform.Anchored = false;
            Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList().Count, Is.EqualTo(1));

            entManager.DeleteEntity(dummy);
            mapManager.DeleteGrid(grid);
            mapManager.DeleteMap(mapId);
        }
    }
}
