using System.Collections.Generic;
using System.Numerics;
using System.Runtime.InteropServices;
using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.EntityManager;

[MemoryDiagnoser]
[Virtual]
public class TransformComponentQueryBenchmark
{
    private ISimulation _simulation = default!;
    private IEntityManager _entityManager = default!;

    [UsedImplicitly]
    [Params(1, 10, 100, 1000)]
    public int N = 10000;

    private List<EntityUid> Ents = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _simulation = RobustServerSimulation
            .NewSimulation()
            .InitializeInstance();

        _entityManager = _simulation.Resolve<IEntityManager>();

        Ents = [];
        for (var i = 0; i < N; i++)
        {
            Ents.Add(_entityManager.SpawnEntity(null, MapCoordinates.Nullspace));
        }
    }

    [Benchmark]
    public int AllEntityQueryEnumerator()
    {
        var a = 0;
        var query = _entityManager.AllEntityQueryEnumerator<TransformComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            a += comp.ChildCount;
        }

        return a;
    }

    [Benchmark]
    public int EntityQueryEnumerator()
    {
        var a = 0;
        var query = _entityManager.EntityQueryEnumerator<TransformComponent>();
        while (query.MoveNext(out _, out var comp))
        {
            a += comp.ChildCount;
        }

        return a;
    }

    [Benchmark]
    public int GetEntityQuery()
    {
        var a = 0;
        var query = _entityManager.GetEntityQuery<TransformComponent>();
        foreach (var uid in CollectionsMarshal.AsSpan(Ents))
        {
            a += query.GetComponent(uid).ChildCount;
        }

        return a;
    }

    [Benchmark]
    public int GetComponent()
    {
        var a = 0;
        foreach (var uid in CollectionsMarshal.AsSpan(Ents))
        {
            a += _entityManager.GetComponent<TransformComponent>(uid).ChildCount;
        }

        return a;
    }

    [Benchmark]
    public int HasComponent()
    {
        var a = 0;
        foreach (var uid in CollectionsMarshal.AsSpan(Ents))
        {
            if (_entityManager.HasComponent<TransformComponent>(uid))
                a += 1;
        }

        return a;
    }

    [Benchmark]
    public int TryGetComponent()
    {
        var a = 0;
        foreach (var uid in CollectionsMarshal.AsSpan(Ents))
        {
            if (_entityManager.TryGetComponent(uid, out TransformComponent? xform))
                a += xform.ChildCount;
        }

        return a;
    }
}
