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
    [Params(10, 100, 1000)]
    public int N;

    [GlobalSetup]
    public void GlobalSetup()
    {
        _path = "/a/b/c/../test";
    }

    [Benchmark]
    public ResPath CreateWithSeparatorResPath()
    {
        ResPath res = default;
        for (var i = 0; i < N; i++)
        {
            res = new ResPath(_path);
        }
        return res;
    }

}
#pragma warning restore CS0612
