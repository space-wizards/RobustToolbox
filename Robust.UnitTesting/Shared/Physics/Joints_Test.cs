using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Server.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Reflection;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture, TestOf(typeof(JointSystem))]
    public sealed class Joints_Test
    {
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
            physicsSystem.SetBodyType(body1, BodyType.Dynamic);
            physicsSystem.SetBodyType(body2, BodyType.Dynamic);

            fixtureSystem.TryCreateFixture(body1, new Fixture(new PhysShapeCircle()
            {
                Radius = 0.1f,
            }, 1, 1, false));

            fixtureSystem.TryCreateFixture(body2, new Fixture(new PhysShapeCircle()
            {
                Radius = 0.1f,
            }, 1, 1, false));

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
}
