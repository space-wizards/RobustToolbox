using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Robust.Benchmarks.NumericsHelpers;

[Virtual, DisassemblyDiagnoser]
public class GetAABBBenchmark
{
    public Vector128<float> X;
    public Vector128<float> Y;

    [Benchmark(Baseline = true)]
    public Vector128<float> GetAABB_NoAvx()
    {
        return SimdHelpers.GetAABBSlow(X, Y);
    }

    [Benchmark]
    public Vector128<float> GetAABB_Avx()
    {
        return SimdHelpers.GetAABBAvx(X, Y);
    }
}
