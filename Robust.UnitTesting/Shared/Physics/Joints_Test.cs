using NUnit.Framework;
using Robust.Server.Physics;
using Robust.Shared.Containers;
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

[TestFixture, TestOf(typeof(JointSystem))]
public sealed class Joints_Test
{
    [Test]
    public void JointsRelayTest()
    {
        var factory = RobustServerSimulation.NewSimulation();
        factory.RegisterComponents(fac =>
        {
            fac.RegisterClass<CollideOnAnchorComponent>();
        });
        var sim = factory.InitializeInstance();

        var entManager = sim.Resolve<IEntityManager>();
        var mapManager = sim.Resolve<IMapManager>();
        var jointSystem = entManager.System<SharedJointSystem>();

        var mapId = mapManager.CreateMap();

        var uidA = entManager.SpawnEntity(null, new MapCoordinates(0f, 0f, mapId));
        var uidB = entManager.SpawnEntity(null, new MapCoordinates(0f, 0f, mapId));
        var uidC = entManager.SpawnEntity(null, new MapCoordinates(0f, 0f, mapId));

        entManager.AddComponent<PhysicsComponent>(uidA);
        entManager.AddComponent<PhysicsComponent>(uidB);
        entManager.AddComponent<PhysicsComponent>(uidC);

        var container = entManager.System<SharedContainerSystem>().EnsureContainer<Container>(uidC, "weh");
        var joint = jointSystem.CreateDistanceJoint(uidA, uidB);
        jointSystem.Update(0.016f);

        container.Insert(uidA, entManager);
        Assert.Multiple(() =>
        {
            Assert.That(container.Contains(uidA));
            Assert.That(entManager.HasComponent<JointRelayTargetComponent>(uidC));
            Assert.That(entManager.GetComponent<JointComponent>(uidA).Relay, Is.EqualTo(uidC));

            container.Remove(uidA);
            Assert.That(entManager.GetComponent<JointRelayTargetComponent>(uidC).Relayed, Is.Empty);
            Assert.That(entManager.GetComponent<JointComponent>(uidA).Relay, Is.EqualTo(null));
        });
        mapManager.DeleteMap(mapId);
    }

    /// <summary>
    /// Assert that if a joint exists between 2 bodies they can collide or not collide correctly.
    /// </summary>
    [Test]
    public void JointsCollidableTest()
    {
        var factory = RobustServerSimulation.NewSimulation();
        var server = factory.InitializeInstance();
        var entManager = server.Resolve<IEntityManager>();
        var mapManager = server.Resolve<IMapManager>();
        var fixtureSystem = entManager.EntitySysManager.GetEntitySystem<FixtureSystem>();
        var jointSystem = entManager.EntitySysManager.GetEntitySystem<JointSystem>();
        var broadphaseSystem = entManager.EntitySysManager.GetEntitySystem<SharedBroadphaseSystem>();
        var physicsSystem = server.Resolve<IEntitySystemManager>().GetEntitySystem<SharedPhysicsSystem>();

        var mapId = mapManager.CreateMap();

        var ent1 = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var ent2 = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
        var body1 = entManager.AddComponent<PhysicsComponent>(ent1);
        var body2 = entManager.AddComponent<PhysicsComponent>(ent2);
        var manager1 = entManager.EnsureComponent<FixturesComponent>(ent1);
        var manager2 = entManager.EnsureComponent<FixturesComponent>(ent2);

        physicsSystem.SetBodyType(ent1, BodyType.Dynamic, manager: manager1, body: body1);
        physicsSystem.SetBodyType(ent2, BodyType.Dynamic, manager: manager2, body: body2);

        fixtureSystem.CreateFixture(ent1, new Fixture("fix1", new PhysShapeCircle(0.1f), 1, 1, false), manager: manager1, body: body1);
        fixtureSystem.CreateFixture(ent2, new Fixture("fix1", new PhysShapeCircle(0.1f), 1, 1, false), manager: manager2, body: body2);

        var joint = jointSystem.CreateDistanceJoint(ent1, ent2);
        Assert.That(joint.CollideConnected, Is.EqualTo(true));
        // Joints are deferred because I hate them so need to make sure it exists
        jointSystem.Update(0.016f);
        Assert.That(entManager.HasComponent<JointComponent>(ent1), Is.EqualTo(true));

        // We should have a contact in both situations.
        broadphaseSystem.FindNewContacts(mapId);
        Assert.That(body1.Contacts, Has.Count.EqualTo(1));

        // Alright now try the other way
        jointSystem.RemoveJoint(joint);
        joint = jointSystem.CreateDistanceJoint(ent2, ent1);
        Assert.That(joint.CollideConnected, Is.EqualTo(true));
        jointSystem.Update(0.016f);
        Assert.That(entManager.HasComponent<JointComponent>(ent1));

        broadphaseSystem.FindNewContacts(mapId);
        Assert.That(body1.Contacts, Has.Count.EqualTo(1));

        mapManager.DeleteMap(mapId);
    }
}
