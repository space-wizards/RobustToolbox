using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Benchmarks.Utility;

public sealed class ResourcePathBench
{
    private string _path = default!;

    [UsedImplicitly]
    [Params(1, 10)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _path = "/a/b/c/../test";
    }

    [Benchmark]
    public void ReadResPath()
    {
        var res = new ResPath(_path);
        for (var i = 0; i < N; i++)
        {
            res = new($"{res}{i}");
        }
    }

    [Benchmark]
    public void ReadResPathFast()
    {
        var res = new ResPath(_path);
        for (var i = 0; i < N; i++)
        {
            res = ResPath.CreateUnsafePath($"{res}{i}");
        }
    }

    [Benchmark]
    public void ReadResourcePathFast()
    {
        var res = new ResourcePath(_path);
        for (var i = 0; i < N; i++)
        {
            res = new ResourcePath($"{res}{i}");
        }
    }
}
