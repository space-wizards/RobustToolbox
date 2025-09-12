using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Robust.Benchmarks.NumericsHelpers;

[Virtual]
public class GetAABBBenchmark
{
    public Vector128<float> X;
    public Vector128<float> Y;

    [Benchmark(Baseline = true)]
    public Vector128<float> GetAABB()
    {
        return SimdHelpers.GetAABBSlow(X, Y);
    }

    [Benchmark]
    public Vector128<float> GetAABB128()
    {
        return SimdHelpers.GetAABB128(X, Y);
    }

    [Benchmark]
    public Vector128<float> GetAABB256()
    {
        return SimdHelpers.GetAABB256(X, Y);
    }
}
