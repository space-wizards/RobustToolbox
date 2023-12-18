using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.Transform;

[Virtual]
public sealed class RecursiveMoveEventTypesBenchmark
{
    [Params(1, 100, 10000)]
    public int N { get; set; }

    private ISimulation _simulation = default!;
    private IEntityManager _entMan = default!;
    private SharedTransformSystem _transform = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _simulation = RobustServerSimulation
            .NewSimulation()
            .InitializeInstance();

        _entMan = _simulation.Resolve<IEntityManager>();
        _transform = _entMan.System<SharedTransformSystem>();
    }

    public void EventBus()
    {

    }
}
