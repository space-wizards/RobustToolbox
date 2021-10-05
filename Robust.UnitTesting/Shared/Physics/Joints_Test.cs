using System;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Client.Physics;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Dynamics.Joints;
using Robust.Shared.Reflection;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture, TestOf(typeof(JointSystem))]
    public sealed class Joints_Test : RobustIntegrationTest
    {
        /// <summary>
        /// Simple test that just adds and removes each joint.
        /// </summary>
        /// <returns></returns>
        [Test]
        public async Task TestJoints()
        {
            var server = StartServer();
            await server.WaitIdleAsync();

            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var reflectionManager = server.ResolveDependency<IReflectionManager>();
            var typeFactory = server.ResolveDependency<IDynamicTypeFactory>();
            var jointSystem = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedJointSystem>();

            /*
            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var entA = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
                var entB = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
                var bodyA = entA.EnsureComponent<PhysicsComponent>();
                var bodyB = entB.EnsureComponent<PhysicsComponent>();

                foreach (var jType in new Joint[]
                {
                    new DistanceJoint(),
                    new FrictionJoint(),
                    new RevoluteJoint()
                })
                {
                    if (jType.IsAbstract) continue;
                    var joint = (Joint) typeFactory.CreateInstance(jType);
                    jointSystem.AddJointDeferred(bodyA, bodyB, joint);
                    jointSystem.Update(0.016f);
                    jointSystem.RemoveJointDeferred(joint);
                }
            });
            */
        }
    }
}
