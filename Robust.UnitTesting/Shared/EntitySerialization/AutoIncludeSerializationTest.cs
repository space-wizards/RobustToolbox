using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Components;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.UnitTesting.Shared.EntitySerialization.EntitySaveTestComponent;

namespace Robust.UnitTesting.Shared.EntitySerialization;

[TestFixture]
public sealed partial class AutoIncludeSerializationTest : RobustIntegrationTest
{
    [Test]
    public async Task TestAutoIncludeSerialization()
    {
        var server = StartServer(new() {Pool = false}); // Pool=false due to TileDef registration
        await server.WaitIdleAsync();
        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var tileMan = server.ResolveDependency<ITileDefinitionManager>();
        var mapPath = new ResPath($"{nameof(AutoIncludeSerializationTest)}_map.yml");
        var gridPath = new ResPath($"{nameof(AutoIncludeSerializationTest)}_grid.yml");

        tileMan.Register(new TileDef("space"));
        var tDef = new TileDef("a");
        tileMan.Register(tDef);

        // Create a map that contains an entity that references a nullspace entity.
        MapId mapId = default;
        Entity<TransformComponent, EntitySaveTestComponent> map = default;
        Entity<TransformComponent, EntitySaveTestComponent> grid = default;
        Entity<TransformComponent, EntitySaveTestComponent> onGrid = default;
        Entity<TransformComponent, EntitySaveTestComponent> offGrid = default;
        Entity<TransformComponent, EntitySaveTestComponent> nullSpace = default;

        void AssertCount(int expected) => Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(expected));

        await server.WaitPost(() =>
        {
            var mapUid = mapSys.CreateMap(out mapId);
            var gridUid = mapMan.CreateGridEntity(mapId);
            mapSys.SetTile(gridUid, Vector2i.Zero, new Tile(tDef.TileId));

            var onGridUid = entMan.SpawnEntity(null, new EntityCoordinates(gridUid, 0.5f, 0.5f));
            var offGridUid = entMan.SpawnEntity(null, new MapCoordinates(10f, 10f, mapId));
            var nullSpaceUid = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

            map = Get(mapUid, entMan);
            grid = Get(gridUid, entMan);
            onGrid = Get(onGridUid, entMan);
            offGrid = Get(offGridUid, entMan);
            nullSpace = Get(nullSpaceUid, entMan);
        });

        await server.WaitRunTicks(5);

        Assert.That(map.Comp1!.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(grid.Comp1!.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp1!.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(offGrid.Comp1!.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(nullSpace.Comp1!.ParentUid, Is.EqualTo(EntityUid.Invalid));

        // Assign unique ids.
        map.Comp2!.Id = nameof(map);
        grid.Comp2!.Id = nameof(grid);
        onGrid.Comp2!.Id = nameof(onGrid);
        offGrid.Comp2!.Id = nameof(offGrid);
        nullSpace.Comp2!.Id = nameof(nullSpace);

        // First simple map loading without any references to other entities.
        // This will cause the null-space entity to be lost.
        // Save the map, then delete all the entities.
        AssertCount(5);
        Assert.That(loader.TrySaveMap(mapId, mapPath));
        Assert.That(loader.TrySaveGrid(grid, gridPath));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        AssertCount(1);
        await server.WaitPost(() => entMan.DeleteEntity(nullSpace));
        AssertCount(0);

        // Load up the file that only saved the grid and check that the expected entities exist.
        await server.WaitPost(() => mapSys.CreateMap(out mapId));
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _)));

