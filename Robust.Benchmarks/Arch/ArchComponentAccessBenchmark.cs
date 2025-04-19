using System;
using System.Runtime.CompilerServices;
using Arch.Core;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Engines;
using Robust.Shared.Analyzers;
using Schedulers;
using static Robust.Benchmarks.EntityManager.ArchetypeComponentAccessBenchmark;

namespace Robust.Benchmarks.Arch;

[MemoryDiagnoser]
[Virtual]
public class ArchComponentAccessBenchmark
{
    private const int N = 10000;

    private static readonly Consumer Consumer = new();
    private Entity _entity;
    private World _world = default!;
    private QueryDescription _singleQuery;
    private QueryDescription _tenQuery;

    private JobScheduler _scheduler = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _scheduler = new JobScheduler(new JobScheduler.Config()
        {
            ThreadPrefixName = "ArchBenchmark"
        });

        _world = World.Create();

        for (var i = 0; i < N; i++)
        {
            var entity = _world.Create();

            // Randomly chosen id
            if (entity.Id == 1584)
                _entity = entity;

            _world.Add(
                entity,
                new Struct1(),
                new Struct2(),
                new Struct3(),
                new Struct4(),
                new Struct5(),
                new Struct6(),
                new Struct7(),
                new Struct8(),
                new Struct9(),
                new Struct10()
            );
        }

        _singleQuery = new QueryDescription().WithAll<Struct1>();
        _tenQuery = new QueryDescription().WithAll<Struct1, Struct2, Struct3, Struct4, Struct5, Struct6, Struct7, Struct8, Struct9, Struct10>();
    }

    [GlobalCleanup]
    public void GlobalCleanup()
    {
        _scheduler.Dispose();
        Environment.Exit(0);
    }

    [Benchmark]
    public Struct1 GetSingle()
    {
        return _world.Get<Struct1>(_entity);
    }

    [Benchmark]
    public (Struct1, Struct2, Struct3, Struct4, Struct5, Struct6, Struct7, Struct8, Struct9, Struct10)
        GetTen()
    {
        return (
                _world.Get<Struct1>(_entity),
                _world.Get<Struct2>(_entity),
                _world.Get<Struct3>(_entity),
                _world.Get<Struct4>(_entity),
                _world.Get<Struct5>(_entity),
                _world.Get<Struct6>(_entity),
                _world.Get<Struct7>(_entity),
                _world.Get<Struct8>(_entity),
                _world.Get<Struct9>(_entity),
                _world.Get<Struct10>(_entity)
        );
    }

    [Benchmark]
    public bool HasSingle()
    {
        return _world.Has<Struct1>(_entity);
    }

    [Benchmark]
    public bool HasTen()
    {
        return _world.Has<Struct1>(_entity) &&
               _world.Has<Struct2>(_entity) &&
               _world.Has<Struct3>(_entity) &&
               _world.Has<Struct4>(_entity) &&
               _world.Has<Struct5>(_entity) &&
               _world.Has<Struct6>(_entity) &&
               _world.Has<Struct7>(_entity) &&
               _world.Has<Struct8>(_entity) &&
               _world.Has<Struct9>(_entity) &&
               _world.Has<Struct10>(_entity);
    }

    [Benchmark]
    public void IterateSingle()
    {
        _world.Query(_singleQuery, static (ref Struct1 s) => Consumer.Consume(s));
    }

    [Benchmark]
    public void IterateSingleInline()
    {
        _world.InlineQuery<QueryConsumer>(_singleQuery);
    }

    [Benchmark]
    public void IterateSingleParallel()
    {
        _world.ParallelQuery(_singleQuery, static (ref Struct1 s) => Consumer.Consume(s));
    }

    [Benchmark]
    public void IterateSingleInlineParallel()
    {
        _world.InlineParallelQuery<QueryConsumer>(_singleQuery);
    }

    [Benchmark]
    public void IterateTen()
    {
        _world.Query(_tenQuery,
            static (
                    ref Struct1 s1, ref Struct2 s2, ref Struct3 s3, ref Struct4 s4,
                    ref Struct5 s5, ref Struct6 s6, ref Struct7 s7, ref Struct8 s8,
                    ref Struct9 s9, ref Struct10 s10) =>
                Consumer.Consume((s1, s2, s3, s4, s5, s6, s7, s8, s9, s10)));
    }

    [Benchmark]
    public void IterateTenInline()
    {
        _world.InlineQuery<QueryConsumer>(_tenQuery);
    }

    [Benchmark]
    public void IterateTenParallel()
    {
        _world.ParallelQuery(_tenQuery,
            static (
                    ref Struct1 s1, ref Struct2 s2, ref Struct3 s3, ref Struct4 s4,
                    ref Struct5 s5, ref Struct6 s6, ref Struct7 s7, ref Struct8 s8,
                    ref Struct9 s9, ref Struct10 s10) =>
                Consumer.Consume((s1, s2, s3, s4, s5, s6, s7, s8, s9, s10)));
    }

    [Benchmark]
    public void IterateTenInlineParallel()
    {
        _world.InlineParallelQuery<QueryConsumer>(_tenQuery);
    }

    private struct QueryConsumer : IForEach
    {
        private static readonly Consumer Consumer = new();

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Update(Entity entity)
        {
            Consumer.Consume(entity);
        }
    }
}
