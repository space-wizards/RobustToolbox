using System;
using Robust.Shared.Timing;

namespace Robust.Shared.Profiling;

/// <summary>
/// Wrapper around <see cref="RStopwatch"/> that also does allocations tracking using <see cref="GC.GetAllocatedBytesForCurrentThread"/>.
/// </summary>
public struct ProfSampler
{
    public RStopwatch Stopwatch;

    // Same tracking rule as RStopwatch' _curTicks
    private long _alloc;

    public static ProfSampler StartNew()
    {
        ProfSampler sampler = new();
        sampler.Start();
        return sampler;
    }

    public void Start()
    {
        if (Stopwatch.IsRunning)
            return;

        Stopwatch.Start();
        var startAlloc = GC.GetAllocatedBytesForCurrentThread();
        _alloc = startAlloc - _alloc;
    }

    public void Restart()
    {
        Stopwatch.Restart();
        _alloc = GC.GetAllocatedBytesForCurrentThread();
    }

    public void Stop()
    {
        if (!Stopwatch.IsRunning)
            return;

        _alloc = ElapsedAlloc;
        Stopwatch.Stop();
    }

    public void Reset()
    {
        this = default;
    }

    public readonly long ElapsedAlloc => Stopwatch.IsRunning
        ? GC.GetAllocatedBytesForCurrentThread() - _alloc
        : _alloc;

    public readonly TimeSpan Elapsed => Stopwatch.Elapsed;
}
