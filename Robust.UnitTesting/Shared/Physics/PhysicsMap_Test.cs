using System.Linq;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics;
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

        var mapId = mapManager.CreateMap();
        var mapId2 = mapManager.CreateMap();
        var mapUid = mapManager.GetMapEntityId(mapId);
        var mapUid2 = mapManager.GetMapEntityId(mapId2);

        var physicsMap = entManager.GetComponent<SharedPhysicsMapComponent>(mapUid);
        var physicsMap2 = entManager.GetComponent<SharedPhysicsMapComponent>(mapUid2);

        var parent = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var parentXform = entManager.GetComponent<TransformComponent>(parent);
        var parentBody = entManager.AddComponent<PhysicsComponent>(parent);
        parentBody.BodyType = BodyType.Dynamic;

        parentBody.SleepingAllowed = false;
        parentBody.WakeBody();
        Assert.That(physicsMap.AwakeBodies, Does.Contain(parentBody));

        var child = entManager.SpawnEntity(null, new EntityCoordinates(parent, Vector2.Zero));
        var childXform = entManager.GetComponent<TransformComponent>(child);
        var childBody = entManager.AddComponent<PhysicsComponent>(child);
        childBody.BodyType = BodyType.Dynamic;

        childBody.SleepingAllowed = false;
        childBody.WakeBody();

        Assert.That(physicsMap.AwakeBodies, Does.Contain(childBody));

        parentXform.AttachParent(mapUid2);

        Assert.That(physicsMap.AwakeBodies, Is.Empty);
        Assert.That(physicsMap.AwakeBodies, Has.Count.EqualTo(2));

        parentXform.AttachParent(mapUid);

        Assert.That(physicsMap.AwakeBodies, Has.Count.EqualTo(2));
        Assert.That(physicsMap2.AwakeBodies, Is.Empty);
    }
}
