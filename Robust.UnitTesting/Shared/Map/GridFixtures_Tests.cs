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

namespace Robust.UnitTesting.Shared.Map
{
    /// <summary>
    /// Tests whether grid fixtures are being generated correctly.
    /// </summary>
    [Parallelizable(ParallelScope.All)]
    [TestFixture]
    public sealed class GridFixtures_Tests : RobustIntegrationTest
    {
        [Test]
        public async Task TestGridFixtures()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var physSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedPhysicsSystem>();

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
                grid.Comp.SetTile(Vector2i.Zero, new Tile(1));

                Assert.That(manager.FixtureCount, Is.EqualTo(1));
                // Also should only be a single tile.
                var bounds = manager.Fixtures.First().Value.Shape.ComputeAABB(new Transform(Vector2.Zero, (float) Angle.Zero.Theta), 0);
                // Poly probably has Box2D's radius added to it so won't be a unit square
                Assert.That(MathHelper.CloseToPercent(Box2.Area(bounds), 1.0f, 0.1f));

                // Now do 2 tiles (same chunk)
                grid.Comp.SetTile(new Vector2i(0, 1), new Tile(1));

                Assert.That(manager.FixtureCount, Is.EqualTo(1));
                bounds = manager.Fixtures.First().Value.Shape.ComputeAABB(new Transform(Vector2.Zero, (float) Angle.Zero.Theta), 0);

                // Even if we add a new tile old fixture should stay the same if they don't connect.
                Assert.That(MathHelper.CloseToPercent(Box2.Area(bounds), 2.0f, 0.1f));

                // If we add a new chunk should be 2 now
                grid.Comp.SetTile(new Vector2i(0, -1), new Tile(1));
                Assert.That(manager.FixtureCount, Is.EqualTo(2));

                physSystem.SetLinearVelocity(grid, Vector2.One, manager: manager, body: gridBody);
                Assert.That(gridBody.LinearVelocity.Length, Is.EqualTo(0f));
            });
        }
    }
}
