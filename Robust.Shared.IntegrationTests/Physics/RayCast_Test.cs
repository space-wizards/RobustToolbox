using System;
using System.Linq;
using System.Numerics;
using NUnit.Framework;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.UnitTesting.Shared.Physics;

[TestFixture]
internal sealed class RayCast_Test
{
    public enum SensorCastKind
    {
        RayAll,
        RayClosest,
        Shape,
    }

    private static TestCaseData[] _rayCases =
    {
        // Ray goes through
        new(new Vector2(0f, 0.5f), Vector2.UnitY * 2f, new Vector2(0f, 1f - PhysicsConstants.PolygonRadius)),

        // Ray stops inside
        new(new Vector2(0f, 0.5f), Vector2.UnitY, new Vector2(0f, 1f - PhysicsConstants.PolygonRadius)),

        // Ray starts inside
        new(new Vector2(0f, 1.5f), Vector2.UnitY, null),

        // No hit
        new(new Vector2(0f, 0.5f), -Vector2.UnitY, null),
    };

    private static TestCaseData[] _shapeCases =
    {
        // Circle
        // - Initial overlap, no shapecast
        new(new PhysShapeCircle(0.5f, Vector2.Zero), new Transform(Vector2.UnitY / 2f, Angle.Zero), Vector2.UnitY, null),

        // - Cast
        new(new PhysShapeCircle(0.5f, Vector2.Zero), new Transform(Vector2.Zero, Angle.Zero), Vector2.UnitY, new Vector2(0f, 1f - PhysicsConstants.PolygonRadius)),

        // - Miss
        new(new PhysShapeCircle(0.5f, Vector2.Zero), new Transform(Vector2.Zero, Angle.Zero), -Vector2.UnitY, null),

        // Polygon
        // - Initial overlap, no shapecast
        new(new SlimPolygon(Box2.UnitCentered), new Transform(Vector2.UnitY / 2f, Angle.Zero), Vector2.UnitY, null),

        // - Cast
        new(new SlimPolygon(Box2.UnitCentered), new Transform(Vector2.Zero, Angle.Zero), Vector2.UnitY, new Vector2(0.5f, 1f - PhysicsConstants.PolygonRadius)),

        // - Miss
        new(new SlimPolygon(Box2.UnitCentered), new Transform(Vector2.Zero, Angle.Zero), -Vector2.UnitY, null),
    };

    private static TestCaseData[] _sensorCases =
    {
        new TestCaseData(SensorCastKind.RayAll, true, false)
            .SetName("RayCast all returns hard fixtures without sensor query flag"),
        new TestCaseData(SensorCastKind.RayAll, true, true)
            .SetName("RayCast all returns hard fixtures with sensor query flag"),
        new TestCaseData(SensorCastKind.RayAll, false, false)
            .SetName("RayCast all filters sensor fixtures without sensor query flag"),
        new TestCaseData(SensorCastKind.RayAll, false, true)
            .SetName("RayCast all returns sensor fixtures with sensor query flag"),
        new TestCaseData(SensorCastKind.RayClosest, true, false)
            .SetName("RayCast closest returns hard fixtures without sensor query flag"),
        new TestCaseData(SensorCastKind.RayClosest, true, true)
            .SetName("RayCast closest returns hard fixtures with sensor query flag"),
        new TestCaseData(SensorCastKind.RayClosest, false, false)
            .SetName("RayCast closest filters sensor fixtures without sensor query flag"),
        new TestCaseData(SensorCastKind.RayClosest, false, true)
            .SetName("RayCast closest returns sensor fixtures with sensor query flag"),
        new TestCaseData(SensorCastKind.Shape, true, false)
            .SetName("ShapeCast returns hard fixtures without sensor query flag"),
        new TestCaseData(SensorCastKind.Shape, true, true)
            .SetName("ShapeCast returns hard fixtures with sensor query flag"),
        new TestCaseData(SensorCastKind.Shape, false, false)
            .SetName("ShapeCast filters sensor fixtures without sensor query flag"),
        new TestCaseData(SensorCastKind.Shape, false, true)
            .SetName("ShapeCast returns sensor fixtures with sensor query flag"),
    };

    [Test, TestCaseSource(nameof(_rayCases))]
    public void RayCast(Vector2 origin, Vector2 direction, Vector2? point)
    {
        var sim = RobustServerSimulation.NewSimulation().RegisterEntitySystems(f =>
        {
            f.LoadExtraSystemType<RayCastSystem>();
        }).InitializeInstance();
        Setup(sim, out var mapId);
        var raycast = sim.System<RayCastSystem>();

        var hits = raycast.CastRayClosest(mapId,
            origin,
            direction,
            new QueryFilter()
            {
                LayerBits = 1,
            });

        if (point == null)
        {
            Assert.That(!hits.Hit);
        }
        else
        {
            Assert.That(hits.Results.First().Point, Is.EqualTo(point.Value));
        }
    }

