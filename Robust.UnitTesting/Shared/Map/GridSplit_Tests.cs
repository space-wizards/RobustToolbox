using System.Linq;
using NUnit.Framework;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map;

[TestFixture]
public sealed class GridSplit_Tests
{
    [Test]
    public void SimpleSplit()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var mapManager = sim.Resolve<IMapManager>();
        var mapId = mapManager.CreateMap();
        var grid = mapManager.CreateGrid(mapId);

        for (var x = 0; x < 3; x++)
        {
            grid.SetTile(new Vector2i(x, 0), new Tile(1));
        }

        Assert.That(mapManager.GetAllMapGrids(mapId).Count(), Is.EqualTo(1));

        grid.SetTile(new Vector2i(1, 0), Tile.Empty);

        Assert.That(mapManager.GetAllMapGrids(mapId).Count(), Is.EqualTo(2));
    }

    [Test]
    public void DonutSplit()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var mapManager = sim.Resolve<IMapManager>();
        var mapId = mapManager.CreateMap();
        var grid = mapManager.CreateGrid(mapId);

        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                grid.SetTile(new Vector2i(x, y), new Tile(1));
            }
        }

        Assert.That(mapManager.GetAllMapGrids(mapId).Count(), Is.EqualTo(1));

        grid.SetTile(Vector2i.One, Tile.Empty);
        Assert.That(mapManager.GetAllMapGrids(mapId).Count(), Is.EqualTo(1));

        grid.SetTile(new Vector2i(1, 2), Tile.Empty);
        Assert.That(mapManager.GetAllMapGrids(mapId).Count(), Is.EqualTo(1));

        grid.SetTile(new Vector2i(1, 0), Tile.Empty);
        Assert.That(mapManager.GetAllMapGrids(mapId).Count(), Is.EqualTo(2));
    }
}
