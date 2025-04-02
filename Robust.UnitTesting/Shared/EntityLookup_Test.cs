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
        private static readonly MapId MapId = new MapId(1);

        private static readonly TestCaseData[] IntersectingCases = new[]
        {
            // Big offset
            new TestCaseData(true, new MapCoordinates(new Vector2(10.5f, 10.5f), MapId), new MapCoordinates(new Vector2(10.5f, 10.5f), MapId), 0.25f, true),
        };

        private static readonly TestCaseData[] InRangeCases = new[]
        {
            new TestCaseData(true, new MapCoordinates(Vector2.One, MapId), new MapCoordinates(Vector2.Zero, MapId), 0.5f, false),
            new TestCaseData(true, new MapCoordinates(new Vector2(10f, 10f), MapId), new MapCoordinates(new Vector2(9.5f, 9.5f), MapId), 0.5f, true),
            // Close but no cigar
            new TestCaseData(true, new MapCoordinates(new Vector2(10f, 10f), MapId), new MapCoordinates(new Vector2(9f, 9f), MapId), 0.5f, false),
            // Large area so useboundsquery
            new TestCaseData(true, new MapCoordinates(new Vector2(0f, 0f), MapId), new MapCoordinates(new Vector2(0f, 1000f), MapId), 999f, false),
            new TestCaseData(true, new MapCoordinates(new Vector2(0f, 0f), MapId), new MapCoordinates(new Vector2(0f, 999f), MapId), 999f, true),

            // NoFixturecases
            new TestCaseData(false, new MapCoordinates(Vector2.One, MapId), new MapCoordinates(Vector2.Zero, MapId), 0.5f, false),
            new TestCaseData(false, new MapCoordinates(new Vector2(10f, 10f), MapId), new MapCoordinates(new Vector2(9.5f, 9.5f), MapId), 0.5f, false),
            // Close but no cigar
            new TestCaseData(false, new MapCoordinates(new Vector2(10f, 10f), MapId), new MapCoordinates(new Vector2(9f, 9f), MapId), 0.5f, false),
        };

        // Remember this test data is relative.
        private static readonly TestCaseData[] Box2Cases = new[]
        {
            new TestCaseData(true, new MapCoordinates(Vector2.One, MapId), Box2.UnitCentered, false),
            new TestCaseData(true, new MapCoordinates(new Vector2(10f, 10f), MapId), Box2.UnitCentered, true),
        };

        private static readonly TestCaseData[] TileCases = new[]
        {
            new TestCaseData(true, new MapCoordinates(Vector2.One, MapId), Vector2i.Zero, false),
            new TestCaseData(true, new MapCoordinates(new Vector2(10f, 10f), MapId), Vector2i.Zero, true),
            // Need to make sure we don't pull out neighbor fixtures even if they barely touch our tile
            new TestCaseData(true, new MapCoordinates(new Vector2(11f + 0.35f, 10f), MapId), Vector2i.Zero, false),
        };

        private EntityUid GetPhysicsEntity(IEntityManager entManager, MapCoordinates spawnPos)
        {
            var ent = entManager.SpawnEntity(null, spawnPos);
            var physics = entManager.AddComponent<PhysicsComponent>(ent);
            entManager.System<FixtureSystem>().TryCreateFixture(ent, new PhysShapeCircle(0.35f, Vector2.Zero), "fix1");
            entManager.System<SharedPhysicsSystem>().SetCanCollide(ent, true, body: physics);
            return ent;
        }

        private Entity<MapGridComponent> SetupGrid(MapId mapId, SharedMapSystem mapSystem, IEntityManager entManager, IMapManager mapManager)
        {
            var grid = mapManager.CreateGridEntity(mapId);
            entManager.System<SharedTransformSystem>().SetLocalPosition(grid.Owner, new Vector2(10f, 10f));
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
            return grid;
        }

        #region Entity

        /*
         * We double these tests just because these have slightly different codepaths at the moment.
         *
         */

        [Test, TestCaseSource(nameof(Box2Cases))]
        public void TestEntityAnyIntersecting(bool physics, MapCoordinates spawnPos, Box2 queryBounds, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            mapSystem.CreateMap(spawnPos.MapId);
            var grid = SetupGrid(spawnPos.MapId, mapSystem, entManager, mapManager);

            if (physics)
                GetPhysicsEntity(entManager, spawnPos);
            else
                entManager.Spawn(null, spawnPos);

            var outcome = lookup.AnyLocalEntitiesIntersecting(grid.Owner, queryBounds, LookupFlags.All);

            Assert.That(outcome, Is.EqualTo(result));
            mapSystem.DeleteMap(spawnPos.MapId);
        }

        [Test, TestCaseSource(nameof(Box2Cases))]
        public void TestEntityAnyLocalIntersecting(bool physics, MapCoordinates spawnPos, Box2 queryBounds, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            mapSystem.CreateMap(spawnPos.MapId);
            var grid = SetupGrid(spawnPos.MapId, mapSystem, entManager, mapManager);

            if (physics)
                GetPhysicsEntity(entManager, spawnPos);
            else
                entManager.Spawn(null, spawnPos);

            var outcome = lookup.AnyLocalEntitiesIntersecting(grid.Owner, queryBounds, LookupFlags.All);

            Assert.That(outcome, Is.EqualTo(result));
            mapSystem.DeleteMap(spawnPos.MapId);
        }

        /// <summary>
        /// Tests Box2 local queries for a particular lookup ID.
        /// </summary>
        [Test, TestCaseSource(nameof(Box2Cases))]
        public void TestEntityGridLocalIntersecting(bool physics, MapCoordinates spawnPos, Box2 queryBounds, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            mapSystem.CreateMap(spawnPos.MapId);
            var grid = SetupGrid(spawnPos.MapId, mapSystem, entManager, mapManager);

            if (physics)
                GetPhysicsEntity(entManager, spawnPos);
            else
                entManager.Spawn(null, spawnPos);

            var entities = new HashSet<Entity<TransformComponent>>();
            lookup.GetLocalEntitiesIntersecting(grid.Owner, queryBounds, entities);

            Assert.That(entities.Count > 0, Is.EqualTo(result));
            mapSystem.DeleteMap(spawnPos.MapId);
        }

        /// <summary>
        /// Tests Box2 local queries for a particular lookup ID.
        /// </summary>
        [Test, TestCaseSource(nameof(TileCases))]
        public void TestEntityGridTileIntersecting(bool physics, MapCoordinates spawnPos, Vector2i queryTile, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            mapSystem.CreateMap(spawnPos.MapId);
            var grid = SetupGrid(spawnPos.MapId, mapSystem, entManager, mapManager);

            if (physics)
                GetPhysicsEntity(entManager, spawnPos);
            else
                entManager.Spawn(null, spawnPos);

            var entities = new HashSet<Entity<TransformComponent>>();
            lookup.GetLocalEntitiesIntersecting(grid.Owner, queryTile, entities);

            Assert.That(entities.Count > 0, Is.EqualTo(result));
            mapSystem.DeleteMap(spawnPos.MapId);
        }

        #endregion

        #region EntityUid

        [Test, TestCaseSource(nameof(InRangeCases))]
        public void TestMapInRange(bool physics, MapCoordinates spawnPos, MapCoordinates queryPos, float range, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            entManager.System<SharedMapSystem>().CreateMap(spawnPos.MapId);

            if (physics)
                GetPhysicsEntity(entManager, spawnPos);
            else
                entManager.Spawn(null, spawnPos);

            Assert.That(lookup.GetEntitiesInRange(queryPos.MapId, queryPos.Position, range).Count > 0, Is.EqualTo(result));
            mapSystem.DeleteMap(spawnPos.MapId);
        }

        [Test, TestCaseSource(nameof(IntersectingCases))]
        public void TestGridIntersecting(bool physics, MapCoordinates spawnPos, MapCoordinates queryPos, float range, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            mapSystem.CreateMap(spawnPos.MapId);
            var grid = SetupGrid(spawnPos.MapId, mapSystem, entManager, mapManager);

            if (physics)
                GetPhysicsEntity(entManager, spawnPos);
            else
                entManager.Spawn(null, spawnPos);

            _ = entManager.SpawnEntity(null, spawnPos);
            var bounds = new Box2Rotated(Box2.CenteredAround(queryPos.Position, new Vector2(range, range)));

            Assert.That(lookup.GetEntitiesIntersecting(queryPos.MapId, bounds).Count > 0, Is.EqualTo(result));
            mapSystem.DeleteMap(spawnPos.MapId);
        }

        [Test, TestCaseSource(nameof(InRangeCases))]
        public void TestGridInRange(bool physics, MapCoordinates spawnPos, MapCoordinates queryPos, float range, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            mapSystem.CreateMap(spawnPos.MapId);
            var grid = SetupGrid(spawnPos.MapId, mapSystem, entManager, mapManager);

            if (physics)
                GetPhysicsEntity(entManager, spawnPos);
            else
                entManager.Spawn(null, spawnPos);

            _ = entManager.SpawnEntity(null, spawnPos);
            Assert.That(lookup.GetEntitiesInRange(queryPos.MapId, queryPos.Position, range).Count > 0, Is.EqualTo(result));
            mapSystem.DeleteMap(spawnPos.MapId);
        }

        [Test, TestCaseSource(nameof(InRangeCases))]
        public void TestMapNoFixtureInRange(bool physics, MapCoordinates spawnPos, MapCoordinates queryPos, float range, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            entManager.System<SharedMapSystem>().CreateMap(spawnPos.MapId);

            if (physics)
                GetPhysicsEntity(entManager, spawnPos);
            else
                entManager.Spawn(null, spawnPos);

            Assert.That(lookup.GetEntitiesInRange(queryPos.MapId, queryPos.Position, range).Count > 0, Is.EqualTo(result));
            mapSystem.DeleteMap(spawnPos.MapId);
        }

        /// <summary>
        /// Tests Box2 local queries for a particular lookup ID.
        /// </summary>
        [Test, TestCaseSource(nameof(Box2Cases))]
        public void TestGridAnyIntersecting(bool physics, MapCoordinates spawnPos, Box2 queryBounds, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            mapSystem.CreateMap(spawnPos.MapId);
            var grid = SetupGrid(spawnPos.MapId, mapSystem, entManager, mapManager);

            if (physics)
                GetPhysicsEntity(entManager, spawnPos);
            else
                entManager.Spawn(null, spawnPos);

            var outcome = lookup.AnyLocalEntitiesIntersecting(grid.Owner, queryBounds, LookupFlags.All);

            Assert.That(outcome, Is.EqualTo(result));
            mapSystem.DeleteMap(spawnPos.MapId);
        }

        /// <summary>
        /// Tests Box2 local queries for a particular lookup ID.
        /// </summary>
        [Test, TestCaseSource(nameof(Box2Cases))]
        public void TestGridLocalIntersecting(bool physics, MapCoordinates spawnPos, Box2 queryBounds, bool result)
        {
            var sim = RobustServerSimulation.NewSimulation();
            var server = sim.InitializeInstance();

            var lookup = server.Resolve<IEntitySystemManager>().GetEntitySystem<EntityLookupSystem>();
            var entManager = server.Resolve<IEntityManager>();
            var mapManager = server.Resolve<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            mapSystem.CreateMap(spawnPos.MapId);
            var grid = SetupGrid(spawnPos.MapId, mapSystem, entManager, mapManager);

            if (physics)
                GetPhysicsEntity(entManager, spawnPos);
            else
                entManager.Spawn(null, spawnPos);

            var entities = new HashSet<EntityUid>();
            lookup.GetLocalEntitiesIntersecting(grid.Owner, queryBounds, entities);

            Assert.That(entities.Count > 0, Is.EqualTo(result));
            mapSystem.DeleteMap(spawnPos.MapId);
        }

        #endregion

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
            var mapSystem = entManager.System<SharedMapSystem>();
            var transformSystem = entManager.System<SharedTransformSystem>();

            var mapId = server.CreateMap().MapId;
            var grid = mapManager.CreateGridEntity(mapId);

            var theMapSpotBeingUsed = new Box2(Vector2.Zero, Vector2.One);
            mapSystem.SetTile(grid, new Vector2i(), new Tile(1));

            Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList(), Is.Empty);

            // Setup and check it actually worked
            var dummy = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
            Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList(), Has.Count.EqualTo(1));

            var xform = entManager.GetComponent<TransformComponent>(dummy);

            // When anchoring it should still get returned.
            transformSystem.AnchorEntity(dummy, xform);
            Assert.That(xform.Anchored, Is.True);
            Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList(), Has.Count.EqualTo(1));

            transformSystem.Unanchor(dummy, xform);
            Assert.That(xform.Anchored, Is.False);
            Assert.That(lookup.GetEntitiesIntersecting(mapId, theMapSpotBeingUsed).ToList().Count, Is.EqualTo(1));

            entManager.DeleteEntity(dummy);
            entManager.DeleteEntity(grid);
            mapSystem.DeleteMap(mapId);
        }
    }
}