    [Test, TestCaseSource(nameof(_shapeCases))]
    public void ShapeCast(IPhysShape shape, Transform origin, Vector2 direction, Vector2? point)
    {
        var sim = RobustServerSimulation.NewSimulation().RegisterEntitySystems(f =>
        {
            f.LoadExtraSystemType<RayCastSystem>();
        }).InitializeInstance();
        Setup(sim, out var mapId);
        var raycast = sim.System<RayCastSystem>();

        var hits = raycast.CastShape(mapId,
            shape,
            origin,
            direction,
            new QueryFilter()
            {
                LayerBits = 1,
            },
            RayCastSystem.RayCastAllCallback);

        if (point == null)
        {
            Assert.That(!hits.Hit);
        }
        else
        {
            Assert.That(hits.Results.First().Point, Is.EqualTo(point.Value));
        }
    }

    [Test, TestCaseSource(nameof(_sensorCases))]
    public void SensorFixtureCasts(SensorCastKind kind, bool hardFixture, bool includeSensors)
    {
        var sim = RobustServerSimulation.NewSimulation().RegisterEntitySystems(f =>
        {
            f.LoadExtraSystemType<RayCastSystem>();
        }).InitializeInstance();
        SetupSensorFixture(sim, out var mapId, out var target, hardFixture);
        var raycast = sim.System<RayCastSystem>();

        var flags = QueryFlags.Dynamic | QueryFlags.Static;
        if (includeSensors)
            flags |= QueryFlags.Sensors;

        var filter = new QueryFilter
        {
            LayerBits = 1,
            Flags = flags,
        };

        var hits = kind switch
        {
            SensorCastKind.RayAll => raycast.CastRay(
                mapId,
                Vector2.UnitX / 2f,
                Vector2.UnitY * 3f,
                filter),
            SensorCastKind.RayClosest => raycast.CastRayClosest(
                mapId,
                Vector2.UnitX / 2f,
                Vector2.UnitY * 3f,
                filter),
            SensorCastKind.Shape => raycast.CastShape(
                mapId,
                new PhysShapeCircle(0.1f),
                new Transform(Vector2.UnitX / 2f, Angle.Zero),
                Vector2.UnitY * 3f,
                filter,
                RayCastSystem.RayCastAllCallback),
            _ => throw new ArgumentOutOfRangeException(nameof(kind), kind, null),
        };

        var expected = hardFixture || includeSensors
            ? new[] { target }
            : Array.Empty<EntityUid>();

        Assert.That(hits.Results.Select(hit => hit.Entity).ToArray(), Is.EqualTo(expected));
    }

    private void Setup(ISimulation sim, out MapId mapId)
    {
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();

        sim.System<SharedMapSystem>().CreateMap(out mapId);

        var grid = mapSystem.CreateGridEntity(mapId);

        for (var i = 0; i < 3; i++)
        {
            mapSystem.SetTile(grid, new Vector2i(i, 0), new Tile(1));
        }

        // Spawn a wall in the middle tile.
        var wall = entManager.SpawnEntity(null, new EntityCoordinates(grid.Owner, new Vector2(1.5f, 0.5f)));

        var physics = entManager.AddComponent<PhysicsComponent>(wall);
        var poly = new PolygonShape();
        poly.SetAsBox(Box2.UnitCentered);
        entManager.System<FixtureSystem>().CreateFixture(wall, "fix1", new Fixture(poly, 1, 1, true));

        entManager.System<SharedPhysicsSystem>().SetCanCollide(wall, true, body: physics);
        Assert.That(physics.CanCollide);

        // Rotate it to be vertical
        entManager.System<SharedTransformSystem>().SetLocalRotation(grid.Owner, Angle.FromDegrees(90));
        entManager.System<SharedTransformSystem>().SetLocalPosition(grid.Owner, Vector2.UnitX / 2f);
    }

    private void SetupSensorFixture(ISimulation sim, out MapId mapId, out EntityUid target, bool hard)
    {
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var fixtureSystem = entManager.System<FixtureSystem>();
        var physicsSystem = entManager.System<SharedPhysicsSystem>();

        mapSystem.CreateMap(out mapId);
        var grid = mapSystem.CreateGridEntity(mapId);

        for (var i = 0; i < 3; i++)
        {
            mapSystem.SetTile(grid, new Vector2i(0, i), new Tile(1));
        }

        target = SpawnCastTarget(entManager, fixtureSystem, physicsSystem, grid.Owner, new Vector2(0.5f, 1f), hard);
    }

    private EntityUid SpawnCastTarget(
        IEntityManager entManager,
        FixtureSystem fixtureSystem,
        SharedPhysicsSystem physicsSystem,
        EntityUid parent,
        Vector2 position,
        bool hard)
    {
        var uid = entManager.SpawnEntity(null, new EntityCoordinates(parent, position));
        var physics = entManager.AddComponent<PhysicsComponent>(uid);
        fixtureSystem.CreateFixture(uid, "fix1", new Fixture(new PhysShapeCircle(0.25f), 1, 1, hard));
        physicsSystem.SetCanCollide(uid, true, body: physics);
        Assert.That(physics.CanCollide);
        return uid;
    }
}
