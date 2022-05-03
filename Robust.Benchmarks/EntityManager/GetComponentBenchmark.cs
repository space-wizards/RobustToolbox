using System;
using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.EntityManager;

[Virtual]
public class GetComponentBenchmark
{
    private ISimulation _simulation = default!;
    private IEntityManager _entityManager = default!;

    [UsedImplicitly]
    [Params(1, 10, 100, 1000)]
    public int N;

    public A[] Comps = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _simulation = RobustServerSimulation
            .NewSimulation()
            .RegisterComponents(f => f.RegisterClass<A>())
            .InitializeInstance();

        _entityManager = _simulation.Resolve<IEntityManager>();

        Comps = new A[N+2];

        var coords = new MapCoordinates(0, 0, new MapId(1));
        _simulation.AddMap(coords.MapId);

        for (var i = 0; i < N; i++)
        {
            var uid = _entityManager.SpawnEntity(null, coords);
            _entityManager.AddComponent<A>(uid);
        }
    }

    [Benchmark]
    public A[] GetComponent()
    {
        for (var i = 2; i <= N+1; i++)
        {
            Comps[i] = _entityManager.GetComponent<A>(new EntityUid(i));
        }

        // Return something so the JIT doesn't optimize out all the GetComponent calls.
        return Comps;
    }

    [ComponentProtoName("A")]
    public sealed class A : Component
    {
    }
}
