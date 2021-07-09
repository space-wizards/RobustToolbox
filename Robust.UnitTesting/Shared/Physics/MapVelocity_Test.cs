using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;

namespace Robust.UnitTesting.Shared.Physics
{
    public class MapVelocity_Test : RobustIntegrationTest
    {
        private const string DummyEntity = "Dummy";

        private static readonly string Prototypes = $@"
- type: entity
  name: {DummyEntity}
  id: {DummyEntity}
  components:
  - type: Physics
    bodyType: Dynamic
";

        [Test]
        public async Task Test()
        {
            var server = StartServer(new ServerIntegrationOptions {ExtraPrototypes = Prototypes});

            await server.WaitIdleAsync();
            var compManager = server.ResolveDependency<IComponentManager>();
            var entityManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            await server.WaitAssertion(() =>
            {
                var mapId = mapManager.CreateMap();
                var grid = mapManager.CreateGrid(mapId);

                Assert.That(compManager.TryGetComponent<PhysicsComponent>(grid.GridEntityId, out var gridPhysics));

                // TODO: Once grid rotations are a stable thing try it with angular velocity too.
                // Check that grid even moving first
                gridPhysics.BodyType = BodyType.Dynamic;
                gridPhysics.LinearVelocity = Vector2.One;
                Assert.That(gridPhysics.LinearVelocity, Is.EqualTo(Vector2.One));

                // Check that map velocity is correct for entity
                var dummy = entityManager.SpawnEntity(DummyEntity, new EntityCoordinates(grid.GridEntityId, Vector2.Zero));
                Assert.That(compManager.TryGetComponent<PhysicsComponent>(dummy.Uid, out var body));
                Assert.That(body.LinearVelocity, Is.EqualTo(Vector2.Zero));

                Assert.That(body.MapLinearVelocity, Is.EqualTo(Vector2.One));

                // Check that the newly parented entity's velocity is correct
                // it should retain its previous velocity despite new parent
                var grid2 = mapManager.CreateGrid(mapId);
                dummy.Transform.AttachParent(entityManager.GetEntity(grid2.GridEntityId));

                Assert.That(body.MapLinearVelocity, Is.EqualTo(Vector2.One));
            });
        }
    }
}
