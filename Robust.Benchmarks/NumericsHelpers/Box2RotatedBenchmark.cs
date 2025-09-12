using System.Numerics;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Robust.Benchmarks.NumericsHelpers;

[Virtual, DisassemblyDiagnoser]
public class Box2RotatedBenchmark
{
    public Box2Rotated Box = new();

    [Benchmark(Baseline = true)]
    public Matrix3x2 GetTransformOld()
    {
        return Box.TransformOld;
    }

    [Benchmark]
    public Matrix3x2 GetTransform()
    {
        return Box.Transform;
    }
}
