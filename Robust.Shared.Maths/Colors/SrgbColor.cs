using System;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths.Colors;

/// <summary>
///     Represents a color with alpha in the sRGB colorspace.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct SrgbColor : IEquatable<SrgbColor>, ISpanFormattable
{
    public float Red;
    public float Green;
    public float Blue;
    public float Alpha;

    public readonly bool IsInGamut
    {
        get => Red <= 1 && Green <= 1 && Blue <= 1 && Red >= 0 && Green >= 0 && Blue >= 0;
    }

    public SrgbColor(float r, float g, float b, float a)
    {
        Red = r;
        Green = g;
        Blue = b;
        Alpha = a;
    }

    /// <summary>
    ///     Converts sRGB color values to HSL color values.
    /// </summary>
    public readonly HslColor ToHsl()
    {
        var max = MathF.Max(Red, MathF.Max(Green, Blue));
        var min = MathF.Min(Red, MathF.Min(Green, Blue));
        var c = max - min;

        var h = 0.0f;
        if (c != 0)
        {
            if (max == Red)
                h = (Green - Blue) / c;
            else if (max == Green)
                h = (Blue - Red) / c + 2.0f;
            else if (max == Blue)
                h = (Red - Green) / c + 4.0f;
        }

        var hue = h / 6.0f;
        if (hue < 0.0f)
            hue += 1.0f;

        var lightness = (max + min) / 2.0f;

        var saturation = 0.0f;
        if (0.0f != lightness && lightness != 1.0f)
            saturation = c / (1.0f - MathF.Abs(2.0f * lightness - 1.0f));

        return new HslColor(hue, saturation, lightness, Alpha);
    }

    /// <summary>
    ///     Converts sRGB color values to HSL color values.
    /// </summary>
    public readonly HsvColor ToHsv()
    {
        var max = MathF.Max(Red, MathF.Max(Green, Blue));
        var min = MathF.Min(Red, MathF.Min(Green, Blue));
        var c = max - min;

        var h = 0.0f;
        if (c != 0)
        {
            if (max == Red)
            {
                h = (Green - Blue) / c % 6.0f;
                if (h < 0f)
                    h += 6.0f;
            }
            else if (max == Green)
                h = (Blue - Red) / c + 2.0f;
            else if (max == Blue)
                h = (Red - Green) / c + 4.0f;
        }

        var hue = h * 60.0f / 360.0f;

        var saturation = 0.0f;
        if (0.0f != max)
            saturation = c / max;

        return new HsvColor(hue, saturation, max, Alpha);
    }

    /// <summary>
    ///     Converts sRGB color values to linear RGB color values.
    /// </summary>
    public readonly LinearSrgbColor ToLinear()
    {
        float r, g, b;
        if (Red <= 0.04045f)
            r = Red / 12.92f;
        else
            r = MathF.Pow((Red + 0.055f) / (1.0f + 0.055f), 2.4f);

        if (Green <= 0.04045f)
            g = Green / 12.92f;
        else
            g = MathF.Pow((Green + 0.055f) / (1.0f + 0.055f), 2.4f);

        if (Blue <= 0.04045f)
            b = Blue / 12.92f;
        else
            b = MathF.Pow((Blue + 0.055f) / (1.0f + 0.055f), 2.4f);

        return new LinearSrgbColor(r, g, b, Alpha);
    }

    /// <summary>
    ///     Converts sRGB color values to sYCC color values.
    /// </summary>
    public readonly SyccColor ToSycc()
    {
        var y = 0.299f * Red + 0.587f * Green + 0.114f * Blue;
        var u = -0.168736f * Red + -0.331264f * Green + 0.5f * Blue;
        var v = 0.5f * Red + -0.418688f * Green + -0.081312f * Blue;

        return new SyccColor(y, u, v, Alpha);
    }

    /// <summary>
    ///     Casts a SrgbColor value to a sRGB Color value.
    /// </summary>
    /// <remarks>
    ///     This is NOT Linear sRGB. Don't put this somewhere that expects Linear sRGB.
    /// </remarks>
    public readonly Color ToColor()
    {
        return new Color(Red, Green, Blue, Alpha);
    }

    public static bool operator ==(SrgbColor left, SrgbColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SrgbColor left, SrgbColor right)
    {
        return !left.Equals(right);
    }

    public readonly override bool Equals(object? obj)
    {
        if (!(obj is SrgbColor))
            return false;

        return Equals((SrgbColor)obj);
    }

    public readonly bool Equals(SrgbColor other)
            => Red == other.Red && Green == other.Green && Blue == other.Blue && Alpha == other.Alpha;

    public readonly override string ToString()
    {
        return $"rgba({Red}, {Green}, {Blue}, {Alpha})";
    }

    public readonly override int GetHashCode()
    {
        return (int) (((uint) (Alpha * byte.MaxValue) << 24) |
                ((uint) (Red * byte.MaxValue) << 16) |
                ((uint) (Green * byte.MaxValue) << 8) |
                (uint) (Blue * byte.MaxValue));
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
            $"rgba({Red}, {Green}, {Blue}, {Alpha})");
    }
}
