using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using static Robust.Benchmarks.EntityManager.ArchetypeComponentAccessBenchmark;

namespace Robust.Benchmarks.EntityManager;

[MemoryDiagnoser]
[Virtual]
public class ArrayAccessBenchmark
{
    [Params(new[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9})]
    public int[] Array = default!;

    [Params(5)]
    public int Element;

    [Params(500)]
    public int Handle;

    public Archetype<int, int, int, int, int, int, int, int, int, int> Archetype = default!;

    [GlobalSetup]
    public void GlobalSetup()
    {
        Archetype = new Archetype<int, int, int, int, int, int, int, int, int, int>(1000);
    }

    [Benchmark]
    public int? GetArrayElement()
    {
        return Consume();
    }

    [Benchmark]
    public int? GetExisting()
    {
        return Consume(Element);
    }

    [Benchmark]
    public int? GetArchetype()
    {
        return Consume(Archetype.GetComponentUnsafeHandle<int>(Handle));
    }

    private int? Consume(int? value = null)
    {
        if (value == null)
            value = Array[5];

        return value;
    }
}
