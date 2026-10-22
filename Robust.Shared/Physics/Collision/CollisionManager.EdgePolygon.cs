using System;
using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Collision;

internal sealed partial class CollisionManager
{
    /// <summary>
    ///     Collides and edge and a polygon, taking into account edge adjacency.
    /// </summary>
    /// <param name="manifold">The manifold.</param>
    /// <param name="edgeA">The edge A.</param>
    /// <param name="xfA">The xf A.</param>
    /// <param name="polygonB">The polygon B.</param>
    /// <param name="xfB">The xf B.</param>
    public void CollideEdgeAndPolygon(ref Manifold manifold, EdgeShape edgeA, in Transform xfA,
        PolygonShape polygonB, in Transform xfB)
    {
        manifold.PointCount = 0;

	    var xf = Transform.MulT(xfA, xfB);

	    var centroidB = Transform.Mul(xf, polygonB.Centroid);

	    var v1 = edgeA.Vertex1;
	    var v2 = edgeA.Vertex2;

	    var edge1 = v2 - v1;
        edge1 = edge1.Normalized();

	    // Normal points to the right for a CCW winding
	    var normal1 = new Vector2(edge1.Y, -edge1.X);
	    float offset1 = Vector2.Dot(normal1, centroidB - v1);

	    bool oneSided = edgeA.OneSided;
	    if (oneSided && offset1 < 0.0f)
            return;

        // Get polygonB in frameA
        var tempPolyCount = polygonB.VertexCount;
        // Can't use Spans because these may get attached to the manifold.
        var tempPolyVerts = new Vector2[tempPolyCount];
        var tempPolyNorms = new Vector2[tempPolyCount];

        for (var i = 0; i < tempPolyCount; ++i)
	    {
		    tempPolyVerts[i] = Transform.Mul(xf, polygonB.Vertices[i]);
		    tempPolyNorms[i] = Transform.Mul(xf.Quaternion2D, polygonB.Normals[i]);
	    }

        DebugTools.Assert(tempPolyVerts.Length == tempPolyCount);
	    float radius = polygonB.Radius + edgeA.Radius;

	    EPAxis edgeAxis = ComputeEdgeSeparation(tempPolyVerts, v1, normal1);

        if (edgeAxis.Separation > radius)
            return;

        var polygonAxis = ComputePolygonSeparation(tempPolyVerts, tempPolyNorms, v1, v2);
	    if (polygonAxis.Separation > radius)
            return;

        // Use hysteresis for jitter reduction.
	    const float k_relativeTol = 0.98f;
	    const float k_absoluteTol = 0.001f;

	    EPAxis primaryAxis;

        if (polygonAxis.Separation - radius > k_relativeTol * (edgeAxis.Separation - radius) + k_absoluteTol)
	    {
		    primaryAxis = polygonAxis;
	    }
	    else
	    {
		    primaryAxis = edgeAxis;
	    }

	    if (oneSided)
	    {
		    // Smooth collision
		    // See https://box2d.org/posts/2020/06/ghost-collisions/

		    var edge0 = v1 - edgeA.Vertex0;
		    edge0 = edge0.Normalized();
		    var normal0 = new Vector2(edge0.Y, -edge0.X);
		    bool convex1 = Vector2Helpers.Cross(edge0, edge1) >= 0.0f;

		    var edge2 = edgeA.Vertex3 - v2;
		    edge2 = edge2.Normalized();
		    var normal2 = new Vector2(edge2.Y, -edge2.X);
		    bool convex2 = Vector2Helpers.Cross(edge1, edge2) >= 0.0f;

		    const float sinTol = 0.1f;
		    bool side1 = Vector2.Dot(primaryAxis.Normal, edge1) <= 0.0f;

		    // Check Gauss Map
		    if (side1)
		    {
			    if (convex1)
			    {
				    if (Vector2Helpers.Cross(primaryAxis.Normal, normal0) > sinTol)
				    {
					    // Skip region
					    return;
				    }

				    // Admit region
			    }
			    else
			    {
				    // Snap region
				    primaryAxis = edgeAxis;
			    }
		    }
		    else
		    {
			    if (convex2)
			    {
				    if (Vector2Helpers.Cross(normal2, primaryAxis.Normal) > sinTol)
				    {
					    // Skip region
					    return;
				    }

				    // Admit region
			    }
			    else
			    {
				    // Snap region
				    primaryAxis = edgeAxis;
			    }
		    }
	    }

	    Span<ClipVertex> clipPoints = stackalloc ClipVertex[2];
        ReferenceFace refFace;

	    if (primaryAxis.Type == EPAxisType.EdgeA)
	    {
		    manifold.Type = ManifoldType.FaceA;

		    // Search for the polygon normal that is most anti-parallel to the edge normal.
		    var bestIndex = 0;
		    float bestValue = Vector2.Dot(primaryAxis.Normal, tempPolyNorms[0]);
		    for (var i = 1; i < tempPolyVerts.Length; ++i)
		    {
			    float value = Vector2.Dot(primaryAxis.Normal, tempPolyNorms[i]);
			    if (value < bestValue)
			    {
				    bestValue = value;
				    bestIndex = i;
			    }
		    }

		    var i1 = bestIndex;
		    var i2 = i1 + 1 < tempPolyVerts.Length ? i1 + 1 : 0;

		    clipPoints[0].V = tempPolyVerts[i1];
		    clipPoints[0].ID.Features.IndexA = 0;
		    clipPoints[0].ID.Features.IndexB = (byte)i1;
		    clipPoints[0].ID.Features.TypeA = (byte) ContactFeatureType.Face;
		    clipPoints[0].ID.Features.TypeB = (byte) ContactFeatureType.Vertex;

		    clipPoints[1].V = tempPolyVerts[i2];
		    clipPoints[1].ID.Features.IndexA = 0;
		    clipPoints[1].ID.Features.IndexB = (byte) i2;
		    clipPoints[1].ID.Features.TypeA = (byte) ContactFeatureType.Face;
		    clipPoints[1].ID.Features.TypeB = (byte) ContactFeatureType.Vertex;

		    refFace.i1 = 0;
            refFace.i2 = 1;
            refFace.v1 = v1;
            refFace.v2 = v2;
            refFace.normal = primaryAxis.Normal;
            refFace.sideNormal1 = -edge1;
            refFace.sideNormal2 = edge1;
	    }
	    else
	    {
		    manifold.Type = ManifoldType.FaceB;

		    clipPoints[0].V = v2;
		    clipPoints[0].ID.Features.IndexA = 1;
		    clipPoints[0].ID.Features.IndexB = (byte) primaryAxis.Index;
		    clipPoints[0].ID.Features.TypeA = (byte) ContactFeatureType.Vertex;
		    clipPoints[0].ID.Features.TypeB = (byte) ContactFeatureType.Face;

		    clipPoints[1].V = v1;
		    clipPoints[1].ID.Features.IndexA = 0;
		    clipPoints[1].ID.Features.IndexB = (byte) primaryAxis.Index;
		    clipPoints[1].ID.Features.TypeA = (byte) ContactFeatureType.Vertex;
		    clipPoints[1].ID.Features.TypeB = (byte) ContactFeatureType.Face;

		    refFace.i1 = primaryAxis.Index;
            refFace.i2 = refFace.i1 + 1 < tempPolyCount ? refFace.i1 + 1 : 0;
            refFace.v1 = tempPolyVerts[refFace.i1];
            refFace.v2 = tempPolyVerts[refFace.i2];
            refFace.normal = tempPolyNorms[refFace.i1];

		    // CCW winding
            refFace.sideNormal1 = new Vector2(refFace.normal.Y, -refFace.normal.X);
            refFace.sideNormal2 = -refFace.sideNormal1;
	    }

        refFace.sideOffset1 = Vector2.Dot(refFace.sideNormal1, refFace.v1);
        refFace.sideOffset2 = Vector2.Dot(refFace.sideNormal2, refFace.v2);

	    // Clip incident edge against reference face side planes
	    Span<ClipVertex> clipPoints1 = stackalloc ClipVertex[2];
	    Span<ClipVertex> clipPoints2 = stackalloc ClipVertex[2];
	    int np;

	    // Clip to side 1
	    np = ClipSegmentToLine(clipPoints1, clipPoints, refFace.sideNormal1, refFace.sideOffset1, refFace.i1);

	    if (np < 2)
            return;

        // Clip to side 2
	    np = ClipSegmentToLine(clipPoints2, clipPoints1, refFace.sideNormal2, refFace.sideOffset2, refFace.i2);

	    if (np < 2)
            return;

        // Now clipPoints2 contains the clipped points.
	    if (primaryAxis.Type == EPAxisType.EdgeA)
	    {
		    manifold.LocalNormal = refFace.normal;
		    manifold.LocalPoint = refFace.v1;
	    }
	    else
	    {
		    manifold.LocalNormal = tempPolyNorms[refFace.i1];
		    manifold.LocalPoint = tempPolyVerts[refFace.i1];
	    }

	    var pointCount = 0;
        var points = manifold.Points.AsSpan;

	    for (var i = 0; i < 2; ++i)
	    {
            var separation = Vector2.Dot(refFace.normal, clipPoints2[i].V - refFace.v1);

		    if (separation <= radius)
		    {
			    ref var cp = ref points[pointCount];

			    if (primaryAxis.Type == EPAxisType.EdgeA)
			    {
				    cp.LocalPoint = Transform.MulT(xf, clipPoints2[i].V);
				    cp.Id = clipPoints2[i].ID;
			    }
			    else
			    {
				    cp.LocalPoint = clipPoints2[i].V;
				    cp.Id.Features.TypeA = clipPoints2[i].ID.Features.TypeB;
				    cp.Id.Features.TypeB = clipPoints2[i].ID.Features.TypeA;
				    cp.Id.Features.IndexA = clipPoints2[i].ID.Features.IndexB;
				    cp.Id.Features.IndexB = clipPoints2[i].ID.Features.IndexA;
			    }

			    ++pointCount;
		    }
	    }

	    manifold.PointCount = pointCount;

    }

