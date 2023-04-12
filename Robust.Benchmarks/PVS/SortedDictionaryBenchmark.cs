using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.GameObjects;

namespace Robust.Benchmarks.PVS;

/// <summary>
/// Compares Dictionary vs SortedDictionary
/// </summary>
[Virtual]
public class SortedDictionaryBenchmark
{
    [Params(32, 128, 256, 512)]
    public int N { get; set; }

    private Dictionary<EntityUid, int> _dict = new();
    private SortedDictionary<EntityUid, int> _sortedDict = new();

    [IterationSetup]
    public void Setup()
    {
        _dict = new Dictionary<EntityUid, int>();
        _sortedDict = new SortedDictionary<EntityUid, int>();
    }

    [Benchmark]
    public void Add()
    {
        for (var i = 0; i < N; i++)
        {
            var uid = new EntityUid(i);
            _dict.Add(uid, i);
        }
    }

    [Benchmark]
    public void SortedAdd()
    {
        for (var i = 0; i < N; i++)
        {
            var uid = new EntityUid(i);
            _sortedDict.Add(uid, i);
        }
    }

    [Benchmark]
    public void AddAndClear()
    {
        for (var i = 0; i < N; i++)
        {
            var uid = new EntityUid(i);
            _dict.Add(uid, i);
        }

        _dict.Clear();
    }

    [Benchmark]
    public void SortedAddAndClear()
    {
        for (var i = 0; i < N; i++)
        {
            var uid = new EntityUid(i);
            _sortedDict.Add(uid, i);
        }

        _sortedDict.Clear();
    }
}
