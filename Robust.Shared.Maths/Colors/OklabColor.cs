using System;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths.Colors;

/// <summary>
///     Represents a color with alpha in the Oklab colorspace.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public partial struct OklabColor : IEquatable<OklabColor>, ISpanFormattable
{
    public float L;
    public float A;
    public float B;
    public float Alpha;

    public float Lr
    {
        get => (1.170873786407767f * L - 0.206f + MathF.Sqrt(MathF.Pow(1.170873786407767f * L - 0.206f, 2) + 0.14050485436893204f * L)) / 2f;
        set
        {
            L = (value * (value + 0.206f)) / (1.170873786407767f * (value + 0.03f));
        }
    }

    public OklabColor(float l, float a, float b, float alpha)
    {
        L = l;
        A = a;
        B = b;
        Alpha = alpha;
    }

    /// <summary>
    ///     Converts Oklab color values to linear sRGB color values.
    /// </summary>
    public readonly LinearSrgbColor ToLinear()
    {
        var l_ = L + 0.3963377774f * A + 0.2158037573f * B;
        var m_ = L - 0.1055613458f * A - 0.0638541728f * B;
        var s_ = L - 0.0894841775f * A - 1.2914855480f * B;

        // convert from non-linear lms to linear lms

        var l = l_ * l_ * l_;
        var m = m_ * m_ * m_;
        var s = s_ * s_ * s_;

        // convert from linear lms to linear srgb

        var r = +4.0767416621f * l - 3.3077115913f * m + 0.2309699292f * s;
        var g = -1.2684380046f * l + 2.6097574011f * m - 0.3413193965f * s;
        var b = -0.0041960863f * l - 0.7034186147f * m + 1.7076147010f * s;

        return new LinearSrgbColor(r, g, b, Alpha);
    }

    /// <summary>
    ///     Converts cartesian Oklab color values to polar Oklch color values.
    /// </summary>
    public readonly OklchColor ToLch()
    {
        var c = MathF.Sqrt(A * A + B * B);
        var h = MathF.Atan2(B, A);
        if (h < 0)
            h += 2 * MathF.PI;

        return new OklchColor(L, c, h, Alpha);
    }

    public static bool operator ==(OklabColor left, OklabColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(OklabColor left, OklabColor right)
    {
        return !left.Equals(right);
    }

    public readonly override bool Equals(object? obj)
    {
        if (!(obj is OklabColor))
            return false;

        return Equals((OklabColor)obj);
    }

    public readonly bool Equals(OklabColor other)
            => L == other.L && A == other.A && B == other.B && Alpha == other.Alpha;

    public readonly override string ToString()
    {
        return $"oklab({L}, {A}, {B}, {Alpha})";
    }

    public readonly override int GetHashCode()
    {
        return (int)(((uint)(Alpha * byte.MaxValue) << 24) |
                ((uint)(L * byte.MaxValue) << 16) |
                ((uint)(A * byte.MaxValue) << 8) |
                (uint)(B * byte.MaxValue));
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString();
    }

    public readonly bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return FormatHelpers.TryFormatInto(
            destination,
            out charsWritten,
            $"oklab({L}, {A}, {B}, {Alpha})");
    }
}
