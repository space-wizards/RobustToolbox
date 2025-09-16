using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths.Colors;

/// <summary>
///     Represents a color with alpha in the sYCC colorspace.
/// </summary>
/// <remarks>
///     Identical to one of the colorspaces referred to as ITU-R BT.601 YCbCr colorspace.
/// </remarks>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct SyccColor : IEquatable<SyccColor>, ISpanFormattable
{
    public float Y;
    public float Cb;
    public float Cr;
    public float Alpha;
    public readonly Vector4 AsVector => Unsafe.BitCast<SyccColor, Vector4>(this);

    public SyccColor(in Vector4 vec)
    {
        this = Unsafe.BitCast<Vector4, SyccColor>(vec);
    }

    public SyccColor(float y, float cb, float cr, float alpha)
    {
        Y = y;
        Cb = cb;
        Cr = cr;
        Alpha = alpha;
    }

    /// <summary>
    ///     Converts sYCC color values to RGB color values.
    /// </summary>
    public readonly SrgbColor ToSrgb()
    {
        var r = 1.0f * Y + 0.0f * Cb + 1.402f * Cr;
        var g = 1.0f * Y + -0.344136f * Cb + -0.714136f * Cr;
        var b = 1.0f * Y + 1.772f * Cb + 0.0f * Cr;

        return new SrgbColor(r, g, b, Alpha);
    }

    public static bool operator ==(SyccColor left, SyccColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(SyccColor left, SyccColor right)
    {
        return !left.Equals(right);
    }

    public readonly override bool Equals(object? obj)
    {
        if (!(obj is SyccColor))
            return false;

        return Equals((SyccColor)obj);
    }

    public readonly bool Equals(SyccColor other)
            => Y == other.Y && Cb == other.Cb && Cr == other.Cr && Alpha == other.Alpha;

    public readonly override string ToString()
    {
        return $"sycc({Y}, {Cb}, {Cr}, {Alpha})";
    }

    public readonly override int GetHashCode()
    {
        return (int)(((uint)(Alpha * byte.MaxValue) << 24) |
                ((uint)(Y * byte.MaxValue) << 16) |
                ((uint)((Cb + 0.5) * byte.MaxValue) << 8) |
                (uint)((Cr + 0.5) * byte.MaxValue));
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
            $"sycc({Y}, {Cb}, {Cr}, {Alpha})");
    }
}
