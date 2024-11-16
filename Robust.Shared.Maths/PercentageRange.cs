using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Robust.Shared.Maths;

[Serializable]
public struct PercentageRange : IEquatable<PercentageRange>
{
    /// <summary>
    /// Minimum range value, e.g. 0.01f == 1%
    /// </summary>
    public float Min;

    /// <summary>
    /// Maximum range value, e.g. 1f == 100%
    /// </summary>
    public float Max;

    public bool IsValid()
    {
        return IsValid(this, out _);
    }

    private bool IsValid([NotNullWhen(false)] out string? exception)
    {
        return IsValid(this, out exception);
    }

    private static bool IsValid(PercentageRange range, [NotNullWhen(false)] out string? exception)
    {
        return IsValid(range.Min, range.Max, out exception);
    }

    private static bool IsValid((float, float) range, [NotNullWhen(false)] out string? exception)
    {
        return IsValid(range.Item1, range.Item2, out exception);
    }

    private static bool IsValid(float min, float max, [NotNullWhen(false)] out string? exception)
    {
        exception = (min, max) switch
        {
            _ when min < 0 || max > 1 => $"Incorrect min and max values. Got: {min}, {max}; expected to be in 0.0 - 1.0 range",
            _ when min > max => $"Incorrect min and max values. Min value is greater than max value: {min}>{max}",
            _ => null
        };

        return exception == null;
    }

    public PercentageRange(float min = 0, float max = 1)
    {
        if (!IsValid(min, max, out var message))
            throw new ArgumentException(message);

        Min = min;
        Max = max;
    }

    public PercentageRange((float, float) range) : this(range.Item1, range.Item2)
    {
    }

    public PercentageRange(ReadOnlySpan<float> range)
    {
        if (range.Length != 2)
            throw new ArgumentException($"An array with 2 items was expected, got: {range.Length}");

        if (!IsValid(range[0], range[1], out var message))
            throw new ArgumentException(message);

        Min = range[0];
        Max = range[1];
    }

    public PercentageRange(IReadOnlyList<float> range)
    {
        if (range.Count != 2)
            throw new ArgumentException($"A list with 2 items was expected, got: {range.Count}");

        if (!IsValid(range[0], range[1], out var message))
            throw new ArgumentException(message);

        Min = range[0];
        Max = range[1];
    }

    public override bool Equals(object? obj)
    {
        return obj is PercentageRange other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(Min, Max);
    }

    public bool Equals(PercentageRange range)
    {
        const double tolerance = 0.0001;
        return Math.Abs(range.Max - Max) < tolerance && Math.Abs(range.Min - Min) < tolerance;
    }

    public static implicit operator (float, float)(PercentageRange r)
    {
        return (r.Min, r.Max);
    }

    public static explicit operator PercentageRange((float, float) r)
    {
        return new PercentageRange(r);
    }

    public static implicit operator (string, string)(PercentageRange r)
    {
        return (((int)(r.Min * 100)).ToString() + '%', ((int)(r.Max * 100)).ToString() + '%');
    }

    public static bool operator ==(PercentageRange range1, PercentageRange range2)
    {
        return range1.Equals(range2);
    }

    public static bool operator !=(PercentageRange range1, PercentageRange range2)
    {
        return !(range1 == range2);
    }

    public override string ToString()
    {
        return $"{Min * 100}-{Max * 100}%";
    }
}
