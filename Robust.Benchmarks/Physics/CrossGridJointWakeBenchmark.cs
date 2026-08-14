using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.Physics;

[Virtual, MemoryDiagnoser]
public class CrossGridJointWakeBenchmark
{
    private ISimulation _simulation = default!;
    private SharedJointSystem _joints = default!;
    private readonly HashSet<EntityUid> _movedGrids = new();

    [Params(100, 1000, 10000)]
    public int JointCount;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _simulation = RobustServerSimulation.NewSimulation().InitializeInstance();
        var entManager = _simulation.Resolve<IEntityManager>();
        var fixtures = entManager.System<FixtureSystem>();
        var mapSystem = entManager.System<SharedMapSystem>();
        var physics = entManager.System<SharedPhysicsSystem>();
        _joints = entManager.System<SharedJointSystem>();

        var (map, mapId) = _simulation.CreateMap();
        var grid = mapSystem.CreateGridEntity(mapId);
        mapSystem.SetTile(grid, Vector2i.Zero, new Tile(1));
        _movedGrids.Add(grid.Owner);

        for (var i = 0; i < JointCount; i++)
        {
            var coordinates = i == 0
                ? new EntityCoordinates(grid, i, 0)
                : new EntityCoordinates(map, i, 0);
            var bodyA = entManager.SpawnEntity(null, coordinates);
            var bodyB = entManager.SpawnEntity(null, new MapCoordinates(i, 1, mapId));
            entManager.AddComponent<PhysicsComponent>(bodyA);
            entManager.AddComponent<PhysicsComponent>(bodyB);
            physics.SetBodyType(bodyA, BodyType.Dynamic);
            physics.SetBodyType(bodyB, BodyType.Dynamic);

            if (i == 0)
            {
                fixtures.TryCreateFixture(bodyA, new PhysShapeCircle(0.1f), "fixture");
                fixtures.TryCreateFixture(bodyB, new PhysShapeCircle(0.1f), "fixture");
            }

            _joints.CreateDistanceJoint(bodyA, bodyB);
        }

        _joints.WakeCrossGridJoints(_movedGrids);
    }

    [Benchmark]
    public void WakeCrossGridJoints()
    {
        _joints.WakeCrossGridJoints(_movedGrids);
    }
}
