using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Robust.Server.Physics;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class GridMerge_Tests
{
    private ISimulation GetSim()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var config = sim.Resolve<IConfigurationManager>();
        config.SetCVar(CVars.GridSplitting, true);

        return sim;
    }

    private static readonly TestCaseData[] MergeCases = new[]
    {
        new TestCaseData(new Vector2i(-1, 0), Angle.Zero, new Box2(-1f, 0f, 1f, 3f)),
        new TestCaseData(new Vector2i(0, 3), Angle.Zero, new Box2(0f, 0f, 1f, 6f)),
        new TestCaseData(new Vector2i(0, 1), Angle.FromDegrees(90), new Box2(-3f, 0f, 1f, 3f)),
        new TestCaseData(new Vector2i(1, 3), Angle.FromDegrees(-90), new Box2(0f, 0f, 4f, 3f)),
    };

    /// <summary>
    /// Checks 2 grids merge properly.
    /// </summary>
    [Test, TestCaseSource(nameof(MergeCases))]
    public void Merge(Vector2i offset, Angle angle, Box2 bounds)
    {
        var sim = GetSim();
        var mapManager = sim.Resolve<IMapManager>();
        var entMan = sim.Resolve<IEntityManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var gridFixtures = entMan.System<GridFixtureSystem>();

        var mapId = sim.CreateMap().MapId;
        var grid1 = mapManager.CreateGridEntity(mapId);
        var grid2 = mapManager.CreateGridEntity(mapId);
        var tiles = new List<(Vector2i, Tile)>();

        for (var y = 0; y < 3; y++)
        {
            tiles.Add((new Vector2i(0, y), new Tile(1)));
        }

        mapSystem.SetTiles(grid1, tiles);
        mapSystem.SetTiles(grid2, tiles);

        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        gridFixtures.Merge(grid1.Owner, grid2.Owner, offset, angle, grid1.Comp, grid2.Comp);

        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        Assert.That(grid1.Comp.LocalAABB, Is.EqualTo(bounds));
    }
}
