using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;
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
        var map = sim.CreateMap().MapId;

        var ent1 = sim.SpawnEntity(null, new MapCoordinates(Vector2.Zero, map));
        var body1 = entManager.AddComponent<PhysicsComponent>(ent1);
        physicsSystem.SetBodyType(ent1, BodyType.Dynamic, body: body1);
        var fixture1 = new Fixture(new Polygon(Box2.UnitCentered), collisionLayer: 1, collisionMask: 0, hard: true);
        fixturesSystem.CreateFixture(ent1, "fix1", fixture1);

        var ent2 = sim.SpawnEntity(null, new MapCoordinates(Vector2.Zero, map));
        var body2 = entManager.AddComponent<PhysicsComponent>(ent2);
        physicsSystem.SetBodyType(ent2, BodyType.Dynamic, body: body2);
        var fixture2 = new Fixture(new Polygon(Box2.UnitCentered), collisionLayer: 0, collisionMask: 1, hard: true);
        fixturesSystem.CreateFixture(ent2, "fix1", fixture2);
        sysManager.GetEntitySystem<SharedBroadphaseSystem>().FindNewContacts();

        Assert.That(physicsSystem.HasContact(fixture1, fixture2));
        // Sanity check
        Assert.That(fixture1.Id, Is.GreaterThan(0));
        Assert.That(fixture2.Id, Is.GreaterThan(0));

        var contact = fixture1.Contacts.Values.First();
        physicsSystem.DestroyContact(contact);
        Assert.That(!physicsSystem.HasContact(fixture1, fixture2));
        Assert.That(physicsSystem.ContactCount == 0);
    }
}
