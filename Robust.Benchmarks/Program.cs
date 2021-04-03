using BenchmarkDotNet.Running;

namespace Robust.Benchmarks
{
    internal class Program
    {
        public static void Main(string[] args)
        {
            // var b = new SerializationReadBenchmark();
            //
            // for (var i = 0; i < 100000; i++)
            // {
            //     b.ReadSeedDataDefinition();
            // }

            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run();
        }
    }
}
