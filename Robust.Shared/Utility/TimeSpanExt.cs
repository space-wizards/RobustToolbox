using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using Robust.Shared.Serialization.Markdown.Value;

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

    /// <summary>
    /// Validates if the input can be turned into a TimeSpan, and outputs it.
    /// </summary>
    /// <param name="node">The data node being validated. It must be either a number (which will be interpreted as seconds) or formatted as a brief alphanumeric timespan.
    /// A valid brief alphanumeric timespan starts with a number and ends with a single letter indicating the time unit used.
    /// It can NOT combine multiple types (like "1h30m"), but it CAN use decimals ("1.5h")</param>
    /// <param name="timeSpan">The TimeSpan result.</param>
    /// <returns>Returns true if the input could be resolved as a TimeSpan.</returns>>
    public static bool TryTimeSpan(ValueDataNode node, out TimeSpan timeSpan)
    {
        return TryTimeSpan(node.Value, out timeSpan);
    }

    /// <summary>
    /// Validates if the input can be turned into a TimeSpan, and outputs it.
    /// </summary>
    /// <param name="str">The string being validated. It must be either a number (which will be interpreted as seconds) or formatted as a brief alphanumeric timespan.
    /// A valid brief alphanumeric timespan starts with a number and ends with a single letter indicating the time unit used.
    /// It can NOT combine multiple types (like "1h30m"), but it CAN use decimals ("1.5h")</param>
    /// <param name="timeSpan">The TimeSpan result.</param>
    /// <returns>Returns true if the input could be resolved as a TimeSpan.</returns>>
    public static bool TryTimeSpan(string str, out TimeSpan timeSpan)
    {
        timeSpan = TimeSpan.Zero;

        // If someone tried to use comma as a decimal separator, they would get orders of magnitude higher numbers than intended
        if (str.Contains(',') || str.Contains(' ') || str.Contains(':'))
            return false;

        // A lot of the checks will be for plain numbers, so might as well rule them out right away, instead of
        // running all the other checks on them. They will need to get parsed later anyway, if they weren't now.
        if (double.TryParse(str, CultureInfo.InvariantCulture, out var v))
        {
            timeSpan = TimeSpan.FromSeconds(v);
            return true;
        }

        // If there aren't even enough characters for a number and a time unit, exit
        if (str.Length <= 1)
            return false;

        // If the input without the last character is still not a valid number, exit
        if (!double.TryParse(str.AsSpan()[..^1], CultureInfo.InvariantCulture, out var number))
            return false;

        // Check the last character of the input for time unit indicators
        switch (str[^1])
        {
            case 's':
            case 'S':
                timeSpan = TimeSpan.FromSeconds(number);
                return true;
            case 'm':
            case 'M':
                timeSpan = TimeSpan.FromMinutes(number);
                return true;
            case 'h':
            case 'H':
                timeSpan = TimeSpan.FromHours(number);
                return true;
            default:
                return false;
        }
    }
}
