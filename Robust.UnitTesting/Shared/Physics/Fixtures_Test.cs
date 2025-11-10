using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class Fixtures_Test
{
    [Test]
    public void TestFixtureId()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = sim.Resolve<IEntityManager>();
        var sysManager = sim.Resolve<IEntitySystemManager>();
        var fixturesSystem = sysManager.GetEntitySystem<FixtureSystem>();
        var physicsSystem = sysManager.GetEntitySystem<SharedPhysicsSystem>();
        var map = sim.CreateMap().MapId;

        var ent1 = sim.SpawnEntity(null, new MapCoordinates(Vector2.Zero, map));
        var body1 = entManager.AddComponent<PhysicsComponent>(ent1);
        physicsSystem.SetBodyType(ent1, BodyType.Dynamic, body: body1);
        var fixture1 = new Fixture();
        fixturesSystem.CreateFixture(ent1, "fix1", fixture1);

        // Slot 0 (just offset by 1 because)
        Assert.That(fixture1.Id, Is.EqualTo(0 + 1));

        fixturesSystem.DestroyFixture(ent1, "fix1");
        Assert.That(fixture1.Id, Is.EqualTo(0));
    }

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
}
