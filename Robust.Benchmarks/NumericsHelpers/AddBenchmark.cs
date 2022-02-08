using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;

namespace Robust.Benchmarks.NumericsHelpers
{
    [Virtual]
    public class AddBenchmark
    {
        [Params(32, 128)]
        public int N { get; set; }

        private float[] _inputA = default!;
        private float[] _inputB = default!;
        private float[] _output = default!;

        [GlobalSetup]
        public void Setup()
        {
            _inputA = new float[N];
            _inputB = new float[N];
            _output = new float[N];
        }

        [Benchmark]
        public void Bench()
        {
            Shared.Maths.NumericsHelpers.Add(_inputA, _inputB, _output);
        }
    }
}
