using System;
using System.Threading;
using System.Threading.Tasks;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Log;
using Robust.Shared.Utility;

namespace Robust.Shared.Threading;

public interface IParallelManager
{
    event Action ParallelCountChanged;

    int ParallelProcessCount { get; }

    /// <summary>
    /// Add the delegate to <see cref="ParallelCountChanged"/> and immediately invoke it.
    /// </summary>
    void AddAndInvokeParallelCountChanged(Action changed);
}

internal interface IParallelManagerInternal : IParallelManager
{
    void Initialize();
}

internal sealed class ParallelManager : IParallelManagerInternal
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    public event Action? ParallelCountChanged;
    public int ParallelProcessCount { get; private set; }

    public void Initialize()
    {
        _cfg.OnValueChanged(CVars.ThreadParallelCount, UpdateCVar, true);
    }

    public void AddAndInvokeParallelCountChanged(Action changed)
    {
        ParallelCountChanged += changed;
        changed();
    }

    private void UpdateCVar(int value)
    {
        var oldCount = ParallelProcessCount;
        ParallelProcessCount = value == 0 ? Environment.ProcessorCount : value;

        if (oldCount != ParallelProcessCount)
            ParallelCountChanged?.Invoke();
    }
}

public static class ParallelManagerExt
{
    public delegate void ParallelResourceAction<T>(int i, ref T resource);

    public static void ParallelForWithResources<T>(
        this IParallelManager manager,
        int fromInclusive,
        int toExclusive,
        T[] resources,
        ParallelResourceAction<T> action)
    {
        var parallelCount = manager.ParallelProcessCount;

        DebugTools.Assert(
            resources.Length >= parallelCount,
            "Resources buffer is too small to fit maximum thread count.");

        var threadIndex = 0;

        Parallel.For(
            fromInclusive, toExclusive,
            new ParallelOptions { MaxDegreeOfParallelism = parallelCount },
            () => Interlocked.Increment(ref threadIndex),
            (i, _, localThreadIdx) =>
            {
                ref var resource = ref resources[localThreadIdx];

                action(i, ref resource);

                return localThreadIdx;
            },
            _ => { });
    }
}
