using System;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths.Colors;

/// <summary>
///     Represents a color with alpha in the Linear sRGB colorspace.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct LinearSrgbColor : IEquatable<LinearSrgbColor>, ISpanFormattable
{
    public float Red;
    public float Green;
    public float Blue;
    public float Alpha;

    public LinearSrgbColor(float r, float g, float b, float a)
    {
        Red = r;
        Green = g;
        Blue = b;
        Alpha = a;
    }

    /// <summary>
    ///     Converts linear RGB color values to sRGB color values.
    /// </summary>
    public readonly SrgbColor ToSrgb()
    {
        float r, g, b;

        if (Red <= 0.0031308)
            r = 12.92f * Red;
        else
            r = (1.0f + 0.055f) * MathF.Pow(Red, 1.0f / 2.4f) - 0.055f;

        if (Green <= 0.0031308)
            g = 12.92f * Green;
        else
            g = (1.0f + 0.055f) * MathF.Pow(Green, 1.0f / 2.4f) - 0.055f;

        if (Blue <= 0.0031308)
            b = 12.92f * Blue;
        else
            b = (1.0f + 0.055f) * MathF.Pow(Blue, 1.0f / 2.4f) - 0.055f;

        return new SrgbColor(r, g, b, Alpha);
    }

    /// <summary>
    ///     Converts linear sRGB color values to Oklab color values.
    /// </summary>
    public readonly OklabColor ToOklab()
    {
        // convert from srgb to linear lms

        var l = 0.4122214708f * Red + 0.5363325363f * Green + 0.0514459929f * Blue;
        var m = 0.2119034982f * Red + 0.6806995451f * Green + 0.1073969566f * Blue;
        var s = 0.0883024619f * Red + 0.2817188376f * Green + 0.6299787005f * Blue;

        // convert from linear lms to non-linear lms

        var l_ = MathF.Cbrt(l);
        var m_ = MathF.Cbrt(m);
        var s_ = MathF.Cbrt(s);

        // convert from non-linear lms to lab

        return new OklabColor(
            0.2104542553f * l_ + 0.7936177850f * m_ - 0.0040720468f * s_,
            1.9779984951f * l_ - 2.4285922050f * m_ + 0.4505937099f * s_,
            0.0259040371f * l_ + 0.7827717662f * m_ - 0.8086757660f * s_,
            Alpha
        );
    }

    public static bool operator ==(LinearSrgbColor left, LinearSrgbColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(LinearSrgbColor left, LinearSrgbColor right)
    {
        return !left.Equals(right);
    }

    public readonly override bool Equals(object? obj)
    {
        if (!(obj is LinearSrgbColor))
            return false;

        return Equals((LinearSrgbColor)obj);
    }

    public readonly bool Equals(LinearSrgbColor other)
            => Red == other.Red && Green == other.Green && Blue == other.Blue && Alpha == other.Alpha;

    public readonly override string ToString()
    {
        return $"linear-rgba({Red}, {Green}, {Blue}, {Alpha})";
    }

    public readonly override int GetHashCode()
    {
        return (int)(((uint)(Alpha * byte.MaxValue) << 24) |
                ((uint)(Red * byte.MaxValue) << 16) |
                ((uint)(Green * byte.MaxValue) << 8) |
                (uint)(Blue * byte.MaxValue));
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
            $"linear-rgba({Red}, {Green}, {Blue}, {Alpha})");
    }
}
