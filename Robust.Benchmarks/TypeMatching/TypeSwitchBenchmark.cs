using BenchmarkDotNet.Attributes;
using Robust.Shared.Analyzers;

namespace Robust.Benchmarks.TypeMatching;

[MemoryDiagnoser]
[Virtual]
public class TypeSwitchBenchmark
{
    private readonly Matcher<Struct> _matcher = new();

    [Benchmark]
    public int BenchmarkInt()
    {
        return _matcher.TypeToInt<int>();
    }

    [Benchmark]
    public int BenchmarkString()
    {
        return _matcher.TypeToInt<string>();
    }

    [Benchmark]
    public int BenchmarkStruct()
    {
        return _matcher.TypeToInt<Struct>();
    }

    [Benchmark]
    public int BenchmarkDouble()
    {
        return _matcher.TypeToInt<double>();
    }

    [Benchmark]
    public int BenchmarkFloat()
    {
        return _matcher.TypeToInt<float>();
    }

    [Benchmark]
    public int BenchmarkClass()
    {
        return _matcher.TypeToInt<Class>();
    }

    private class Matcher<T1>
    {
        public int TypeToInt<T>(T val = default!)
        {
            return val switch
            {
                int => 1,
                string => 2,
                double => 3,
                T1 => 4,
                Class => 5,
                _ => 6
            };
        }
    }

    private struct Struct
    {
    }

    private class Class
    {
    }
}
