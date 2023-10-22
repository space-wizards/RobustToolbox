using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture]
    public sealed class GridRotation_Tests : RobustIntegrationTest
    {
        // Because integration tests are ten billion percent easier we'll just do all the rotation tests here.
        // These are mainly looking out for situations where the grid is rotated 90 / 180 degrees and we
        // need to rotate points about the grid's origin which is a /very/ common source of bugs.

        [Test]
        public async Task TestLocalWorldConversions()
        {
            var server = StartServer();

            await server.WaitIdleAsync();

            var entMan = server.ResolveDependency<IEntityManager>();
            var mapMan = server.ResolveDependency<IMapManager>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapMan.CreateMap();
                var grid = mapMan.CreateGridEntity(mapId);
                var gridEnt = grid.Owner;
                var coordinates = new EntityCoordinates(gridEnt, new Vector2(10, 0));

                // if no rotation and 0,0 position should just be the same coordinate.
                Assert.That(entMan.GetComponent<TransformComponent>(gridEnt).WorldRotation, Is.EqualTo(Angle.Zero));
                Assert.That(grid.Comp.WorldToLocal(coordinates.Position), Is.EqualTo(coordinates.Position));

                // Rotate 180 degrees should show -10, 0 for the position in map-terms and 10, 0 for the position in entity terms (i.e. no change).
                entMan.GetComponent<TransformComponent>(gridEnt).WorldRotation += new Angle(MathF.PI);
                Assert.That(entMan.GetComponent<TransformComponent>(gridEnt).WorldRotation, Is.EqualTo(new Angle(MathF.PI)));
                // Check the map coordinate rotates correctly
                Assert.That(grid.Comp.WorldToLocal(new Vector2(10, 0)).EqualsApprox(new Vector2(-10, 0), 0.01f));
                Assert.That(grid.Comp.LocalToWorld(coordinates.Position).EqualsApprox(new Vector2(-10, 0), 0.01f));

                // Now we'll do the same for 180 degrees.
                entMan.GetComponent<TransformComponent>(gridEnt).WorldRotation += MathF.PI / 2f;
                // If grid facing down then worldpos of 10, 0 gets rotated 90 degrees CCW and hence should be 0, 10
                Assert.That(grid.Comp.WorldToLocal(new Vector2(10, 0)).EqualsApprox(new Vector2(0, 10), 0.01f));
                // If grid facing down then local 10,0 pos should just return 0, -10 given it's aligned with the rotation.
                Assert.That(grid.Comp.LocalToWorld(coordinates.Position).EqualsApprox(new Vector2(0, -10), 0.01f));
            });
        }

        [Test]
        public async Task TestChunkRotations()
        {
            // This is mainly checking for the purposes of rendering at this stage.
            var server = StartServer();

            await server.WaitIdleAsync();

            var entMan = server.ResolveDependency<IEntityManager>();
            var mapMan = server.ResolveDependency<IMapManager>();
            var mapSystem = entMan.System<SharedMapSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapMan.CreateMap();
                var grid = mapMan.CreateGridEntity(mapId);
                var gridEnt = grid.Owner;

                /* Test for map chunk rotations */
                var tile = new Tile(1);

                for (var x = 0; x < 2; x++)
                {
                    for (var y = 0; y < 10; y++)
                    {
                        grid.Comp.SetTile(new Vector2i(x, y), tile);
                    }
                }

                var chunks = grid.Comp.GetMapChunks().Select(c => c.Value).ToList();

                Assert.That(chunks.Count, Is.EqualTo(1));
                var chunk = chunks[0];
                var aabb = mapSystem.CalcWorldAABB(gridEnt, grid, chunk);
                var bounds = new Box2(new Vector2(0, 0), new Vector2(2, 10));

                // With all cardinal directions these should align.
                Assert.That(aabb, Is.EqualTo(bounds));

                entMan.GetComponent<TransformComponent>(gridEnt).LocalRotation = new Angle(Math.PI);
                aabb = mapSystem.CalcWorldAABB(gridEnt, grid, chunk);
                bounds = new Box2(new Vector2(-2, -10), new Vector2(0, 0));

                Assert.That(aabb.EqualsApprox(bounds), $"Expected bounds of {aabb} and got {bounds}");

                entMan.GetComponent<TransformComponent>(gridEnt).LocalRotation = new Angle(-Math.PI / 2);
                aabb = mapSystem.CalcWorldAABB(gridEnt, grid, chunk);
                bounds = new Box2(new Vector2(0, -2), new Vector2(10, 0));

                Assert.That(aabb.EqualsApprox(bounds), $"Expected bounds of {aabb} and got {bounds}");

                entMan.GetComponent<TransformComponent>(gridEnt).LocalRotation = new Angle(-Math.PI / 4);
                aabb = mapSystem.CalcWorldAABB(gridEnt, grid, chunk);
                bounds = new Box2(new Vector2(0, -1.4142135f), new Vector2(8.485281f, 7.071068f));

                Assert.That(aabb.EqualsApprox(bounds), $"Expected bounds of {aabb} and got {bounds}");
            });
        }
    }
}
