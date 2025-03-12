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

[TestFixture, TestOf(typeof(PhysicsMapComponent))]
public sealed class PhysicsMap_Test
{
    /// <summary>
    /// If a body has a child does its child's physicsmap get updated.
    /// </summary>
    [Test]
    public void RecursiveMapChange()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var physSystem = entManager.System<SharedPhysicsSystem>();
        var fixtureSystem = entManager.System<FixtureSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();

        var mapUid = mapSystem.CreateMap();
        var mapUid2 = mapSystem.CreateMap();

        var physicsMap = entManager.GetComponent<PhysicsMapComponent>(mapUid);
        var physicsMap2 = entManager.GetComponent<PhysicsMapComponent>(mapUid2);

        var parent = entManager.SpawnEntity(null, new EntityCoordinates(mapUid, Vector2.Zero));
        var parentXform = entManager.GetComponent<TransformComponent>(parent);
        var parentBody = entManager.AddComponent<PhysicsComponent>(parent);

        physSystem.SetBodyType(parent, BodyType.Dynamic);
        physSystem.SetSleepingAllowed(parent, parentBody, false);
        fixtureSystem.CreateFixture(parent, "fix1", new Fixture(new PhysShapeCircle(0.5f), 0, 0, false), body: parentBody);
        physSystem.WakeBody(parent);
        Assert.That(physicsMap.AwakeBodies, Does.Contain(parentBody));

        var child = entManager.SpawnEntity(null, new EntityCoordinates(parent, Vector2.Zero));
        var childBody = entManager.AddComponent<PhysicsComponent>(child);

        physSystem.SetBodyType(child, BodyType.Dynamic);
        physSystem.SetSleepingAllowed(child, childBody, false);
        fixtureSystem.CreateFixture(child, "fix1", new Fixture(new PhysShapeCircle(0.5f), 0, 0, false), body: childBody);
        physSystem.WakeBody(child, body: childBody);

        Assert.That(physicsMap.AwakeBodies, Does.Contain(childBody));

        xformSystem.SetParent(parent, parentXform, mapUid2);

        Assert.That(physicsMap.AwakeBodies, Is.Empty);
        Assert.That(physicsMap2.AwakeBodies, Has.Count.EqualTo(2));

        xformSystem.SetParent(parent, parentXform, mapUid);

        Assert.That(physicsMap.AwakeBodies, Has.Count.EqualTo(2));
        Assert.That(physicsMap2.AwakeBodies, Is.Empty);
    }
}
