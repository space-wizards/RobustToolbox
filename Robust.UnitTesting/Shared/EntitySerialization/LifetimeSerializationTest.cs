using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;
using static Robust.UnitTesting.Shared.EntitySerialization.EntitySaveTestComponent;

namespace Robust.UnitTesting.Shared.EntitySerialization;

[TestFixture]
public sealed partial class LifetimeSerializationTest : RobustIntegrationTest
{
    /// <summary>
    /// Check that whether or not an entity has been map-initialized is preserved across saves & loads.
    /// </summary>
    [Test]
    public async Task TestLifetimeSerialization()
    {
        var server = StartServer();
        await server.WaitIdleAsync();
        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var preInitPath = new ResPath($"{nameof(LifetimeSerializationTest)}_preInit.yml");
        var postInitPath = new ResPath($"{nameof(LifetimeSerializationTest)}_postInit.yml");
        var pausedPostInitPath = new ResPath($"{nameof(LifetimeSerializationTest)}_paused.yml");

        // Create a pre-init map, and spawn multiple entities on it
        Entity<TransformComponent, EntitySaveTestComponent> map = default;
        Entity<TransformComponent, EntitySaveTestComponent> entA = default;
        Entity<TransformComponent, EntitySaveTestComponent> entB = default;
        Entity<TransformComponent, EntitySaveTestComponent> childA = default;
        Entity<TransformComponent, EntitySaveTestComponent> childB = default;

        await server.WaitPost(() =>
        {
            var mapUid = mapSys.CreateMap(out var mapId, runMapInit: false);
            var entAUid = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var entBUid = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var childAUid = entMan.SpawnEntity(null, new EntityCoordinates(entAUid, 0, 0));
            var childBUid = entMan.SpawnEntity(null, new EntityCoordinates(entBUid, 0, 0));
            map = Get(mapUid, entMan);
            entA = Get(entAUid, entMan);
            entB = Get(entBUid, entMan);
            childA = Get(childAUid, entMan);
            childB = Get(childBUid, entMan);
            map.Comp2.Id = nameof(map);
            entA.Comp2.Id = nameof(entA);
            entB.Comp2.Id = nameof(entB);
            childA.Comp2.Id = nameof(childA);
            childB.Comp2.Id = nameof(childB);
        });

        void AssertPaused(bool expected, params EntityUid[] uids)
        {
            foreach (var uid in uids)
            {
                Assert.That(entMan.GetComponent<MetaDataComponent>(uid).EntityPaused, Is.EqualTo(expected));
            }
        }

        void AssertPreInit(bool expected, params EntityUid[] uids)
        {
            foreach (var uid in uids)
            {
                Assert.That(entMan!.GetComponent<MetaDataComponent>(uid).EntityLifeStage,
                    expected
                        ? Is.LessThan(EntityLifeStage.MapInitialized)
                        : Is.EqualTo(EntityLifeStage.MapInitialized));
            }
        }

        // All entities should initially be un-initialized and paused.
        AssertPaused(true, map, entA, entB, childA, childB);
        AssertPreInit(true, map, entA, entB, childA, childB);
        Assert.That(loader.TrySaveMap(map, preInitPath));

        async Task Delete()
        {
            Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(5));
            await server.WaitPost(() => entMan.DeleteEntity(map));
            Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));
        }

        async Task Load(ResPath f, DeserializationOptions? o)
        {
            Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));
            await server.WaitPost(() => Assert.That(loader.TryLoadMap(f, out _, out _, o)));
            Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(5));
        }

        void FindAll()
        {
            map = Find(nameof(map), entMan);
            entA = Find(nameof(entA), entMan);
            entB = Find(nameof(entB), entMan);
            childA = Find(nameof(childA), entMan);
            childB = Find(nameof(childB), entMan);
        }

        async Task Reload(ResPath f, DeserializationOptions? o = null)
        {
            await Delete();
            await Load(f, o);
            FindAll();
        }

        // Saving and loading the pre-init map should have no effect.
        await Reload(preInitPath);
        AssertPaused(true, map, entA, entB, childA, childB);
        AssertPreInit(true, map, entA, entB, childA, childB);

        // Saving and loading with the map-init option set to true should initialize & unpause all entities
        var opts = DeserializationOptions.Default with {InitializeMaps = true};
        await Reload(preInitPath, opts);
        AssertPaused(false, map, entA, entB, childA, childB);
        AssertPreInit(false, map, entA, entB, childA, childB);
        Assert.That(loader.TrySaveMap(map, postInitPath));

        // re-loading the post-init map should keep everything initialized, even without explicitly asking to initialize maps.
        await Reload(postInitPath);
        AssertPaused(false, map, entA, entB, childA, childB);
        AssertPreInit(false, map, entA, entB, childA, childB);

        // Load & initialize a pre-init map, but with the pause maps option enabled.
        opts = DeserializationOptions.Default with {InitializeMaps = true, PauseMaps = true};
        await Reload(preInitPath, opts);
        AssertPaused(true, map, entA, entB, childA, childB);
        AssertPreInit(false, map, entA, entB, childA, childB);
        Assert.That(loader.TrySaveMap(map, pausedPostInitPath));

        // The pause map option also works when loading un-paused post-init maps
        opts = DeserializationOptions.Default with {PauseMaps = true};
        await Reload(postInitPath, opts);
        AssertPaused(true, map, entA, entB, childA, childB);
        AssertPreInit(false, map, entA, entB, childA, childB);

        // loading & initializing a post-init map should cause no issues.
        opts = DeserializationOptions.Default with {InitializeMaps = true};
        await Reload(postInitPath, opts);
        AssertPaused(false, map, entA, entB, childA, childB);
        AssertPreInit(false, map, entA, entB, childA, childB);

        // Loading a paused post init map does NOT automatically un-pause entities
        await Reload(pausedPostInitPath);
        AssertPaused(true, map, entA, entB, childA, childB);
        AssertPreInit(false, map, entA, entB, childA, childB);

        // The above holds even if we are explicitly initialising maps.
        opts = DeserializationOptions.Default with {InitializeMaps = true};
        await Reload(pausedPostInitPath, opts);
        AssertPaused(true, map, entA, entB, childA, childB);
        AssertPreInit(false, map, entA, entB, childA, childB);

        // And re-paused an already paused map should have no impact.
        opts = DeserializationOptions.Default with {InitializeMaps = true, PauseMaps = true};
        await Reload(pausedPostInitPath, opts);
        AssertPaused(true, map, entA, entB, childA, childB);
        AssertPreInit(false, map, entA, entB, childA, childB);
    }
}
