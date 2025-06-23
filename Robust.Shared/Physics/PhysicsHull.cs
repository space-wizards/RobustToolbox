using System;
using System.Numerics;

namespace Robust.Shared.Physics;

public struct PhysicsHull
{
    public static Span<Vector2> ComputePoints(ReadOnlySpan<Vector2> points, int count)
    {
        var hull = InternalPhysicsHull.ComputeHull(points, count);
        return hull.Points;
    }
}
