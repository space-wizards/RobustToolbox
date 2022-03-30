using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Running;
using System;
using Robust.Benchmarks.Configs;
using Robust.Benchmarks.Exporters;

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
            var val = Environment.GetEnvironmentVariable("ROBUST_BENCHMARKS_ENABLE_SQL");
            Console.WriteLine($"Value of env var: '{val}'");
            var sqlBenchmarks = val != null;
            if(sqlBenchmarks)
                Console.WriteLine("Found ROBUST_BENCHMARKS_ENABLE_SQL, using SQLExporter.");
            BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, sqlBenchmarks ? DefaultSQLConfig.Instance : null);
#endif
        }
    }
}
