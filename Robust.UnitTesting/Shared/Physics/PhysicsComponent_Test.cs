using System.Collections.Generic;
using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture]
    [TestOf(typeof(PhysicsComponent))]
    public sealed class PhysicsComponent_Test : RobustIntegrationTest
    {
        [Test]
        public async Task TestPointLinearImpulse()
        {
            var server = StartServer();
            await server.WaitIdleAsync();
            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var fixtureSystem = server.ResolveDependency<IEntitySystemManager>()
                .GetEntitySystem<FixtureSystem>();
            var physicsSystem = server.ResolveDependency<IEntitySystemManager>()
                .GetEntitySystem<SharedPhysicsSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var boxEnt = entManager.SpawnEntity(null, new MapCoordinates(Vector2.Zero, mapId));
                var box = entManager.AddComponent<PhysicsComponent>(boxEnt);
                var poly = new PolygonShape();
                poly.SetAsBox(0.5f, 0.5f);
                var fixture = fixtureSystem.CreateFixture(box, poly);
                physicsSystem.SetDensity(fixture, 1f);
                physicsSystem.SetFixedRotation(box, false);
                physicsSystem.SetBodyType(box, BodyType.Dynamic);
                Assert.That(box.InvI, Is.GreaterThan(0f));

                // Check regular impulse works
                physicsSystem.ApplyLinearImpulse(box, new Vector2(0f, 1f));
                Assert.That(box.LinearVelocity.Length, Is.GreaterThan(0f));

                // Reset the box
                physicsSystem.SetLinearVelocity(box, Vector2.Zero);
                Assert.That(box.LinearVelocity.Length, Is.EqualTo(0f));
                Assert.That(box.AngularVelocity, Is.EqualTo(0f));

                // Check the angular impulse is applied from the point
                physicsSystem.ApplyLinearImpulse(box, new Vector2(0f, 1f), new Vector2(0.5f, 0f));
                Assert.That(box.LinearVelocity.Length, Is.GreaterThan(0f));
                Assert.That(box.AngularVelocity, Is.Not.EqualTo(0f));
            });
        }
    }
}
