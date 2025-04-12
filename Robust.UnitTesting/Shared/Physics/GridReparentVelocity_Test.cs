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
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture, TestOf(typeof(SharedPhysicsSystem))]
public sealed class GridReparentVelocity_Test
{
    private ISimulation _sim = default!;
    private IEntitySystemManager _systems = default!;
    private IEntityManager _entManager = default!;
    private IMapManager _mapManager = default!;
    private FixtureSystem _fixtureSystem = default!;
    private SharedMapSystem _mapSystem = default!;
    private SharedPhysicsSystem _physSystem = default!;

    // Test objects.
    private EntityUid _mapUid = default!;
    private MapId _mapId = default!;
    private EntityUid _gridUid = default!;
    private EntityUid _objUid = default!;

    [OneTimeSetUp]
    public void FixtureSetup()
    {
        _sim = RobustServerSimulation.NewSimulation()
            .InitializeInstance();

        _systems = _sim.Resolve<IEntitySystemManager>();
        _entManager = _sim.Resolve<IEntityManager>();
        _mapManager = _sim.Resolve<IMapManager>();
        _fixtureSystem = _systems.GetEntitySystem<FixtureSystem>();
        _mapSystem = _systems.GetEntitySystem<SharedMapSystem>();
        _physSystem = _systems.GetEntitySystem<SharedPhysicsSystem>();
    }

    [SetUp]
    public void Setup()
    {
        _mapUid = _mapSystem.CreateMap(out _mapId);

        // Spawn a 1x1 grid centered at (0.5, 0.5), ensure it's movable and its velocity has no damping.
        var gridEnt = _mapManager.CreateGridEntity(_mapId);
        var gridPhys = _entManager.GetComponent<PhysicsComponent>(gridEnt);
        _physSystem.SetSleepingAllowed(gridEnt, gridPhys, false);
        _physSystem.SetBodyType(gridEnt, BodyType.Dynamic, body: gridPhys);
        _physSystem.SetLinearDamping(gridEnt, gridPhys, 0.0f);
        _physSystem.SetAngularDamping(gridEnt, gridPhys, 0.0f);

        _mapSystem.SetTile(gridEnt, Vector2i.Zero, new Tile(1));
        _physSystem.WakeBody(gridEnt, body: gridPhys);

        _gridUid = gridEnt.Owner;
    }

    // Spawn a bullet-like test object at the given position.
    public EntityUid SetupTestObject(EntityCoordinates coords)
    {
        var obj = _entManager.SpawnEntity(null, coords);

        var objPhys = _entManager.EnsureComponent<PhysicsComponent>(obj);
        var objFix = _entManager.EnsureComponent<FixturesComponent>(obj);

        // Set up physics (no velocity damping, dynamic body, physics enabled)
        _entManager.GetComponent<PhysicsComponent>(obj);
        _physSystem.SetSleepingAllowed(obj, objPhys, false);
        _physSystem.SetBodyType(obj, BodyType.Dynamic, body: objPhys);
        _physSystem.SetLinearDamping(obj, objPhys, 0.0f);
        _physSystem.SetAngularDamping(obj, objPhys, 0.0f);

        // Set up fixture.
        var poly = new PolygonShape();
        poly.SetAsBox(0.1f, 0.1f);
        _fixtureSystem.CreateFixture(obj, "fix1", new Fixture(poly, 0, 0, false), manager: objFix, body: objPhys);
        _physSystem.WakeBody(obj, body: objPhys);

        return obj;
    }

    [TearDown]
    public void Teardown()
    {
        _entManager.DeleteEntity(_gridUid);
        _gridUid = default!;
        _entManager.DeleteEntity(_objUid);
        _objUid = default!;
        _mapSystem.DeleteMap(_mapId);
        _mapId = default!;
        _entManager.DeleteEntity(_mapUid);
    }

