using System.Numerics;

namespace Robust.Shared.Maths;

/// <summary>
/// A complete three-dimensional local transform.
/// </summary>
public readonly record struct SpatialTransform(Vector3 Position, Quaternion Rotation, Vector3 Scale)
{
    public static readonly SpatialTransform Identity = new(Vector3.Zero, Quaternion.Identity, Vector3.One);

    public Matrix4x4 Matrix => SpatialMath.CreateTransform(Position, Rotation, Scale);

    public Matrix4x4 InverseMatrix => SpatialMath.CreateInverseTransform(Position, Rotation, Scale);

    public Vector3 TransformPoint(Vector3 point)
    {
        return Vector3.Transform(point, Matrix);
    }

    public Vector3 InverseTransformPoint(Vector3 point)
    {
        return Vector3.Transform(point, InverseMatrix);
    }
}
