using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Client.GameObjects.Components;

/// <summary>
/// Asserts that content can correctly override an occluder's directions instead of relying on the default anchoring behaviour.
/// The directions are used for connecting occluders together.
/// </summary>
[TestFixture]
public sealed class OccluderDirectionsTest : RobustIntegrationTest
{
    /* See https://github.com/space-wizards/RobustToolbox/pull/2528 for why this is commented out as the technology isn't there yet.
    [Test]
    public async Task TestOccluderOverride()
    {
        var client = StartClient();

        await client.WaitIdleAsync();

        var entManager = client.ResolveDependency<IEntityManager>();
        var mapManager = client.ResolveDependency<IMapManager>();

        var overrider = new OccluderOverrider();
        entManager.EventBus.SubscribeEvent<OccluderDirectionsEvent>(EventSource.Local, overrider, EventHandler);

        await client.WaitAssertion(() =>
        {
            var mapId = mapManager.CreateMap();
            var occ = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
            var occluder = entManager.AddComponent<ClientOccluderComponent>(occ);

            Assert.That(occluder.Occluding, Is.EqualTo(OccluderDir.None));
            occluder.Update();
            Assert.That(occluder.Occluding, Is.EqualTo(OccluderDir.East));
        });
    }

    private static void EventHandler(ref OccluderDirectionsEvent ev)
    {
        ev.Handled = true;
        ev.Directions = OccluderDir.East;
    }

    private sealed class OccluderOverrider : IEntityEventSubscriber {}
    */
}
