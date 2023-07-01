using System;
using System.Numerics;
using System.Runtime.CompilerServices;

namespace Robust.Shared.Maths;

public static class Vector2Helpers
{
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
    public static float Normalize(this Vector2 vec)
    {
        var length = vec.Length();

        if (length < float.Epsilon)
        {
            return 0f;
        }

        var invLength = 1f / length;
        vec.X *= invLength;
        vec.Y *= invLength;
        return length;
    }

    /// <summary>
    /// Perform the cross product on a scalar and a vector. In 2D this produces
    /// a vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Cross(float s, in Vector2 a)
    {
        return new Vector2(s * a.Y, -s * a.X);
    }

    /// <summary>
    /// Perform the cross product on a scalar and a vector. In 2D this produces
    /// a vector.
    /// </summary>
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Vector2 Cross(in Vector2 a, in float s)
    {
        return new Vector2(-s * a.Y, s * a.X);
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
}
