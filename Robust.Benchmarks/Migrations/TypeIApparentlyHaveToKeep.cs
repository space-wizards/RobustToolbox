using System;
using BenchmarkDotNet.Mathematics;
using Robust.Benchmarks.Exporters;

namespace Robust.Benchmarks.Migrations;

public class BenchmarkRunReport
{
    public BenchmarkRunParameter[] Parameters { get; set; } = Array.Empty<BenchmarkRunParameter>();
    public Statistics Statistics { get; set; } = default!;
}
