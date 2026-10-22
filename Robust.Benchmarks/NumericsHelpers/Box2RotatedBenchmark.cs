using System.Numerics;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Robust.Benchmarks.NumericsHelpers;

[Virtual, DisassemblyDiagnoser]
public class Box2RotatedBenchmark
{
    public Box2Rotated Box = new(Box2.UnitCentered.Translated(new Vector2(1, 2)), Angle.FromDegrees(37), new Vector2(1, 2));
    public Matrix3x2 Matrix = Matrix3x2.CreateScale(1.5f, 0.5f) * Matrix3x2.CreateRotation(0.5f) * Matrix3x2.CreateTranslation(3, -1);
    private Matrix3x2 _matrixResult;
    private Box2 _boxResult;

    [Benchmark]
    public void GetTransform()
    {
        _matrixResult = Box.Transform;
    }

    [Benchmark(Baseline = true)]
    public void TransformBoxOld()
    {
        _boxResult = (Box.Transform * Matrix).TransformBox(Box.Box);
    }

    [Benchmark]
    public void TransformBox()
    {
        _boxResult = Matrix.TransformBox(Box);
    }
}
