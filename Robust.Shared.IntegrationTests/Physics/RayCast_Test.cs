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

        var hits = CastUpwards(raycast, kind, mapId, filter);

        var expected = hardFixture || includeSensors
            ? new[] { target }
            : Array.Empty<EntityUid>();

        Assert.That(hits.Results.Select(hit => hit.Entity).ToArray(), Is.EqualTo(expected));
    }

    /// <summary>
    /// Casts also hit entities parented directly to a map, which are in the map's broadphase rather than a grid's.
    /// </summary>
    [Test]
    public void CastsHitOffGridEntities([Values] SensorCastKind kind)
    {
        var sim = RobustServerSimulation.NewSimulation().RegisterEntitySystems(f =>
        {
            f.LoadExtraSystemType<RayCastSystem>();
        }).InitializeInstance();

        var entManager = sim.Resolve<IEntityManager>();
        var mapUid = entManager.System<SharedMapSystem>().CreateMap(out var mapId);
        var target = SpawnCastTarget(entManager,
            entManager.System<FixtureSystem>(),
            entManager.System<SharedPhysicsSystem>(),
            mapUid,
            new Vector2(0.5f, 1f),
            true);
        var raycast = sim.System<RayCastSystem>();

        var hits = CastUpwards(raycast, kind, mapId, new QueryFilter { LayerBits = 1 });

        Assert.That(hits.Results.Select(hit => hit.Entity).ToArray(), Is.EqualTo(new[] { target }));
    }

    /// <summary>
    /// A closer hit found on a later broadphase replaces the closest hit so far, and still has to be returned in map
    /// coordinates.
    /// </summary>
    [Test]
    public void ClosestHitFromLaterBroadphaseIsInMapCoordinates()
    {
        var sim = RobustServerSimulation.NewSimulation().RegisterEntitySystems(f =>
        {
            f.LoadExtraSystemType<RayCastSystem>();
        }).InitializeInstance();

        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var fixtureSystem = entManager.System<FixtureSystem>();
        var physicsSystem = entManager.System<SharedPhysicsSystem>();

        var mapUid = mapSystem.CreateMap(out var mapId);

        // The map's broadphase is checked first, so this hit is found first even though it's farther away.
        SpawnCastTarget(entManager, fixtureSystem, physicsSystem, mapUid, new Vector2(0.5f, 2.5f), true);

        // Offset the grid so its local coordinates differ from map coordinates.
        var grid = mapSystem.CreateGridEntity(mapId);
        entManager.System<SharedTransformSystem>().SetLocalPosition(grid.Owner, new Vector2(5f, 0f));
        mapSystem.SetTile(grid, new Vector2i(-5, 1), new Tile(1));
        var closer = SpawnCastTarget(entManager, fixtureSystem, physicsSystem, grid.Owner, new Vector2(-4.5f, 1.5f), true);

        var raycast = sim.System<RayCastSystem>();
        var hits = raycast.CastRayClosest(mapId, Vector2.UnitX / 2f, Vector2.UnitY * 3f, new QueryFilter { LayerBits = 1 });

        Assert.That(hits.Results.Select(hit => hit.Entity).ToArray(), Is.EqualTo(new[] { closer }));
        // Where the ray enters the closer target's circle, which has a radius of 0.25.
        Assert.That(hits.Results[0].Point.X, Is.EqualTo(0.5f).Within(0.001f));
        Assert.That(hits.Results[0].Point.Y, Is.EqualTo(1.25f).Within(0.001f));
    }

    /// <summary>
    /// Casts a ray, or a small circle for <see cref="SensorCastKind.Shape"/>, from (0.5, 0) three units up.
    /// </summary>
    private static RayResult CastUpwards(RayCastSystem raycast, SensorCastKind kind, MapId mapId, QueryFilter filter)
    {
        return kind switch
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
    }

    private void Setup(ISimulation sim, out MapId mapId)
    {
        var entManager = sim.Resolve<IEntityManager>();
        var mapSystem = entManager.System<SharedMapSystem>();

        mapSystem.CreateMap(out mapId);

        var grid = mapSystem.CreateGridEntity(mapId);

        for (var i = 0; i < 3; i++)
        {
            mapSystem.SetTile(grid, new Vector2i(i, 0), new Tile(1));
        }

        // Spawn a wall in the middle tile.
        var wall = entManager.SpawnAttachedTo(null, new EntityCoordinates(grid.Owner, new Vector2(1.5f, 0.5f)));

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
        var uid = entManager.SpawnAttachedTo(null, new EntityCoordinates(parent, position));
        var physics = entManager.AddComponent<PhysicsComponent>(uid);
        fixtureSystem.CreateFixture(uid, "fix1", new Fixture(new PhysShapeCircle(0.25f), 1, 1, hard));
        physicsSystem.SetCanCollide(uid, true, body: physics);
        Assert.That(physics.CanCollide);
        return uid;
    }
}
