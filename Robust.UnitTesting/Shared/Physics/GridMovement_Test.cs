using System.Threading.Tasks;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture, TestOf(typeof(SharedBroadphaseSystem))]
public sealed class GridMovement_Test : RobustIntegrationTest
{
    [Test]
    public async Task TestFindGridContacts()
    {
        var server = StartServer();

        await server.WaitIdleAsync();

        // Checks that FindGridContacts succesfully overlaps a grid + map broadphase physics body
        var systems = server.ResolveDependency<IEntitySystemManager>();
        var fixtureSystem = systems.GetEntitySystem<FixtureSystem>();
        var mapManager = server.ResolveDependency<IMapManager>();
        var entManager = server.ResolveDependency<IEntityManager>();
        var physSystem = systems.GetEntitySystem<SharedPhysicsSystem>();

        await server.WaitAssertion(() =>
        {
            var mapId = mapManager.CreateMap();
            var grid = mapManager.CreateGrid(mapId);

            // Setup 1 body on grid, 1 body off grid, and assert that it's all gucci.
            grid.SetTile(Vector2i.Zero, new Tile(1));
            var fixtures = entManager.GetComponent<FixturesComponent>(grid.Owner);
            Assert.That(fixtures.FixtureCount, Is.EqualTo(1));

            var onGrid = entManager.SpawnEntity(null, new EntityCoordinates(grid.Owner, 0.5f, 0.5f ));
            var onGridBody = entManager.AddComponent<PhysicsComponent>(onGrid);
            physSystem.SetBodyType(onGrid, BodyType.Dynamic, body: onGridBody);
            var shapeA = new PolygonShape();
            shapeA.SetAsBox(0.5f, 0.5f);
            fixtureSystem.CreateFixture(onGrid, new Fixture("fix1", shapeA, 1, 0, false), body: onGridBody);
            Assert.That(fixtureSystem.GetFixtureCount(onGrid), Is.EqualTo(1));
            Assert.That(entManager.GetComponent<TransformComponent>(onGrid).ParentUid, Is.EqualTo(grid.Owner));
            physSystem.WakeBody(onGrid, body: onGridBody);
            Assert.That(onGridBody.Awake);

            var offGrid = entManager.SpawnEntity(null, new MapCoordinates(new Vector2(10f, 10f), mapId));
            var offGridBody = entManager.AddComponent<PhysicsComponent>(offGrid);
            physSystem.SetBodyType(offGrid, BodyType.Dynamic, body: offGridBody);
            var shapeB = new PolygonShape();
            shapeB.SetAsBox(0.5f, 0.5f);
            fixtureSystem.CreateFixture(offGrid, new Fixture("fix1", shapeB, 0, 1, false), body: offGridBody);
            Assert.That(fixtureSystem.GetFixtureCount(offGrid), Is.EqualTo(1));
            Assert.That(entManager.GetComponent<TransformComponent>(offGrid).ParentUid, Is.Not.EqualTo((grid.Owner)));
            physSystem.WakeBody(offGrid, body: offGridBody);
            Assert.That(offGridBody.Awake);

            // Alright just a quick validation then we start the actual damn test.
            physSystem.Update(0.001f);

            Assert.That(onGridBody.ContactCount, Is.EqualTo(0));

            // Alright now move the grid on top of the off grid body, run physics for a frame and see if they contact
            entManager.GetComponent<TransformComponent>(grid.Owner).LocalPosition = new Vector2(10f, 10f);
            physSystem.Update(0.001f);

            Assert.That(onGridBody.ContactCount, Is.EqualTo(1));
        });
    }
}
