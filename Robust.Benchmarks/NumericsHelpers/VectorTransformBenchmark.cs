using BenchmarkDotNet.Attributes;
using System.Numerics;
using Robust.Shared.Analyzers;
using Robust.Shared.Maths;
using NVector2 = System.Numerics.Vector2;
using Vector2 = Robust.Shared.Maths.Vector2;
using System;

namespace Robust.Benchmarks.NumericsHelpers
{
    [Virtual]
    [DisassemblyDiagnoser(printSource: true, exportHtml:true)]
    public class VectorTransformBenchmark
    {
        public Matrix3 matrix = Matrix3.CreateTransform((0.1f, 0.2f), 0.5f, (1f, 1.1f));
        public Matrix3x2 nmatrix = Matrix3x2.CreateScale(1f, 1.1f)*Matrix3x2.CreateRotation(0.5f)* Matrix3x2.CreateTranslation(0.1f, 0.2f);

        public Vector2 vector = Vector2.One;
        public NVector2 nvector = NVector2.One;
        
        [Benchmark(Baseline =true, Description = "No SIMD")]
        public Vector2 BenchNoSimd()
        {
            return Matrix3.TransformVec2NoSimd(in matrix, Matrix3.TransformVec2NoSimd(in matrix, Matrix3.TransformVec2NoSimd(in matrix, vector)));
        }

        [Benchmark(Description = "Using SSE")]
        public Vector2 BenchSse()
        {
            return Matrix3.TransformVec2Sse(in matrix, Matrix3.TransformVec2Sse(in matrix, Matrix3.TransformVec2Sse(in matrix, vector)));
        }

        [Benchmark(Description = "Using SSE3")]
        public Vector2 BenchSse3()
        {
            return Matrix3.TransformVec2Sse3(in matrix, Matrix3.TransformVec2Sse3(in matrix, Matrix3.TransformVec2Sse3(in matrix, vector)));
        }

        [Benchmark(Description = "Using FMA")]
        public Vector2 BenchFma()
        {
            return Matrix3.TransformVec2Fma(in matrix, Matrix3.TransformVec2Fma(in matrix, Matrix3.TransformVec2Fma(in matrix, vector)));
        }


        [Benchmark(Description = "System.Numerics")]
        public NVector2 BenchNumerics()
        {
            // even faster than the best I can get with simd
            return NVector2.Transform(NVector2.Transform(NVector2.Transform(nvector, nmatrix), nmatrix), nmatrix);
        }
    }
}
