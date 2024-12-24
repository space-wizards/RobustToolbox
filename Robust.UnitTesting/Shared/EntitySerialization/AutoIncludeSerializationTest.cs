using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.EntitySerialization;

/// <summary>
/// Simple component that stores a reference to another entity.
/// </summary>
[RegisterComponent]
public sealed partial class TestEntityRefComponent : Component
{
    [DataField] public EntityUid? Entity;

    /// <summary>
    /// Give each entity a unique id to identify them across map saves & loads.
    /// </summary>
    [DataField] public string? Id;
}

/// <summary>
/// Dummy tile definition for serializing grids.
/// </summary>
public sealed class TileDef(string id) : ITileDefinition
{
    public ushort TileId { get; set; }
    public string Name => id;
    public string ID => id;
    public ResPath? Sprite => null;
    public Dictionary<Direction, ResPath> EdgeSprites => new();
    public int EdgeSpritePriority => 0;
    public float Friction => 0;
    public byte Variants => 0;
    public void AssignTileId(ushort id) => TileId = id;
}

[TestFixture]
public sealed partial class AutoIncludeSerializationTest : RobustIntegrationTest
{

    [Test]
    public async Task TestAutoIncludeSerialization()
    {
        var server = StartServer();
        await server.WaitIdleAsync();
        var ent = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var tileMan = server.ResolveDependency<ITileDefinitionManager>();
        var mapPath = new ResPath($"{nameof(AutoIncludeSerializationTest)}_map.yml");
        var gridPath = new ResPath($"{nameof(AutoIncludeSerializationTest)}_grid.yml");

        tileMan.Register(new TileDef("space"));
        var tDef = new TileDef("a");
        tileMan.Register(tDef);

        Entity<TransformComponent, TestEntityRefComponent> Find(string name, Entity<TestEntityRefComponent>[] ents)
        {
            var found = ents!.FirstOrNull(x => x.Comp.Id == name);
            Assert.That(found, Is.Not.Null);
            return (found!.Value.Owner, server.Transform(found.Value.Owner), found.Value.Comp);
        }

        // Create a map that contains an entity that references a nullspace entity.
        MapId mapId = default;
        Entity<TransformComponent, TestEntityRefComponent> map = default;
        Entity<TransformComponent, TestEntityRefComponent> grid = default;
        Entity<TransformComponent, TestEntityRefComponent> onGrid = default;
        Entity<TransformComponent, TestEntityRefComponent> offGrid = default;
        Entity<TransformComponent, TestEntityRefComponent> nullSpace = default;

        await server.WaitPost(() =>
        {
            var mapUid = mapSys.CreateMap(out mapId);
            var gridUid = mapMan.CreateGridEntity(mapId);
            mapSys.SetTile(gridUid, Vector2i.Zero, new Tile(tDef.TileId));

            var onGridUid = ent.SpawnEntity(null, new EntityCoordinates(gridUid, 0.5f, 0.5f));
            var offGridUid = ent.SpawnEntity(null, new MapCoordinates(10f, 10f, mapId));
            var nullSpaceUid = ent.SpawnEntity(null, MapCoordinates.Nullspace);

            map = (mapUid, server.Transform(mapUid), ent.AddComponent<TestEntityRefComponent>(mapUid));
            grid = (gridUid, server.Transform(gridUid), ent.AddComponent<TestEntityRefComponent>(gridUid));
            onGrid = (onGridUid, server.Transform(onGridUid), ent.AddComponent<TestEntityRefComponent>(onGridUid));
            offGrid = (offGridUid, server.Transform(offGridUid), ent.AddComponent<TestEntityRefComponent>(offGridUid));
            nullSpace = (nullSpaceUid, server.Transform(nullSpaceUid), ent.AddComponent<TestEntityRefComponent>(
                nullSpaceUid));
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
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(5));
        await server.WaitPost(() => loader.SaveMap(mapId, mapPath));
        await server.WaitPost(() => loader.SaveGrid(grid, gridPath));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(1));
        await server.WaitPost(() => ent.DeleteEntity(nullSpace));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(0));

