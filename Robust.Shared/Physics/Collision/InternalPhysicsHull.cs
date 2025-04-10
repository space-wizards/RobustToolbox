using System;
using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics;

/// <summary>
/// Convex hull used for poly collision.
/// </summary>
internal ref struct InternalPhysicsHull
{
    public Span<Vector2> Points;
    public int Count;

    internal InternalPhysicsHull(Span<Vector2> vertices, int count) : this()
    {
        Count = count;
        Points = vertices[..count];
    }

    private static InternalPhysicsHull RecurseHull(Vector2 p1, Vector2 p2, Span<Vector2> ps, int count)
    {
        InternalPhysicsHull hull = new()
        {
            Count = 0
        };

        if (count == 0)
        {
            return hull;
        }

        // create an edge vector pointing from p1 to p2
        var e = p2 - p1;
        e.Normalize();

        // discard points left of e and find point furthest to the right of e
        Span<Vector2> rightPoints = stackalloc Vector2[PhysicsConstants.MaxPolygonVertices];
        var rightCount = 0;

        var bestIndex = 0;
        float bestDistance = Vector2Helpers.Cross(ps[bestIndex] - p1, e);
        if (bestDistance > 0.0f)
        {
            rightPoints[rightCount++] = ps[bestIndex];
        }

        for (var i = 1; i < count; ++i)
        {
            float distance = Vector2Helpers.Cross(ps[i] - p1, e);
            if (distance > bestDistance)
            {
                bestIndex = i;
                bestDistance = distance;
            }

            if (distance > 0.0f)
            {
                rightPoints[rightCount++] = ps[i];
            }
        }

        if (bestDistance < 2.0f * PhysicsConstants.LinearSlop)
        {
            return hull;
        }

        hull.Points = new Vector2[PhysicsConstants.MaxPolygonVertices];
        var bestPoint = ps[bestIndex];

        // compute hull to the right of p1-bestPoint
        InternalPhysicsHull hull1 = RecurseHull(p1, bestPoint, rightPoints, rightCount);

        // compute hull to the right of bestPoint-p2
        InternalPhysicsHull hull2 = RecurseHull(bestPoint, p2, rightPoints, rightCount);

        // stich together hulls
        for (var i = 0; i < hull1.Count; ++i)
        {
            hull.Points[hull.Count++] = hull1.Points[i];
        }

        hull.Points[hull.Count++] = bestPoint;

        for (var i = 0; i < hull2.Count; ++i)
        {
            hull.Points[hull.Count++] = hull2.Points[i];
        }

        DebugTools.Assert(hull.Count < PhysicsConstants.MaxPolygonVertices);

        return hull;
    }

    // quickhull algorithm
    // - merges vertices based on b2_linearSlop
    // - removes collinear points using b2_linearSlop
    // - returns an empty hull if it fails
    public static InternalPhysicsHull ComputeHull(ReadOnlySpan<Vector2> points, int count)
    {
        InternalPhysicsHull hull = new();

        if (count is < 3 or > PhysicsConstants.MaxPolygonVertices)
        {
            hull.Count = 0;
            DebugTools.Assert(false);
		    // check your data
		    return hull;
	    }

	    count = Math.Min(count, PhysicsConstants.MaxPolygonVertices);

        Box2 aabb = new Box2(float.MaxValue, float.MaxValue, float.MinValue, float.MinValue);

	    // Perform aggressive point welding. First point always remains.
	    // Also compute the bounding box for later.
	    Span<Vector2> ps = stackalloc Vector2[PhysicsConstants.MaxPolygonVertices];
	    var n = 0;
	    const float tolSqr = 16.0f * PhysicsConstants.LinearSlop * PhysicsConstants.LinearSlop;
	    for (var i = 0; i < count; ++i)
	    {
		    aabb.BottomLeft = Vector2.Min(aabb.BottomLeft, points[i]);
		    aabb.TopRight = Vector2.Max(aabb.TopRight, points[i]);

		    var vi = points[i];

		    bool unique = true;
		    for (var j = 0; j < i; ++j)
		    {
			    var vj = points[j];

			    float distSqr = (vj - vi).LengthSquared();
			    if (distSqr < tolSqr)
			    {
				    unique = false;
				    break;
			    }
		    }

		    if (unique)
		    {
			    ps[n++] = vi;
		    }
	    }

	    if (n < 3)
	    {
		    // all points very close together, check your data and check your scale
		    return hull;
	    }

	    // Find an extreme point as the first point on the hull
	    var c = aabb.Center;
	    var i1 = 0;
        float dsq1 = (ps[i1] - c).LengthSquared();
	    for (var i = 1; i < n; ++i)
        {
            float dsq = (ps[i] - c).LengthSquared();
		    if (dsq > dsq1)
		    {
			    i1 = i;
			    dsq1 = dsq;
		    }
	    }

	    // remove p1 from working set
	    var p1 = ps[i1];
	    ps[i1] = ps[n - 1];
	    n = n - 1;

	    var i2 = 0;
        float dsq2 = (ps[i2] - p1).LengthSquared();
	    for (var i = 1; i < n; ++i)
        {
            float dsq = (ps[i] - p1).LengthSquared();
		    if (dsq > dsq2)
		    {
			    i2 = i;
			    dsq2 = dsq;
		    }
	    }

	    // remove p2 from working set
	    var p2 = ps[i2];
	    ps[i2] = ps[n - 1];
	    n = n - 1;

	    // split the points into points that are left and right of the line p1-p2.
	    Span<Vector2> rightPoints = stackalloc Vector2[PhysicsConstants.MaxPolygonVertices - 2];
	    var rightCount = 0;

	    Span<Vector2> leftPoints = stackalloc Vector2[PhysicsConstants.MaxPolygonVertices - 2];
	    var leftCount = 0;

	    var e = p2 - p1;
	    e.Normalize();

	    for (var i = 0; i < n; ++i)
	    {
		    float d = Vector2Helpers.Cross(ps[i] - p1, e);

		    // slop used here to skip points that are very close to the line p1-p2
		    if (d >= 2.0f * PhysicsConstants.LinearSlop)
		    {
			    rightPoints[rightCount++] = ps[i];
		    }
		    else if (d <= -2.0f * PhysicsConstants.LinearSlop)
		    {
			    leftPoints[leftCount++] = ps[i];
		    }
	    }

	    // compute hulls on right and left
	    var hull1 = RecurseHull(p1, p2, rightPoints, rightCount);
	    var hull2 = RecurseHull(p2, p1, leftPoints, leftCount);

	    if (hull1.Count == 0 && hull2.Count == 0)
        {
            hull.Count = 0;
		    // all points collinear
		    return hull;
	    }

        hull.Points = new Vector2[PhysicsConstants.MaxPolygonVertices];

	    // stitch hulls together, preserving CCW winding order
	    hull.Points[hull.Count++] = p1;

	    for (var i = 0; i < hull1.Count; ++i)
	    {
		    hull.Points[hull.Count++] = hull1.Points[i];
	    }

	    hull.Points[hull.Count++] = p2;

	    for (var i = 0; i < hull2.Count; ++i)
	    {
		    hull.Points[hull.Count++] = hull2.Points[i];
	    }

	    DebugTools.Assert(hull.Count <= PhysicsConstants.MaxPolygonVertices);

	    // merge collinear
	    bool searching = true;
	    while (searching && hull.Count > 2)
	    {
		    searching = false;

		    for (var i = 0; i < hull.Count; ++i)
		    {
			    i1 = i;
			    i2 = (i + 1) % hull.Count;
			    var i3 = (i + 2) % hull.Count;

			    p1 = hull.Points[i1];
			    p2 = hull.Points[i2];
			    var p3 = hull.Points[i3];

			    e = p3 - p1;
			    e.Normalize();

			    var v = p2 - p1;
			    float distance = Vector2Helpers.Cross(p2 - p1, e);
			    if (distance <= 2.0f * PhysicsConstants.LinearSlop)
			    {
				    // remove midpoint from hull
				    for (var j = i2; j < hull.Count - 1; ++j)
				    {
					    hull.Points[j] = hull.Points[j + 1];
				    }
				    hull.Count -= 1;

				    // continue searching for collinear points
				    searching = true;

				    break;
			    }
		    }
	    }

	    if (hull.Count < 3)
	    {
		    // all points collinear, shouldn't be reached since this was validated above
		    hull.Count = 0;
	    }

	    return hull;
    }

    public static bool ValidateHull(InternalPhysicsHull hull)
    {
        if (hull.Count < 3 || PhysicsConstants.MaxPolygonVertices < hull.Count)
        {
            return false;
        }

        // test that every point is behind every edge
        for (var i = 0; i < hull.Count; ++i)
        {
            // create an edge vector
            var i1 = i;
            var i2 = i < hull.Count - 1 ? i1 + 1 : 0;
            var p = hull.Points[i1];
            var e = hull.Points[i2] - p;
            e.Normalize();

            for (var j = 0; j < hull.Count; ++j)
            {
                // skip points that subtend the current edge
                if (j == i1 || j == i2)
                {
                    continue;
                }

                float distance = Vector2Helpers.Cross(hull.Points[j] - p, e);
                if (distance >= 0.0f)
                {
                    return false;
                }
            }
        }

        // test for collinear points
        for (var i = 0; i < hull.Count; ++i)
        {
            var i1 = i;
            var i2 = (i + 1) % hull.Count;
            var i3 = (i + 2) % hull.Count;

            var p1 = hull.Points[i1];
            var p2 = hull.Points[i2];
            var p3 = hull.Points[i3];

            var e = p3 - p1;
            e.Normalize();

            var v = p2 - p1;
            float distance = Vector2Helpers.Cross(p2 - p1, e);
            if (distance <= PhysicsConstants.LinearSlop)
            {
                // p1-p2-p3 are collinear
                return false;
            }
        }

        return true;
    }
}
