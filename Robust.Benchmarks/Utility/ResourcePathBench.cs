using BenchmarkDotNet.Attributes;
using JetBrains.Annotations;
using Robust.Shared.Analyzers;
using Robust.Shared.Utility;

#pragma warning disable CS0612
namespace Robust.Benchmarks.Utility;

[Virtual]
public class ResourcePathBench
{
    private string _path = default!;

    [UsedImplicitly]
    [Params(1, 10, 100)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _path = "/a/b/c/../test";
    }

    [Benchmark]
    public ResPath ReadResPath()
    {
        var res = new ResPath(_path);
        for (var i = 0; i < N; i++)
        {
            res = new(_path);
        }
        return res;
    }

    [Benchmark]
    public ResPath ReadResPathFast()
    {
        var res = new ResPath(_path);
        for (var i = 0; i < N; i++)
        {
            res = ResPath.CreateUnsafePath(_path);
        }

        return res;
    }

    [Benchmark]
    public ResourcePath ReadResourcePath()

    {
        var res = new ResourcePath(_path);
        for (var i = 0; i < N; i++)
        {
            res = new ResourcePath(_path);
        }
        return res;
    }
}
#pragma warning restore CS0612
