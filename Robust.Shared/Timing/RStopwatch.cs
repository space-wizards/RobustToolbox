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

    /// <summary>
    ///     Creates and immediately starts a new stopwatch.
    /// </summary>
    public static RStopwatch StartNew()
    {
        RStopwatch watch = new();
        watch.Start();
        return watch;
    }

    /// <summary>
    ///     Starts a stopwatch if it wasn't already.
    /// </summary>
    /// <remarks>
    ///     Starting the same stopwatch twice does nothing.
    /// </remarks>
    public void Start()
    {
        if (IsRunning)
            return;

        var startTime = SStopwatch.GetTimestamp();
        _curTicks = startTime - _curTicks;
        IsRunning = true;
    }

    /// <summary>
    ///     Restarts the stopwatch, ensuring it is running regardless of the state it was in before.
    /// </summary>
    public void Restart()
    {
        IsRunning = true;
        _curTicks = SStopwatch.GetTimestamp();
    }

    /// <summary>
    ///     Stops the stopwatch, freezing its count if it is running.
    /// </summary>
    /// <remarks>
    ///     Does nothing if the stopwatch wasn't already running.
    /// </remarks>
    public void Stop()
    {
        if (!IsRunning)
            return;

        _curTicks = ElapsedTicks;
        IsRunning = false;
    }

    /// <summary>
    ///     Completely resets the stopwatch to an unstarted state with no elapsed time.
    ///     Strictly equivalent to <c>default</c>.
    /// </summary>
    public void Reset()
    {
        this = default;
    }

    /// <summary>
    ///     The amount of elapsed time in <see cref="TimeSpan.Ticks"/>
    /// </summary>
    public readonly long ElapsedTicks => IsRunning ? SStopwatch.GetTimestamp() - _curTicks : _curTicks;

    /// <summary>
    ///     The amount of elapsed time, in real time.
    /// </summary>
    public readonly TimeSpan Elapsed => new(ElapsedTimeTicks());

    private readonly long ElapsedTimeTicks()
    {
        return (long)(TicksToTimeTicks * ElapsedTicks);
    }
}
