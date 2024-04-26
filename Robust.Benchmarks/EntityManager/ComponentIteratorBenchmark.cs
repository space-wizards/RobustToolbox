using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.EntityManager;

public partial class ComponentIteratorBenchmark
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

        var map = _simulation.CreateMap().MapId;
        var coords = new MapCoordinates(default, map);

        for (var i = 0; i < N; i++)
        {
            var uid = _entityManager.SpawnEntity(null, coords);
            _entityManager.AddComponent<A>(uid);
        }
    }

    [Benchmark]
    public A[] ComponentStructEnumerator()
    {
        var query = _entityManager.EntityQueryEnumerator<A>();
        var i = 0;

        while (query.MoveNext(out var comp))
        {
            Comps[i] = comp;
            i++;
        }

        return Comps;
    }

    [Benchmark]
    public A[] ComponentIEnumerable()
    {
        var i = 0;

        foreach (var comp in _entityManager.EntityQuery<A>())
        {
            Comps[i] = comp;
            i++;
        }

        return Comps;
    }

    [ComponentProtoName("A")]
    public sealed partial class A : Component
    {
    }
}
