using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
using static Robust.UnitTesting.Shared.EntitySerialization.EntitySaveTestComponent;

namespace Robust.UnitTesting.Shared.EntitySerialization;

/// <summary>
/// Test that loading a pre-init map/grid onto a post-init map should initialize, while loading a post-init map/grid
/// onto a paused map should pause it.
/// </summary>
[TestFixture]
public sealed partial class MapMergeTest : RobustIntegrationTest
{
    [Test]
    public async Task TestMapMerge()
    {
        var server = StartServer(new() {Pool = false}); // Pool=false due to TileDef registration
        await server.WaitIdleAsync();
        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var tileMan = server.ResolveDependency<ITileDefinitionManager>();

        var mapPath = new ResPath($"{nameof(TestMapMerge)}_map.yml");
        var gridPath = new ResPath($"{nameof(TestMapMerge)}_grid.yml");

        tileMan.Register(new TileDef("space"));
        var tDef = new TileDef("a");
        tileMan.Register(tDef);

        MapId mapId = default;
        Entity<TransformComponent, EntitySaveTestComponent> map = default;
        Entity<TransformComponent, EntitySaveTestComponent> ent = default;
        Entity<TransformComponent, EntitySaveTestComponent> grid = default;

        await server.WaitPost(() =>
        {
            var mapUid = mapSys.CreateMap(out mapId, runMapInit: false);
            var gridEnt = mapMan.CreateGridEntity(mapId);
            mapSys.SetTile(gridEnt, Vector2i.Zero, new Tile(tDef.TileId));
            var entUid = entMan.SpawnEntity(null, new MapCoordinates(10, 10, mapId));
            map = Get(mapUid, entMan);
            ent = Get(entUid, entMan);
            grid = Get(gridEnt.Owner, entMan);
        });

        void AssertPaused(EntityUid uid, bool expected = true)
        {
            Assert.That(entMan.GetComponent<MetaDataComponent>(uid).EntityPaused, Is.EqualTo(expected));
        }

        void AssertPreInit(EntityUid uid, bool expected = true)
        {
            Assert.That(entMan!.GetComponent<MetaDataComponent>(uid).EntityLifeStage,
                expected
                    ? Is.LessThan(EntityLifeStage.MapInitialized)
                    : Is.EqualTo(EntityLifeStage.MapInitialized));
        }

        map.Comp2!.Id = nameof(map);
        ent.Comp2!.Id = nameof(ent);
        grid.Comp2!.Id = nameof(grid);

        AssertPaused(map);
        AssertPreInit(map);
        AssertPaused(ent);
        AssertPreInit(ent);
        AssertPaused(grid);
        AssertPreInit(grid);

        // Save then delete everything
        await server.WaitAssertion(() => Assert.That(loader.TrySaveMap(map, mapPath)));
        await server.WaitAssertion(() => Assert.That(loader.TrySaveGrid(grid, gridPath)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(3));
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load a grid onto a pre-init map.
        await server.WaitPost(() => mapSys.CreateMap(out mapId, runMapInit: false));
        Assert.That(mapSys.IsInitialized(mapId), Is.False);
        Assert.That(mapSys.IsPaused(mapId), Is.True);
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(1));
        grid = Find(nameof(grid), entMan);
        AssertPaused(grid);
        AssertPreInit(grid);
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Merge a map onto a pre-init map.
        await server.WaitPost(() => mapSys.CreateMap(out mapId, runMapInit: false));
        Assert.That(mapSys.IsInitialized(mapId), Is.False);
        Assert.That(mapSys.IsPaused(mapId), Is.True);
        await server.WaitAssertion(() => Assert.That(loader.TryMergeMap(mapId, mapPath, out _)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(2)); // The loaded map entity gets deleted after merging
        ent = Find(nameof(ent), entMan);
        grid = Find(nameof(grid), entMan);
        AssertPaused(grid);
        AssertPreInit(grid);
        AssertPaused(ent);
        AssertPreInit(ent);
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load a grid onto a post-init map.
        await server.WaitPost(() => mapSys.CreateMap(out mapId, runMapInit: true));
        Assert.That(mapSys.IsInitialized(mapId), Is.True);
        Assert.That(mapSys.IsPaused(mapId), Is.False);
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(1));
        grid = Find(nameof(grid), entMan);
        AssertPaused(grid, false);
        AssertPreInit(grid, false);
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Merge a map onto a post-init map.
        await server.WaitPost(() => mapSys.CreateMap(out mapId, runMapInit: true));
        Assert.That(mapSys.IsInitialized(mapId), Is.True);
        Assert.That(mapSys.IsPaused(mapId), Is.False);
        await server.WaitAssertion(() => Assert.That(loader.TryMergeMap(mapId, mapPath, out _)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(2));
        ent = Find(nameof(ent), entMan);
        grid = Find(nameof(grid), entMan);
        AssertPaused(grid, false);
        AssertPreInit(grid, false);
        AssertPaused(ent, false);
        AssertPreInit(ent, false);
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load a grid onto a paused post-init map.
        await server.WaitPost(() => mapSys.CreateMap(out mapId, runMapInit: true));
        await server.WaitPost(() => mapSys.SetPaused(mapId, true));
        Assert.That(mapSys.IsInitialized(mapId), Is.True);
        Assert.That(mapSys.IsPaused(mapId), Is.True);
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(1));
        grid = Find(nameof(grid), entMan);
        AssertPaused(grid);
        AssertPreInit(grid, false);
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Merge a map onto a paused post-init map.
        await server.WaitPost(() => mapSys.CreateMap(out mapId, runMapInit: true));
        await server.WaitPost(() => mapSys.SetPaused(mapId, true));
        Assert.That(mapSys.IsInitialized(mapId), Is.True);
        Assert.That(mapSys.IsPaused(mapId), Is.True);
        await server.WaitAssertion(() => Assert.That(loader.TryMergeMap(mapId, mapPath, out _)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(2));
        ent = Find(nameof(ent), entMan);
        grid = Find(nameof(grid), entMan);
        AssertPaused(grid);
        AssertPreInit(grid, false);
        AssertPaused(ent);
        AssertPreInit(ent, false);
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Check that the map initialization deserialziation options have no effect.
        // We are loading onto an existing map, deserialization shouldn't modify it directly.


        // Load a grid onto a pre-init map, with InitializeMaps = true
        await server.WaitPost(() => mapSys.CreateMap(out mapId, runMapInit: false));
        Assert.That(mapSys.IsInitialized(mapId), Is.False);
        Assert.That(mapSys.IsPaused(mapId), Is.True);
        var opts = DeserializationOptions.Default with {InitializeMaps = true};
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _, opts)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(1));
        grid = Find(nameof(grid), entMan);
        AssertPaused(grid);
        AssertPreInit(grid);
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Merge a map onto a pre-init map, with InitializeMaps = true
        await server.WaitPost(() => mapSys.CreateMap(out mapId, runMapInit: false));
        Assert.That(mapSys.IsInitialized(mapId), Is.False);
        Assert.That(mapSys.IsPaused(mapId), Is.True);
        opts = DeserializationOptions.Default with {InitializeMaps = true};
        await server.WaitAssertion(() => Assert.That(loader.TryMergeMap(mapId, mapPath, out _, opts)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(2)); // The loaded map entity gets deleted after merging
        ent = Find(nameof(ent), entMan);
        grid = Find(nameof(grid), entMan);
        AssertPaused(grid);
        AssertPreInit(grid);
        AssertPaused(ent);
        AssertPreInit(ent);
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load a grid onto a post-init map, with PauseMaps = true
        await server.WaitPost(() => mapSys.CreateMap(out mapId, runMapInit: true));
        Assert.That(mapSys.IsInitialized(mapId), Is.True);
        Assert.That(mapSys.IsPaused(mapId), Is.False);
        opts = DeserializationOptions.Default with {PauseMaps = true};
        await server.WaitAssertion(() => Assert.That(loader.TryLoadGrid(mapId, gridPath, out _, opts)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(1));
        grid = Find(nameof(grid), entMan);
        AssertPaused(grid, false);
        AssertPreInit(grid, false);
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));

        // Load a grid onto a post-init map, with PauseMaps = true
        await server.WaitPost(() => mapSys.CreateMap(out mapId, runMapInit: true));
        Assert.That(mapSys.IsInitialized(mapId), Is.True);
        Assert.That(mapSys.IsPaused(mapId), Is.False);
        opts = DeserializationOptions.Default with {PauseMaps = true};
        await server.WaitAssertion(() => Assert.That(loader.TryMergeMap(mapId, mapPath, out _, opts)));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(2));
        ent = Find(nameof(ent), entMan);
        grid = Find(nameof(grid), entMan);
        AssertPaused(grid, false);
        AssertPreInit(grid, false);
        AssertPaused(ent, false);
        AssertPreInit(ent, false);
        await server.WaitPost(() => mapSys.DeleteMap(mapId));
        Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));
    }
}
