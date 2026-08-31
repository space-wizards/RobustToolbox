using System;
using System.Numerics;

namespace Robust.Shared.Maths;

/// <summary>
/// Shared conventions and operations for the engine's three-dimensional coordinate system.
/// </summary>
/// <remarks>
/// Robust uses a right-handed coordinate system: +X is east, +Y is north, and +Z is up.
/// Rotations are unit quaternions and transforms use the row-vector convention used by
/// <see cref="System.Numerics"/>.
/// </remarks>
public static class SpatialMath
{
    public const float QuaternionEqualityTolerance = 0.000001f;

    public static Quaternion Normalize(Quaternion rotation)
    {
        if (rotation.LengthSquared() < float.Epsilon)
            return Quaternion.Identity;

        return Quaternion.Normalize(rotation);
    }

    public static Quaternion FromYaw(Angle yaw)
    {
        return Quaternion.CreateFromAxisAngle(Vector3.UnitZ, (float) yaw.Theta);
    }

    public static Angle Yaw(this Quaternion rotation)
    {
        rotation = Normalize(rotation);

        var sin = 2f * (rotation.W * rotation.Z + rotation.X * rotation.Y);
        var cos = 1f - 2f * (rotation.Y * rotation.Y + rotation.Z * rotation.Z);
        return new Angle(MathF.Atan2(sin, cos));
    }

    public static bool EqualsApprox(this Quaternion left, Quaternion right, float tolerance = QuaternionEqualityTolerance)
    {
        left = Normalize(left);
        right = Normalize(right);
        return 1f - MathF.Abs(Quaternion.Dot(left, right)) <= tolerance;
    }

    public static bool EqualsApprox(this Vector3 left, Vector3 right, float tolerance = 0.00001f)
    {
        return Vector3.DistanceSquared(left, right) <= tolerance * tolerance;
    }

    public static Vector3 Rotate(this Quaternion rotation, Vector3 vector)
    {
        return Vector3.Transform(vector, Normalize(rotation));
    }

    public static Vector2 XY(this Vector3 vector)
    {
        return new Vector2(vector.X, vector.Y);
    }

    public static Vector3 WithZ(this Vector2 vector, float z = 0f)
    {
        return new Vector3(vector.X, vector.Y, z);
    }

    /// <summary>
    /// Composes a local orientation with its parent orientation.
    /// </summary>
    public static Quaternion Compose(Quaternion local, Quaternion parent)
    {
        return Normalize(Quaternion.Concatenate(local, parent));
    }

    /// <summary>
    /// Converts a world orientation into the local space of a parent orientation.
    /// </summary>
    public static Quaternion RelativeTo(Quaternion world, Quaternion parent)
    {
        return Normalize(Quaternion.Concatenate(world, Quaternion.Inverse(Normalize(parent))));
    }

    public static Matrix4x4 CreateTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        return Matrix4x4.CreateScale(scale)
               * Matrix4x4.CreateFromQuaternion(Normalize(rotation))
               * Matrix4x4.CreateTranslation(position);
    }

    public static Matrix4x4 CreateInverseTransform(Vector3 position, Quaternion rotation, Vector3 scale)
    {
        var inverseScale = new Vector3(1f / scale.X, 1f / scale.Y, 1f / scale.Z);
        return Matrix4x4.CreateTranslation(-position)
               * Matrix4x4.CreateFromQuaternion(Quaternion.Inverse(Normalize(rotation)))
               * Matrix4x4.CreateScale(inverseScale);
    }
}
