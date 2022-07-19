using System;

namespace Robust.Shared.Utility;

/// <summary>
/// Helper functions for <see cref="TimeSpan"/>
/// </summary>
public static class TimeSpanExt
{
    /// <summary>
    /// Multiply a <see cref="TimeSpan"/> with an integer factor.
    /// </summary>
    /// <remarks>
    /// <see cref="TimeSpan"/> only has a multiplication operator for doubles,
    /// so this is necessary to avoid round-tripping through floating point calculations.
    /// </remarks>
    public static TimeSpan Mul(this TimeSpan time, long factor)
    {
        return TimeSpan.FromTicks(time.Ticks * factor);
    }
}