    private static EPAxis ComputeEdgeSeparation(Span<Vector2> tempPolyVerts, Vector2 v1, Vector2 normal1)
    {
        EPAxis axis = new()
        {
            Type = EPAxisType.EdgeA,
            Index = -1,
            Separation = float.MinValue,
            Normal = Vector2.Zero
        };

        Span<Vector2> axes = stackalloc Vector2[2] { normal1, -normal1 };

        // Find axis with least overlap (min-max problem)
        for (var j = 0; j < 2; ++j)
        {
            float sj = float.MaxValue;

            // Find deepest polygon vertex along axis j
            for (var i = 0; i < tempPolyVerts.Length; ++i)
            {
                float si = Vector2.Dot(axes[j], tempPolyVerts[i] - v1);
                if (si < sj)
                {
                    sj = si;
                }
            }

            if (sj > axis.Separation)
            {
                axis.Index = j;
                axis.Separation = sj;
                axis.Normal = axes[j];
            }
        }

        return axis;
    }

    private EPAxis ComputePolygonSeparation(Span<Vector2> tempPolyVerts, Span<Vector2> tempPolyNorms, Vector2 v1,
        Vector2 v2)
    {
        EPAxis axis = new()
        {
            Type = EPAxisType.Unknown,
            Index = -1,
            Separation = float.MinValue,
            Normal = Vector2.Zero
        };

        for (var i = 0; i < tempPolyVerts.Length; ++i)
        {
            var n = -tempPolyNorms[i];

            float s1 = Vector2.Dot(n, tempPolyVerts[i] - v1);
            float s2 = Vector2.Dot(n, tempPolyVerts[i] - v2);
            float s = MathF.Min(s1, s2);

            if (s > axis.Separation)
            {
                axis.Type = EPAxisType.EdgeB;
                axis.Index = i;
                axis.Separation = s;
                axis.Normal = n;
            }
        }

        return axis;
    }
}
