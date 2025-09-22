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

[Virtual]
public class PhysicsTumblerBenchmark
{
    private ISimulation _sim = default!;

    [GlobalSetup]
    public void Setup()
    {
        _sim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = _sim.Resolve<IEntityManager>();
        var physics = entManager.System<SharedPhysicsSystem>();
        var fixtures = entManager.System<FixtureSystem>();
        var mapUid = entManager.System<SharedMapSystem>().CreateMap(out var mapId);
        SetupTumbler(entManager, mapId);

        for (var i = 0; i < 300; i++)
        {
            entManager.TickUpdate(0.016f, false);
            var boxUid = entManager.SpawnEntity(null, new MapCoordinates(0f, 10f, mapId));
            var box = entManager.AddComponent<PhysicsComponent>(boxUid);
            physics.SetBodyType(boxUid, BodyType.Dynamic, body: box);
            physics.SetFixedRotation(boxUid, false, body: box);
            var shape = new PolygonShape();
            shape.SetAsBox(0.125f, 0.125f);
            fixtures.CreateFixture(boxUid, "fix1", new Fixture(shape, 2, 2, true, 0.0625f), body: box);
            physics.WakeBody(boxUid, body: box);
            physics.SetSleepingAllowed(boxUid, box, false);
        }

        if (entManager.TryGetComponent(mapUid, out BroadphaseComponent? mapBroadphase))
            entManager.System<SharedBroadphaseSystem>().RebuildBottomUp(mapBroadphase);
    }

    [Benchmark]
    public void Tumbler()
    {
        var entManager = _sim.Resolve<IEntityManager>();

        for (var i = 0; i < 1000; i++)
        {
            entManager.TickUpdate(0.016f, false);
        }
    }

    private void SetupTumbler(IEntityManager entManager, MapId mapId)
    {
        var physics = entManager.System<SharedPhysicsSystem>();
        var fixtures = entManager.System<FixtureSystem>();
        var joints = entManager.System<SharedJointSystem>();

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
        fixtures.CreateFixture(bodyUid, "fix1", new Fixture(shape1, 2, 0, true, 20f));

        var shape2 = new PolygonShape();
        shape2.SetAsBox(0.5f, 10.0f, new Vector2(-10.0f, 0.0f), 0f);
        fixtures.CreateFixture(bodyUid, "fix2", new Fixture(shape2, 2, 0, true, 20f));

        var shape3 = new PolygonShape();
        shape3.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, 10.0f), 0f);
        fixtures.CreateFixture(bodyUid, "fix3", new Fixture(shape3, 2, 0, true, 20f));

        var shape4 = new PolygonShape();
        shape4.SetAsBox(10.0f, 0.5f, new Vector2(0.0f, -10.0f), 0f);
        fixtures.CreateFixture(bodyUid, "fix4", new Fixture(shape4, 2, 0, true, 20f));

        physics.WakeBody(groundUid, body: ground);
        physics.WakeBody(bodyUid, body: body);
        var revolute = joints.CreateRevoluteJoint(groundUid, bodyUid);
        revolute.LocalAnchorA = new Vector2(0f, 10f);
        revolute.LocalAnchorB = new Vector2(0f, 0f);
        revolute.ReferenceAngle = 0f;
        revolute.MotorSpeed = 0.05f * MathF.PI;
        revolute.MaxMotorTorque = 100000000f;
        revolute.EnableMotor = true;
    }
}
