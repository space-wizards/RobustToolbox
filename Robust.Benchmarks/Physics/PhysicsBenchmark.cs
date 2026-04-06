using System;
using System.Numerics;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.Physics;

[Virtual, MediumRunJob]
public class PhysicsBenchmark
{
    // TODO: Rain
    // Large pyramid
    // Joint Grid
    // Spinner
    // Washer

    const float frameTime = 0.016f;

    #region Many Pyramids

    private ISimulation _manyPyramidSim = default!;

    [GlobalSetup(Target = nameof(ManyPyramids))]
    public void PyramidSetup()
    {
        _manyPyramidSim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = _manyPyramidSim.Resolve<IEntityManager>();
        entManager.System<SharedMapSystem>().CreateMap(out var mapId);
        SetupManyPyramids(entManager, mapId);
    }

    [Benchmark]
    public void ManyPyramids()
    {
        var entManager = _manyPyramidSim.Resolve<IEntityManager>();

        for (var i = 0; i < (1f / frameTime) * 10; i++)
        {
            entManager.TickUpdate(frameTime, false);
        }
    }

    private void SetupManyPyramids(IEntityManager entManager, MapId mapId)
    {
        int baseCount = 10;
        float extent = 0.5f;
        int rowCount = 20; // 5
        int columnCount = 5;

        // Setup ground
        var physics = entManager.System<SharedPhysicsSystem>();
        var fixtures = entManager.System<FixtureSystem>();
        physics.SetGravity(new Vector2(0f, -9.8f));

        // Setup boxes
        float a = 0.5f;
        PolygonShape shape = new();
        shape.SetAsBox(a, a);

        float groundDeltaY = 2.0f * extent * ( baseCount + 1.0f );
        float groundWidth = 2.0f * extent * columnCount * ( baseCount + 1.0f );

        float groundY = 0.0f;

        for ( int i = 0; i < rowCount; ++i )
        {
            var groundUid = entManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
            var ground = entManager.AddComponent<PhysicsComponent>(groundUid);

            var horizontal = new EdgeShape(new Vector2(-0.5f * 2.0f * groundWidth, groundY), new Vector2(0.5f * 2.0f * groundWidth, groundY));
            fixtures.CreateFixture(groundUid, "fix1", new Fixture(horizontal, 2, 2, true), body: ground);
            physics.WakeBody(groundUid, body: ground);
            groundY += groundDeltaY;
        }

        float baseWidth = 2.0f * extent * baseCount;
        float baseY = 0.0f;

        for ( int i = 0; i < rowCount; ++i )
        {
            for ( int j = 0; j < columnCount; ++j )
            {
                float centerX = -0.5f * groundWidth + j * ( baseWidth + 2.0f * extent ) + extent;
                CreateSmallPyramid(entManager, mapId, baseCount, extent, centerX, baseY);
            }

            baseY += groundDeltaY;
        }
    }

    private void CreateSmallPyramid(IEntityManager entManager, MapId mapId, int baseCount, float extent, float centerX, float baseY)
    {
        var physics = entManager.System<SharedPhysicsSystem>();
        var fixtures = entManager.System<FixtureSystem>();
        var shape = new PolygonShape();
        shape.SetAsBox(extent, extent);

        for ( int i = 0; i < baseCount; ++i )
        {
            float y = ( 2.0f * i + 1.0f ) * extent + baseY;

            for ( int j = i; j < baseCount; ++j )
            {
                float x = ( i + 1.0f ) * extent + 2.0f * ( j - i ) * extent + centerX - 0.5f;

                var boxUid = entManager.SpawnEntity(null, new MapCoordinates(new Vector2(x, y), mapId));
                var box = entManager.AddComponent<PhysicsComponent>(boxUid);
                physics.SetBodyType(boxUid, BodyType.Dynamic, body: box);

                fixtures.CreateFixture(boxUid, "fix1", new Fixture(shape, 2, 2, true, 5f), body: box);
                physics.WakeBody(boxUid);
            }
        }
    }

    #endregion

    #region Smash

    private ISimulation _smashSim = default!;

    [GlobalSetup(Target = nameof(Smash))]
    public void SmashSetup()
    {
        _smashSim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = _smashSim.Resolve<IEntityManager>();

        entManager.System<SharedMapSystem>().CreateMap(out var mapId);

        var physics = entManager.System<SharedPhysicsSystem>();
        var fixtures = entManager.System<FixtureSystem>();
        var joints = entManager.System<SharedJointSystem>();
        var xformSystem = entManager.System<SharedTransformSystem>();
        physics.SetGravity(new Vector2(0f, -9.8f));

        {
            var smashBox = new PolygonShape();
            smashBox.SetAsBox(4f, 4f);

            var bodyUid = entManager.SpawnEntity(null, new MapCoordinates(0f, 10f, mapId));
            var body = entManager.AddComponent<PhysicsComponent>(bodyUid);

            physics.SetBodyType(bodyUid, BodyType.Dynamic, body: body);
            physics.SetSleepingAllowed(bodyUid, body, false);
            physics.SetFixedRotation(bodyUid, false, body: body);
            xformSystem.SetLocalPosition(bodyUid, new Vector2(-20f, 0f));
            physics.SetLinearVelocity(bodyUid, new Vector2(40f, 0f));

            fixtures.TryCreateFixture(bodyUid, smashBox, "fix1", density: 8f, hard: true);
        }

        float d = 0.4f;
        var box = new PolygonShape();
        box.SetAsBox(0.5f * d, 0.5f * d);

        int columns = 120; // 20
        int rows = 80; // 10

        for ( int i = 0; i < columns; ++i )
        {
            for ( int j = 0; j < rows; ++j )
            {
                var bodyUid = entManager.SpawnEntity(null, new MapCoordinates(i * d + 30f, ( j - rows / 2.0f ) * d, mapId));
                var body = entManager.AddComponent<PhysicsComponent>(bodyUid);

                physics.SetBodyType(bodyUid, BodyType.Dynamic, body: body);
                physics.SetSleepingAllowed(bodyUid, body, false);
                physics.SetFixedRotation(bodyUid, false, body: body);
                xformSystem.SetLocalPosition(bodyUid, new Vector2(-20f, 0f));
                physics.SetLinearVelocity(bodyUid, new Vector2(40f, 0f));

                fixtures.TryCreateFixture(bodyUid, box, "fix1",  hard: true);
                physics.WakeBody(bodyUid);
            }
        }
    }

