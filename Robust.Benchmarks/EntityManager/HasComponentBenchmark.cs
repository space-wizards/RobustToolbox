using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.EntityManager;

[Virtual]
public partial class HasComponentBenchmark
{
    private static readonly Consumer Consumer = new();

    private ISimulation _simulation = default!;
    private IEntityManager _entityManager = default!;

    private ComponentRegistration _compReg = default!;

    private A _dummyA = new();

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
        var map = _simulation.CreateMap().Uid;
        var coords = new EntityCoordinates(map, default);
        _compReg = _entityManager.ComponentFactory.GetRegistration(typeof(A));

        for (var i = 0; i < N; i++)
        {
            var uid = _entityManager.SpawnEntity(null, coords);
            _entityManager.AddComponent<A>(uid);
        }
    }

    [Benchmark]
    public void HasComponentGeneric()
    {
        for (var i = 2; i <= N+1; i++)
        {
            var uid = new EntityUid(i);
            var result = _entityManager.HasComponent<A>(uid);
            Consumer.Consume(result);
        }
    }

    [Benchmark]
    public void HasComponentCompReg()
    {
        for (var i = 2; i <= N+1; i++)
        {
            var uid = new EntityUid(i);
            var result = _entityManager.HasComponent(uid, _compReg);
            Consumer.Consume(result);
        }
    }

    [Benchmark]
    public void HasComponentType()
    {
        for (var i = 2; i <= N+1; i++)
        {
            var uid = new EntityUid(i);
            var result = _entityManager.HasComponent(uid, typeof(A));
            Consumer.Consume(result);
        }
    }

    [Benchmark]
    public void HasComponentGetType()
    {
        for (var i = 2; i <= N+1; i++)
        {
            var uid = new EntityUid(i);
            var type = _dummyA.GetType();
            var result = _entityManager.HasComponent(uid, type);
            Consumer.Consume(result);
        }
    }

    [ComponentProtoName("A")]
    public sealed partial class A : Component
    {
    }
}
