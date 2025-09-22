using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Collections;

namespace Robust.Benchmarks.Collections;

[Virtual]
public class ValueListEnumerationBenchmarks
{
    [Params(4, 16, 64)]
    public int N { get; set; }

    private sealed class Data(int i)
    {
        public readonly int I = i;
    }

    private ValueList<Data> _valueList;
    private Data[] _array = default!;

    [GlobalSetup]
    public void Setup()
    {
        var list = new List<Data>(N);
        for (var i = 0; i < N; i++)
        {
            list.Add(new(i));
        }

        _array = list.ToArray();
        _valueList = new(list.ToArray());
    }

    [Benchmark]
    public int ValueList()
    {
        var total = 0;
        foreach (var ev in _valueList)
        {
            total += ev.I;
        }

        return total;
    }

    [Benchmark]
    public int ValueListSpan()
    {
        var total = 0;
        foreach (var ev in _valueList.Span)
        {
            total += ev.I;
        }

        return total;
    }

    [Benchmark]
    public int Array()
    {
        var total = 0;
        foreach (var ev in _array)
        {
            total += ev.I;
        }

        return total;
    }

    [Benchmark]
    public int Span()
    {
        var total = 0;
        foreach (var ev in _array.AsSpan())
        {
            total += ev.I;
        }

        return total;
    }
}
