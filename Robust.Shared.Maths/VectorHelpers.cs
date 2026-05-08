using System.Numerics;

namespace Robust.Shared.Maths;

public static class VectorHelpers
{
    public static Vector3 InterpolateCubic(Vector3 preA, Vector3 a, Vector3 b, Vector3 postB, float t)
    {
        return a + (b - preA + (preA * 2.0f - a * 5.0f + b * 4.0f - postB + ((a - b) * 3.0f + postB - preA) * t) * t) * t * 0.5f;
    }

    public static Vector4 InterpolateCubic(Vector4 preA, Vector4 a, Vector4 b, Vector4 postB, float t)
    {
        return a + (b - preA + (preA * 2.0f - a * 5.0f + b * 4.0f - postB + ((a - b) * 3.0f + postB - preA) * t) * t) * t * 0.5f;
    }

    public static void Deconstruct(this Vector3 vector, out float x, out float y, out float z)
    {
        x = vector.X;
        y = vector.Y;
        z = vector.Z;
    }

    public static void Deconstruct(this Vector4 vector, out float x, out float y, out float z, out float w)
    {
        x = vector.X;
        y = vector.Y;
        z = vector.Z;
        w = vector.W;
    }
}
