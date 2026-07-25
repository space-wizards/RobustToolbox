using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Threading;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.EntityManager;

public partial class ComponentIteratorBenchmark
{
    private ISimulation _simulation = default!;
    private Shared.GameObjects.EntityManager _entityManager = default!;
    private IParallelManager _parallelManager = default!;

    [UsedImplicitly]
    [Params(1, 10, 100, 1000, 10000)]
    public int N;

    public A[] Comps = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _simulation = RobustServerSimulation
            .NewSimulation()
            .RegisterDependencies(c => c.Register<IParallelManager, ParallelManager>(overwrite: true))
            .RegisterComponents(f => f.RegisterClass<A>())
            .InitializeInstance();

        _entityManager = (Shared.GameObjects.EntityManager)_simulation.Resolve<IEntityManager>();
        _parallelManager = _simulation.Resolve<IParallelManager>();

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
    [BenchmarkCategory("Component Manipulation")]
    public void ComponentManipulationStruct()
    {
        var query = _entityManager.EntityQueryEnumerator<A>();

        while (query.MoveNext(out var comp))
        {
            comp.Foo = 0xB00BA;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Component Manipulation")]
    public void ComponentManipulationQuery()
    {
        foreach (var comp in _entityManager.EntityQuery<A>())
        {
            comp.Foo = 0xB00BA;
        }
    }

    [Benchmark]
    [BenchmarkCategory("Component Manipulation")]
    public void ComponentManipulationParallelJob()
    {
        var query = _entityManager.EntityQueryEnumerator<A>();
        var list = new List<A>(_entityManager.Count<A>());

        while (query.MoveNext(out var comp))
        {
            list.Add(comp);
        }

        var job = new SetNumberParallelJob(list);
        _parallelManager.ProcessNow(job, list.Count);
    }

    [Benchmark]
    [BenchmarkCategory("Component Manipulation")]
    public void ComponentManipulationParallelEnumEntJob()
    {
        _parallelManager.ProcessNow(new SetNumberEnumEntJob(_entityManager));
    }

    private struct SetNumberEnumEntJob(Shared.GameObjects.EntityManager entityManager) : IParallelEnumerateEntitiesRobustJob<A>
    {
        public int MinimumBatchParallel => 1;
        public int BatchSize => 50;

        public Shared.GameObjects.EntityManager EntityManager { get; set; } = entityManager;

        public void Execute(EntityUid uid, A component)
        {
            component.Foo = 0xB00BA;
        }
    }

    private struct SetNumberParallelJob(List<A> Components) : IParallelRobustJob
    {
        public int MinimumBatchParallel => 1;
        public int BatchSize => 50;

        public void Execute(int index)
        {
            Components[index].Foo = 0xB00BA;
        }
    }

    [ComponentProtoName("A")]
    public sealed partial class A : Component
    {
        public int Foo = 0;
    }
}
