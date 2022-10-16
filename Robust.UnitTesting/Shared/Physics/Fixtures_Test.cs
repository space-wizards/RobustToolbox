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
    public void SetDensity()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();
        var sysManager = sim.Resolve<IEntitySystemManager>();
        var fixturesSystem = sysManager.GetEntitySystem<FixtureSystem>();
        var physicsSystem = sysManager.GetEntitySystem<SharedPhysicsSystem>();
        var map = mapManager.CreateMap();

        var ent = sim.SpawnEntity(null, new MapCoordinates(Vector2.Zero, map));
        var body = entManager.AddComponent<PhysicsComponent>(ent);
        physicsSystem.SetBodyType(body, BodyType.Dynamic);
        var fixture = new Fixture();
        fixturesSystem.CreateFixture(body, fixture);

        physicsSystem.SetDensity(fixture, 10f);
        Assert.That(fixture.Density, Is.EqualTo(10f));
        Assert.That(body.Mass, Is.EqualTo(10f));

        mapManager.DeleteMap(map);
    }
}
