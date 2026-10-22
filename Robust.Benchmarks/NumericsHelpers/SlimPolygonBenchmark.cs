using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Benchmarks.NumericsHelpers;

// SlimPolyon is internal, so this won't compile without changes.
/*
[Virtual, DisassemblyDiagnoser]
public class SlimPolygonBenchmark
{
    public Box2Rotated RotBox = new();

    [Benchmark]
    public SlimPolygon RotatedBox()
    {
        return new SlimPolygon(in RotBox);
    }
}
*/