        // Load up the file that only saved the grid and check that the expected entities exist.
        await server.WaitPost(() => mapSys.CreateMap(out mapId));
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _)));

        var ents = ent.AllEntities<TestEntityRefComponent>();
        Assert.That(ents.Length, Is.EqualTo(2));
        grid = Find(nameof(grid), ents);
        onGrid = Find(nameof(onGrid), ents);
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));

        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(0));

        // Load up the map, and check that the expected entities exist.
        Entity<MapComponent>? loadedMap = default;
        HashSet<Entity<MapGridComponent>>? loadedGrids = default!;
        await server.WaitAssertion(() => Assert.That(loader.TryLoadMap(mapPath, out loadedMap, out loadedGrids)));
        mapId = loadedMap!.Value.Comp.MapId;
        Assert.That(loadedGrids.Count, Is.EqualTo(1));

        ents = ent.AllEntities<TestEntityRefComponent>();
        Assert.That(ents.Length, Is.EqualTo(4)); // only 4 - nullspace entity was lost
        map = Find(nameof(map), ents);
        grid = Find(nameof(grid), ents);
        onGrid = Find(nameof(onGrid), ents);
        offGrid = Find(nameof(offGrid), ents);

        Assert.That(map.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(grid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(offGrid.Comp1.ParentUid, Is.EqualTo(map.Owner));

        // Re-spawn the nullspace entity
        await server.WaitPost(() =>
        {
            var nullSpaceUid = ent.SpawnEntity(null, MapCoordinates.Nullspace);
            nullSpace = (nullSpaceUid, server.Transform(nullSpaceUid), ent.AddComponent<TestEntityRefComponent>(
                nullSpaceUid));
            nullSpace.Comp2.Id = nameof(nullSpace);
        });

        // Repeat the previous saves, but with an entity that references the null-space entity.
        onGrid.Comp2.Entity = nullSpace.Owner;

        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(5));
        await server.WaitPost(() => loader.SaveMap(mapId, mapPath));
        await server.WaitPost(() => loader.SaveGrid(grid, gridPath));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(1));
        await server.WaitPost(() => ent.DeleteEntity(nullSpace));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(0));

        // Load up the file that only saved the grid and check that the expected entities exist.
        await server.WaitPost(() => mapSys.CreateMap(out mapId));
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _)));

        ents = ent.AllEntities<TestEntityRefComponent>();
        Assert.That(ents.Length, Is.EqualTo(3));
        grid = Find(nameof(grid), ents);
        onGrid = Find(nameof(onGrid), ents);
        nullSpace = Find(nameof(nullSpace), ents);
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(onGrid.Comp2.Entity, Is.EqualTo(nullSpace.Owner));
        Assert.That(nullSpace.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));

        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(1));
        await server.WaitPost(() => ent.DeleteEntity(nullSpace));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(0));

        // Load up the map, and check that the expected entities exist.
        await server.WaitAssertion(() => Assert.That(loader.TryLoadMap(mapPath, out loadedMap, out loadedGrids)));
        mapId = loadedMap!.Value.Comp.MapId;
        Assert.That(loadedGrids.Count, Is.EqualTo(1));

        ents = ent.AllEntities<TestEntityRefComponent>();
        Assert.That(ents.Length, Is.EqualTo(5));
        map = Find(nameof(map), ents);
        grid = Find(nameof(grid), ents);
        onGrid = Find(nameof(onGrid), ents);
        offGrid = Find(nameof(offGrid), ents);
        nullSpace = Find(nameof(nullSpace), ents);

        Assert.That(map.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(grid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(offGrid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp2.Entity, Is.EqualTo(nullSpace.Owner));
        Assert.That(nullSpace.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));

        // Check that attempting to save a reference to a non-null-space entity does not auto-include it.
        Entity<TransformComponent, TestEntityRefComponent> otherMap = default;
        await server.WaitPost(() =>
        {
            var uid = mapSys.CreateMap();
            otherMap = (uid, server.Transform(uid), ent.AddComponent<TestEntityRefComponent>(uid));
            otherMap.Comp2.Id = nameof(otherMap);
        });
        onGrid.Comp2.Entity = otherMap.Owner;

        // By default it should log an error, but tests don't have a nice way to validate that an error was logged, so we'll just suppress it.
        var opts = SerializationOptions.Default with {MissingEntityBehaviour = MissingEntityBehaviour.Ignore};
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(6));
        await server.WaitPost(() => loader.SaveMap(mapId, mapPath, opts));
        await server.WaitPost(() => loader.SaveGrid(grid, gridPath, opts));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        await server.WaitPost(() => ent.DeleteEntity(nullSpace));
        await server.WaitPost(() => ent.DeleteEntity(otherMap));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(0));

        // Check the grid file
        await server.WaitPost(() => mapSys.CreateMap(out mapId));
        var dOpts = DeserializationOptions.Default with {LogInvalidEntities = false};
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _, dOpts)));
        ents = ent.AllEntities<TestEntityRefComponent>();
        Assert.That(ents.Length, Is.EqualTo(2));
        grid = Find(nameof(grid), ents);
        onGrid = Find(nameof(onGrid), ents);
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(0));

        // Check the map file
        await server.WaitAssertion(() => Assert.That(loader.TryLoadMap(mapPath, out loadedMap, out loadedGrids, dOpts)));
        mapId = loadedMap!.Value.Comp.MapId;
        Assert.That(loadedGrids.Count, Is.EqualTo(1));
        ents = ent.AllEntities<TestEntityRefComponent>();
        Assert.That(ents.Length, Is.EqualTo(4));
        map = Find(nameof(map), ents);
        grid = Find(nameof(grid), ents);
        onGrid = Find(nameof(onGrid), ents);
        offGrid = Find(nameof(offGrid), ents);
        Assert.That(map.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(grid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(offGrid.Comp1.ParentUid, Is.EqualTo(map.Owner));

        // repeat the check, but this time with auto inclusion fully enabled.
        Entity<TransformComponent, TestEntityRefComponent> otherEnt;
        await server.WaitPost(() =>
        {
            var uid = mapSys.CreateMap(out var otherMapId);
            otherMap = (uid, server.Transform(uid), ent.AddComponent<TestEntityRefComponent>(uid));
            otherMap.Comp2.Id = nameof(otherMap);
            var otherUid = ent.SpawnEntity(null, new MapCoordinates(0, 0, otherMapId));
            otherEnt = (otherUid, server.Transform(otherUid), ent.AddComponent<TestEntityRefComponent>(otherUid));
            otherEnt.Comp2.Id = nameof(otherEnt);
        });

        onGrid.Comp2.Entity = otherMap.Owner;

        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(6));
        opts = opts with {MissingEntityBehaviour = MissingEntityBehaviour.AutoIncludeChildren};
        await server.WaitPost(() => loader.SaveEntity(map.Owner, mapPath, opts));
        await server.WaitPost(() => loader.SaveEntity(grid.Owner, gridPath, opts));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        await server.WaitPost(() => ent.DeleteEntity(otherMap));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(0));

        // Check the grid file
        await server.WaitPost(() => mapSys.CreateMap(out mapId));
        var mapLoadOpts = MapLoadOptions.Default with
        {
            DeserializationOptions = DeserializationOptions.Default with {LogOrphanedGrids = false}
        };
        LoadResult? result = default;
        await server.WaitAssertion(() => Assert.That(loader.TryLoadEntities(gridPath, out result, mapLoadOpts)));
        Assert.That(result!.Grids.Count, Is.EqualTo(1));
        Assert.That(result.Maps.Count, Is.EqualTo(2));
        ents = ent.AllEntities<TestEntityRefComponent>();
        Assert.That(ents.Length, Is.EqualTo(4));
        grid = Find(nameof(grid), ents);
        onGrid = Find(nameof(onGrid), ents);
        otherMap = Find(nameof(otherMap), ents);
        otherEnt = Find(nameof(otherEnt), ents);
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(otherEnt.Comp1.ParentUid, Is.EqualTo(otherMap.Owner));
        Assert.That(otherMap.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        await server.WaitPost(() => ent.DeleteEntity(otherMap));
        await server.WaitPost(() => ent.DeleteEntity(grid.Comp1.ParentUid));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(0));

        // Check the map file
        await server.WaitAssertion(() => Assert.That(loader.TryLoadEntities(mapPath, out result)));
        Assert.That(result!.Grids.Count, Is.EqualTo(1));
        Assert.That(result.Maps.Count, Is.EqualTo(2));
        Assert.That(loadedGrids.Count, Is.EqualTo(1));
        ents = ent.AllEntities<TestEntityRefComponent>();
        Assert.That(ents.Length, Is.EqualTo(6));
        map = Find(nameof(map), ents);
        grid = Find(nameof(grid), ents);
        onGrid = Find(nameof(onGrid), ents);
        offGrid = Find(nameof(offGrid), ents);
        otherMap = Find(nameof(otherMap), ents);
        otherEnt = Find(nameof(otherEnt), ents);
        Assert.That(map.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        Assert.That(grid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(onGrid.Comp1.ParentUid, Is.EqualTo(grid.Owner));
        Assert.That(offGrid.Comp1.ParentUid, Is.EqualTo(map.Owner));
        Assert.That(otherEnt.Comp1.ParentUid, Is.EqualTo(otherMap.Owner));
        Assert.That(otherMap.Comp1.ParentUid, Is.EqualTo(EntityUid.Invalid));
        await server.WaitPost(() => ent.DeleteEntity(map));
        await server.WaitPost(() => ent.DeleteEntity(otherMap));
        Assert.That(ent.Count<TestEntityRefComponent>(), Is.EqualTo(0));
    }
}
