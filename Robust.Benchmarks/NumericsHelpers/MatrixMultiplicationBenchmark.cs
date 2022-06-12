using BenchmarkDotNet.Attributes;
using System.Numerics;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;

namespace Robust.Benchmarks.NumericsHelpers
{
    [Virtual]
    [DisassemblyDiagnoser(printSource: true, exportHtml:true)]
    public class MatrixMultiplicationBenchmark
    {
        public Matrix3 matrixA = Matrix3.Identity;
        public Matrix3x2 nmatrixA = Matrix3x2.Identity;
        public Matrix3 matrixB = Matrix3.Identity;
        public Matrix3x2 nmatrixB = Matrix3x2.Identity;

        [Benchmark(Baseline = true, Description = "No Simd")]
        public Matrix3 BenchNoSimd()
        {
            Matrix3.Multiply3x2(in matrixA, in matrixB, out var result);
            Matrix3.Multiply3x2(in result, in matrixA, out var result2);
            Matrix3.Multiply3x2(in result2, in matrixB, out var result3);
            return result3;
        }

        [Benchmark(Description = "Using Fma")]
        public Matrix3 MultiplyFma()
        {
            Matrix3.MultiplyFma(in matrixA, in matrixB, out var result);
            Matrix3.MultiplyFma(in result, in matrixA, out var result2);
            Matrix3.MultiplyFma(in result2, in matrixB, out var result3);
            return result3;
        }

        [Benchmark(Description = "Using Sse")]
        public Matrix3 MultiplySse()
        {
            Matrix3.MultiplySse(in matrixA, in matrixB, out var result);
            Matrix3.MultiplySse(in result, in matrixA, out var result2);
            Matrix3.MultiplySse(in result2, in matrixB, out var result3);
            return result3;
        }

        // for whatever reason this seems to be slower than the operator form?
        [Benchmark(Description = "System.Numerics")]
        public Matrix3x2 BenchNumerics()
        {
            return Matrix3x2.Multiply(Matrix3x2.Multiply(Matrix3x2.Multiply(nmatrixA, nmatrixB), nmatrixA), nmatrixB);
        }

        [Benchmark(Description = "System.Numerics (operator)")]
        public Matrix3x2 BenchNumericsOperator()
        {
            return nmatrixA * nmatrixB * nmatrixA * nmatrixB;
        }
    }
}
