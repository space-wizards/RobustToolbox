using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared;
using Robust.Shared.Configuration;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using Robust.Server.Physics.Components;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Map;

[Parallelizable(ParallelScope.All | ParallelScope.Fixtures)]
[TestFixture]
internal sealed class GridSplit_Tests
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
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();

        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapSystem.CreateGridEntity(mapId);
        var grid = gridEnt.Comp;
        grid.CanSplit = false;

        for (var x = 0; x < 5; x++)
        {
            mapSystem.SetTile(gridEnt, new Vector2i(x, 0), new Tile(1));
        }

        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(1, 0), Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        grid.CanSplit = true;
        mapSystem.SetTile(gridEnt, new Vector2i(2, 0), Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void SimpleSplit()
    {
        var sim = GetSim();
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapSystem.CreateGridEntity(mapId);

        for (var x = 0; x < 3; x++)
        {
            mapSystem.SetTile(gridEnt, new Vector2i(x, 0), new Tile(1));
        }

        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(1, 0), Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void CVarDisabledNoSplit()
    {
        var sim = GetSim();
        var entManager = sim.Resolve<IEntityManager>();
        var config = sim.Resolve<IConfigurationManager>();
        config.SetCVar(CVars.GridSplitting, false);

        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapSystem.CreateGridEntity(mapId);

        for (var x = 0; x < 3; x++)
        {
            mapSystem.SetTile(gridEnt, new Vector2i(x, 0), new Tile(1));
        }

        mapSystem.SetTile(gridEnt, new Vector2i(1, 0), Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));
        Assert.That(entManager.HasComponent<GridSplitNodeComponent>(gridEnt.Owner), Is.False);

        config.SetCVar(CVars.GridSplitting, true);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void CVarEnableDisableRebuildsSplitNodes()
    {
        var sim = GetSim();
        var entManager = sim.Resolve<IEntityManager>();
        var config = sim.Resolve<IConfigurationManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapSystem.CreateGridEntity(mapId);

        Assert.That(entManager.HasComponent<GridSplitNodeComponent>(gridEnt.Owner), Is.True);

        config.SetCVar(CVars.GridSplitting, false);
        Assert.That(entManager.HasComponent<GridSplitNodeComponent>(gridEnt.Owner), Is.False);

        for (var x = 0; x < 3; x++)
        {
            mapSystem.SetTile(gridEnt, new Vector2i(x, 0), new Tile(1));
        }

        mapSystem.SetTile(gridEnt, new Vector2i(1, 0), Tile.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));
            Assert.That(entManager.HasComponent<GridSplitNodeComponent>(gridEnt.Owner), Is.False);
        });

        config.SetCVar(CVars.GridSplitting, true);

        var splitGrids = mapSystem.GetAllGrids(mapId).ToArray();
        Assert.Multiple(() =>
        {
            Assert.That(splitGrids, Has.Length.EqualTo(2));

            foreach (var grid in splitGrids)
            {
                Assert.That(entManager.HasComponent<GridSplitNodeComponent>(grid.Owner), Is.True);
            }
        });

        config.SetCVar(CVars.GridSplitting, false);

        foreach (var grid in splitGrids)
        {
            Assert.That(entManager.HasComponent<GridSplitNodeComponent>(grid.Owner), Is.False);
        }

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void MapComponentGridDoesNotSplit()
    {
        var sim = GetSim();
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var map = sim.CreateMap();
        var mapGrid = entManager.AddComponent<MapGridComponent>(map.Uid);
        var mapGridEnt = new Entity<MapGridComponent>(map.Uid, mapGrid);

        for (var x = 0; x < 3; x++)
        {
            mapSystem.SetTile(mapGridEnt, new Vector2i(x, 0), new Tile(1));
        }

        mapSystem.SetTile(mapGridEnt, new Vector2i(1, 0), Tile.Empty);

        Assert.Multiple(() =>
        {
            Assert.That(mapSystem.GetAllGrids(map.MapId).Count(), Is.EqualTo(1));
            Assert.That(entManager.HasComponent<GridSplitNodeComponent>(map.Uid), Is.False);
        });

        mapSystem.DeleteMap(map.MapId);
    }

    [Test]
    public void SplitAcrossChunks()
    {
        var sim = GetSim();
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapSystem.CreateGridEntity(mapId);
        var chunkSize = gridEnt.Comp.ChunkSize;

        mapSystem.SetTile(gridEnt, new Vector2i(chunkSize - 1, 0), new Tile(1));
        mapSystem.SetTile(gridEnt, new Vector2i(chunkSize, 0), new Tile(1));
        mapSystem.SetTile(gridEnt, new Vector2i(chunkSize + 1, 0), new Tile(1));

        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(chunkSize, 0), Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void FourWaySplit()
    {
        var sim = GetSim();
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapSystem.CreateGridEntity(mapId);
        var center = new Vector2i(1, 1);

        mapSystem.SetTile(gridEnt, center, new Tile(1));
        mapSystem.SetTile(gridEnt, center + new Vector2i(0, 1), new Tile(1));
        mapSystem.SetTile(gridEnt, center + new Vector2i(1, 0), new Tile(1));
        mapSystem.SetTile(gridEnt, center + new Vector2i(0, -1), new Tile(1));
        mapSystem.SetTile(gridEnt, center + new Vector2i(-1, 0), new Tile(1));

        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, center, Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(4));

        foreach (var grid in mapSystem.GetAllGrids(mapId))
        {
            Assert.That(mapSystem.GetAllTiles(grid.Owner, grid.Comp).Count(), Is.EqualTo(1));
        }

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void SplitReCentersNewGridTiles()
    {
        var sim = GetSim();
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapSystem.CreateGridEntity(mapId);
        var oldGrid = gridEnt.Owner;
        var oldGridXform = entManager.GetComponent<TransformComponent>(oldGrid);
        var oldGridPos = xformSystem.GetWorldPosition(oldGridXform);
        var splitTile = new Vector2i(1000, 0);
        var removedTile = new Vector2i(1001, 0);
        var retainedTile = new Vector2i(1002, 0);
        var splitTileWorldPos = oldGridPos + splitTile;

        mapSystem.SetTile(gridEnt, splitTile, new Tile(1));
        mapSystem.SetTile(gridEnt, removedTile, new Tile(1));
        mapSystem.SetTile(gridEnt, retainedTile, new Tile(1));

        mapSystem.SetTile(gridEnt, removedTile, Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        var newGrid = mapSystem.GetAllGrids(mapId).Single(x => x.Owner != oldGrid);
        var newGridTiles = mapSystem.GetAllTiles(newGrid.Owner, newGrid.Comp).ToArray();
        var newGridXform = entManager.GetComponent<TransformComponent>(newGrid.Owner);
        var newGridBody = entManager.GetComponent<PhysicsComponent>(newGrid.Owner);

        Assert.Multiple(() =>
        {
            Assert.That(newGridTiles, Has.Length.EqualTo(1));
            Assert.That(newGridTiles[0].GridIndices, Is.EqualTo(Vector2i.Zero));
            Assert.That(Vector2.Distance(xformSystem.GetWorldPosition(newGridXform), splitTileWorldPos), Is.LessThan(0.001f));
            Assert.That(newGridBody.LocalCenter.Length(), Is.LessThan(2f));
        });

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void DonutSplit()
    {
        var sim = GetSim();
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapSystem.CreateGridEntity(mapId);

        for (var x = 0; x < 3; x++)
        {
            for (var y = 0; y < 3; y++)
            {
                mapSystem.SetTile(gridEnt, new Vector2i(x, y), new Tile(1));
            }
        }

        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, Vector2i.One, Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(1, 2), Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(1, 0), Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        mapSystem.DeleteMap(mapId);
    }

    [Test]
    public void TriSplit()
    {
        var sim = GetSim();
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapSystem.CreateGridEntity(mapId);

        for (var x = 0; x < 3; x++)
        {
            mapSystem.SetTile(gridEnt , new Vector2i(x, 0), new Tile(1));
        }

        mapSystem.SetTile(gridEnt, Vector2i.One, new Tile(1));

        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        mapSystem.SetTile(gridEnt, new Vector2i(1, 0), Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(3));

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
        var mapSystem = sim.Resolve<IEntityManager>().System<SharedMapSystem>();
        var transformSystem = sim.Resolve<IEntityManager>().System<SharedTransformSystem>();
        var mapId = sim.CreateMap().MapId;
        var gridEnt = mapSystem.CreateGridEntity(mapId);
        var grid = gridEnt.Comp;

        for (var x = 0; x < 4; x++)
        {
            mapSystem.SetTile(gridEnt, new Vector2i(x, 0), new Tile(1));
        }

        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(1));

        var dummy = entManager.SpawnEntity(null, new EntityCoordinates(gridEnt, new Vector2(3.5f, 0.5f)));
        var dummyXform = entManager.GetComponent<TransformComponent>(dummy);
        var anchored = entManager.SpawnEntity(null, new EntityCoordinates(gridEnt, new Vector2(3.5f, 0.5f)));
        var anchoredXform = entManager.GetComponent<TransformComponent>(anchored);

        transformSystem.AnchorEntity((anchored, anchoredXform), gridEnt);
        Assert.That(anchoredXform.Anchored);

        mapSystem.SetTile(gridEnt, new Vector2i(2, 0), Tile.Empty);
        Assert.That(mapSystem.GetAllGrids(mapId).Count(), Is.EqualTo(2));

        var newGrid = mapSystem.GetAllGrids(mapId).First(x => x.Comp != grid);
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
