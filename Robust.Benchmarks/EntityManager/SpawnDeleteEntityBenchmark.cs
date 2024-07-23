using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.EntityManager;

[Virtual]
public partial class SpawnDeleteEntityBenchmark
{
    private ISimulation _simulation = default!;
    private IEntityManager _entityManager = default!;

    private MapCoordinates _mapCoords = MapCoordinates.Nullspace;
    private EntityCoordinates _entCoords = EntityCoordinates.Invalid;

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
        var (map, mapId) = _simulation.CreateMap();
        _mapCoords = new MapCoordinates(default, mapId);
        _entCoords = new EntityCoordinates(map, 0, 0);
    }

    [Benchmark(Baseline = true)]
    public void SpawnDeleteEntityMapCoords()
    {
        for (var i = 0; i < N; i++)
        {
            var uid = _entityManager.SpawnEntity(null, _mapCoords);
            _entityManager.DeleteEntity(uid);
        }
    }

    [Benchmark]
    public void SpawnDeleteEntityEntCoords()
    {
        for (var i = 0; i < N; i++)
        {
            var uid = _entityManager.SpawnEntity(null, _entCoords);
            _entityManager.DeleteEntity(uid);
        }
    }

    [ComponentProtoName("A")]
    public sealed partial class A : Component
    {
    }
}
