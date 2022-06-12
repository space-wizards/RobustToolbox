using BenchmarkDotNet.Attributes;
using System.Numerics;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Robust.Benchmarks.NumericsHelpers
{
    [Virtual]
    [DisassemblyDiagnoser(printSource: true, exportHtml:true)]
    public class MatrixInversionBenchmark
    {
        public Matrix3 matrix = Matrix3.Identity;
        public Matrix3x2 nmatrix = Matrix3x2.Identity;

        [Benchmark(Baseline =true, Description = "Normal calculation")]
        public Matrix3 BenchNoSimd()
        {
            // 3x3 will naturally be slower than 3x2 inversion, but this is still slow.
            Matrix3.Invert(matrix, out var result);
            return result;
        }

        [Benchmark(Description = "Equivalent using System.Numerics")]
        public Matrix3x2 BenchNumerics()
        {
            Matrix3x2.Invert(nmatrix, out var result);
            return result;
        }
    }
}
