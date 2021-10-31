using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture, TestOf(typeof(CollisionWakeSystem))]
    public class CollisionWake_Test : RobustIntegrationTest
    {
        private const string Prototype = @"
- type: entity
  name: dummy
  id: CollisionWakeTestItem
  components:
  - type: Transform
  - type: Physics
    bodyType: Dynamic
  - type: CollisionWake
";

        /// <summary>
        /// Test whether a CollisionWakeComponent correctly turns off collision on a grid and leaves it on off of a grid.
        /// </summary>
        [Test]
        public async Task TestCollisionWakeGrid()
        {
            var options = new ServerIntegrationOptions {ExtraPrototypes = Prototype};
            options.CVarOverrides["physics.timetosleep"] = "0.0";
            var server = StartServer(options);
            await server.WaitIdleAsync();

            var entManager = server.ResolveDependency<IEntityManager>();
            var mapManager = server.ResolveDependency<IMapManager>();

            IMapGrid grid = default!;
            MapId mapId = default!;
            PhysicsComponent physics = default!;

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                grid = mapManager.CreateGrid(mapId);

                var entity = entManager.SpawnEntity("CollisionWakeTestItem", new MapCoordinates(Vector2.One, mapId));
                physics = entManager.GetComponent<PhysicsComponent>(entity.Uid);
            });

            // Should still be collidable
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That(physics.Awake, Is.EqualTo(false));
                Assert.That(physics.CanCollide, Is.EqualTo(true));

                physics.Owner.Transform.AttachParent(entManager.GetEntity(grid.GridEntityId));
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That(physics.Awake, Is.EqualTo(false));
                Assert.That(physics.CanCollide, Is.EqualTo(false));

                physics.Owner.Transform.AttachParent(mapManager.GetMapEntity(mapId));
            });

            // Juussttt in case we'll re-parent it to the map and check its collision is back on.
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That(physics.Awake, Is.EqualTo(false));
                Assert.That(physics.CanCollide, Is.EqualTo(true));
            });
        }
    }
}
