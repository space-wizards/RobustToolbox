using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Map
{
    /// <summary>
    /// Tests whether grid fixtures are being generated correctly.
    /// </summary>
    [TestFixture]
    public class GridFixtures_Tests : RobustIntegrationTest
    {
        [Test]
        public async Task TestGridFixtures()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var gridFixtures = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<GridFixtureSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var grid = mapManager.CreateGrid(mapId);

                // Should be nothing if grid empty
                Assert.That(entManager.ComponentManager.TryGetComponent(grid.GridEntityId, out PhysicsComponent gridBody));
                Assert.That(gridBody.Fixtures.Count, Is.EqualTo(0));

                // 1 fixture if we only ever update the 1 chunk
                grid.SetTile(Vector2i.Zero, new Tile(1));
                gridFixtures.Process();

                Assert.That(gridBody.Fixtures.Count, Is.EqualTo(1));
                // Also should only be a single tile.
                var bounds = gridBody.Fixtures[0].Shape.CalculateLocalBounds(Angle.Zero);
                // Poly probably has Box2D's radius added to it so won't be a unit square
                Assert.That(MathHelper.CloseTo(Box2.Area(bounds), 1.0f, 0.1f));

                // Now do 2 tiles (same chunk)
                grid.SetTile(Vector2i.One, new Tile(1));
                gridFixtures.Process();

                Assert.That(gridBody.Fixtures.Count, Is.EqualTo(1));
                bounds = gridBody.Fixtures[0].Shape.CalculateLocalBounds(Angle.Zero);
                // Because it's a diagonal tile it will actually be 2x2 area (until we get accurate hitboxes anyway).
                Assert.That(MathHelper.CloseTo(Box2.Area(bounds), 4.0f, 0.1f));

                // If we add a new chunk should be 2 now
                grid.SetTile(new Vector2i(-1, -1), new Tile(1));
                gridFixtures.Process();
                Assert.That(gridBody.Fixtures.Count, Is.EqualTo(2));
            });
        }
    }
}
