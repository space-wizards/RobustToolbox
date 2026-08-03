using NUnit.Framework;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Utility;

namespace Robust.UnitTesting.Shared.EntitySerialization;

[TestFixture]
internal sealed class PendingComponentRemovalSerializationTest : RobustIntegrationTest
{
    [Test]
    public async Task PendingComponentIsNotSerialized()
    {
        var server = StartServer();
        await server.WaitIdleAsync();

        var entMan = server.EntMan;
        var loader = server.System<MapLoaderSystem>();
        var mapSystem = server.System<SharedMapSystem>();
        var path = new ResPath($"{nameof(PendingComponentIsNotSerialized)}.yml");
        MapId mapId = default;

        await server.WaitPost(() =>
        {
            mapSystem.CreateMap(out mapId);
            var uid = entMan.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var component = entMan.AddComponent<PendingRemovalTestComponent>(uid);

            // Deferred removals are processed at the end of the tick. Saving before then used to serialize the
            // component even though it had already been queued for removal.
            entMan.RemoveComponentDeferred(uid, component);
            Assert.That(entMan.Count<PendingRemovalTestComponent>(), Is.EqualTo(1));
            Assert.That(loader.TrySaveMap(mapId, path), Is.True);
        });

        // Let the deferred removal finish, then remove the original map before loading the saved one.
        await server.WaitRunTicks(1);
        Assert.That(entMan.Count<PendingRemovalTestComponent>(), Is.EqualTo(0));
        await server.WaitPost(() => mapSystem.DeleteMap(mapId));

        // If the pending component was serialized, loading the map will add it back.
        await server.WaitPost(() => Assert.That(loader.TryLoadMap(path, out _, out _), Is.True));
        Assert.That(entMan.Count<PendingRemovalTestComponent>(), Is.EqualTo(0));
    }
}

[RegisterComponent]
internal sealed partial class PendingRemovalTestComponent : Component;
