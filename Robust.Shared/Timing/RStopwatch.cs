using System;

namespace Robust.Shared.Timing;

using SStopwatch = System.Diagnostics.Stopwatch;

// That's "Robust Stopwatch", so that it doesn't conflict with the BCL.
// Ignore the other one it sucks.

/// <summary>
/// Struct equivalent to <see cref="SStopwatch"/>, avoids allocating.
/// </summary>
public struct RStopwatch
{
    // If IsRunning, then _curTicks is the tick we started counting at.
    // If !IsRunning, then _curTicks is the currently accumulated count.
    public bool IsRunning { get; private set; }
    private long _curTicks;

    private static readonly double TicksToTimeTicks = (double)TimeSpan.TicksPerSecond / SStopwatch.Frequency;

    public static RStopwatch StartNew()
    {
        RStopwatch watch = new();
        watch.Start();
        return watch;
    }

    public void Start()
    {
        if (IsRunning)
            return;

        var startTime = SStopwatch.GetTimestamp();
        _curTicks = startTime - _curTicks;
        IsRunning = true;
    }

    public void Restart()
    {
        IsRunning = true;
        _curTicks = SStopwatch.GetTimestamp();
    }

    public void Stop()
    {
        if (!IsRunning)
            return;

        _curTicks = ElapsedTicks;
        IsRunning = false;
    }

    public void Reset()
    {
        this = default;
    }

    public readonly long ElapsedTicks => IsRunning ? SStopwatch.GetTimestamp() - _curTicks : _curTicks;
    public readonly TimeSpan Elapsed => new(ElapsedTimeTicks());

    private readonly long ElapsedTimeTicks()
    {
        return (long)(TicksToTimeTicks * ElapsedTicks);
    }
}
