using System;
using System.Numerics;

namespace Robust.Shared.Maths;

/// <summary>
/// A normalized ray in three-dimensional space.
/// </summary>
public readonly record struct Ray3
{
    public readonly Vector3 Origin;
    public readonly Vector3 Direction;

    public Ray3(Vector3 origin, Vector3 direction)
    {
        if (!float.IsFinite(origin.X) || !float.IsFinite(origin.Y) || !float.IsFinite(origin.Z))
            throw new ArgumentOutOfRangeException(nameof(origin));
        if (!float.IsFinite(direction.X) || !float.IsFinite(direction.Y) || !float.IsFinite(direction.Z) ||
            direction.LengthSquared() < float.Epsilon)
            throw new ArgumentOutOfRangeException(nameof(direction));

        Origin = origin;
        Direction = Vector3.Normalize(direction);
    }

    public Vector3 GetPoint(float distance)
    {
        return Origin + Direction * distance;
    }

    /// <summary>
    /// Returns the nearest non-negative intersection distance with an axis-aligned box.
    /// </summary>
    public bool TryIntersect(Box3 bounds, out float distance)
    {
        var minDistance = 0f;
        var maxDistance = float.PositiveInfinity;

        if (!IntersectAxis(Origin.X, Direction.X, bounds.Min.X, bounds.Max.X, ref minDistance, ref maxDistance) ||
            !IntersectAxis(Origin.Y, Direction.Y, bounds.Min.Y, bounds.Max.Y, ref minDistance, ref maxDistance) ||
            !IntersectAxis(Origin.Z, Direction.Z, bounds.Min.Z, bounds.Max.Z, ref minDistance, ref maxDistance))
        {
            distance = 0f;
            return false;
        }

        distance = minDistance;
        return true;
    }

    private static bool IntersectAxis(
        float origin,
        float direction,
        float minimum,
        float maximum,
        ref float minDistance,
        ref float maxDistance)
    {
        if (MathF.Abs(direction) < 0.0000001f)
            return origin >= minimum && origin <= maximum;

        var inverse = 1f / direction;
        var near = (minimum - origin) * inverse;
        var far = (maximum - origin) * inverse;

        if (near > far)
            (near, far) = (far, near);

        minDistance = MathF.Max(minDistance, near);
        maxDistance = MathF.Min(maxDistance, far);
        return minDistance <= maxDistance;
    }
}
