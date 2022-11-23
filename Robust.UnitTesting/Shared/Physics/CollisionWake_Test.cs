using NUnit.Framework;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Components;
using System.Threading.Tasks;
using Robust.Shared.Map.Components;

namespace Robust.UnitTesting.Shared.Physics
{
    [TestFixture, TestOf(typeof(CollisionWakeSystem))]
    public sealed class CollisionWake_Test : RobustIntegrationTest
    {
        private const string Prototype = @"
- type: entity
  name: dummy
  id: CollisionWakeTestItem
  components:
  - type: Transform
  - type: Physics
    bodyType: Dynamic
  - type: Fixtures
    fixtures:
    - shape:
        !type:PhysShapeCircle
        radius: 0.35
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

            MapGridComponent grid = default!;
            MapId mapId = default!;
            PhysicsComponent entityOnePhysics = default!;
            TransformComponent xform = default!;
            PhysicsComponent entityTwoPhysics = default!;

            await server.WaitPost(() =>
            {
                mapId = mapManager.CreateMap();
                grid = mapManager.CreateGrid(mapId);
                grid.SetTile(Vector2i.Zero, new Tile(1));

                var entityOne = entManager.SpawnEntity("CollisionWakeTestItem", new MapCoordinates(Vector2.One * 2f, mapId));
                entityOnePhysics = entManager.GetComponent<PhysicsComponent>(entityOne);
                xform = entManager.GetComponent<TransformComponent>(entityOne);
                Assert.That(xform.ParentUid == mapManager.GetMapEntityId(mapId));

                var entityTwo = entManager.SpawnEntity("CollisionWakeTestItem", new EntityCoordinates(grid.GridEntityId, new Vector2(0.5f, 0.5f)));
                entityTwoPhysics = entManager.GetComponent<PhysicsComponent>(entityTwo);
                Assert.That(entManager.GetComponent<TransformComponent>(entityTwo).ParentUid == grid.GridEntityId);

            });

            // Item 1 Should still be collidable
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That(entityOnePhysics.Awake, Is.EqualTo(false));
                Assert.That(entityOnePhysics.CanCollide, Is.EqualTo(true));

                xform.LocalPosition = new Vector2(0.5f, 0.5f);
                xform.AttachParent(grid.GridEntityId);

                // Entity 2 should immediately not be collidable on spawn
                Assert.That(entityTwoPhysics.Awake, Is.EqualTo(false));
                Assert.That(entityTwoPhysics.CanCollide, Is.EqualTo(false));
            });

            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That(entityOnePhysics.Awake, Is.EqualTo(false));
                Assert.That(entityOnePhysics.CanCollide, Is.EqualTo(false));

                xform.LocalPosition = Vector2.One * 2f;
                xform.AttachParent(mapManager.GetMapEntityId(mapId));
            });

            // Juussttt in case we'll re-parent it to the map and check its collision is back on.
            await server.WaitRunTicks(1);

            await server.WaitAssertion(() =>
            {
                Assert.That(entityOnePhysics.Awake, Is.EqualTo(false));
                Assert.That(entityOnePhysics.CanCollide, Is.EqualTo(true));
            });
        }
    }
}
