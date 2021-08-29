using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Map
{
    [TestFixture]
    public class GridContraction_Test : RobustIntegrationTest
    {
        [Test]
        public async Task TestGridDeletes()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var grid = mapManager.CreateGrid(mapId);
                var gridEntity = entManager.GetEntity(grid.GridEntityId);

                for (var i = 0; i < 10; i++)
                {
                    grid.SetTile(new Vector2i(i, 0), new Tile(1));
                }

                for (var i = 10; i >= 0; i--)
                {
                    grid.SetTile(new Vector2i(i, 0), Tile.Empty);
                }

                Assert.That(gridEntity.Deleted);
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

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var grid = mapManager.CreateGrid(mapId);

                for (var i = 0; i < 10; i++)
                {
                    grid.SetTile(new Vector2i(i, 0), new Tile(1));
                }

                for (var i = 10; i >= 0; i--)
                {
                    grid.SetTile(new Vector2i(i, 0), Tile.Empty);
                }

                Assert.That(!entManager.GetEntity(grid.GridEntityId).Deleted);
            });
        }
    }
}
