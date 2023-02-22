using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;

namespace Robust.Benchmarks.Arrays;

[MemoryDiagnoser]
[Virtual]
public class ArrayAccessBenchmark
{
    [Params(new[] {0, 1, 2, 3, 4, 5, 6, 7, 8, 9})]
    public int[] Array = default!;

    [Params(5)]
    public int Element;

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

    private int? Consume(int? value = null)
    {
        if (value == null)
            value = Array[5];

        return value;
    }
}
