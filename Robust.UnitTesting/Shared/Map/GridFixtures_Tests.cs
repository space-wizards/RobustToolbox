using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;

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

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var grid = mapManager.CreateGrid(mapId);

                // Should be nothing if grid empty
                Assert.That(entManager.ComponentManager.TryGetComponent(grid.GridEntityId, out PhysicsComponent gridBody));
                Assert.That(gridBody.Fixtures.Count, Is.EqualTo(0));
                Assert.That(gridBody.BodyType, Is.EqualTo(BodyType.Static));

                // 1 fixture if we only ever update the 1 chunk
                grid.SetTile(Vector2i.Zero, new Tile(1));

                Assert.That(gridBody.Fixtures.Count, Is.EqualTo(1));
                // Also should only be a single tile.
                var bounds = gridBody.Fixtures[0].Shape.ComputeAABB(new Transform(Vector2.Zero, (float) Angle.Zero.Theta), 0);
                // Poly probably has Box2D's radius added to it so won't be a unit square
                Assert.That(MathHelper.CloseTo(Box2.Area(bounds), 1.0f, 0.1f));

                // Now do 2 tiles (same chunk)
                grid.SetTile(Vector2i.One, new Tile(1));

                Assert.That(gridBody.Fixtures.Count, Is.EqualTo(1));
                bounds = gridBody.Fixtures[0].Shape.ComputeAABB(new Transform(Vector2.Zero, (float) Angle.Zero.Theta), 0);
                // Because it's a diagonal tile it will actually be 2x2 area (until we get accurate hitboxes anyway).
                Assert.That(MathHelper.CloseTo(Box2.Area(bounds), 4.0f, 0.1f));

                // If we add a new chunk should be 2 now
                grid.SetTile(new Vector2i(-1, -1), new Tile(1));
                Assert.That(gridBody.Fixtures.Count, Is.EqualTo(2));

                gridBody.LinearVelocity = Vector2.One;
                Assert.That(gridBody.LinearVelocity.Length, Is.EqualTo(0f));
            });
        }

        /// <summary>
        /// Assert client and server get the same result when creating grid-fixtures
        /// </summary>
        [Test]
        public async Task TestClientServerGridFixtures()
        {
            var client = StartClient();
            var server = StartServer();

            await Task.WhenAll(client.WaitIdleAsync(), server.WaitIdleAsync());

            var cMapManager = client.ResolveDependency<IMapManager>();
            var sMapManager = server.ResolveDependency<IMapManager>();

            var cCompManager = client.ResolveDependency<IComponentManager>();
            var sCompManager = server.ResolveDependency<IComponentManager>();

            var cEntManager = client.ResolveDependency<IEntityManager>();

            client.SetConnectTarget(server);
            var netMan = client.ResolveDependency<IClientNetManager>();

            IMapGrid grid = default!;

            await client.WaitPost(() =>
            {
                netMan.ClientConnect(null!, 0, null!);
            });

            await client.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                var mapId = sMapManager.CreateMap();
                grid = sMapManager.CreateGrid(mapId);
                Assert.That(sCompManager.GetComponent<PhysicsComponent>(grid.GridEntityId).FixtureCount, Is.EqualTo(0));
            });

            await server.WaitRunTicks(1);
            await client.WaitRunTicks(1);

            await client.WaitAssertion(() =>
            {
                Assert.That(cEntManager.EntityExists(grid.GridEntityId));

                Assert.That(cCompManager.GetComponent<PhysicsComponent>(grid.GridEntityId).FixtureCount, Is.EqualTo(0));
            });
        }
    }
}
