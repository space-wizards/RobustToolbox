using System.Numerics.Tensors;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using Robust.Shared.Analyzers;

namespace Robust.Benchmarks.NumericsHelpers;

[Virtual]
[GroupBenchmarksBy(BenchmarkLogicalGroupRule.ByMethod)]
[MemoryDiagnoser]
[DisassemblyDiagnoser]
public class AddBenchmark
{
    [Params(32, 128, 256, 512, 1024, 2048, 4096, 8192, 16384)]
    public int N { get; set; }

    private float[] _inputA = default!;
    private float[] _inputB = default!;
    private float[] _output = default!;

    [GlobalSetup]
    public void Setup()
    {
        _inputA = new float[N];
        _inputB = new float[N];
        _output = new float[N];
    }

    [Benchmark]
    public void BenchNumericsHelpers()
    {
        Shared.Maths.NumericsHelpers.Add(_inputA, _inputB, _output);
    }

    [Benchmark]
    public void BenchTensor()
    {
        TensorPrimitives.Add(_inputA, _inputB, _output);
    }
}
