using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Markdown.Mapping;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.EntitySerialization;

[TestFixture]
internal sealed partial class ZLevelGridSerializationTest : RobustIntegrationTest
{
    [Test]
    public async Task GridSaveSkipsCrossMapLinks()
    {
        var server = StartServer(new() { Pool = false });
        await server.WaitIdleAsync();

        var entMan = server.EntMan;
        var mapSystem = server.System<SharedMapSystem>();
        var zLevels = server.System<ZLevelSystem>();
        var loader = server.System<MapLoaderSystem>();
        var path = new ResPath($"{nameof(ZLevelGridSerializationTest)}_linked-grid.yml");
        MapId destinationMapId = default;
        Entity<MapGridComponent> lowerGrid = default;

        await server.WaitPost(() =>
        {
            var lowerMap = mapSystem.CreateMap(out var lowerMapId);
            var upperMap = mapSystem.CreateMap(out var upperMapId);
            Assert.That(zLevels.TryCreateMapNetwork([lowerMap, upperMap], out _), Is.True);
            lowerGrid = mapSystem.CreateGridEntity(lowerMapId);
            var upperGrid = mapSystem.CreateGridEntity(upperMapId);
            Assert.That(zLevels.TryLinkGrids(lowerGrid, upperGrid), Is.True);
            mapSystem.CreateMap(out destinationMapId);
        });

        await server.WaitAssertion(() => Assert.That(loader.TrySaveGrid(lowerGrid, path), Is.True));
        Entity<MapGridComponent>? loadedGrid = null;
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(destinationMapId, path, out loadedGrid), Is.True));

        Assert.That(loadedGrid, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(entMan.HasComponent<ZLevelGridComponent>(loadedGrid.Value.Owner), Is.False);
            Assert.That(zLevels.TryGetGridAbove(loadedGrid.Value.Owner, out _), Is.False);
            Assert.That(zLevels.TryGetGridBelow(loadedGrid.Value.Owner, out _), Is.False);
        });
    }

    [Test]
    public async Task FullSaveRestoresExplicitLinksAndRelativePoses()
    {
        var server = StartServer(new() { Pool = false });
        await server.WaitIdleAsync();

        var entMan = server.EntMan;
        var mapSystem = server.System<SharedMapSystem>();
        var transform = server.System<SharedTransformSystem>();
        var zLevels = server.System<ZLevelSystem>();
        var loader = server.System<MapLoaderSystem>();
        MapId lowerMapId = default;
        MapId middleMapId = default;
        MapId upperMapId = default;
        MappingDataNode? save = null;

        await server.WaitAssertion(() =>
        {
            var lowerMap = mapSystem.CreateMap(out lowerMapId);
            var middleMap = mapSystem.CreateMap(out middleMapId);
            var upperMap = mapSystem.CreateMap(out upperMapId);
            Assert.That(zLevels.TryCreateMapNetwork([lowerMap, middleMap, upperMap], out _), Is.True);

            var lowerGrid = mapSystem.CreateGridEntity(lowerMapId);
            var middleGrid = mapSystem.CreateGridEntity(middleMapId);
            var upperGrid = mapSystem.CreateGridEntity(upperMapId);
            transform.SetWorldPositionRotation(middleGrid, new Vector2(1f, 2f), Angle.FromDegrees(20));
            transform.SetWorldPositionRotation(upperGrid, new Vector2(3f, 4f), Angle.FromDegrees(30));
            Assert.That(zLevels.TryLinkGrids(lowerGrid, middleGrid, align: false), Is.True);
            Assert.That(zLevels.TryLinkGrids(middleGrid, upperGrid, align: false), Is.True);
            Assert.That(loader.TrySerializeAllEntities(out save), Is.True);
        });

        Assert.That(save, Is.Not.Null);
        await server.WaitPost(() =>
        {
            mapSystem.DeleteMap(lowerMapId);
            mapSystem.DeleteMap(middleMapId);
            mapSystem.DeleteMap(upperMapId);
        });

        LoadResult? result = null;
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGeneric(save!, "z-level-full-save", out result), Is.True));
        Assert.That(result, Is.Not.Null);

        var loadedLowerMap = result!.Maps.Single(map => entMan.GetComponent<ZLevelMapComponent>(map).Depth == 0);
        var loadedMiddleMap = result.Maps.Single(map => entMan.GetComponent<ZLevelMapComponent>(map).Depth == 1);
        var loadedUpperMap = result.Maps.Single(map => entMan.GetComponent<ZLevelMapComponent>(map).Depth == 2);
        var loadedLowerGrid = result.Grids.Single(grid => transform.GetMap(grid.Owner) == loadedLowerMap.Owner);
        var loadedMiddleGrid = result.Grids.Single(grid => transform.GetMap(grid.Owner) == loadedMiddleMap.Owner);
        var loadedUpperGrid = result.Grids.Single(grid => transform.GetMap(grid.Owner) == loadedUpperMap.Owner);

        Assert.Multiple(() =>
        {
            Assert.That(zLevels.TryGetGridAbove(loadedLowerGrid, out var gridAbove), Is.True);
            Assert.That(gridAbove, Is.EqualTo(loadedMiddleGrid.Owner));
            Assert.That(zLevels.TryGetGridBelow(loadedMiddleGrid, out var gridBelow), Is.True);
            Assert.That(gridBelow, Is.EqualTo(loadedLowerGrid.Owner));
            Assert.That(zLevels.TryGetGridAbove(loadedMiddleGrid, out gridAbove), Is.True);
            Assert.That(gridAbove, Is.EqualTo(loadedUpperGrid.Owner));
            Assert.That(zLevels.TryGetGridBelow(loadedUpperGrid, out gridBelow), Is.True);
            Assert.That(gridBelow, Is.EqualTo(loadedMiddleGrid.Owner));
            Assert.That(transform.GetWorldPosition(loadedUpperGrid), Is.Approximately(new Vector2(3f, 4f)));
            Assert.That(transform.GetWorldRotation(loadedUpperGrid).Degrees, Is.EqualTo(30f).Within(0.001f));
        });
    }
}
