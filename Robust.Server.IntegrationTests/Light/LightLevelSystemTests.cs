using System.Numerics;
using NUnit.Framework;
using Robust.Server.ComponentTrees;
using Robust.Server.GameObjects;
using Robust.Shared;
using Robust.Shared.ComponentTrees;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Light;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.UnitTesting.Server;

namespace Robust.Server.IntegrationTests.Light;

[TestFixture]
public sealed class LightLevelSystemTests
{
    private const string InvalidMaskPrototype = @"
- type: lightMask
  id: InvalidCone
  maskPath: /Textures/invalid.png
  lightCones:
  - direction: 0
    innerWidth: 90
    outerWidth: 45
";

    private static ISimulation NewSimulation(bool lightTree)
    {
        return RobustServerSimulation
            .NewSimulation()
            .RegisterComponents(f =>
            {
                f.RegisterClass<PointLightComponent>();
                f.RegisterClass<LightTreeComponent>();
            })
            .RegisterEntitySystems(f =>
            {
                IoCManager.Resolve<IConfigurationManager>().SetCVar(CVars.LookupEnableServerLightTree, lightTree);
                var recursiveMove = typeof(SharedLightTreeSystem).Assembly.GetType("Robust.Shared.ComponentTrees.RecursiveMoveSystem")!;
                typeof(IEntitySystemManager).GetMethod(nameof(IEntitySystemManager.LoadExtraSystemType))!
                    .MakeGenericMethod(recursiveMove)
                    .Invoke(f, null);
                f.LoadExtraSystemType<ServerOccluderSystem>();
                f.LoadExtraSystemType<LightTreeSystem>();
                f.LoadExtraSystemType<PointLightSystem>();
                f.LoadExtraSystemType<LightLevelSystem>();
            })
            .InitializeInstance();
    }

    private static T Sys<T>(ISimulation sim) where T : IEntitySystem
    {
        return sim.Resolve<IEntitySystemManager>().GetEntitySystem<T>();
    }

    [Test]
    public void AmbientOnlyLighting()
    {
        var sim = NewSimulation(true);
        var map = sim.CreateMap();
        var mapSys = Sys<SharedMapSystem>(sim);
        var light = Sys<LightLevelSystem>(sim);

        mapSys.SetAmbientLight(map.MapId, Color.Red);

        Assert.That(light.TryCalculateLightColor(new MapCoordinates(Vector2.Zero, map.MapId), out var color), Is.True);
        Assert.That(color.R, Is.EqualTo(Color.Red.R).Within(0.001f));
        Assert.That(color.G, Is.EqualTo(Color.Red.G).Within(0.001f));
        Assert.That(color.B, Is.EqualTo(Color.Red.B).Within(0.001f));
    }

    [Test]
    public void MapLightingDisabledReturnsFullyLitEvenWithServerTreeDisabled()
    {
        var sim = NewSimulation(false);
        var map = sim.CreateMap();
        var light = Sys<LightLevelSystem>(sim);

        sim.Resolve<IEntityManager>().GetComponent<MapComponent>(map.Uid).LightingEnabled = false;

        Assert.That(light.TryCalculateLightColor(new MapCoordinates(Vector2.Zero, map.MapId), out var color), Is.True);
        Assert.That(color, Is.EqualTo(Color.White));
        Assert.That(light.TryCalculateLightLevel(new MapCoordinates(Vector2.Zero, map.MapId), out var level), Is.True);
        Assert.That(level, Is.EqualTo(1f));
    }

    [Test]
    public void EnabledMapReturnsUnavailableWhenServerTreeDisabled()
    {
        var sim = NewSimulation(false);
        var map = sim.CreateMap();
        var light = Sys<LightLevelSystem>(sim);

        Assert.That(light.TryCalculateLightColor(new MapCoordinates(Vector2.Zero, map.MapId), out _), Is.False);
        Assert.That(light.CalculateLightColor(new MapCoordinates(Vector2.Zero, map.MapId)), Is.EqualTo(Color.Black));
    }

    [Test]
    public void NonShadowLightPassesThroughOccluder()
    {
        var sim = NewSimulation(true);
        var map = sim.CreateMap();
        AddLight(sim, map.MapId, Vector2.Zero, castShadows: false);
        AddOccluder(sim, map.MapId, new Vector2(2, 0));

        Assert.That(Sys<LightLevelSystem>(sim).TryCalculateLightLevel(new MapCoordinates(new Vector2(4, 0), map.MapId), out var level), Is.True);
        Assert.That(level, Is.GreaterThan(0f));
    }

