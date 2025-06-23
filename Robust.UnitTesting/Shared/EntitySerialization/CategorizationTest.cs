using System.Linq;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.EntitySerialization;

[TestFixture]
public sealed partial class CategorizationTest : RobustIntegrationTest
{
    /// <summary>
    /// Check that file categories are correctly assigned when saving & loading different combinations of entites.
    /// </summary>
    [Test]
    [TestOf(typeof(FileCategory))]
    public async Task TestCategorization()
    {
        var server = StartServer(new() {Pool = false}); // Pool=false due to TileDef registration
        await server.WaitIdleAsync();
        var entMan = server.EntMan;
        var meta = server.System<MetaDataSystem>();
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var mapMan = server.ResolveDependency<IMapManager>();
        var tileMan = server.ResolveDependency<ITileDefinitionManager>();
        var path = new ResPath($"{nameof(TestCategorization)}.yml");

        tileMan.Register(new TileDef("space"));
        var tDef = new TileDef("a");
        tileMan.Register(tDef);

        EntityUid mapA = default;
        EntityUid mapB = default;
        EntityUid gridA = default; // grid on map A
        EntityUid gridB = default; // grid on map B
        EntityUid entA = default;  // ent on grid A
        EntityUid entB = default;  // ent on grid B
        EntityUid entC = default; // a separate entity on grid B
        EntityUid child = default; // child of entB
        EntityUid @null = default; // nullspace entity

        await server.WaitPost(() =>
        {
            mapA = mapSys.CreateMap(out var mapIdA);
            mapB = mapSys.CreateMap(out var mapIdB);
            var gridEntA = mapMan.CreateGridEntity(mapIdA);
            var gridEntB = mapMan.CreateGridEntity(mapIdB);
            mapSys.SetTile(gridEntA, Vector2i.Zero, new Tile(tDef.TileId));
            mapSys.SetTile(gridEntB, Vector2i.Zero, new Tile(tDef.TileId));
            gridA = gridEntA.Owner;
            gridB = gridEntB.Owner;
            entA = entMan.SpawnEntity(null, new EntityCoordinates(gridA, 0.5f, 0.5f));
            entB = entMan.SpawnEntity(null, new EntityCoordinates(gridB, 0.5f, 0.5f));
            entC = entMan.SpawnEntity(null, new EntityCoordinates(gridB, 0.5f, 0.5f));
            child = entMan.SpawnEntity(null, new EntityCoordinates(entB, 0.5f, 0.5f));
            @null = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
        });

        FileCategory Save(params EntityUid[] ents)
        {
            FileCategory cat = FileCategory.Unknown;
            Assert.That(loader.TrySaveGeneric(ents.ToHashSet(), path, out cat));
            return cat;
        }

        async Task<LoadResult> Load(FileCategory expected, int count)
        {
            var opts = MapLoadOptions.Default with
            {
                ExpectedCategory = expected,
                DeserializationOptions = DeserializationOptions.Default with { LogOrphanedGrids = false}
            };
            LoadResult? result = null;
            await server.WaitAssertion(() => Assert.That(loader.TryLoadGeneric(path, out result, opts)));
            Assert.That(result!.Category, Is.EqualTo(expected));
            Assert.That(result.Entities, Has.Count.EqualTo(count));
            return result;
        }

        async Task SaveAndLoad(FileCategory expected, int count, params EntityUid[] ents)
        {
            var cat = Save(ents);
            Assert.That(cat, Is.EqualTo(expected));
            var result = await Load(expected, count);
            await server.WaitPost(() => loader.Delete(result));
        }

        // Saving a single entity works as expected, even if it also serializes their children
        await SaveAndLoad(FileCategory.Entity, 1, entA);
        await SaveAndLoad(FileCategory.Entity, 2, entB);
        await SaveAndLoad(FileCategory.Entity, 1, child);

        // Including nullspace entities doesn't change the category, though a file containing only null-space entities
        // is "unkown". Maybe in future they will get their own category
        await SaveAndLoad(FileCategory.Entity, 2, entA, @null);
        await SaveAndLoad(FileCategory.Entity, 3, entB, @null);
        await SaveAndLoad(FileCategory.Entity, 2, child, @null);
        await SaveAndLoad(FileCategory.Unknown, 1, @null);

        // More than one entity is unknown
        await SaveAndLoad(FileCategory.Unknown, 3, entA, entB);
        await SaveAndLoad(FileCategory.Unknown, 4, entA, entB, @null);

        // Saving grids works as expected. All counts are 1 higher than expected due to a map being automatically created.
        await SaveAndLoad(FileCategory.Grid, 3, gridA);
        await SaveAndLoad(FileCategory.Grid, 5, gridB);
        await SaveAndLoad(FileCategory.Grid, 4, gridA, @null);
        await SaveAndLoad(FileCategory.Grid, 6, gridB, @null);

        // And saving maps also works
        await SaveAndLoad(FileCategory.Map, 3, mapA);
        await SaveAndLoad(FileCategory.Map, 5, mapB);
        await SaveAndLoad(FileCategory.Map, 4, mapA, @null);
        await SaveAndLoad(FileCategory.Map, 6, mapB, @null);

        // Combinations of grids, entities, and maps, are unknown
        await SaveAndLoad(FileCategory.Unknown, 4, mapA, child);
        await SaveAndLoad(FileCategory.Unknown, 4, gridA, child);
        await SaveAndLoad(FileCategory.Unknown, 8, gridA, mapB);
        await SaveAndLoad(FileCategory.Unknown, 5, mapA, child, @null);
        await SaveAndLoad(FileCategory.Unknown, 5, gridA, child, @null);
        await SaveAndLoad(FileCategory.Unknown, 9, gridA, mapB, @null);
    }
}
