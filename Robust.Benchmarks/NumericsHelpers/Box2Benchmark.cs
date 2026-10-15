using System.Numerics;
using System.Runtime.Intrinsics;
using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Robust.Benchmarks.NumericsHelpers;

[Virtual, DisassemblyDiagnoser]
public class Box2Benchmark
{
    public Box2 Box = new(-1, -2, 3, 4);
    public Vector2 Point = new(0.75f, 1.25f);
    public Matrix3x2 Matrix = Matrix3x2.CreateRotation(0.5f) * Matrix3x2.CreateTranslation(3, -1);
    private Box2 _boxResult;

    [Benchmark]
    public void Transform()
    {
        _boxResult = Matrix.TransformBox(Box);
    }

    [Benchmark(Baseline = true)]
    public bool ContainsOld()
    {
        var xOk = Point.X >= Box.Left ^ Point.X > Box.Right;
        var yOk = Point.Y >= Box.Bottom ^ Point.Y > Box.Top;
        return xOk && yOk;
    }

    [Benchmark]
    public bool ContainsCurrent()
    {
        return Box.Contains(Point);
    }
}

[Virtual, DisassemblyDiagnoser]
public class Box2NanBenchmark
{
    public Box2 Box = new(-1, -2, 3, 4);
    public Box2 NanBox = new(float.NaN, -2, 3, 4);

    [Benchmark(Baseline = true)]
    public bool HasNanOldFalse()
    {
        return Vector128.EqualsAny(
            Box.AsVector4.AsVector128(),
            Vector128.Create(float.NaN));
    }

    [Benchmark]
    public bool HasNanCurrentFalse()
    {
        return Box.HasNan();
    }

    [Benchmark]
    public bool HasNanOldTrue()
    {
        return Vector128.EqualsAny(
            NanBox.AsVector4.AsVector128(),
            Vector128.Create(float.NaN));
    }

    [Benchmark]
    public bool HasNanCurrentTrue()
    {
        return NanBox.HasNan();
    }
}

[Virtual, DisassemblyDiagnoser]
public class Box2UnionPerimeterBenchmark
{
    public Box2 BoxA = new(-1, -2, 3, 4);
    public Box2 BoxB = new(2, -3, 5, 1);

    [Benchmark(Baseline = true)]
    public float UnionThenPerimeter()
    {
        return Box2.Perimeter(BoxA.Union(BoxB));
    }

    [Benchmark]
    public float UnionPerimeter()
    {
        return Box2.UnionPerimeter(BoxA, BoxB);
    }
}
