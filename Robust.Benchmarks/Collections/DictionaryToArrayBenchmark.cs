using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Utility;

namespace Robust.Benchmarks.Collections;

[MemoryDiagnoser]
public class DictionaryToArrayBenchmark
{
    private Dictionary<int, int> _dictionary = default!;

    [Params(0, 1, 8, 64, 1024)]
    public int Count { get; set; }

    [GlobalSetup]
    public void Setup()
    {
        _dictionary = new Dictionary<int, int>(Count);

        for (var i = 0; i < Count; i++)
        {
            _dictionary.Add(i, i);
        }
    }

    [Benchmark(Baseline = true)]
    public KeyValuePair<int, int>[] DotNetEnumerableToArray()
    {
        return Enumerable.ToArray(_dictionary);
    }

    [Benchmark]
    public KeyValuePair<int, int>[] RobustDictionaryToArray()
    {
        return Extensions.ToArray(_dictionary);
    }
}
