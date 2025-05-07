using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.UnitTesting.Shared.EntitySerialization.EntitySaveTestComponent;

namespace Robust.UnitTesting.Shared.EntitySerialization;

[TestFixture]
public sealed partial class OrphanSerializationTest : RobustIntegrationTest
{
    /// <summary>
    /// Check that we can save & load a file containing multiple orphaned (non-grid) entities.
    /// </summary>
    [Test]
    public async Task TestMultipleOrphanSerialization()
    {
        var server = StartServer();
        await server.WaitIdleAsync();
        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var xform = server.System<SharedTransformSystem>();
        var pathA = new ResPath($"{nameof(TestMultipleOrphanSerialization)}_A.yml");
        var pathB = new ResPath($"{nameof(TestMultipleOrphanSerialization)}_B.yml");
        var pathCombined = new ResPath($"{nameof(TestMultipleOrphanSerialization)}_C.yml");

        // Spawn multiple entities on a map
        MapId mapId = default;
        Entity<TransformComponent, EntitySaveTestComponent> entA = default;
        Entity<TransformComponent, EntitySaveTestComponent> entB = default;
        Entity<TransformComponent, EntitySaveTestComponent> child = default;

        await server.WaitPost(() =>
        {
            mapSys.CreateMap(out mapId);
            var entAUid = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var entBUid = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var childUid = entMan.SpawnEntity(null, new EntityCoordinates(entBUid, 0, 0));
            entA = Get(entAUid, entMan);
            entB = Get(entBUid, entMan);
            child = Get(childUid, entMan);
            entA.Comp2.Id = nameof(entA);
            entB.Comp2.Id = nameof(entB);
            child.Comp2.Id = nameof(child);
            xform.SetLocalPosition(entB.Owner, new (100,100));
        });

        // Entities are not in null-space
        Assert.That(entA.Comp1!.ParentUid, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(entB.Comp1!.ParentUid, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(child.Comp1!.ParentUid, Is.EqualTo(entB.Owner));

        // Save the entities without their map
        Assert.That(loader.TrySaveEntity(entA, pathA));
        Assert.That(loader.TrySaveEntity(entB, pathB));
        Assert.That(loader.TrySaveGeneric([entA.Owner, entB.Owner], pathCombined, out var cat));
        Assert.That(cat, Is.EqualTo(FileCategory.Unknown));

        // Delete all the entities.
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(3));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load in the file containing only entA.
        await server.WaitAssertion(() => Assert.That(loader.TryLoadEntity(pathA, out _)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(1));
        entA = Find(nameof(entA), entMan);
        Assert.That(entA.Comp1!.ParentUid, Is.EqualTo(EntityUid.Invalid));
        await server.WaitPost(() => entMan.DeleteEntity(entA));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load in the file containing entB and its child
        await server.WaitAssertion(() => Assert.That(loader.TryLoadEntity(pathB, out _)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(2));
        entB = Find(nameof(entB), entMan);
        child = Find(nameof(child), entMan);
        // Even though the entities are in null-space their local position is preserved.
        // This is so that you can save multiple entities on a map, without saving the map, while still preserving
        // relative positions for loading them onto some other map.
        Assert.That(entB.Comp1.LocalPosition, Is.Approximately(new Vector2(100, 100)));
        Assert.That(entB.Comp1!.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(child.Comp1!.ParentUid, Is.EqualTo(entB.Owner));
        await server.WaitPost(() => entMan.DeleteEntity(entB));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load the file that contains both of them
        LoadResult? result = null;
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGeneric(pathCombined, out result)));
        Assert.That(result!.Category, Is.EqualTo(FileCategory.Unknown));
        Assert.That(result.Orphans, Has.Count.EqualTo(2));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(3));
        entA = Find(nameof(entA), entMan);
        entB = Find(nameof(entB), entMan);
        child = Find(nameof(child), entMan);
        Assert.That(entA.Comp1!.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(entB.Comp1!.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(entB.Comp1.LocalPosition, Is.Approximately(new Vector2(100, 100)));
        Assert.That(child.Comp1!.ParentUid, Is.EqualTo(entB.Owner));
        await server.WaitPost(() => entMan.DeleteEntity(entA));
        await server.WaitPost(() => entMan.DeleteEntity(entB));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));
    }

    /// <summary>
    /// Check that we can save & load a file containing multiple orphaned grid entities.
    /// </summary>
    [Test]
    public async Task TestOrphanedGridSerialization()
    {
        var server = StartServer(new() {Pool = false}); // Pool=false due to TileDef registration
        await server.WaitIdleAsync();
        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var xform = server.System<SharedTransformSystem>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var tileMan = server.ResolveDependency<ITileDefinitionManager>();
        var pathA = new ResPath($"{nameof(TestOrphanedGridSerialization)}_A.yml");
        var pathB = new ResPath($"{nameof(TestOrphanedGridSerialization)}_B.yml");
        var pathCombined = new ResPath($"{nameof(TestOrphanedGridSerialization)}_C.yml");

        tileMan.Register(new TileDef("space"));
        var tDef = new TileDef("a");
        tileMan.Register(tDef);

        // Spawn multiple entities on a map
        MapId mapId = default;
        Entity<TransformComponent, EntitySaveTestComponent> map = default;
        Entity<TransformComponent, EntitySaveTestComponent> gridA = default;
        Entity<TransformComponent, EntitySaveTestComponent> gridB = default;
        Entity<TransformComponent, EntitySaveTestComponent> child = default;

        await server.WaitPost(() =>
        {
            var mapUid = mapSys.CreateMap(out mapId);
            map = Get(mapUid, entMan);

            var gridAUid = mapMan.CreateGridEntity(mapId);
            mapSys.SetTile(gridAUid, Vector2i.Zero, new Tile(tDef.TileId));
            gridA = Get(gridAUid, entMan);
            xform.SetLocalPosition(gridA.Owner, new(100, 100));

            var gridBUid = mapMan.CreateGridEntity(mapId);
            mapSys.SetTile(gridBUid, Vector2i.Zero, new Tile(tDef.TileId));
            gridB = Get(gridBUid, entMan);

            var childUid = entMan.SpawnEntity(null, new EntityCoordinates(gridBUid, 0.5f, 0.5f));
            child = Get(childUid, entMan);

            map.Comp2.Id = nameof(map);
            gridA.Comp2.Id = nameof(gridA);
            gridB.Comp2.Id = nameof(gridB);
            child.Comp2.Id = nameof(child);
        });

        await server.WaitRunTicks(5);

        // grids are not in null-space
        Assert.That(gridA.Comp1!.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(gridB.Comp1!.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(child.Comp1!.ParentUid, Is.EqualTo(gridB.Owner));
        Assert.That(map.Comp1!.ParentUid, Is.EqualTo(EntityUid.Invalid));

        // Save the grids without their map
        await server.WaitAssertion(() => Assert.That(loader.TrySaveGrid(gridA, pathA)));
        await server.WaitAssertion(() => Assert.That(loader.TrySaveGrid(gridB, pathB)));
        FileCategory cat = default;
        await server.WaitAssertion(() => Assert.That(loader.TrySaveGeneric([gridA.Owner, gridB.Owner], pathCombined, out cat)));
        Assert.That(cat, Is.EqualTo(FileCategory.Unknown));

        // Delete all the entities.
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(4));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load in the file containing only gridA.
        EntityUid newMap = default;
        await server.WaitPost(() => newMap = mapSys.CreateMap(out mapId));
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, pathA, out _)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(1));
        gridA = Find(nameof(gridA), entMan);
        Assert.That(gridA.Comp1.LocalPosition, Is.Approximately(new Vector2(100, 100)));
        Assert.That(gridA.Comp1!.ParentUid, Is.EqualTo(newMap));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load in the file containing gridB and its child
        await server.WaitPost(() => newMap = mapSys.CreateMap(out mapId));
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, pathB, out _)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(2));
        gridB = Find(nameof(gridB), entMan);
        child = Find(nameof(child), entMan);
        Assert.That(gridB.Comp1!.ParentUid, Is.EqualTo(newMap));
        Assert.That(child.Comp1!.ParentUid, Is.EqualTo(gridB.Owner));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load the file that contains both of them.
        // This uses the generic loader, and should automatically create maps for both grids.
        LoadResult? result = null;
        var opts = MapLoadOptions.Default with
        {
            DeserializationOptions = DeserializationOptions.Default with {LogOrphanedGrids = false}
        };
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGeneric(pathCombined, out result, opts)));
        Assert.That(result!.Category, Is.EqualTo(FileCategory.Unknown));
        Assert.That(result.Grids, Has.Count.EqualTo(2));
        Assert.That(result.Maps, Has.Count.EqualTo(2));
        Assert.That(entMan.Count<LoadedMapComponent>(), Is.EqualTo(0));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(3));
        gridA = Find(nameof(gridA), entMan);
        gridB = Find(nameof(gridB), entMan);
        child = Find(nameof(child), entMan);
        Assert.That(gridA.Comp1.LocalPosition, Is.Approximately(new Vector2(100, 100)));
        Assert.That(gridA.Comp1!.ParentUid, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(gridB.Comp1!.ParentUid, Is.Not.EqualTo(EntityUid.Invalid));
        Assert.That(child.Comp1!.ParentUid, Is.EqualTo(gridB.Owner));
        await server.WaitPost(() =>
        {
            foreach (var ent in result.Maps)
            {
                entMan.DeleteEntity(ent.Owner);
            }
        });
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));
    }
}
