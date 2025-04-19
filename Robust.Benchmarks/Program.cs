#if DEBUG
using BenchmarkDotNet.Configs;
#endif
using System;
using BenchmarkDotNet.Running;
using Robust.Benchmarks.Configs;

namespace Robust.Benchmarks
{
    internal static class Program
    {
        // --allCategories=ctg1,ctg2
        // --anyCategories=ctg1,ctg2
        public static void Main(string[] args)
        {
#if DEBUG
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("\nWARNING: YOU ARE RUNNING A DEBUG BUILD, USE A RELEASE BUILD FOR AN ACCURATE BENCHMARK");
            Console.WriteLine("THE DEBUG BUILD IS ONLY GOOD FOR FIXING A CRASHING BENCHMARK\n");
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, new DebugInProcessConfig());
#else
            var config = RobustConfig.Instance;
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
#endif
        }
    }
}
