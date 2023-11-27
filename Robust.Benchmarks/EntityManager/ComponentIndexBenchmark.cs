using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using BenchmarkDotNet.Attributes;
using Robust.Shared.GameObjects;

namespace Robust.Benchmarks.EntityManager;

public class ComponentIndexBenchmark
{
    // Just a bunch of types to bloat the test lists.

    private readonly CompIndexFetcher _compIndexFetcherDirect;
    private readonly IFetcher _compIndexFetcher;
    private readonly DictFetcher _dictFetcherDirect;
    private readonly IFetcher _dictFetcher;


    public ComponentIndexBenchmark()
    {
        _compIndexFetcherDirect = new CompIndexFetcher();
        _compIndexFetcher = _compIndexFetcherDirect;
        _dictFetcherDirect = new DictFetcher();
        _dictFetcher = _dictFetcherDirect;
    }

    [GlobalSetup]
    public void Setup()
    {
        var types = typeof(ComponentIndexBenchmark)
            .GetNestedTypes(BindingFlags.NonPublic)
            .Where(t => t.Name.StartsWith("TestType"))
            .ToArray();

        _compIndexFetcher.Init(types);
        _dictFetcher.Init(types);
    }

    [Benchmark]
    public int BenchCompIndex() => _compIndexFetcher.Get<TestType50>();

    [Benchmark]
    public int BenchDict() => _dictFetcher.Get<TestType50>();

    [Benchmark]
    public int BenchCompIndexDirect() => _compIndexFetcherDirect.Get<TestType50>();

    [Benchmark]
    public int BenchDictDirect() => _dictFetcherDirect.Get<TestType50>();

    private static CompIdx ArrayIndexFor<T>() => CompArrayIndex<T>.Idx;

    private static int _compIndexMaster = -1;

    private static class CompArrayIndex<T>
    {
        // ReSharper disable once StaticMemberInGenericType
        public static readonly CompIdx Idx = new(Interlocked.Increment(ref _compIndexMaster));
    }

    private static CompIdx GetCompIdIndex(Type type)
    {
        return (CompIdx)typeof(CompArrayIndex<>)
            .MakeGenericType(type)
            .GetField(nameof(CompArrayIndex<int>.Idx), BindingFlags.Static | BindingFlags.Public)!
            .GetValue(null)!;
    }

    private interface IFetcher
    {
        void Init(Type[] types);

        int Get<T>();
    }

    private sealed class CompIndexFetcher : IFetcher
    {
        private int[] _values = Array.Empty<int>();

        public void Init(Type[] types)
        {
            var max = types.Max(t => GetCompIdIndex(t).Value);

            _values = new int[max + 1];

            var i = 0;
            foreach (var type in types)
            {
                _values[GetCompIdIndex(type).Value] = i++;
            }
        }

        public int Get<T>()
        {
            return _values[CompArrayIndex<T>.Idx.Value];
        }
    }

    private sealed class DictFetcher : IFetcher
    {
        private readonly Dictionary<Type, int> _values = new();

        public void Init(Type[] types)
        {
            var i = 0;
            foreach (var type in types)
            {
                _values[type] = i++;
            }
        }

        public int Get<T>()
        {
            return _values[typeof(T)];
        }
    }

    // Just a bunch of types to pad the size of the arrays and such.

    // @formatter:off
    // ReSharper disable UnusedType.Local
    private sealed class TestType1{}
    private sealed class TestType2{}
    private sealed class TestType3{}
    private sealed class TestType4{}
    private sealed class TestType5{}
    private sealed class TestType6{}
    private sealed class TestType7{}
    private sealed class TestType8{}
    private sealed class TestType9{}
    private sealed class TestType10{}
    private sealed class TestType11{}
    private sealed class TestType12{}
    private sealed class TestType13{}
    private sealed class TestType14{}
    private sealed class TestType15{}
    private sealed class TestType16{}
    private sealed class TestType17{}
    private sealed class TestType18{}
    private sealed class TestType19{}
    private sealed class TestType20{}
    private sealed class TestType21{}
    private sealed class TestType22{}
    private sealed class TestType23{}
    private sealed class TestType24{}
    private sealed class TestType25{}
    private sealed class TestType26{}
    private sealed class TestType27{}
    private sealed class TestType28{}
    private sealed class TestType29{}
    private sealed class TestType30{}
    private sealed class TestType31{}
    private sealed class TestType32{}
    private sealed class TestType33{}
    private sealed class TestType34{}
    private sealed class TestType35{}
    private sealed class TestType36{}
    private sealed class TestType37{}
    private sealed class TestType38{}
    private sealed class TestType39{}
    private sealed class TestType40{}
    private sealed class TestType41{}
    private sealed class TestType42{}
    private sealed class TestType43{}
    private sealed class TestType44{}
    private sealed class TestType45{}
    private sealed class TestType46{}
    private sealed class TestType47{}
    private sealed class TestType48{}
    private sealed class TestType49{}
    private sealed class TestType50{}
    private sealed class TestType51{}
    private sealed class TestType52{}
    private sealed class TestType53{}
    private sealed class TestType54{}
    private sealed class TestType55{}
    private sealed class TestType56{}
    private sealed class TestType57{}
    private sealed class TestType58{}
    private sealed class TestType59{}
    private sealed class TestType60{}
    private sealed class TestType61{}
    private sealed class TestType62{}
    private sealed class TestType63{}
    private sealed class TestType64{}
    private sealed class TestType65{}
    private sealed class TestType66{}
    private sealed class TestType67{}
    private sealed class TestType68{}
    private sealed class TestType69{}
    private sealed class TestType70{}
    private sealed class TestType71{}
    private sealed class TestType72{}
    private sealed class TestType73{}
    private sealed class TestType74{}
    private sealed class TestType75{}
    private sealed class TestType76{}
    private sealed class TestType77{}
    private sealed class TestType78{}
    private sealed class TestType79{}
    private sealed class TestType80{}
    private sealed class TestType81{}
    private sealed class TestType82{}
    private sealed class TestType83{}
    private sealed class TestType84{}
    private sealed class TestType85{}
    private sealed class TestType86{}
    private sealed class TestType87{}
    private sealed class TestType88{}
    private sealed class TestType89{}
    private sealed class TestType90{}
    private sealed class TestType91{}
    private sealed class TestType92{}
    private sealed class TestType93{}
    private sealed class TestType94{}
    private sealed class TestType95{}
    private sealed class TestType96{}
    private sealed class TestType97{}
    private sealed class TestType98{}
    private sealed class TestType99{}
    // ReSharper restore UnusedType.Local
    // @formatter:on
}