    [Benchmark]
    public void Smash()
    {
        var entManager = _smashSim.Resolve<IEntityManager>();

        for (var i = 0; i < (1f / frameTime) * 10; i++)
        {
            entManager.TickUpdate(frameTime, false);
        }
    }

    #endregion

    #region Tumbler

    private ISimulation _tumblerSim = default!;

    [GlobalSetup(Target = nameof(Tumbler))]
    public void TumblerSetup()
    {
        _tumblerSim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = _tumblerSim.Resolve<IEntityManager>();

        entManager.System<SharedMapSystem>().CreateMap(out var mapId);
        SetupTumbler(entManager, mapId);
    }

    [Benchmark]
    public void Tumbler()
    {
        var entManager = _tumblerSim.Resolve<IEntityManager>();

        for (var i = 0; i < 1 / frameTime * 10; i++)
        {
            entManager.TickUpdate(frameTime, false);
        }
    }

    private void SetupTumbler(IEntityManager entManager, MapId mapId)
    {
        var physics = entManager.System<SharedPhysicsSystem>();
        var fixtures = entManager.System<FixtureSystem>();
        var joints = entManager.System<SharedJointSystem>();
        physics.SetGravity(new Vector2(0f, -9.8f));

        {
            var groundUid = entManager.SpawnEntity(null, new MapCoordinates(0f, 0f, mapId));
            var ground = entManager.AddComponent<PhysicsComponent>(groundUid);
            // Due to lookup changes fixtureless bodies are invalid, so
            var cShape = new PhysShapeCircle(1f);
            fixtures.CreateFixture(groundUid, "fix1", new Fixture(cShape, 0, 0, false));

            var bodyUid = entManager.SpawnEntity(null, new MapCoordinates(0f, 10f, mapId));
            var body = entManager.AddComponent<PhysicsComponent>(bodyUid);

            physics.SetBodyType(bodyUid, BodyType.Dynamic, body: body);
            physics.SetSleepingAllowed(bodyUid, body, false);
            physics.SetFixedRotation(bodyUid, false, body: body);


            // TODO: Box2D just deref, bleh shape structs someday
            var shape1 = new PolygonShape();
            shape1.SetAsBox(0.5f, 10.0f, new Vector2(10.0f, 0.0f), 0.0f);
            fixtures.CreateFixture(bodyUid, "fix1", new Fixture(shape1, 2, 0, true, 50f));

            var shape2 = new PolygonShape();
            shape2.SetAsBox(0.5f, 10.0f, new Vector2(-10.0f, 0.0f), 0f);
            fixtures.CreateFixture(bodyUid, "fix2", new Fixture(shape2, 2, 0, true, 50f));

            var shape3 = new PolygonShape();
            shape3.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, 10.0f), 0f);
            fixtures.CreateFixture(bodyUid, "fix3", new Fixture(shape3, 2, 0, true, 50f));

            var shape4 = new PolygonShape();
            shape4.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, -10.0f), 0f);
            fixtures.CreateFixture(bodyUid, "fix4", new Fixture(shape4, 2, 0, true, 50f));

            physics.WakeBody(groundUid, body: ground);
            physics.WakeBody(bodyUid, body: body);
            var revolute = joints.CreateRevoluteJoint(groundUid, bodyUid);

            var motorSpeed = 25f;

            revolute.LocalAnchorA = new Vector2(0f, 10f);
            revolute.LocalAnchorB = new Vector2(0f, 0f);
            revolute.ReferenceAngle = 0f;
            revolute.MotorSpeed = MathF.PI / 180f * motorSpeed;
            revolute.MaxMotorTorque = 100000000f;
            revolute.EnableMotor = true;
        }

        // Make boxes
        {
            var gridCount = 20; // 45
            var y = -0.2f * gridCount + 10f;

            var a = 0.125f;
            PolygonShape shape = new();
            shape.SetAsBox(a, a);

            for (var i = 0; i < gridCount; i++)
            {
                var x = -0.2f * gridCount;

                for (var j = 0; j < gridCount; j++)
                {
                    var boxUid = entManager.SpawnEntity(null, new MapCoordinates(new Vector2(x, y), mapId));
                    var body = entManager.AddComponent<PhysicsComponent>(boxUid);
                    physics.SetBodyType(boxUid, BodyType.Dynamic, body: body);

                    fixtures.CreateFixture(boxUid, "fix1", new Fixture(shape, 2, 2, true, 5f), body: body);
                    x += 0.4f;

                    physics.WakeBody(boxUid, body: body);
                    physics.SetSleepingAllowed(boxUid, body, false);
                }

                y += 0.4f;
            }
        }
    }

    #endregion
}
