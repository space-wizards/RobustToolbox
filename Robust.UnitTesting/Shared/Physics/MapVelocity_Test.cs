using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics
{
    public sealed class MapVelocity_Test : RobustIntegrationTest
    {
        private const string DummyEntity = "Dummy";

        private static readonly string Prototypes = $@"
- type: entity
  name: {DummyEntity}
  id: {DummyEntity}
  components:
  - type: Physics
    bodyType: Dynamic
  - type: Fixtures
";
        [Test]
        public async Task TestMapVelocities()
        {
            var server = StartServer(new ServerIntegrationOptions {ExtraPrototypes = Prototypes});

            await server.WaitIdleAsync();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var physicsSys = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedPhysicsSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var grid = mapManager.CreateGrid(mapId);
                var grid2 = mapManager.CreateGrid(mapId);

                Assert.That(entityManager.TryGetComponent<PhysicsComponent>(grid.GridEntityId, out var gridPhysics));
                gridPhysics.BodyType = BodyType.Dynamic;

                Vector2 offset = new(3, 4);
                Vector2 expectedFinalVelocity = new Vector2(-4, 3) * 2 + Vector2.One;

                var dummy = entityManager.SpawnEntity(DummyEntity, new EntityCoordinates(grid.GridEntityId, offset));
                Assert.That(entityManager.TryGetComponent(dummy, out PhysicsComponent body));
                Assert.That(entityManager.TryGetComponent(dummy, out TransformComponent xform));
                xform.AttachParent(grid.GridEntityId);

                // Test Linear Velocities
                gridPhysics.LinearVelocity = Vector2.One;
                Assert.That(body.LinearVelocity, Is.Approximately(Vector2.Zero, 1e-6));
                Assert.That(body.AngularVelocity, Is.Approximately(0f, 1e-6));

                var linearVelocity = physicsSys.GetMapLinearVelocity(dummy, body);
                var angularVelocity = physicsSys.GetMapAngularVelocity(dummy, body);
                var velocities = physicsSys.GetMapVelocities(dummy, body);
                Assert.That(linearVelocity, Is.Approximately(Vector2.One, 1e-6));
                Assert.That(angularVelocity, Is.Approximately(0f, 1e-6));
                Assert.That(velocities.Item1, Is.Approximately(linearVelocity, 1e-6));
                Assert.That(velocities.Item2, Is.Approximately(angularVelocity, 1e-6));

                // Add angular velocity
                gridPhysics.AngularVelocity = 2;
                Assert.That(body.LinearVelocity, Is.EqualTo(Vector2.Zero));
                Assert.That(body.AngularVelocity, Is.EqualTo(0f));

                linearVelocity = physicsSys.GetMapLinearVelocity(dummy, body);
                angularVelocity = physicsSys.GetMapAngularVelocity(dummy, body);
                velocities = physicsSys.GetMapVelocities(dummy, body);
                Assert.That(linearVelocity, Is.Approximately(expectedFinalVelocity, 1e-6));
                Assert.That(angularVelocity, Is.Approximately(2f, 1e-6));
                Assert.That(velocities.Item1, Is.Approximately(linearVelocity, 1e-6));
                Assert.That(velocities.Item2, Is.Approximately(angularVelocity, 1e-6));

                // Check that velocity does not change when changing parent
                xform.AttachParent(grid2.GridEntityId);
                linearVelocity = physicsSys.GetMapLinearVelocity(dummy, body);
                angularVelocity = physicsSys.GetMapAngularVelocity(dummy, body);
                velocities = physicsSys.GetMapVelocities(dummy, body);
                Assert.That(linearVelocity, Is.Approximately(expectedFinalVelocity, 1e-6));
                Assert.That(angularVelocity, Is.Approximately(2f, 1e-6));
                Assert.That(velocities.Item1, Is.Approximately(linearVelocity, 1e-6));
                Assert.That(velocities.Item2, Is.Approximately(angularVelocity, 1e-6));
            });
        }

        // Check that if something has more than one parent, the velocities are properly added
        [Test]
        public async Task TestNestedParentVelocities()
        {
            var server = StartServer(new ServerIntegrationOptions { ExtraPrototypes = Prototypes });

            await server.WaitIdleAsync();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();
            var physicsSys = server.ResolveDependency<IEntitySystemManager>().GetEntitySystem<SharedPhysicsSystem>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var grid = mapManager.CreateGrid(mapId);

                Assert.That(entityManager.TryGetComponent<PhysicsComponent>(grid.GridEntityId, out var gridPhysics));
                gridPhysics.BodyType = BodyType.Dynamic;

                Vector2 offset1 = new(2, 0);
                var dummy1 = entityManager.SpawnEntity(DummyEntity, new EntityCoordinates(grid.GridEntityId, offset1));
                Assert.That(entityManager.TryGetComponent(dummy1, out PhysicsComponent body1));
                Assert.That(entityManager.TryGetComponent(dummy1, out TransformComponent xform1));
                xform1.AttachParent(grid.GridEntityId);

                // create another entity attached to the dummy1
                Vector2 offset2 = new(-1, 0);
                var dummy2 = entityManager.SpawnEntity(DummyEntity, new EntityCoordinates(dummy1, offset2));
                Assert.That(entityManager.TryGetComponent(dummy2, out PhysicsComponent body2));
                Assert.That(entityManager.TryGetComponent(dummy2, out TransformComponent xform2));
                xform2.AttachParent(dummy1);

                Assert.That(xform2.WorldPosition, Is.Approximately(new Vector2(1, 0), 1e-6));

                gridPhysics.LinearVelocity = new Vector2(1, 0);
                gridPhysics.AngularVelocity = 1;

                // check that dummy2 properly gets the velocities from its grand-parent
                var linearVelocity = physicsSys.GetMapLinearVelocity(dummy2, body2);
                var angularVelocity = physicsSys.GetMapAngularVelocity(dummy2, body2);
                var velocities = physicsSys.GetMapVelocities(dummy2, body2);
                Assert.That(linearVelocity, Is.Approximately(Vector2.One, 1e-6));
                Assert.That(angularVelocity, Is.Approximately(1f, 1e-6));
                Assert.That(velocities.Item1, Is.Approximately(linearVelocity, 1e-6));
                Assert.That(velocities.Item2, Is.Approximately(angularVelocity, 1e-6));

                // check that if we make move in the opposite direction, but spin in the same direction, then dummy2 is
                // (for this moment in time) stationary, but still rotating.
                body1.LinearVelocity = -gridPhysics.LinearVelocity;
                body1.AngularVelocity = gridPhysics.AngularVelocity;
                linearVelocity = physicsSys.GetMapLinearVelocity(dummy2, body2);
                angularVelocity = physicsSys.GetMapAngularVelocity(dummy2, body2);
                velocities = physicsSys.GetMapVelocities(dummy2, body2);
                Assert.That(linearVelocity, Is.Approximately(Vector2.Zero, 1e-6));
                Assert.That(angularVelocity, Is.Approximately(2f, 1e-6));
                Assert.That(velocities.Item1, Is.Approximately(linearVelocity, 1e-6));
                Assert.That(velocities.Item2, Is.Approximately(angularVelocity, 1e-6));

                // but not if we update the local position:
                xform2.WorldPosition = Vector2.Zero;
                linearVelocity = physicsSys.GetMapLinearVelocity(dummy2, body2);
                angularVelocity = physicsSys.GetMapAngularVelocity(dummy2, body2);
                velocities = physicsSys.GetMapVelocities(dummy2, body2);
                Assert.That(linearVelocity, Is.Approximately(new Vector2(0, -2), 1e-6));
                Assert.That(angularVelocity, Is.Approximately(2f, 1e-6));
                Assert.That(velocities.Item1, Is.Approximately(linearVelocity, 1e-6));
                Assert.That(velocities.Item2, Is.Approximately(angularVelocity, 1e-6));
            });
        }
    }
}
