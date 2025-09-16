using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths.Colors;

/// <summary>
///     Represents a color with alpha in the CIEXYZ colorspace.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct CiexyzColor : IEquatable<CiexyzColor>, ISpanFormattable
{
    public float X;
    public float Y;
    public float Z;
    public float Alpha;
    public readonly Vector4 AsVector => Unsafe.BitCast<CiexyzColor, Vector4>(this);

    public CiexyzColor(in Vector4 vec)
    {
        this = Unsafe.BitCast<Vector4, CiexyzColor>(vec);
    }

    public CiexyzColor(float x, float y, float z, float alpha)
    {
        X = x;
        Y = y;
        Z = z;
        Alpha = alpha;
    }

    /// <summary>
    ///     Converts CIEXYZ color values to Linear sRGB color values.
    /// </summary>
    public readonly LinearSrgbColor ToLinear()
    {
        float r = +3.2406255f * X - 1.5372080f * Y - 0.4986286f * Z;
        float g = -0.9689307f * X + 1.8757561f * Y + 0.0415175f * Z;
        float b = +0.0557101f * X - 0.2040211f * Y + 1.0569959f * Z;

        return new LinearSrgbColor(r, g, b, Alpha);
    }


    /// <summary>
    ///     Interpolate two colors with a lambda, AKA returning the two colors combined with a ratio of
    ///     <paramref name="λ" />.
    /// </summary>
    /// <param name="α"></param>
    /// <param name="β"></param>
    /// <param name="λ">
    ///     A value ranging from 0-1. The higher the value the more is taken from <paramref name="β" />,
    ///     with 0.5 being 50% of both colors, 0.25 being 25% of <paramref name="β" /> and 75%
    ///     <paramref name="α" />.
    /// </param>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static CiexyzColor InterpolateBetween(CiexyzColor α, CiexyzColor β, float λ)
    {
        var result = Vector4.Lerp(α.AsVector, β.AsVector, λ);
        return new(result.X, result.Y, result.Z, result.W);
    }

    public static bool operator ==(CiexyzColor left, CiexyzColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(CiexyzColor left, CiexyzColor right)
    {
        return !left.Equals(right);
    }

    public readonly override bool Equals(object? obj)
    {
        if (!(obj is CiexyzColor))
            return false;

        return Equals((CiexyzColor)obj);
    }

    public readonly bool Equals(CiexyzColor other)
            => X == other.X && Y == other.Y && Z == other.Z && Alpha == other.Alpha;

    public readonly override string ToString()
    {
        return $"ciexyz({X}, {Y}, {Z}, {Alpha})";
    }

    public readonly override int GetHashCode()
    {
        return (int)(((uint)(Alpha * byte.MaxValue) << 24) |
                ((uint)(X * byte.MaxValue) << 16) |
                ((uint)(Y * byte.MaxValue) << 8) |
                (uint)(Z * byte.MaxValue));
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
            $"ciexyz({X}, {Y}, {Z}, {Alpha})");
    }
}
