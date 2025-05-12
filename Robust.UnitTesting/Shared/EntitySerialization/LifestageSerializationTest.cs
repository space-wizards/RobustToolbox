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
public sealed partial class LifestageSerializationTest : RobustIntegrationTest
{
    /// <summary>
    /// Check that whether or not an entity has been paused or map-initialized is preserved across saves & loads.
    /// </summary>
    [Test]
    public async Task TestLifestageSerialization()
    {
        var server = StartServer();
        await server.WaitIdleAsync();
        var entMan = server.EntMan;
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var preInitPath = new ResPath($"{nameof(TestLifestageSerialization)}_preInit.yml");
        var postInitPath = new ResPath($"{nameof(TestLifestageSerialization)}_postInit.yml");
        var pausedPostInitPath = new ResPath($"{nameof(TestLifestageSerialization)}_paused.yml");

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
            await server.WaitAssertion(() => Assert.That(loader.TryLoadMap(f, out _, out _, o)));
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

    /// <summary>
    /// Variant of <see cref="TestLifestageSerialization"/> that has multiple maps and combinations. E.g., a single
    /// paused entity on an un-paused map.
    /// </summary>
    [Test]
    public async Task TestMixedLifestageSerialization()
    {
        var server = StartServer();
        await server.WaitIdleAsync();
        var entMan = server.EntMan;
        var meta = server.System<MetaDataSystem>();
        var mapSys = server.System<SharedMapSystem>();
        var loader = server.System<MapLoaderSystem>();
        var path = new ResPath($"{nameof(TestMixedLifestageSerialization)}.yml");
        var altPath = new ResPath($"{nameof(TestMixedLifestageSerialization)}_alt.yml");

        Entity<TransformComponent, EntitySaveTestComponent> mapA = default; // preinit Map
        Entity<TransformComponent, EntitySaveTestComponent> mapB = default; // postinit unpaused Map
        Entity<TransformComponent, EntitySaveTestComponent> entA = default; // postinit entity on preinit map
        Entity<TransformComponent, EntitySaveTestComponent> entB = default; // paused entity on postinit unpaused map
        Entity<TransformComponent, EntitySaveTestComponent> entC = default; // preinit entity on postinit map
        Entity<TransformComponent, EntitySaveTestComponent> nullA = default; // postinit nullspace entity
        Entity<TransformComponent, EntitySaveTestComponent> nullB = default; // preinit nullspace entity
        Entity<TransformComponent, EntitySaveTestComponent> nullC = default; // paused postinit nullspace entity

        await server.WaitPost(() =>
        {
            var mapAUid = mapSys.CreateMap(out var mapIdA, runMapInit: false);
            var mapBUid = mapSys.CreateMap(out var mapIdB, runMapInit: true);

            var entAUid = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapIdA));
            entMan.RunMapInit(entAUid, entMan.GetComponent<MetaDataComponent>(entAUid));
            meta.SetEntityPaused(entAUid, false);

            var entBUid = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapIdB));
            meta.SetEntityPaused(entBUid, true);

            var entCUid = entMan.CreateEntityUninitialized(null, new MapCoordinates(0, 0, mapIdB));
            entMan.InitializeAndStartEntity(entCUid, doMapInit: false);

            var nullAUid = entMan.SpawnEntity(null, MapCoordinates.Nullspace);

            var nullBUid = entMan.CreateEntityUninitialized(null, MapCoordinates.Nullspace);
            entMan.InitializeAndStartEntity(nullBUid, doMapInit: false);

            var nullCUid = entMan.SpawnEntity(null, MapCoordinates.Nullspace);
            meta.SetEntityPaused(nullCUid, true);

            mapA = Get(mapAUid, entMan);
            mapB = Get(mapBUid, entMan);
            entA = Get(entAUid, entMan);
            entB = Get(entBUid, entMan);
            entC = Get(entCUid, entMan);
            nullA = Get(nullAUid, entMan);
            nullB = Get(nullBUid, entMan);
            nullC = Get(nullCUid, entMan);

