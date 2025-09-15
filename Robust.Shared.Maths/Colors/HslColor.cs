using System;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths.Colors;

/// <summary>
///     Represents a color with alpha in the HSL transformation of sRGB colorspace.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct HslColor : IEquatable<HslColor>, ISpanFormattable
{
    public float Hue;
    public float Saturation;
    public float Lightness;
    public float Alpha;

    public HslColor(float r, float g, float b, float a)
    {
        Hue = r;
        Saturation = g;
        Lightness = b;
        Alpha = a;
    }

    /// <summary>
    ///     Converts HSL color values to RGB color values.
    /// </summary>
    public readonly SrgbColor ToSrgb()
    {
        var hue = (Hue - MathF.Truncate(Hue)) * 360.0f;

        var c = (1.0f - MathF.Abs(2.0f * Lightness - 1.0f)) * Saturation;

        var h = hue / 60.0f;
        var X = c * (1.0f - MathF.Abs(h % 2.0f - 1.0f));

        float r, g, b;
        if (0.0f <= h && h < 1.0f)
        {
            r = c;
            g = X;
            b = 0.0f;
        }
        else if (1.0f <= h && h < 2.0f)
        {
            r = X;
            g = c;
            b = 0.0f;
        }
        else if (2.0f <= h && h < 3.0f)
        {
            r = 0.0f;
            g = c;
            b = X;
        }
        else if (3.0f <= h && h < 4.0f)
        {
            r = 0.0f;
            g = X;
            b = c;
        }
        else if (4.0f <= h && h < 5.0f)
        {
            r = X;
            g = 0.0f;
            b = c;
        }
        else if (5.0f <= h && h < 6.0f)
        {
            r = c;
            g = 0.0f;
            b = X;
        }
        else
        {
            r = 0.0f;
            g = 0.0f;
            b = 0.0f;
        }

        var m = Lightness - c / 2.0f;
        return new SrgbColor(r + m, g + m, b + m, Alpha);
    }

    public static bool operator ==(HslColor left, HslColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(HslColor left, HslColor right)
    {
        return !left.Equals(right);
    }

    public readonly override bool Equals(object? obj)
    {
        if (!(obj is HslColor))
            return false;

        return Equals((HslColor)obj);
    }

    public readonly bool Equals(HslColor other)
            => Hue == other.Hue && Saturation == other.Saturation && Lightness == other.Lightness && Alpha == other.Alpha;

    public readonly override string ToString()
    {
        return $"hsl({Hue}, {Saturation}, {Lightness}, {Alpha})";
    }

    public readonly override int GetHashCode()
    {
        return (int) (((uint) (Alpha * byte.MaxValue) << 24) |
                ((uint) (Hue * byte.MaxValue) << 16) |
                ((uint) (Saturation * byte.MaxValue) << 8) |
                (uint) (Lightness * byte.MaxValue));
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
            $"hsl({Hue}, {Saturation}, {Lightness}, {Alpha})");
    }
}
