using System.Numerics;
using NUnit.Framework;
using Robust.Client.GameObjects;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.UnitTesting;

namespace Robust.UnitTesting.Client.GameObjects.Systems;

[TestFixture]
[TestOf(typeof(ClientOccluderSystem))]
public sealed class ClientOccluderSystemTests : RobustUnitTest
{
    public override UnitTestProject Project => UnitTestProject.Client;

    [Test]
    public void EmbeddedPointLightCacheInvalidatesForLightAndOccluderMovement()
    {
        var entMan = IoCManager.Resolve<IEntityManager>();
        var mapSystem = entMan.System<SharedMapSystem>();
        var transform = entMan.System<SharedTransformSystem>();
        var occluders = entMan.System<ClientOccluderSystem>();

        mapSystem.CreateMap(out var mapId);
        var grid = mapSystem.CreateGridEntity(mapId);

        var occluderUid = entMan.SpawnEntity(null, new EntityCoordinates(grid, Vector2.Zero));
        entMan.AddComponent<OccluderComponent>(occluderUid);
        occluders.FrameUpdate(0f);

        var lightUid = entMan.SpawnEntity(null, new EntityCoordinates(grid, Vector2.Zero));
        var light = entMan.AddComponent<PointLightComponent>(lightUid);
        var farLightPosition = new Vector2(10f, 0f);
        var farLightUid = entMan.SpawnEntity(null, new EntityCoordinates(grid, farLightPosition));
        var farLight = entMan.AddComponent<PointLightComponent>(farLightUid);

        Assert.That(occluders.IsPointLightEmbeddedInOccluder(mapId, lightUid, light, Vector2.Zero), Is.True);
        Assert.That(light.EmbeddedOccluderCacheValid, Is.True);
        Assert.That(light.EmbeddedOccluderCacheValue, Is.True);
        Assert.That(light.EmbeddedOccluderCacheMap, Is.EqualTo(mapId));
        Assert.That(light.EmbeddedOccluderCachePosition, Is.EqualTo(Vector2.Zero));

        Assert.That(occluders.IsPointLightEmbeddedInOccluder(mapId, farLightUid, farLight, farLightPosition), Is.False);
        Assert.That(farLight.EmbeddedOccluderCacheValid, Is.True);
        Assert.That(farLight.EmbeddedOccluderCacheValue, Is.False);

        // Same light position + same occluder layout should keep using the cached result.
        Assert.That(occluders.IsPointLightEmbeddedInOccluder(mapId, lightUid, light, Vector2.Zero), Is.True);
        Assert.That(light.EmbeddedOccluderCacheValid, Is.True);
        Assert.That(light.EmbeddedOccluderCachePosition, Is.EqualTo(Vector2.Zero));

        // Moving the light changes the cache key, forcing the point query to update the cache.
        var outsideOccluder = new Vector2(2f, 0f);
        transform.SetCoordinates(lightUid, new EntityCoordinates(grid, outsideOccluder));

        Assert.That(occluders.IsPointLightEmbeddedInOccluder(mapId, lightUid, light, outsideOccluder), Is.False);
        Assert.That(light.EmbeddedOccluderCachePosition, Is.EqualTo(outsideOccluder));
        Assert.That(light.EmbeddedOccluderCacheValue, Is.False);

        // Setup the cache with the light embedded again, then move the occluder out from under it.
        transform.SetCoordinates(lightUid, new EntityCoordinates(grid, Vector2.Zero));
        Assert.That(occluders.IsPointLightEmbeddedInOccluder(mapId, lightUid, light, Vector2.Zero), Is.True);

        transform.SetCoordinates(occluderUid, new EntityCoordinates(grid, outsideOccluder));

        Assert.That(light.EmbeddedOccluderCacheValid, Is.False);
        Assert.That(farLight.EmbeddedOccluderCacheValid, Is.True);
        Assert.That(farLight.EmbeddedOccluderCacheValue, Is.False);

        Assert.That(occluders.IsPointLightEmbeddedInOccluder(mapId, lightUid, light, Vector2.Zero), Is.False);
        Assert.That(light.EmbeddedOccluderCachePosition, Is.EqualTo(Vector2.Zero));
        Assert.That(light.EmbeddedOccluderCacheValue, Is.False);
    }
}
