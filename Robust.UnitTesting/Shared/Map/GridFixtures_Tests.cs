using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map;

/// <summary>
/// Tests whether grid fixtures are being generated correctly.
/// </summary>
[Parallelizable(ParallelScope.All)]
[TestFixture]
public sealed class GridFixtures_Tests : RobustIntegrationTest
{
    /// <summary>
    /// Tests that grid fixtures match what's expected.
    /// </summary>
    [Test]
    public void TestGridFixtureDeletion()
    {
        var server = RobustServerSimulation.NewSimulation().InitializeInstance();
        var map = server.CreateMap();
        var entManager = server.Resolve<IEntityManager>();
        var grid = server.Resolve<IMapManager>().CreateGridEntity(map.MapId);
        var mapSystem = entManager.System<SharedMapSystem>();
        var fixtures = entManager.GetComponent<FixturesComponent>(grid);

        mapSystem.SetTiles(grid, new List<(Vector2i GridIndices, Tile Tile)>()
        {
            (Vector2i.Zero, new Tile(1)),
            (Vector2i.Right, new Tile(1)),
            (Vector2i.Right * 2, new Tile(1)),
            (Vector2i.Up, new Tile(1)),
        });

        Assert.That(fixtures.FixtureCount, Is.EqualTo(2));
        Assert.That(grid.Comp.LocalAABB.Equals(new Box2(0f, 0f, 3f, 2f)));

        mapSystem.SetTile(grid, Vector2i.Up, Tile.Empty);

        Assert.That(fixtures.FixtureCount, Is.EqualTo(1));
        Assert.That(grid.Comp.LocalAABB.Equals(new Box2(0f, 0f, 3f, 1f)));
    }

    [Test]
    public async Task TestGridFixtures()
    {
        var server = StartServer();
        await server.WaitIdleAsync();

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var physSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedPhysicsSystem>();
        var mapSystem = entManager.EntitySysManager.GetEntitySystem<SharedMapSystem>();

        await server.WaitAssertion(() =>
        {
            entManager.System<SharedMapSystem>().CreateMap(out var mapId);
            var grid = mapManager.CreateGridEntity(mapId);

            // Should be nothing if grid empty
            Assert.That(entManager.TryGetComponent(grid, out PhysicsComponent? gridBody));
            Assert.That(entManager.TryGetComponent(grid, out FixturesComponent? manager));
            Assert.That(manager!.FixtureCount, Is.EqualTo(0));
            Assert.That(gridBody!.BodyType, Is.EqualTo(BodyType.Static));

            // 1 fixture if we only ever update the 1 chunk
            mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));

            Assert.That(manager.FixtureCount, Is.EqualTo(1));
            // Also should only be a single tile.
            var bounds = manager.Fixtures.First().Value.Shape.ComputeAABB(new Transform(Vector2.Zero, (float) Angle.Zero.Theta), 0);
            // Poly probably has Box2D's radius added to it so won't be a unit square
            Assert.That(MathHelper.CloseToPercent(Box2.Area(bounds), 1.0f, 0.1f));

            // Now do 2 tiles (same chunk)
            mapSystem.SetTile(grid, new Vector2i(0, 1), new Tile(1));

            Assert.That(manager.FixtureCount, Is.EqualTo(1));
            bounds = manager.Fixtures.First().Value.Shape.ComputeAABB(new Transform(Vector2.Zero, (float) Angle.Zero.Theta), 0);

            // Even if we add a new tile old fixture should stay the same if they don't connect.
            Assert.That(MathHelper.CloseToPercent(Box2.Area(bounds), 2.0f, 0.1f));

            // If we add a new chunk should be 2 now
            mapSystem.SetTile(grid, new Vector2i(0, -1), new Tile(1));
            Assert.That(manager.FixtureCount, Is.EqualTo(2));

            physSystem.SetLinearVelocity(grid, Vector2.One, manager: manager, body: gridBody);
            Assert.That(gridBody.LinearVelocity.Length, Is.EqualTo(0f));
        });
    }
}
