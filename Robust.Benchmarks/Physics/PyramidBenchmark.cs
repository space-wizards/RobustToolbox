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
public class PhysicsPyramidBenchmark
{
    private ISimulation _sim = default!;

    [GlobalSetup]
    public void Setup()
    {
        _sim = RobustServerSimulation.NewSimulation().InitializeInstance();

        var entManager = _sim.Resolve<IEntityManager>();
        entManager.System<SharedMapSystem>().CreateMap(out var mapId);
        SetupTumbler(entManager, mapId);

        for (var i = 0; i < 300; i++)
        {
            entManager.TickUpdate(0.016f, false);
        }
    }

    [Benchmark]
    public void Pyramid()
    {
        var entManager = _sim.Resolve<IEntityManager>();

        for (var i = 0; i < 5000; i++)
        {
            entManager.TickUpdate(0.016f, false);
        }
    }

    private void SetupTumbler(IEntityManager entManager, MapId mapId)
    {
        const byte count = 20;

        // Setup ground
        var physics = entManager.System<SharedPhysicsSystem>();
        var fixtures = entManager.System<FixtureSystem>();
        var groundUid = entManager.SpawnEntity(null, new MapCoordinates(0, 0, mapId));
        var ground = entManager.AddComponent<PhysicsComponent>(groundUid);

        var horizontal = new EdgeShape(new Vector2(40, 0), new Vector2(-40, 0));
        fixtures.CreateFixture(groundUid, "fix1", new Fixture(horizontal, 2, 2, true), body: ground);
        physics.WakeBody(groundUid, body: ground);

        // Setup boxes
        float a = 0.5f;
        PolygonShape shape = new();
        shape.SetAsBox(a, a);

        var x = new Vector2(-7.0f, 0.75f);
        Vector2 y;
        Vector2 deltaX = new Vector2(0.5625f, 1.25f);
        Vector2 deltaY = new Vector2(1.125f, 0.0f);

        for (var i = 0; i < count; ++i)
        {
            y = x;

            for (var j = i; j < count; ++j)
            {
                var boxUid = entManager.SpawnEntity(null, new MapCoordinates(y, mapId));
                var box = entManager.AddComponent<PhysicsComponent>(boxUid);
                physics.SetBodyType(boxUid, BodyType.Dynamic, body: box);

                fixtures.CreateFixture(boxUid, "fix1", new Fixture(shape, 2, 2, true, 5f), body: box);
                y += deltaY;

                physics.WakeBody(boxUid, body: box);
                physics.SetSleepingAllowed(boxUid, box, false);
            }

            x += deltaX;
        }
    }
}
