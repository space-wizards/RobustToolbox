using System.Numerics;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Robust.Benchmarks.NumericsHelpers;

[Virtual, DisassemblyDiagnoser]
public class Box2Benchmark
{
    public Box2 Box = new();
    public Matrix3x2 Matrix = new();

    [Benchmark]
    public Box2 Transform()
    {
        return Matrix.TransformBox(Box);
    }
}
