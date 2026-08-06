using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
internal sealed class Fixtures_Test
{
    private const string FixtureId = "fix1";

    [Test]
    public void SetDensity()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = sim.Resolve<IEntityManager>();
        var sysManager = sim.Resolve<IEntitySystemManager>();
        var fixturesSystem = sysManager.GetEntitySystem<FixtureSystem>();
        var physicsSystem = sysManager.GetEntitySystem<SharedPhysicsSystem>();
        var mapSystem = sysManager.GetEntitySystem<SharedMapSystem>();
        var map = sim.CreateMap().MapId;

        var ent = sim.SpawnEntity(null, new MapCoordinates(Vector2.Zero, map));
        var body = entManager.AddComponent<PhysicsComponent>(ent);
        physicsSystem.SetBodyType(ent, BodyType.Dynamic, body: body);
        var fixture = new Fixture();
        fixturesSystem.CreateFixture(ent, "fix1", fixture);

        physicsSystem.SetDensity(ent, "fix1", fixture, 10f);
        Assert.That(fixture.Density, Is.EqualTo(10f));
        Assert.That(body.Mass, Is.EqualTo(10f));

        mapSystem.DeleteMap(map);
    }

    [Test]
    public void RemoveFixturesComponentClearsQueuedProxy()
    {
        var (sim, ent, fixture, proxy, _, _, _, _, broadphase) = SetupQueuedProxy();
        var entManager = sim.Resolve<IEntityManager>();

        entManager.RemoveComponent<FixturesComponent>(ent);

        AssertProxyReleased(sim, fixture, proxy);
        Assert.That(broadphase.StaticTree.GetProxy(proxy.ProxyId), Is.Null);
    }

    [Test]
    public void RemovePhysicsThenFixturesComponentClearsQueuedProxy()
    {
        var (sim, ent, fixture, proxy, _, _, _, _, broadphase) = SetupQueuedProxy();
        var entManager = sim.Resolve<IEntityManager>();

        entManager.RemoveComponent<PhysicsComponent>(ent);

        if (entManager.HasComponent<FixturesComponent>(ent))
            entManager.RemoveComponent<FixturesComponent>(ent);

        AssertProxyReleased(sim, fixture, proxy);
        Assert.That(broadphase.StaticTree.GetProxy(proxy.ProxyId), Is.Null);
    }

    [Test]
    public void RemoveFixturesThenPhysicsComponentClearsQueuedProxy()
    {
        var (sim, ent, fixture, proxy, _, _, _, _, broadphase) = SetupQueuedProxy();
        var entManager = sim.Resolve<IEntityManager>();

        entManager.RemoveComponent<FixturesComponent>(ent);
        entManager.RemoveComponent<PhysicsComponent>(ent);

        AssertProxyReleased(sim, fixture, proxy);
        Assert.That(broadphase.StaticTree.GetProxy(proxy.ProxyId), Is.Null);
    }

    [Test]
    public void BroadphaseRemovalClearsQueuedProxyAndDestroyFixtureStaysSafe()
    {
        var (sim, ent, fixture, proxy, mapEnt, _, fixturesSystem, _, _) = SetupQueuedProxy();
        var entManager = sim.Resolve<IEntityManager>();

        entManager.RemoveComponent<BroadphaseComponent>(mapEnt);
        AssertProxyReleased(sim, fixture, proxy);

        fixturesSystem.DestroyFixture(ent, FixtureId, fixture);

        AssertProxyReleased(sim, fixture, proxy);
    }

    [Test]
    public void MoveToNullspaceClearsQueuedProxy()
    {
        var (sim, ent, fixture, proxy, _, _, _, _, broadphase) = SetupQueuedProxy();
        var entManager = sim.Resolve<IEntityManager>();
        var xformSystem = entManager.System<SharedTransformSystem>();
        var xform = entManager.GetComponent<TransformComponent>(ent);

        xformSystem.DetachEntity(ent, xform);

        AssertProxyReleased(sim, fixture, proxy);
        Assert.That(broadphase.StaticTree.GetProxy(proxy.ProxyId), Is.Null);
    }

    [Test]
    public void DeleteEntityClearsQueuedProxy()
    {
        var (sim, ent, fixture, proxy, _, _, _, _, broadphase) = SetupQueuedProxy();
        var entManager = sim.Resolve<IEntityManager>();

        entManager.DeleteEntity(ent);

        AssertProxyReleased(sim, fixture, proxy);
        Assert.That(broadphase.StaticTree.GetProxy(proxy.ProxyId), Is.Null);
    }

    [Test]
    public void FixtureProxyCleanupIsIdempotent()
    {
        var (sim, ent, fixture, proxy, _, _, _, lookup, broadphase) = SetupQueuedProxy();
        var entManager = sim.Resolve<IEntityManager>();
        var xform = entManager.GetComponent<TransformComponent>(ent);

        lookup.ReleaseProxies(ent, fixture, xform);
        lookup.ReleaseProxies(ent, fixture, xform);

        AssertProxyReleased(sim, fixture, proxy);
        Assert.That(broadphase.StaticTree.GetProxy(proxy.ProxyId), Is.Null);
    }

    [Test]
    public void DestroyFixtureRemovesProxyAndContacts()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var fixturesSystem = entManager.System<FixtureSystem>();
        var physicsSystem = entManager.System<SharedPhysicsSystem>();
        var broadphaseSystem = entManager.System<SharedBroadphaseSystem>();
        var (mapEnt, mapId) = sim.CreateMap();
        var broadphase = entManager.GetComponent<BroadphaseComponent>(mapEnt);

        var entA = CreatePhysicsEntity(sim, mapId, BodyType.Dynamic);
        var entB = CreatePhysicsEntity(sim, mapId);
        var fixtureA = entManager.GetComponent<FixturesComponent>(entA).Fixtures[FixtureId];
        var fixtureB = entManager.GetComponent<FixturesComponent>(entB).Fixtures[FixtureId];
        var proxy = fixtureA.Proxies[0];

        broadphaseSystem.FindNewContacts();

        var bodyA = entManager.GetComponent<PhysicsComponent>(entA);
        var bodyB = entManager.GetComponent<PhysicsComponent>(entB);
        Assert.That(bodyA.ContactCount, Is.EqualTo(1));
        Assert.That(bodyB.ContactCount, Is.EqualTo(1));

        fixturesSystem.DestroyFixture(entA, FixtureId, fixtureA);

        AssertProxyReleased(sim, fixtureA, proxy);
        Assert.That(broadphase.DynamicTree.GetProxy(proxy.ProxyId), Is.Null);
        Assert.That(fixtureA.Contacts, Is.Empty);
        Assert.That(fixtureB.Contacts, Is.Empty);
        Assert.That(bodyA.ContactCount, Is.EqualTo(0));
        Assert.That(bodyB.ContactCount, Is.EqualTo(0));

        broadphaseSystem.FindNewContacts();
    }

    private static (
        ISimulation Sim,
        EntityUid Ent,
        Fixture Fixture,
        FixtureProxy Proxy,
        EntityUid MapEnt,
        MapId MapId,
        FixtureSystem Fixtures,
        EntityLookupSystem Lookup,
        BroadphaseComponent Broadphase) SetupQueuedProxy()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var fixturesSystem = entManager.System<FixtureSystem>();
        var lookup = entManager.System<EntityLookupSystem>();
        var (mapEnt, mapId) = sim.CreateMap();
        var broadphase = entManager.GetComponent<BroadphaseComponent>(mapEnt);
        var ent = CreatePhysicsEntity(sim, mapId);
        var fixture = entManager.GetComponent<FixturesComponent>(ent).Fixtures[FixtureId];
        var proxy = fixture.Proxies[0];

        Assert.That(fixture.ProxyCount, Is.EqualTo(1));
        Assert.That(fixture.Proxies, Has.Length.EqualTo(1));
        Assert.That(entManager.System<SharedPhysicsSystem>().MoveBuffer, Does.Contain(proxy));
        Assert.That(broadphase.StaticTree.GetProxy(proxy.ProxyId)!.Equals(proxy));

        return (sim, ent, fixture, proxy, mapEnt, mapId, fixturesSystem, lookup, broadphase);
    }

    private static EntityUid CreatePhysicsEntity(ISimulation sim, MapId mapId, BodyType bodyType = BodyType.Static)
    {
        var entManager = sim.Resolve<IEntityManager>();
        var fixturesSystem = entManager.System<FixtureSystem>();
        var physicsSystem = entManager.System<SharedPhysicsSystem>();
        var ent = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var body = entManager.AddComponent<PhysicsComponent>(ent);
        var shape = new PolygonShape();
        shape.SetAsBox(0.5f, 0.5f);

        physicsSystem.SetBodyType(ent, bodyType, body: body);
        fixturesSystem.CreateFixture(ent, FixtureId, new Fixture(shape, 1, 1, true), body: body);
        physicsSystem.SetCanCollide(ent, true, body: body);

        return ent;
    }

    private static void AssertProxyReleased(ISimulation sim, Fixture fixture, FixtureProxy proxy)
    {
        var entManager = sim.Resolve<IEntityManager>();

        Assert.That(entManager.System<SharedPhysicsSystem>().MoveBuffer, Does.Not.Contain(proxy));
        Assert.That(fixture.ProxyCount, Is.EqualTo(0));
        Assert.That(fixture.Proxies, Is.Empty);
        entManager.System<SharedBroadphaseSystem>().FindNewContacts();
    }
}
