using System.Numerics;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Components;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Physics.Systems;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.Physics;

[Virtual, MemoryDiagnoser]
public class BroadphaseReinsertBenchmark
{
    private IEntityManager _entManager = default!;
    private EntityLookupSystem _lookup = default!;
    private EntityUid _root;
    private TransformComponent _rootXform = default!;

    [Params(10, 100, 500)]
    public int Children;

    [GlobalSetup]
    public void GlobalSetup()
    {
        var sim = RobustServerSimulation.NewSimulation().InitializeInstance();
        _entManager = sim.Resolve<IEntityManager>();
        _lookup = _entManager.System<EntityLookupSystem>();
        var xforms = _entManager.System<SharedTransformSystem>();
        var physics = _entManager.System<SharedPhysicsSystem>();
        var fixtures = _entManager.System<FixtureSystem>();
        var mapSys = _entManager.System<SharedMapSystem>();

        var (map, mapId) = sim.CreateMap();
        var grid = mapSys.CreateGridEntity(mapId);
        var gridUid = grid.Owner;
        mapSys.SetTile(grid, Vector2i.Zero, new Tile(1));
        xforms.SetCoordinates(gridUid, new EntityCoordinates(map, new Vector2(10f, 10f)));
        xforms.SetLocalRotation(gridUid, Angle.FromDegrees(35));

        _root = _entManager.SpawnEntity(null, new EntityCoordinates(gridUid, new Vector2(0.5f, 0.5f)));
        _rootXform = _entManager.GetComponent<TransformComponent>(_root);

        var parent = _root;
        for (var i = 0; i < Children; i++)
        {
            var child = _entManager.SpawnEntity(null, new EntityCoordinates(parent, new Vector2(0.001f, 0f)));

            if (i % 4 == 0)
            {
                var xform = _entManager.GetComponent<TransformComponent>(child);
                var body = _entManager.AddComponent<PhysicsComponent>(child);
                var shape = new PolygonShape();
                shape.SetAsBox(0.01f, 0.01f);
                fixtures.CreateFixture(
                    child,
                    "fix1",
                    new Fixture(shape, 0, 0, false),
                    body: body,
                    xform: xform);
                physics.SetCanCollide(child, true, body: body);
                _lookup.FindAndAddToEntityTree(child, false, xform);
            }

            parent = child;
        }
    }

    [Benchmark]
    public void RemoveAndReinsertRecursive()
    {
        _lookup.RemoveFromEntityTree(_root, _rootXform);
        _lookup.FindAndAddToEntityTree(_root, true, _rootXform);
    }
}
