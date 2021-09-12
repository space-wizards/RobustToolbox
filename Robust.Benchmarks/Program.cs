using BenchmarkDotNet.Running;

namespace Robust.Benchmarks
{
    internal class Program
    {
        // --allCategories=ctg1,ctg2
        // --anyCategories=ctg1,ctg2
        public static void Main(string[] args)
        {
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args);
        }
    }
}
