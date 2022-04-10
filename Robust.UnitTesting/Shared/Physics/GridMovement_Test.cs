using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Dynamics;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture, TestOf(typeof(SharedBroadphaseSystem))]
public class GridMovement_Test : RobustIntegrationTest
{
    [Test]
    public async Task TestFindGridContacts()
    {
        var server = StartServer();

        await server.WaitIdleAsync();

        // Checks that FindGridContacts succesfully overlaps a grid + map broadphase physics body
        var systems = server.ResolveDependency<IEntitySystemManager>();
        var broadphase = systems.GetEntitySystem<SharedBroadphaseSystem>();
        var fixtureSystem = systems.GetEntitySystem<FixtureSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var entManager = server.ResolveDependency<IEntityManager>();

        await server.WaitAssertion(() =>
        {
            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);

            // Setup 1 body on grid, 1 body off grid, and assert that it's all gucci.
            grid.SetTile(Vector2i.Zero, new Tile(1));
            var physics = entManager.GetComponent<PhysicsComponent>(grid.GridEntityId);
            var fixtures = entManager.GetComponent<FixturesComponent>(grid.GridEntityId);
            Assert.That(fixtures.FixtureCount, Is.EqualTo(1));

            var onGrid = entManager.SpawnEntity(null, new EntityCoordinates(grid.GridEntityId, 0.5f, 0.5f ));
            var onGridBody = entManager.AddComponent<PhysicsComponent>(onGrid);
            onGridBody.BodyType = BodyType.Dynamic;
            var shapeA = new PolygonShape();
            shapeA.SetAsBox(-0.5f, 0.5f);
            var fixtureA = fixtureSystem.CreateFixture(onGridBody, shapeA);
            fixtureA.CollisionMask = 1;
            Assert.That(onGridBody.FixtureCount, Is.EqualTo(1));
            Assert.That(entManager.GetComponent<TransformComponent>(onGrid).ParentUid, Is.EqualTo(grid.GridEntityId));

            var offGrid = entManager.SpawnEntity(null, new MapCoordinates(new Vector2(10f, 10f), mapId));
            var offGridBody = entManager.AddComponent<PhysicsComponent>(offGrid);
            offGridBody.BodyType = BodyType.Dynamic;
            var shapeB = new PolygonShape();
            shapeB.SetAsBox(-0.5f, 0.5f);
            var fixtureB = fixtureSystem.CreateFixture(offGridBody, shapeB);
            fixtureB.CollisionLayer = 1;
            Assert.That(offGridBody.FixtureCount, Is.EqualTo(1));
            Assert.That(entManager.GetComponent<TransformComponent>(offGrid).ParentUid, Is.Not.EqualTo((grid.GridEntityId)));

            // Alright just a quick validation then we start the actual damn test.

            var physicsMap = entManager.GetComponent<SharedPhysicsMapComponent>(mapManager.GetMapEntityId(mapId));
            physicsMap.Step(0.001f, false);

            Assert.That(onGridBody.ContactCount, Is.EqualTo(0));

            // Alright now move the grid on top of the off grid body, run physics for a frame and see if they contact
            entManager.GetComponent<TransformComponent>(grid.GridEntityId).LocalPosition = new Vector2(10f, 10f);
            physicsMap.Step(0.001f, false);

            Assert.That(onGridBody.ContactCount, Is.EqualTo(1));
        });
    }
}
