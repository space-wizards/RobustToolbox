using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture]
    public sealed class GridContraction_Test : RobustIntegrationTest
    {
        [Test]
        public async Task TestGridDeletes()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var mapSystem = entManager.EntitySysManager.GetEntitySystem<SharedMapSystem>();

            await server.WaitAssertion(() =>
            {
                entManager.System<SharedMapSystem>().CreateMap(out var mapId);
                var grid = mapManager.CreateGridEntity(mapId);
                var gridEntity = grid.Owner;

                for (var i = 0; i < 10; i++)
                {
                    mapSystem.SetTile(grid, new Vector2i(i, 0), new Tile(1));
                }

                for (var i = 10; i >= 0; i--)
                {
                    mapSystem.SetTile(grid, new Vector2i(i, 0), Tile.Empty);
                }

                Assert.That(entManager.Deleted(gridEntity));
            });
        }

        [Test]
        public async Task TestGridNoDeletes()
        {
            var options = new ServerIntegrationOptions()
            {
                CVarOverrides =
                {
                    {
                        CVars.GameDeleteEmptyGrids.Name, "false"
                    }
                }
            };
            var server = StartServer(options);
            await server.WaitIdleAsync();

            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var mapSystem = entManager.System<SharedMapSystem>();

            await server.WaitAssertion(() =>
            {
                entManager.System<SharedMapSystem>().CreateMap(out var mapId);
                var grid = mapManager.CreateGridEntity(mapId);

                for (var i = 0; i < 10; i++)
                {
                    mapSystem.SetTile(grid, new Vector2i(i, 0), new Tile(1));
                }

                for (var i = 10; i >= 0; i--)
                {
                    mapSystem.SetTile(grid, new Vector2i(i, 0), Tile.Empty);
                }

                Assert.That(!((!entManager.EntityExists(grid) ? EntityLifeStage.Deleted : entManager.GetComponent<MetaDataComponent>(grid).EntityLifeStage) >= EntityLifeStage.Deleted));
            });
        }
    }
}
