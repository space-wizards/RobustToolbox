using System.Numerics;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Robust.Benchmarks.NumericsHelpers;

[Virtual, DisassemblyDiagnoser]
public class Box2RotatedBenchmark
{
    public Box2Rotated Box = new();

    [Benchmark]
    public Matrix3x2 GetTransform()
    {
        return Box.Transform;
    }
}