    // Moves an object off of a moving grid, checks for conservation of linear velocity.
    [Test]
    public void TestLinearVelocityOnlyMoveOffGrid()
    {
        // Spawn our test object in the middle of the grid, ensure it has no damping.
        _objUid = SetupTestObject(new EntityCoordinates(_gridUid, 0.5f, 0.5f));

        Assert.Multiple(() =>
        {
            // Our object should start on the grid.
            Assert.That(_entManager.GetComponent<TransformComponent>(_objUid).ParentUid, Is.EqualTo(_gridUid));

            // Set the velocity of the grid and our object.
            Assert.That(_physSystem.SetLinearVelocity(_objUid, new Vector2(3.5f, 4.75f)), Is.True);
            Assert.That(_physSystem.SetLinearVelocity(_gridUid, new Vector2(1.0f, 2.0f)), Is.True);

            // Wait a second to clear the grid
            _physSystem.Update(1.0f);

            // The object should be parented to the map and maintain its map velocity, the grid should be unchanged.
            var objXform = _entManager.GetComponent<TransformComponent>(_objUid);
            var gridXform = _entManager.GetComponent<TransformComponent>(_gridUid);
            Assert.That(objXform.ParentUid, Is.EqualTo(_mapUid), $"Object is not on map - actual position: {objXform.ParentUid} {objXform.LocalPosition}, grid position: {gridXform.ParentUid} {gridXform.LocalPosition}");
            Assert.That(_entManager.GetComponent<PhysicsComponent>(_objUid).LinearVelocity, Is.EqualTo(new Vector2(4.5f, 6.75f)));
            Assert.That(_entManager.GetComponent<PhysicsComponent>(_gridUid).LinearVelocity, Is.EqualTo(new Vector2(1.0f, 2.0f)));
        });
    }

    [Test]
    // Moves an object onto a moving grid, checks for conservation of linear velocity.
    public void TestLinearVelocityOnlyMoveOntoGrid()
    {
        // Spawn our test object 1 m off of the middle of the grid in both directions.
        _objUid = SetupTestObject(new EntityCoordinates(_mapUid, 1.5f, 1.5f));

        Assert.Multiple(() =>
        {
            // Assert that we start off the grid.
            Assert.That(_entManager.GetComponent<TransformComponent>(_objUid).ParentUid, Is.EqualTo(_mapUid));

            // Set the velocity of the grid and our object.
            Assert.That(_physSystem.SetLinearVelocity(_objUid, new Vector2(-2.0f, -3.0f)), Is.True);
            Assert.That(_physSystem.SetLinearVelocity(_gridUid, new Vector2(-1.0f, -2.0f)), Is.True);

            // Wait a second to move onto the middle of the grid
            _physSystem.Update(1.0f);

            // The object should be parented to the grid and maintain its map velocity (slowing down), the grid should be unchanged.
            var objXform = _entManager.GetComponent<TransformComponent>(_objUid);
            var gridXform = _entManager.GetComponent<TransformComponent>(_gridUid);
            Assert.That(objXform.ParentUid, Is.EqualTo(_gridUid), $"Object is not on grid - actual position: {objXform.ParentUid} {objXform.LocalPosition}, grid position: {gridXform.ParentUid} {gridXform.LocalPosition}");
            Assert.That(_entManager.GetComponent<PhysicsComponent>(_objUid).LinearVelocity, Is.EqualTo(new Vector2(-1.0f, -1.0f)));
            Assert.That(_entManager.GetComponent<PhysicsComponent>(_gridUid).LinearVelocity, Is.EqualTo(new Vector2(-1.0f, -2.0f)));
        });
    }