            mapA.Comp2.Id = nameof(mapA);
            mapB.Comp2.Id = nameof(mapB);
            entA.Comp2.Id = nameof(entA);
            entB.Comp2.Id = nameof(entB);
            entC.Comp2.Id = nameof(entC);
            nullA.Comp2.Id = nameof(nullA);
            nullB.Comp2.Id = nameof(nullB);
            nullC.Comp2.Id = nameof(nullC);
        });

        string? Name(EntityUid uid)
        {
            return entMan.GetComponentOrNull<EntitySaveTestComponent>(uid)?.Id;
        }

        void AssertPaused(bool expected, params EntityUid[] uids)
        {
            foreach (var uid in uids)
            {
                Assert.That(entMan.GetComponent<MetaDataComponent>(uid).EntityPaused, Is.EqualTo(expected), Name(uid));
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

        void Save(ResPath f)
        {
            Assert.That(loader.TrySaveGeneric([mapA, mapB, nullA, nullB, nullC], f, out _));
        }

        async Task Delete()
        {
            Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(8));
            await server.WaitPost(() => entMan.DeleteEntity(mapA));
            await server.WaitPost(() => entMan.DeleteEntity(mapB));
            await server.WaitPost(() => entMan.DeleteEntity(nullA));
            await server.WaitPost(() => entMan.DeleteEntity(nullB));
            await server.WaitPost(() => entMan.DeleteEntity(nullC));
            Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));
        }

        async Task Load(ResPath f, DeserializationOptions? o)
        {
            Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(0));
            var oo = MapLoadOptions.Default with
            {
                DeserializationOptions = o ?? DeserializationOptions.Default
            };
            await server.WaitAssertion(() => Assert.That(loader.TryLoadGeneric(f, out _, oo)));
            Assert.That(entMan.Count<EntitySaveTestComponent>(), Is.EqualTo(8));
        }

        void FindAll()
        {
            mapA = Find(nameof(mapA), entMan);
            mapB = Find(nameof(mapB), entMan);
            entA = Find(nameof(entA), entMan);
            entB = Find(nameof(entB), entMan);
            entC = Find(nameof(entC), entMan);
            nullA = Find(nameof(nullA), entMan);
            nullB = Find(nameof(nullB), entMan);
            nullC = Find(nameof(nullC), entMan);
        }

        async Task Reload(ResPath f, DeserializationOptions? o = null)
        {
            await Delete();
            await Load(f, o);
            FindAll();
        }

        // All entities should initially be in their respective expected states.
        // entC (pre-mapinit entity on a post-mapinit map) is a bit fucky, and I don't know if that should even be allowed.
        // Note that its just pre-init, not paused, as pre-mapinit entities get paused due to the maps state, not as a general result of being pre-mapinit.
        // If this ever changes, these assers need fixing.
        AssertPaused(true, mapA, entB, nullC);
        AssertPaused(false, mapB, entA, entC, nullA, nullB);
        AssertPreInit(true, mapA, entC, nullB);
        AssertPreInit(false, mapB, entA, entB, nullA, nullC);

        // Saving and re-loading entities should leave their metadata unchanged.
        Save(path);
        await Reload(path);
        AssertPaused(true, mapA, entB, nullC);
        AssertPaused(false, mapB, entA, entC, nullA, nullB);
        AssertPreInit(true, mapA, entC, nullB);
        AssertPreInit(false, mapB, entA, entB, nullA, nullC);

        // reload maps with the mapinit option. This should only affect mapA, as entA is the only one on the map and it
        // is already initialized,
        var opts = DeserializationOptions.Default with {InitializeMaps = true};
        await Reload(path, opts);
        AssertPaused(true, entB, nullC);
        AssertPaused(false, mapA, mapB, entA, entC, nullA, nullB);
        AssertPreInit(true, entC, nullB);
        AssertPreInit(false, mapA, mapB, entA, entB, nullA, nullC);

        // Reloading the new configuration changes nothing
        Save(altPath);
        await Reload(altPath, opts);
        AssertPaused(true, entB, nullC);
        AssertPaused(false, mapA, mapB, entA, entC, nullA, nullB);
        AssertPreInit(true, entC, nullB);
        AssertPreInit(false, mapA, mapB, entA, entB, nullA, nullC);

        // Pause all maps. This will not actually pause entityA, as mapA is already paused (due to being pre-init), so
        // it will not iterate through its children. Maybe this will change in future, but I don't think we should even
        // be trying to actively support having post-init entities on a pre-init map. This is subject to maybe change
        // one day, though if it does the option should be changed to PauseEntities to clarify that it will pause ALL
        // entities, not just maps.
        opts = DeserializationOptions.Default with {PauseMaps = true};
        await Reload(path, opts);
        AssertPaused(true, mapA, mapB, entC, entB, nullC);
        AssertPaused(false, entA, nullA, nullB);
        AssertPreInit(true, mapA, entC, nullB);
        AssertPreInit(false, mapB, entA, entB, nullA, nullC);

        // Reloading the new configuration changes nothing
        Save(altPath);
        await Reload(altPath, opts);
        AssertPaused(true, mapA, mapB, entC, entB, nullC);
        AssertPaused(false, entA, nullA, nullB);
        AssertPreInit(true, mapA, entC, nullB);
        AssertPreInit(false, mapB, entA, entB, nullA, nullC);

        // Initialise and pause all maps. Similar to the previous test with entA, this will not affect entC even
        // though it is pre-init, because it is on a post-init map. Again, this is subject to maybe change one day.
        // Though if it does, the option should be changed to MapInitializeEntities to clarify that it will mapinit ALL
        // entities, not just maps.
        opts = DeserializationOptions.Default with {InitializeMaps = true, PauseMaps = true};
        await Reload(path, opts);
        AssertPaused(true, mapA, mapB, entB, entC, nullC);
        AssertPaused(false, entA, nullA, nullB);
        AssertPreInit(true, entC, nullB);
        AssertPreInit(false, mapA, mapB, entA, entB, nullA, nullC);

        // Reloading the new configuration changes nothing
        Save(altPath);
        await Reload(altPath, opts);
        AssertPaused(true, mapA, mapB, entB, entC, nullC);
        AssertPaused(false, entA, nullA, nullB);
        AssertPreInit(true, entC, nullB);
        AssertPreInit(false, mapA, mapB, entA, entB, nullA, nullC);
    }
}
