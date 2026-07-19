using System;
using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Timing;
using Robust.UnitTesting.Server;

namespace Robust.Benchmarks.Timing;

public sealed class EntityTimerSystemBenchmark
{
    private static readonly TimeSpan FarDeadline = TimeSpan.FromHours(1);
    private readonly EntityTimerId[] _readyIds = new EntityTimerId[10];
    private EntityTimerSystem _timers = default!;
    private Entity<BenchmarkTimerComponent> _owner;

    [UsedImplicitly]
    [Params(1_000, 100_000)]
    public int FarTimerCount;

    [GlobalSetup]
    public void Setup()
    {
        var simulation = RobustServerSimulation
            .NewSimulation()
            .RegisterComponents(factory => factory.RegisterClass<BenchmarkTimerComponent>())
            .RegisterEntitySystems(factory => factory.LoadExtraSystemType<BenchmarkTimerSystem>())
            .InitializeInstance();

        var entities = simulation.Resolve<IEntityManager>();
        _timers = simulation.System<EntityTimerSystem>();
        var uid = entities.SpawnEntity(null, MapCoordinates.Nullspace);
        _owner = new Entity<BenchmarkTimerComponent>(uid, entities.AddComponent<BenchmarkTimerComponent>(uid));

        for (var i = 0; i < FarTimerCount; i++)
        {
            _timers.SetTimerAt(
                _owner,
                new EntityTimerId($"far-{i}"),
                FarDeadline);
        }

        for (var i = 0; i < _readyIds.Length; i++)
        {
            _readyIds[i] = new EntityTimerId($"ready-{i}");
        }
    }

    [Benchmark(Baseline = true)]
    public void UpdateWithOnlyFarTimers()
    {
        _timers.UpdateTimers(noPredictions: false);
    }

    [Benchmark]
    public void ScheduleAndUpdateTenReadyTimers()
    {
        foreach (var id in _readyIds)
        {
            _timers.SetTimerAt(_owner, id, TimeSpan.Zero);
        }

        _timers.UpdateTimers(noPredictions: false);
    }
}

internal sealed class BenchmarkTimerSystem : EntitySystem
{
    public override void Initialize()
    {
        SubscribeLocalEvent<BenchmarkTimerComponent, EntityTimerEvent>(OnTimer);
    }

    private static void OnTimer(
        EntityUid uid,
        BenchmarkTimerComponent component,
        ref EntityTimerEvent args)
    {
    }
}

internal sealed partial class BenchmarkTimerComponent : Component;
