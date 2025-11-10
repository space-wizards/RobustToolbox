using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class Contacts_Test
{
    /// <summary>
    /// Asserts pair keys get updated and removed as relevant.
    /// </summary>
    [Test]
    public void PairKeys()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = sim.Resolve<IEntityManager>();
        var sysManager = sim.Resolve<IEntitySystemManager>();
        var fixturesSystem = sysManager.GetEntitySystem<FixtureSystem>();
        var physicsSystem = sysManager.GetEntitySystem<SharedPhysicsSystem>();
        var mapSystem = sysManager.GetEntitySystem<SharedMapSystem>();
        var map = sim.CreateMap().MapId;

        var ent1 = sim.SpawnEntity(null, new MapCoordinates(Vector2.Zero, map));
        var body1 = entManager.AddComponent<PhysicsComponent>(ent1);
        physicsSystem.SetBodyType(ent1, BodyType.Dynamic, body: body1);
        var fixture1 = new Fixture();
        fixturesSystem.CreateFixture(ent1, "fix1", fixture1);

        // Slot 0 (just offset by 1 because)
        Assert.That(fixture1.Id, Is.EqualTo(0 + 1));

        var ent2 = sim.SpawnEntity(null, new MapCoordinates(Vector2.Zero, map));
        var body2 = entManager.AddComponent<PhysicsComponent>(ent2);
        physicsSystem.SetBodyType(ent2, BodyType.Dynamic, body: body2);
        var fixture2 = new Fixture();
        fixturesSystem.CreateFixture(ent2, "fix1", fixture2);


    }
}
