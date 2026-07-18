using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using System.Numerics;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.Physics;

[Virtual, MemoryDiagnoser]
public class BroadphaseDetachBenchmark
{
    private ISimulation _simulation = default!;
    private IEntityManager _entManager = default!;
    private EntityLookupSystem _lookup = default!;
    private SharedMapSystem _map = default!;

    private EntityUid _root;
    private TransformComponent _rootXform = default!;
    private EntityUid[] _children = default!;

    [UsedImplicitly]
    [Params(0, 20, 50, 100)]
    public int Children;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _simulation = RobustServerSimulation.NewSimulation().InitializeInstance();
        _entManager = _simulation.Resolve<IEntityManager>();
        var systems = _entManager.EntitySysManager;
        _lookup = systems.GetEntitySystem<EntityLookupSystem>();
        _map = systems.GetEntitySystem<SharedMapSystem>();

        var (mapUid, mapId) = _simulation.CreateMap();
        var grid = _map.CreateGridEntity(mapId);
        _map.SetTile(grid, Vector2i.Zero, new Tile(1));

        _root = _entManager.SpawnEntity(null, new EntityCoordinates(grid.Owner, new Vector2(0.5f, 0.5f)));
        _rootXform = _entManager.GetComponent<TransformComponent>(_root);
        _children = new EntityUid[Children];

        var parent = _root;
        for (var i = 0; i < Children; i++)
        {
            var child = _entManager.SpawnEntity(null, new EntityCoordinates(parent, new Vector2(0.001f, 0f)));
            _children[i] = child;
            parent = child;
        }
    }

    [IterationSetup]
    public void IterationSetup()
    {
        _lookup.FindAndAddToEntityTree(_root, true, _rootXform);
    }

    [Benchmark(Baseline = true)]
    public void RemoveEveryEntityRecursive()
    {
        for (var i = _children.Length - 1; i >= 0; i--)
        {
            var child = _children[i];
            _lookup.RemoveFromEntityTree(child, _entManager.GetComponent<TransformComponent>(child));
        }

        _lookup.RemoveFromEntityTree(_root, _rootXform);
    }

    [Benchmark]
    public void RemoveRootOnly()
    {
        _lookup.RemoveFromEntityTree(_root, _rootXform);
    }
}