    [Test]
    public void ShadowCastingLightBlockedByOccluder()
    {
        var sim = NewSimulation(true);
        var map = sim.CreateMap();
        AddLight(sim, map.MapId, Vector2.Zero, castShadows: true);
        AddOccluder(sim, map.MapId, new Vector2(2, 0));

        Assert.That(Sys<LightLevelSystem>(sim).TryCalculateLightLevel(new MapCoordinates(new Vector2(4, 0), map.MapId), out var level), Is.True);
        Assert.That(level, Is.EqualTo(0f).Within(0.001f));
    }

    [Test]
    public void RuntimeOffsetChangeUpdatesTreeBounds()
    {
        var sim = NewSimulation(true);
        var map = sim.CreateMap();
        var uid = AddLight(sim, map.MapId, new Vector2(10, 0), castShadows: false, radius: 2f);
        var light = Sys<LightLevelSystem>(sim);
        var pointLight = sim.Resolve<IEntityManager>().GetComponent<PointLightComponent>(uid);

        Assert.That(light.TryCalculateLightLevel(new MapCoordinates(Vector2.Zero, map.MapId), out var before), Is.True);
        Assert.That(before, Is.EqualTo(0f).Within(0.001f));

        Sys<PointLightSystem>(sim).SetOffset(uid, new Vector2(-10, 0), pointLight);

        Assert.That(light.TryCalculateLightLevel(new MapCoordinates(Vector2.Zero, map.MapId), out var after), Is.True);
        Assert.That(after, Is.GreaterThan(0f));
    }

    [Test]
    public void InvalidMaskConePrototypeValuesFail()
    {
        var sim = NewSimulation(false);
        var proto = sim.Resolve<IPrototypeManager>();

        Assert.Throws<PrototypeLoadException>(() => proto.LoadString(InvalidMaskPrototype, true));
    }

    /*
    [Test]
    public void NestedContainerTransferAndRemovalUpdatesOcclusion()
    {
        var sim = NewSimulation(false);
        var map = sim.CreateMap();
        var entMan = sim.Resolve<IEntityManager>();
        var containers = Sys<SharedContainerSystem>(sim);
        var lightUid = AddLight(sim, map.MapId, Vector2.Zero, castShadows: false);
        var light = entMan.GetComponent<PointLightComponent>(lightUid);
        var outerA = sim.SpawnEntity(null, new MapCoordinates(Vector2.Zero, map.MapId));
        var outerB = sim.SpawnEntity(null, new MapCoordinates(Vector2.Zero, map.MapId));
        var inner = sim.SpawnEntity(null, new MapCoordinates(Vector2.Zero, map.MapId));

        entMan.AddComponent<ContainerManagerComponent>(outerA);
        entMan.AddComponent<ContainerManagerComponent>(outerB);
        entMan.AddComponent<ContainerManagerComponent>(inner);
        var outerAContainer = containers.MakeContainer<Container>(outerA, "outerA");
        var outerBContainer = containers.MakeContainer<Container>(outerB, "outerB");
        var innerContainer = containers.MakeContainer<Container>(inner, "inner");
        outerAContainer.OccludesLight = true;
        outerBContainer.OccludesLight = true;
        innerContainer.OccludesLight = false;

        containers.Insert(lightUid, innerContainer);
        containers.Insert(inner, outerAContainer);
        Assert.That(light.ContainerOccluded, Is.True);

        containers.Remove(inner, outerAContainer);
        containers.Insert(inner, outerBContainer);
        Assert.That(light.ContainerOccluded, Is.True);

        containers.Remove(inner, outerBContainer);
        Assert.That(light.ContainerOccluded, Is.False);
    }
    */

    private static EntityUid AddLight(ISimulation sim, MapId mapId, Vector2 position, bool castShadows, float radius = 6f)
    {
        var uid = sim.SpawnEntity(null, new MapCoordinates(position, mapId));
        var entMan = sim.Resolve<IEntityManager>();
        var light = entMan.AddComponent<PointLightComponent>(uid);
        Sys<PointLightSystem>(sim).SetRadius(uid, radius, light);
        Sys<PointLightSystem>(sim).SetCastShadows(uid, castShadows, light);
        Sys<PointLightSystem>(sim).SetColor(uid, Color.White, light);
        Sys<PointLightSystem>(sim).SetEnergy(uid, 1f, light);
        return uid;
    }

    private static void AddOccluder(ISimulation sim, MapId mapId, Vector2 position)
    {
        var uid = sim.SpawnEntity(null, new MapCoordinates(position, mapId));
        var entMan = sim.Resolve<IEntityManager>();
        var occluder = entMan.AddComponent<OccluderComponent>(uid);
        Sys<ServerOccluderSystem>(sim).SetBoundingBox(uid, new Box2(-0.25f, -2f, 0.25f, 2f), occluder);
    }
}
