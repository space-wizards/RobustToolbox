using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths.Colors;

/// <summary>
///     Represents a color with alpha in the Oklab colorspace.
/// </summary>
[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct OklchColor : IEquatable<OklchColor>, ISpanFormattable
{
    public float L;
    public float C;
    public float H;
    public float Alpha;
    public readonly Vector4 AsVector => Unsafe.BitCast<OklchColor, Vector4>(this);

    public float Lr
    {
        get => (1.170873786407767f * L - 0.206f + MathF.Sqrt(MathF.Pow(1.170873786407767f * L - 0.206f, 2) + 0.14050485436893204f * L)) / 2f;
        set
        {
            L = (value * (value + 0.206f)) / (1.170873786407767f * (value + 0.03f));
        }
    }

    public OklchColor(in Vector4 vec)
    {
        this = Unsafe.BitCast<Vector4, OklchColor>(vec);
    }

    public OklchColor(float l, float c, float h, float alpha)
    {
        L = l;
        C = c;
        H = h;
        Alpha = alpha;
    }

    /// <summary>
    ///     Converts polar Oklch color values to cartesian Oklab color values.
    /// </summary>
    public readonly OklabColor ToOklab()
    {
        var a = C * MathF.Cos(H);
        var b = C * MathF.Sin(H);

        return new OklabColor(L, a, b, Alpha);
    }

    public static bool operator ==(OklchColor left, OklchColor right)
    {
        return left.Equals(right);
    }

    public static bool operator !=(OklchColor left, OklchColor right)
    {
        return !left.Equals(right);
    }

    public readonly override bool Equals(object? obj)
    {
        if (!(obj is OklchColor))
            return false;

        return Equals((OklchColor)obj);
    }

    public readonly bool Equals(OklchColor other)
            => L == other.L && C == other.C && H == other.H && Alpha == other.Alpha;

    public readonly override string ToString()
    {
        return $"oklch({L}, {C}, {H}, {Alpha})";
    }

    public readonly override int GetHashCode()
    {
        return (int)(((uint)(Alpha * byte.MaxValue) << 24) |
                ((uint)(L * byte.MaxValue) << 16) |
                ((uint)(C * byte.MaxValue) << 8) |
                (uint)(H * byte.MaxValue));
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
            $"oklch({L}, {C}, {H}, {Alpha})");
    }
}
