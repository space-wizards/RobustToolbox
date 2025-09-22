using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using JetBrains.Annotations;

namespace Robust.Shared.Maths;

public static class Vector2Helpers
{
    public static readonly Vector2 Infinity = new(float.PositiveInfinity, float.PositiveInfinity);
    public static readonly Vector2 NaN = new(float.NaN, float.NaN);

    /// <summary>
    /// Half of a unit vector.
    /// </summary>
    public static readonly Vector2 Half = new(0.5f, 0.5f);

	[Pure]
    public static bool IsValid(this Vector2 v)
    {
        if (float.IsNaN(v.X) || float.IsNaN(v.Y))
        {
            return false;
        }

        if (float.IsInfinity(v.X) || float.IsInfinity(v.Y))
        {
            return false;
        }

        return true;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    [Pure]
    public static Vector2 MulAdd(Vector2 a, float s, Vector2 b)
    {
        return new Vector2(a.X + s * b.X, a.Y + s * b.Y);
    }
    
    public static Vector2 GetLengthAndNormalize(this Vector2 v, ref float length)
    {
        length = v.Length();
        if (length < float.Epsilon)
        {
            return Vector2.Zero;
        }

        float invLength = 1.0f / length;
        var n = new Vector2(invLength * v.X, invLength * v.Y);
        return n;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 InterpolateCubic(Vector2 preA, Vector2 a, Vector2 b, Vector2 postB, float t)
    {
        return a + (b - preA + (preA * 2.0f - a * 5.0f + b * 4.0f - postB + ((a - b) * 3.0f + postB - preA) * t) * t) * t * 0.5f;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsApprox(this Vector2 vec, Vector2 otherVec)
    {
        return MathHelper.CloseTo(vec.X, otherVec.X) && MathHelper.CloseTo(vec.Y, otherVec.Y);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsApprox(this Vector2 vec, Vector2 otherVec, double tolerance)
    {
        return MathHelper.CloseTo(vec.X, otherVec.X, tolerance) && MathHelper.CloseTo(vec.Y, otherVec.Y, tolerance);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Normalized(this Vector2 vec)
    {
        var length = vec.Length();
        return new Vector2(vec.X / length, vec.Y / length);
    }

    /// <summary>
    /// Normalizes this vector if its length > 0, otherwise sets it to 0.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Normalize(ref this Vector2 vec)
    {
        var length = vec.Length();

        if (length < float.Epsilon)
        {
            vec = Vector2.Zero;
            return 0f;
        }

        var invLength = 1f / length;
        vec.X *= invLength;
        vec.Y *= invLength;
        return length;
    }

    /// <summary>
    /// Compares the lengths of two vectors.
    /// </summary>
    /// <remarks>
    /// Avoids square root computations by using squared lengths.
    /// </remarks>
    /// <returns>
    /// a positive value if <paramref name="a"/> is longer than <paramref name="b"/>,
    /// a negative value if <paramref name="b"/> is longer than <paramref name="a"/>,
    /// or 0 if <paramref name="a"/> and <paramref name="b"/> have equal lengths.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CompareLength(Vector2 a, Vector2 b)
    {
        return a.LengthSquared() - b.LengthSquared();
    }

    /// <summary>
    /// Compares the length of a vector with a scalar.
    /// </summary>
    /// <remarks>
    /// Avoids a square root computation by using squared length.
    /// </remarks>
    /// <returns>
    /// a positive value if <paramref name="vec"/> is longer than <paramref name="scalar"/>,
    /// a negative value if <paramref name="vec"/> is shorter than <paramref name="scalar"/>,
    /// or 0 if <paramref name="vec"/> has a length equal to <paramref name="scalar"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CompareLength(Vector2 vec, float scalar)
    {
        return vec.LengthSquared() - (scalar * scalar);
    }

    /// <summary>
    /// Compares the length of this vector with a scalar.
    /// </summary>
    /// <remarks>
    /// Avoids a square root computation by using squared length.
    /// </remarks>
    /// <returns>
    /// a positive value if this vector is longer than <paramref name="scalar"/>,
    /// a negative value if this vector is shorter than <paramref name="scalar"/>,
    /// or 0 if this vector has a length equal to <paramref name="scalar"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CompareLengthTo(this Vector2 vec, float scalar)
    {
        return CompareLength(vec, scalar);
    }

    /// <summary>
    /// Compares the length of this vector with another.
    /// </summary>
    /// <remarks>
    /// Avoids square root computations by using squared lengths.
    /// </remarks>
    /// <returns>
    /// a positive value if this vector is longer than <paramref name="otherVec"/>,
    /// a negative value if this vector is shorter than <paramref name="otherVec"/>,
    /// or 0 if this vector and <paramref name="otherVec"/> have equal lengths.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CompareLengthTo(this Vector2 thisVec, Vector2 otherVec)
    {
        return CompareLength(thisVec, otherVec);
    }

    /// <summary>
    /// Is the length of this vector greater than <paramref name="scalar"/>?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLongerThan(this Vector2 vec, float scalar)
    {
        return CompareLength(vec, scalar) > 0;
    }

    /// <summary>
    /// Is the length of this vector greater than the length of <paramref name="otherVec"/>?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLongerThan(this Vector2 thisVec, Vector2 otherVec)
    {
        return CompareLength(thisVec, otherVec) > 0;
    }

    /// <summary>
    /// Is the length of this vector greater than or equal to <paramref name="scalar"/>?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLongerThanOrEqualTo(this Vector2 vec, float scalar)
    {
        return CompareLength(vec, scalar) >= 0;
    }

    /// <summary>
    /// Is the length of this vector greater than or equal to the length of <paramref name="otherVec"/>?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsLongerThanOrEqualTo(this Vector2 thisVec, Vector2 otherVec)
    {
        return CompareLength(thisVec, otherVec) >= 0;
    }

    /// <summary>
    /// Is the length of this vector less than <paramref name="scalar"/>?
    /// </summary>
    /// <param name="vec"></param>
    /// <param name="scalar"></param>
    /// <returns></returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsShorterThan(this Vector2 vec, float scalar)
    {
        return CompareLength(vec, scalar) < 0;
    }

    /// <summary>
    /// Is the length of this vector less than the length of <paramref name="otherVec"/>?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsShorterThan(this Vector2 thisVec, Vector2 otherVec)
    {
        return CompareLength(thisVec, otherVec) < 0;
    }

    /// <summary>
    /// Is the length of this vector less than or equal to <paramref name="scalar"/>?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsShorterThanOrEqualTo(this Vector2 vec, float scalar)
    {
        return CompareLength(vec, scalar) <= 0;
    }

    /// <summary>
    /// Is the length of this vector less than or equal to the length of <paramref name="otherVec"/>?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsShorterThanOrEqualTo(this Vector2 thisVec, Vector2 otherVec)
    {
        return CompareLength(thisVec, otherVec) <= 0;
    }

    /// <summary>
    /// Returns true if this vector's length is equal to <paramref name="scalar"/>.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool LengthEquals(this Vector2 thisVec, float scalar)
    {
        return CompareLength(thisVec, scalar) == 0;
    }

    /// <summary>
    /// Is this vector the same length as <paramref name="otherVec"/>?
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsEqualLengthTo(this Vector2 thisVec, Vector2 otherVec)
    {
        return CompareLength(thisVec, otherVec) == 0;
    }

    /// <summary>
    /// Compares the length of a vector with a scalar.
    /// </summary>
    /// <remarks>
    /// Avoids a square root computation by using squared length.
    /// </remarks>
    /// <returns>
    /// a positive value if <paramref name="vec"/> is shorter than <paramref name="scalar"/>,
    /// a negative value if <paramref name="vec"/> is longer than <paramref name="scalar"/>,
    /// or 0 if <paramref name="vec"/> has a length equal to <paramref name="scalar"/>.
    /// </returns>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float CompareLength(float scalar, Vector2 vec)
    {
        return (scalar * scalar) - vec.LengthSquared();
    }

    /// <summary>
    /// Is the length of this vector zero?
    /// </summary>
    public static bool IsLengthZero(this Vector2 vec)
    {
        return vec.LengthSquared() == 0;
    }

    /// <summary>
    /// Perform the cross product on a scalar and a vector. In 2D this produces
    /// a vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Cross(float s, in Vector2 a)
    {
        return new(-s * a.Y, s * a.X);
    }

    [Pure]
    public static Vector2 RightPerp(this Vector2 v)
    {
        return new Vector2(v.Y, -v.X);
    }

    /// <summary>
    /// Perform the cross product on a scalar and a vector. In 2D this produces
    /// a vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Cross(in Vector2 a, in float s)
    {
        return new(s * a.Y, -s * a.X);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static float Cross(Vector2 a, Vector2 b)
    {
        return a.X * b.Y - a.Y * b.X;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i Floored(this Vector2 vec)
    {
        return new Vector2i((int) MathF.Floor(vec.X), (int) MathF.Floor(vec.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2i Ceiled(this Vector2 vec)
    {
        return new Vector2i((int) MathF.Ceiling(vec.X), (int) MathF.Ceiling(vec.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Rounded(this Vector2 vec)
    {
        return new Vector2(MathF.Round(vec.X), MathF.Round(vec.Y));
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static void Deconstruct(this Vector2 vec, out float x, out float y)
    {
        x = vec.X;
        y = vec.Y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool EqualsApproxPercent(this Vector2 vec, Vector2 other, double tolerance = 0.0001)
    {
        return MathHelper.CloseToPercent(vec.X, other.X, tolerance) && MathHelper.CloseToPercent(vec.Y, other.Y, tolerance);
    }
}
