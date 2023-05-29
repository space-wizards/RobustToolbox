using System;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;
using Robust.Shared.Random;

namespace Robust.Benchmarks.Math;

[Virtual]
public class VectorBenchmark
{
    [Params(32)]
    public int N { get; set; }

    private Vector2[] _inputA = default!;
    private System.Numerics.Vector2[] _inputB = default!;

    [GlobalSetup]
    public void Setup()
    {
        _inputA = new Vector2[N];
        _inputB = new System.Numerics.Vector2[N];
        var random = new Random();

        for (var i = 0; i < N; i++)
        {
            _inputA[i] = new Vector2(random.NextFloat() * 32f, random.NextFloat() * 32f);
            _inputB[i] = new System.Numerics.Vector2(random.NextFloat() * 32f, random.NextFloat() * 32f);
        }
    }

    [Benchmark]
    public void AddRobust()
    {
        Span<Vector2> lengths = stackalloc Vector2[N];

        for (var i = 0; i < N; i++)
        {
            lengths[i] = _inputA[i] + Vector2.One;
        }
    }

    [Benchmark]
    public void AddNumerics()
    {
        Span<System.Numerics.Vector2> lengths = stackalloc System.Numerics.Vector2[N];

        for (var i = 0; i < N; i++)
        {
            lengths[i] = _inputB[i] + System.Numerics.Vector2.One;
        }
    }

    [Benchmark]
    public void LengthRobust()
    {
        Span<float> lengths = stackalloc float[N];

        for (var i = 0; i < N; i++)
        {
            lengths[i] = _inputA[i].Length;
        }
    }

    [Benchmark]
    public void LengthNumerics()
    {
        Span<float> lengths = stackalloc float[N];

        for (var i = 0; i < N; i++)
        {
            lengths[i] = _inputB[i].Length();
        }
    }

    [Benchmark]
    public void MinRobust()
    {
        Span<Vector2> lengths = stackalloc Vector2[N];

        for (var i = 0; i < N; i++)
        {
            lengths[i] = Vector2.ComponentMin(_inputA[i], _inputA[(i + 1) % _inputA.Length]);
        }
    }

    [Benchmark]
    public void MinNumerics()
    {
        Span<System.Numerics.Vector2> lengths = stackalloc System.Numerics.Vector2[N];

        for (var i = 0; i < N; i++)
        {
            lengths[i] = System.Numerics.Vector2.Min(_inputB[i], _inputB[(i + 1) % _inputB.Length]);
        }
    }

    [Benchmark]
    public void DotRobust()
    {
        Span<float> lengths = stackalloc float[N];

        for (var i = 0; i < N; i++)
        {
            lengths[i] = Vector2.Dot(_inputA[i], _inputA[(i + 1) % _inputA.Length]);
        }
    }

    [Benchmark]
    public void DotNumerics()
    {
        Span<float> lengths = stackalloc float[N];

        for (var i = 0; i < N; i++)
        {
            lengths[i] = System.Numerics.Vector2.Dot(_inputB[i], _inputB[(i + 1) % _inputB.Length]);
        }
    }

    [Benchmark]
    public void DistanceRobust()
    {
        Span<float> lengths = stackalloc float[N];

        for (var i = 0; i < N; i++)
        {
            lengths[i] = (_inputA[i] - _inputA[(i + 1) % _inputA.Length]).Length;
        }
    }

    [Benchmark]
    public void DistanceNumerics()
    {
        Span<float> lengths = stackalloc float[N];

        for (var i = 0; i < N; i++)
        {
            lengths[i] = System.Numerics.Vector2.Distance(_inputB[i], _inputB[(i + 1) % _inputB.Length]);
        }
    }
}