    [Test]
    // Moves a rotating object off of a rotating grid, checks for conservation of angular velocity.
    public void TestLinearAndAngularVelocityMoveOffGrid()
    {
        // Spawn our test object in the middle of the grid.
        _objUid = SetupTestObject(new EntityCoordinates(_gridUid, 0.5f, 0.5f));

        Assert.Multiple(() =>
        {
            // Our object should start on the grid.
            Assert.That(_entManager.GetComponent<TransformComponent>(_objUid).ParentUid, Is.EqualTo(_gridUid));

            // Set the velocity of the grid and our object.
            Assert.That(_physSystem.SetLinearVelocity(_objUid, new Vector2(3.5f, 4.75f)), Is.True);
            Assert.That(_physSystem.SetAngularVelocity(_objUid, 1.0f), Is.True);
            Assert.That(_physSystem.SetLinearVelocity(_gridUid, new Vector2(1.0f, 2.0f)), Is.True);
            Assert.That(_physSystem.SetAngularVelocity(_gridUid, 2.0f), Is.True);

            // Wait a second to clear the grid
            _physSystem.Update(1.0f);

            // The object should be parented to the map and maintain its map velocity, the grid should be unchanged.
            var objXform = _entManager.GetComponent<TransformComponent>(_objUid);
            var gridXform = _entManager.GetComponent<TransformComponent>(_gridUid);
            Assert.That(objXform.ParentUid, Is.EqualTo(_mapUid), $"Object is not on map - actual position: {objXform.ParentUid} {objXform.LocalPosition}, grid position: {gridXform.ParentUid} {gridXform.LocalPosition}");
            // Not checking object's linear velocity in this case, non-zero contribution from grid angular velocity.
            Assert.That(_entManager.GetComponent<PhysicsComponent>(_objUid).AngularVelocity, Is.EqualTo(3.0f));
            var gridPhys = _entManager.GetComponent<PhysicsComponent>(_gridUid);
            Assert.That(gridPhys.LinearVelocity, Is.EqualTo(new Vector2(1.0f, 2.0f)));
            Assert.That(gridPhys.AngularVelocity, Is.EqualTo(2.0f));
        });
    }

    [Test]
    // Moves a rotating object onto a rotating grid, checks for conservation of angular velocity.
    public void TestLinearAndAngularVelocityMoveOntoGrid()
    {
        // Spawn our test object 1 m off of the middle of the grid in both directions.
        _objUid = SetupTestObject(new EntityCoordinates(_mapUid, 1.5f, 1.5f));

        Assert.Multiple(() =>
        {
            // Assert that we start off the grid.
            Assert.That(_entManager.GetComponent<TransformComponent>(_objUid).ParentUid, Is.EqualTo(_mapUid));

            // Set the velocity of the grid and our object.
            Assert.That(_physSystem.SetLinearVelocity(_objUid, new Vector2(-2.0f, -3.0f)), Is.True);
            Assert.That(_physSystem.SetAngularVelocity(_objUid, 1.0f), Is.True);
            Assert.That(_physSystem.SetLinearVelocity(_gridUid, new Vector2(-1.0f, -2.0f)), Is.True);
            Assert.That(_physSystem.SetAngularVelocity(_gridUid, 2.0f), Is.True);

            // Wait a second to move onto the middle of the grid
            _physSystem.Update(1.0f);

            // The object should be parented to the grid and maintain its map velocity (slowing down), the grid should be unchanged.
            var objXform = _entManager.GetComponent<TransformComponent>(_objUid);
            var gridXform = _entManager.GetComponent<TransformComponent>(_gridUid);
            Assert.That(objXform.ParentUid, Is.EqualTo(_gridUid), $"Object is not on grid - actual position: {objXform.ParentUid} {objXform.LocalPosition}, grid position: {gridXform.ParentUid} {gridXform.LocalPosition}");
            // Not checking object's linear velocity in this case, non-zero contribution from grid angular velocity.
            Assert.That(_entManager.GetComponent<PhysicsComponent>(_objUid).AngularVelocity, Is.EqualTo(-1.0f));
            var gridPhys = _entManager.GetComponent<PhysicsComponent>(_gridUid);
            Assert.That(gridPhys.LinearVelocity, Is.EqualTo(new Vector2(-1.0f, -2.0f)));
            Assert.That(gridPhys.AngularVelocity, Is.EqualTo(2.0f));
        });
    }
}
