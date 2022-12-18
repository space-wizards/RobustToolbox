using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;

namespace Robust.Benchmarks.NumericsHelpers;

[Virtual]
[DisassemblyDiagnoser()]
public class HorizontalAddBenchmark
{
    [Params(8, 32, 128)]
    public int N { get; set; }

    private float[] _inputA = default!;

    [GlobalSetup]
    public void Setup()
    {
        _inputA = new float[N];
    }

    [Benchmark]
    public float Bench()
    {
        return Shared.Maths.NumericsHelpers.HorizontalAdd(_inputA);
    }
}
