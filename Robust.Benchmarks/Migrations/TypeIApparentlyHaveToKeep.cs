using System;
using BenchmarkDotNet.Mathematics;

namespace Robust.Benchmarks.Migrations;

public class BenchmarkRunReport
{
    public BenchmarkRunParameter[] Parameters { get; set; } = Array.Empty<BenchmarkRunParameter>();
    public Statistics Statistics { get; set; } = default!;
}

public class BenchmarkRunParameter
{
    public string Name { get; set; } = string.Empty;
    public object Value { get; set; } = default!;
}
