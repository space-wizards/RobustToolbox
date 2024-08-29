using System.Numerics;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
public sealed class JointDeletion_Test : RobustIntegrationTest
{
    [Test]
    public async Task JointDeletionTest()
    {
        var server = StartServer();
        await server.WaitIdleAsync();

        var entManager = server.ResolveDependency<IEntityManager>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var susManager = server.ResolveDependency<IEntitySystemManager>();
        var jointSystem = susManager.GetEntitySystem<SharedJointSystem>();
        var broadphase = susManager.GetEntitySystem<SharedBroadphaseSystem>();
        var fixSystem = susManager.GetEntitySystem<FixtureSystem>();
        var physicsSystem = susManager.GetEntitySystem<SharedPhysicsSystem>();

        DistanceJoint joint = default!;
        EntityUid ent1;
        EntityUid ent2 = default!;
        PhysicsComponent body1;
        PhysicsComponent body2 = default!;
        MapId mapId = default!;

        await server.WaitPost(() =>
        {
            entManager.System<SharedMapSystem>().CreateMap(out mapId);
            ent1 = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
            ent2 = entManager.SpawnEntity(null, new MapCoordinates(Vector2.One, mapId));

            body1 = entManager.AddComponent<PhysicsComponent>(ent1);
            body2 = entManager.AddComponent<PhysicsComponent>(ent2);
            var manager1 = entManager.EnsureComponent<FixturesComponent>(ent1);
            var manager2 = entManager.EnsureComponent<FixturesComponent>(ent2);
            entManager.AddComponent<CollisionWakeComponent>(ent2);

            physicsSystem.SetBodyType(ent1, BodyType.Dynamic, manager: manager1, body: body1);
            physicsSystem.SetBodyType(ent2, BodyType.Dynamic, manager: manager2, body: body2);
            physicsSystem.SetCanCollide(ent1, true, manager: manager1, body: body1);
            physicsSystem.SetCanCollide(ent2, true, manager: manager2, body: body2);
            var shape = new PolygonShape();
            shape.SetAsBox(0.5f, 0.5f);

            fixSystem.CreateFixture(ent2, "fix1", new Fixture(shape, 0, 0, false), manager: manager2, body: body2);

            joint = jointSystem.CreateDistanceJoint(ent1, ent2, id: "distance-joint");
            joint.CollideConnected = false;
        });

        await server.WaitRunTicks(1);

        await server.WaitAssertion(() =>
        {
            Assert.That(joint.Enabled);
            physicsSystem.SetAwake(ent2, body2, false);
            Assert.That(!body2.Awake);

            entManager.DeleteEntity(ent2);
            broadphase.FindNewContacts(mapId);
        });
    }
}