        AssertCount(2);
        grid = Find(nameof(grid), entMan);
        onGrid = Find(nameof(onGrid), entMan);
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));

        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        AssertCount(0);

        // Load up the map, and check that the expected entities exist.
        Entity<MapComponent>? loadedMap = default;
        HashSet<Entity<MapGridComponent>>? loadedGrids = default!;
        await server.WaitAssertion(() => Assert.That(loader.TryLoadMap(mapPath, out loadedMap, out loadedGrids)));
        mapId = loadedMap!.Value.Comp.MapId;
        Assert.That(loadedGrids, Has.Count.EqualTo(1));

        AssertCount(4);
        map = Find(nameof(map), entMan);
        grid = Find(nameof(grid), entMan);
        onGrid = Find(nameof(onGrid), entMan);
        offGrid = Find(nameof(offGrid), entMan);

        Assert.That(map.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(grid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(offGrid.Comp1.ParentUid, Is.EqualTo(map.Owner));

        // Re-spawn the nullspace entity
        await server.WaitPost(() =>
        {
            var nullSpaceUid = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            nullSpace = Get(nullSpaceUid, entMan);
            nullSpace.Comp2.Id = nameof(nullSpace);
        });

        // Repeat the previous saves, but with an entity that references the null-space entity.
        onGrid.Comp2.Entity = nullSpace.Owner;

        AssertCount(5);
        Assert.That(loader.TrySaveMap(mapId, mapPath));
        Assert.That(loader.TrySaveGrid(grid, gridPath));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        AssertCount(1);
        await server.WaitPost(() => entMan.DeleteEntity(nullSpace));
        AssertCount(0);

        // Load up the file that only saved the grid and check that the expected entities exist.
        await server.WaitPost(() => mapSys.CreateMap(out mapId));
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _)));

        AssertCount(3);
        grid = Find(nameof(grid), entMan);
        onGrid = Find(nameof(onGrid), entMan);
        nullSpace = Find(nameof(nullSpace), entMan);
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(onGrid.Comp2.Entity, Is.EqualTo(nullSpace.Owner));
        Assert.That(nullSpace.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));

        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        AssertCount(1);
        await server.WaitPost(() => entMan.DeleteEntity(nullSpace));
        AssertCount(0);

        // Load up the map, and check that the expected entities exist.
        await server.WaitAssertion(() => Assert.That(loader.TryLoadMap(mapPath, out loadedMap, out loadedGrids)));
        mapId = loadedMap!.Value.Comp.MapId;
        Assert.That(loadedGrids, Has.Count.EqualTo(1));

        AssertCount(5);
        map = Find(nameof(map), entMan);
        grid = Find(nameof(grid), entMan);
        onGrid = Find(nameof(onGrid), entMan);
        offGrid = Find(nameof(offGrid), entMan);
        nullSpace = Find(nameof(nullSpace), entMan);

        Assert.That(map.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(grid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(offGrid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp2.Entity, Is.EqualTo(nullSpace.Owner));
        Assert.That(nullSpace.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));

        // Check that attempting to save a reference to a non-null-space entity does not auto-include it.
        Entity<TransformComponent, EntitySaveTestComponent> otherMap = default;
        await server.WaitPost(() =>
        {
            var otherMapUid = mapSys.CreateMap();
            otherMap = Get(otherMapUid, entMan);
            otherMap.Comp2.Id = nameof(otherMap);
        });
        onGrid.Comp2.Entity = otherMap.Owner;

        // By default it should log an error, but tests don't have a nice way to validate that an error was logged, so we'll just suppress it.
        var opts = SerializationOptions.Default with {MissingEntityBehaviour = MissingEntityBehaviour.Ignore};
        AssertCount(6);
        Assert.That(loader.TrySaveMap(mapId, mapPath, opts));
        Assert.That(loader.TrySaveGrid(grid, gridPath, opts));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        await server.WaitPost(() => entMan.DeleteEntity(nullSpace));
        await server.WaitPost(() => entMan.DeleteEntity(otherMap));
        AssertCount(0);

        // Check the grid file
        await server.WaitPost(() => mapSys.CreateMap(out mapId));
        var dOpts = DeserializationOptions.Default with {LogInvalidEntities = false};
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _, dOpts)));
        AssertCount(2);
        grid = Find(nameof(grid), entMan);
        onGrid = Find(nameof(onGrid), entMan);
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        AssertCount(0);

        // Check the map file
        await server.WaitAssertion(() => Assert.That(loader.TryLoadMap(mapPath, out loadedMap, out loadedGrids, dOpts)));
        mapId = loadedMap!.Value.Comp.MapId;
        Assert.That(loadedGrids, Has.Count.EqualTo(1));
        AssertCount(4);
        map = Find(nameof(map), entMan);
        grid = Find(nameof(grid), entMan);
        onGrid = Find(nameof(onGrid), entMan);
        offGrid = Find(nameof(offGrid), entMan);
        Assert.That(map.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(grid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(offGrid.Comp1.ParentUid, Is.EqualTo(map.Owner));

        // repeat the check, but this time with auto inclusion fully enabled.
        Entity<TransformComponent, EntitySaveTestComponent> otherEnt = default;
        await server.WaitPost(() =>
        {
            var otherMapUid = mapSys.CreateMap(out var otherMapId);
            otherMap = Get(otherMapUid, entMan);
            otherMap.Comp2.Id = nameof(otherMap);

            var otherEntUid = entMan.SpawnEntity(null, new MapCoordinates(0, 0, otherMapId));
            otherEnt = Get(otherEntUid, entMan);
            otherEnt.Comp2.Id = nameof(otherEnt);

            var nullSpaceUid = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            nullSpace = Get(nullSpaceUid, entMan);
            nullSpace.Comp2.Id = nameof(nullSpace);
        });

        onGrid.Comp2.Entity = otherMap.Owner;
        otherEnt.Comp2!.Entity = nullSpace;

        AssertCount(7);
        opts = opts with {MissingEntityBehaviour = MissingEntityBehaviour.AutoInclude};
        Assert.That(loader.TrySaveGeneric(map.Owner, mapPath, out var cat, opts));
        Assert.That(cat, Is.EqualTo(FileCategory.Unknown));
        Assert.That(loader.TrySaveGeneric(grid.Owner, gridPath, out cat, opts));
        Assert.That(cat, Is.EqualTo(FileCategory.Unknown));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        await server.WaitPost(() => entMan.DeleteEntity(otherMap));
        await server.WaitPost(() => entMan.DeleteEntity(nullSpace));
        AssertCount(0);

        // Check the grid file
        await server.WaitPost(() => mapSys.CreateMap(out mapId));
        var mapLoadOpts = MapLoadOptions.Default with
        {
            DeserializationOptions = DeserializationOptions.Default with {LogOrphanedGrids = false}
        };
        LoadResult? result = default;
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGeneric(gridPath, out result, mapLoadOpts)));
        Assert.That(result!.Grids, Has.Count.EqualTo(1));
        Assert.That(result.Orphans, Is.Empty); // Grid was orphaned, but was adopted after a new map was created
        Assert.That(result.Maps, Has.Count.EqualTo(2));
        Assert.That(result.NullspaceEntities, Has.Count.EqualTo(1));
        Assert.That(entMan.Count<LoadedMapComponent>(), Is.EqualTo(1)); // auto-generated map isn't marked as "loaded"
        AssertCount(5);
        grid = Find(nameof(grid), entMan);
        onGrid = Find(nameof(onGrid), entMan);
        otherMap = Find(nameof(otherMap), entMan);
        otherEnt = Find(nameof(otherEnt), entMan);
        nullSpace = Find(nameof(nullSpace), entMan);
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(otherEnt.Comp1.ParentUid, Is.EqualTo(otherMap.Owner));
        Assert.That(otherMap.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(nullSpace.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        await server.WaitPost(() => entMan.DeleteEntity(otherMap));
        await server.WaitPost(() => entMan.DeleteEntity(grid.Comp1.ParentUid));
        await server.WaitPost(() => entMan.DeleteEntity(nullSpace));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        AssertCount(0);

        // Check the map file
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGeneric(mapPath, out result)));
        Assert.That(result.Orphans, Is.Empty);
        Assert.That(result.NullspaceEntities, Has.Count.EqualTo(1));
        Assert.That(result.Grids, Has.Count.EqualTo(1));
        Assert.That(result.Maps, Has.Count.EqualTo(2));
        Assert.That(entMan.Count<LoadedMapComponent>(), Is.EqualTo(2));
        AssertCount(7);
        map = Find(nameof(map), entMan);
        grid = Find(nameof(grid), entMan);
        onGrid = Find(nameof(onGrid), entMan);
        offGrid = Find(nameof(offGrid), entMan);
        otherMap = Find(nameof(otherMap), entMan);
        otherEnt = Find(nameof(otherEnt), entMan);
        nullSpace = Find(nameof(nullSpace), entMan);
        Assert.That(map.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(grid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(offGrid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(otherEnt.Comp1.ParentUid, Is.EqualTo(otherMap.Owner));
        Assert.That(otherMap.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(nullSpace.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        await server.WaitPost(() => entMan.DeleteEntity(map));
        await server.WaitPost(() => entMan.DeleteEntity(otherMap));
        await server.WaitPost(() => entMan.DeleteEntity(nullSpace));
        AssertCount(0);

        Assert.That(entMan.Count<LoadedMapComponent>(), Is.EqualTo(0));
        Assert.That(entMan.Count<MapComponent>(), Is.EqualTo(0));
    }
}
