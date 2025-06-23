using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
public sealed class GridSplit_Tests
{
    private ISimulation GetSim()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var config = sim.Resolve<IConfigurationManager>();
        config.SetCVar(CVars.GridSplitting, true);

        return sim;
    }

    /// <summary>
    /// Does the grid correctly not split when it's disabled.
    /// </summary>
    [Test]
    public void NoSplit()
    {
        var sim = GetSim();
        var mapManager = sim.Resolve<IMapManager>();
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();

        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapManager.CreateGridEntity(mapId);
        var grid = gridEnt.Comp;
        grid.CanSplit = false;

        for (var x = 0; x < 5; x++)
        {
            mapSystem.SetTile(gridEnt, new Vector2i(x, 0), new Tile(1));
        }

        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(1, 0), Tile.Empty);
        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        grid.CanSplit = true;
        mapSystem.SetTile(gridEnt, new Vector2i(2, 0), Tile.Empty);
        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void SimpleSplit()
    {
        var sim = GetSim();
        var mapManager = sim.Resolve<IMapManager>();
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapManager.CreateGridEntity(mapId);

        for (var x = 0; x < 3; x++)
        {
            mapSystem.SetTile(gridEnt, new Vector2i(x, 0), new Tile(1));
        }

        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(1, 0), Tile.Empty);
        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void DonutSplit()
    {
        var sim = GetSim();
        var mapManager = sim.Resolve<IMapManager>();
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapManager.CreateGridEntity(mapId);

        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                mapSystem.SetTile(gridEnt, new Vector2i(x, y), new Tile(1));
            }
        }

        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, Vector2i.One, Tile.Empty);
        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(1, 2), Tile.Empty);
        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(1, 0), Tile.Empty);
        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void TriSplit()
    {
        var sim = GetSim();
        var mapManager = sim.Resolve<IMapManager>();
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapManager.CreateGridEntity(mapId);

        for (var x = 0; x < 3; x++)
        {
            mapSystem.SetTile(gridEnt , new Vector2i(x, 0), new Tile(1));
        }

        mapSystem.SetTile(gridEnt, Vector2i.One, new Tile(1));

        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(1, 0), Tile.Empty);
        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(3));

        mapSystem.DeleteMap(mapId);
    }

    /// <summary>
    /// Checks GridId and Parents update correctly for re-parented entities.
    /// </summary>
    [Test]
    public void ReparentSplit()
    {
        var sim = GetSim();
        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var transformSystem = sim.Resolve<IEntityManager>().System<SharedTransformSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapManager.CreateGridEntity(mapId);
        var grid = gridEnt.Comp;

        for (var x = 0; x < 4; x++)
        {
            mapSystem.SetTile(gridEnt, new Vector2i(x, 0), new Tile(1));
        }

        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        var dummy = entManager.SpawnEntity(null, new EntityCoordinates(gridEnt, new Vector2(3.5f, 0.5f)));
        var dummyXform = entManager.GetComponent<TransformComponent>(dummy);
        var anchored = entManager.SpawnEntity(null, new EntityCoordinates(gridEnt, new Vector2(3.5f, 0.5f)));
        var anchoredXform = entManager.GetComponent<TransformComponent>(anchored);

        transformSystem.AnchorEntity((anchored, anchoredXform), gridEnt);
        Assert.That(anchoredXform.Anchored);

        mapSystem.SetTile(gridEnt, new Vector2i(2, 0), Tile.Empty);
        Assert.That(mapManager.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        var newGrid = mapManager.GetAllGrids(mapId).First(x => x.Comp != grid);
        var newGridXform = entManager.GetComponent<TransformComponent>(newGrid.Owner);

        Assert.Multiple(() =>
        {
            // Assertions baby
            Assert.That(anchoredXform.Anchored);
            Assert.That(anchoredXform.ParentUid, Is.EqualTo(newGrid.Owner));
            Assert.That(anchoredXform.GridUid, Is.EqualTo(newGrid.Owner));
            Assert.That(newGridXform._children, Does.Contain(anchored));

            Assert.That(dummyXform.ParentUid, Is.EqualTo(newGrid.Owner));
            Assert.That(dummyXform.GridUid, Is.EqualTo(newGrid.Owner));
            Assert.That(newGridXform._children, Does.Contain(dummy));
        });
        mapSystem.DeleteMap(mapId);
    }
}
