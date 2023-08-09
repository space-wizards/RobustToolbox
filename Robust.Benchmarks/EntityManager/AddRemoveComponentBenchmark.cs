using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.EntityManager;

[Virtual]
public class AddRemoveComponentBenchmark
{
    private ISimulation _simulation = default!;
    private IEntityManager _entityManager = default!;

    [UsedImplicitly]
    [Params(1, 10, 100, 1000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _simulation = RobustServerSimulation
            .NewSimulation()
            .RegisterComponents(f => f.RegisterClass<A>())
            .InitializeInstance();

        _entityManager = _simulation.Resolve<IEntityManager>();

        var coords = new MapCoordinates(0, 0, new MapId(1));
        _simulation.AddMap(coords.MapId);

        for (var i = 0; i < N; i++)
        {
            _entityManager.SpawnEntity(null, coords);
        }
    }

    [Benchmark]
    public void AddRemoveComponent()
    {
        for (var i = 2; i <= N+1; i++)
        {
            var uid = new EntityUid(i);
            _entityManager.AddComponent<A>(uid);
            _entityManager.RemoveComponent<A>(uid);
        }
    }

    [ComponentProtoName("A")]
    public sealed class A : Component
    {
    }
}
