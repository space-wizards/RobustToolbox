using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.UnitTesting.Shared.EntitySerialization.EntitySaveTestComponent;

namespace Robust.UnitTesting.Shared.EntitySerialization;

[TestFixture]
internal sealed partial class ChunkEntitySerializationTest : RobustIntegrationTest
{
    private const string TestTileDefId = "a";
    private const string TestPrototypes = $@"
- type: testTileDef
  id: space

- type: testTileDef
  id: {TestTileDefId}
";

    [Test]
    public async Task GridSaveLoadIncludesChunkEntities()
    {
        var server = StartServer(new() { Pool = false, ExtraPrototypes = TestPrototypes });
        await server.WaitIdleAsync();

        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var chunks = server.System<ChunkEntitySystem>();
        var tileMan = server.ResolveDependency<ITileDefinitionManager>();
        var gridPath = new ResPath($"{nameof(ChunkEntitySerializationTest)}_grid.yml");

        SerializationTestHelper.LoadTileDefs(server.ProtoMan, tileMan, "space");
        var tDef = server.ProtoMan.Index<TileDef>(TestTileDefId);

        MapId mapId = default;
        EntityUid grid = default;
        var chunkIndices = new Vector2i(2, 3);

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out mapId);
            grid = mapSys.CreateGridEntity(mapId);
            mapSys.SetTile((grid, entMan.GetComponent<MapGridComponent>(grid)), Vector2i.Zero, new Tile(tDef.TileId));

            var chunk = chunks.GetOrCreateChunk(grid, chunkIndices);
            Get(chunk.Owner, entMan).Comp2.Id = "grid-chunk";
        });

        await server.WaitRunTicks(5);
        Assert.That(loader.TrySaveGrid(grid, gridPath));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        Entity<MapGridComponent>? loadedGrid = default;
        await server.WaitPost(() => mapSys.CreateMap(out mapId));
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out loadedGrid)));

        AssertLoadedChunk(entMan, chunks, loadedGrid!.Value.Owner, chunkIndices, "grid-chunk");
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
    }

    [Test]
    public async Task MapSaveLoadIncludesMapAndGridChunkEntities()
    {
        var server = StartServer(new() { Pool = false, ExtraPrototypes = TestPrototypes });
        await server.WaitIdleAsync();

        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var chunks = server.System<ChunkEntitySystem>();
        var tileMan = server.ResolveDependency<ITileDefinitionManager>();
        var mapPath = new ResPath($"{nameof(ChunkEntitySerializationTest)}_map.yml");

        SerializationTestHelper.LoadTileDefs(server.ProtoMan, tileMan, "space");
        var tDef = server.ProtoMan.Index<TileDef>(TestTileDefId);

        MapId mapId = default;
        EntityUid map = default;
        EntityUid grid = default;
        var mapChunkIndices = new Vector2i(1, 1);
        var gridChunkIndices = new Vector2i(2, 3);

        await server.WaitPost(() =>
        {
            map = mapSys.CreateMap(out mapId);
            grid = mapSys.CreateGridEntity(mapId);
            mapSys.SetTile((grid, entMan.GetComponent<MapGridComponent>(grid)), Vector2i.Zero, new Tile(tDef.TileId));

            var mapChunk = chunks.GetOrCreateChunk(map, mapChunkIndices);
            Get(mapChunk.Owner, entMan).Comp2.Id = "map-chunk";

            var gridChunk = chunks.GetOrCreateChunk(grid, gridChunkIndices);
            Get(gridChunk.Owner, entMan).Comp2.Id = "grid-chunk";
        });

        await server.WaitRunTicks(5);
        Assert.That(loader.TrySaveMap(mapId, mapPath));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        Entity<MapComponent>? loadedMap = default;
        HashSet<Entity<MapGridComponent>>? loadedGrids = default;
        await server.WaitAssertion(() => Assert.That(loader.TryLoadMap(mapPath, out loadedMap, out loadedGrids)));

        Assert.That(loadedGrids, Has.Count.EqualTo(1));
        AssertLoadedChunk(entMan, chunks, loadedMap!.Value.Owner, mapChunkIndices, "map-chunk");
        AssertLoadedChunk(entMan, chunks, loadedGrids!.First().Owner, gridChunkIndices, "grid-chunk");
        await server.WaitPost(() => mapSys.DeleteMap(loadedMap.Value.Comp.MapId));
    }

    [Test]
    public async Task ChunkEntityWithoutMapOrGridRootDeletesOnStartup()
    {
        var server = StartServer(new() { Pool = false });
        await server.WaitIdleAsync();

        var entMan = server.EntMan;
        EntityUid root = default;
        EntityUid chunk = default;

        await server.WaitPost(() =>
        {
            root = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            chunk = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            entMan.AddComponent(chunk, new ChunkEntityComponent
            {
                Root = root,
                Chunk = new Vector2i(1, 2),
            });
        });

        await server.WaitRunTicks(1);

        Assert.That(entMan.Deleted(chunk), Is.True);
        await server.WaitPost(() => entMan.DeleteEntity(root));
    }

    private static void AssertLoadedChunk(
        IEntityManager entMan,
        ChunkEntitySystem chunks,
        EntityUid root,
        Vector2i indices,
        string id)
    {
        Assert.That(chunks.TryGetChunk(root, indices, out var loadedChunk), Is.True);
        Assert.That(loadedChunk!.Value.Comp.Root, Is.EqualTo(root));
        Assert.That(loadedChunk.Value.Comp.Chunk, Is.EqualTo(indices));

        var marker = entMan.GetComponent<EntitySaveTestComponent>(loadedChunk.Value.Owner);
        Assert.That(marker.Id, Is.EqualTo(id));

        var container = entMan.GetComponent<PvsChunkContainerComponent>(root);
        Assert.That(container.ChunkEntities, Does.Contain(loadedChunk.Value.Owner));

        var xform = entMan.GetComponent<TransformComponent>(loadedChunk.Value.Owner);
        Assert.That(xform.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(xform.MapID, Is.EqualTo(MapId.Nullspace));
    }
}
