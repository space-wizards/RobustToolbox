using System.Linq;
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

[TestFixture, TestOf(typeof(SharedPhysicsMapComponent))]
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
        var mapManager = sim.Resolve<IMapManager>();
        var physSystem = sim.Resolve<IEntitySystemManager>().GetEntitySystem<SharedPhysicsSystem>();
        var fixtureSystem = sim.Resolve<IEntitySystemManager>().GetEntitySystem<FixtureSystem>();

        var mapId = mapManager.CreateMap();
        var mapId2 = mapManager.CreateMap();
        var mapUid = mapManager.GetMapEntityId(mapId);
        var mapUid2 = mapManager.GetMapEntityId(mapId2);

        var physicsMap = entManager.GetComponent<SharedPhysicsMapComponent>(mapUid);
        var physicsMap2 = entManager.GetComponent<SharedPhysicsMapComponent>(mapUid2);

        var parent = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var parentXform = entManager.GetComponent<TransformComponent>(parent);
        var parentBody = entManager.AddComponent<PhysicsComponent>(parent);

        physSystem.SetBodyType(parentBody, BodyType.Dynamic);
        physSystem.SetSleepingAllowed(parentBody, false);
        fixtureSystem.CreateFixture(parentBody, new Fixture(parentBody, new PhysShapeCircle { Radius = 0.5f }));
        physSystem.WakeBody(parentBody);
        Assert.That(physicsMap.AwakeBodies, Does.Contain(parentBody));

        var child = entManager.SpawnEntity(null, new EntityCoordinates(parent, Vector2.Zero));
        var childBody = entManager.AddComponent<PhysicsComponent>(child);

        physSystem.SetBodyType(childBody, BodyType.Dynamic);
        physSystem.SetSleepingAllowed(childBody, false);
        fixtureSystem.CreateFixture(childBody, new Fixture(childBody, new PhysShapeCircle { Radius = 0.5f }));
        physSystem.WakeBody(childBody);

        Assert.That(physicsMap.AwakeBodies, Does.Contain(childBody));

        parentXform.AttachParent(mapUid2);

        Assert.That(physicsMap.AwakeBodies, Is.Empty);
        Assert.That(physicsMap2.AwakeBodies, Has.Count.EqualTo(2));

        parentXform.AttachParent(mapUid);

        Assert.That(physicsMap.AwakeBodies, Has.Count.EqualTo(2));
        Assert.That(physicsMap2.AwakeBodies, Is.Empty);
    }
}
