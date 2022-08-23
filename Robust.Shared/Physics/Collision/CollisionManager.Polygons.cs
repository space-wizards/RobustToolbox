using System;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Collision;

internal sealed partial class CollisionManager
{
    /// <summary>
    /// Find the max separation between poly1 and poly2 using edge normals from poly1.
    /// </summary>
    /// <param name="edgeIndex">Index of the edge.</param>
    /// <param name="poly1">The poly1.</param>
    /// <param name="xf1">The XF1.</param>
    /// <param name="poly2">The poly2.</param>
    /// <param name="xf2">The XF2.</param>
    /// <returns></returns>
    private static float FindMaxSeparation(out int edgeIndex, PolygonShape poly1, in Transform xf1,
        PolygonShape poly2, in Transform xf2)
    {
        // MIT License

        // Copyright (c) 2019 Erin Catto

        // Permission is hereby granted, free of charge, to any person obtaining a copy
        // of this software and associated documentation files (the "Software"), to deal
        // in the Software without restriction, including without limitation the rights
        // to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
        // copies of the Software, and to permit persons to whom the Software is
        // furnished to do so, subject to the following conditions:

        // The above copyright notice and this permission notice shall be included in all
        // copies or substantial portions of the Software.

        // THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
        // IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
        // FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
        // AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
        // LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
        // OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
        // SOFTWARE.

        var n1s = poly1.Normals;
        var v1s = poly1.Vertices;
        var v2s = poly2.Vertices;
        var count1 = v1s.Length;
        var count2 = v2s.Length;
        var xf = Transform.MulT(xf2, xf1);

        var bestIndex = 0;
        var maxSeparation = float.MinValue;

        for (var i = 0; i < count1; i++)
        {
            // Get poly1 normal in frame2.
            var n = Transform.Mul(xf.Quaternion2D, n1s[i]);
            var v1 = Transform.Mul(xf, v1s[i]);

            // Find deepest point for normal i.
            var si = float.MaxValue;
            for (var j = 0; j < count2; ++j)
            {
                var sij = Vector2.Dot(n, v2s[j] - v1);
                if (sij < si)
                {
                    si = sij;
                }
            }

            if (si > maxSeparation)
            {
                maxSeparation = si;
                bestIndex = i;
            }
        }

        edgeIndex = bestIndex;
        return maxSeparation;
    }

    private static void FindIncidentEdge(Span<ClipVertex> c, PolygonShape poly1, in Transform xf1, int edge1, PolygonShape poly2, in Transform xf2)
    {
        var normals1 = poly1.Normals;

        var count2 = poly2.Vertices.Length;
        var vertices2 = poly2.Vertices;
        var normals2 = poly2.Normals;

        DebugTools.Assert(0 <= edge1 && edge1 < poly1.Vertices.Length);

        // Get the normal of the reference edge in poly2's frame.
        var normal1 = Transform.MulT(xf2.Quaternion2D, Transform.Mul(xf1.Quaternion2D, normals1[edge1]));

        // Find the incident edge on poly2.
        var index = 0;
        var minDot = float.MaxValue;

        for (int i = 0; i < count2; ++i)
        {
            var dot = Vector2.Dot(normal1, normals2[i]);

            if (dot < minDot)
            {
                minDot = dot;
                index = i;
            }
        }

        // Build the clip vertices for the incident edge.
        var i1 = index;
        var i2 = i1 + 1 < count2 ? i1 + 1 : 0;

        ref var cv0 = ref c[0];

        cv0.V = Transform.Mul(xf2, vertices2[i1]);
        cv0.ID.Features.IndexA = (byte) edge1;
        cv0.ID.Features.IndexB = (byte) i1;
        cv0.ID.Features.TypeA = (byte) ContactFeatureType.Face;
        cv0.ID.Features.TypeB = (byte) ContactFeatureType.Vertex;

        ref var cv1 = ref c[1];
        cv1.V = Transform.Mul(xf2, vertices2[i2]);
        cv1.ID.Features.IndexA = (byte) edge1;
        cv1.ID.Features.IndexB = (byte) i2;
        cv1.ID.Features.TypeA = (byte) ContactFeatureType.Face;
        cv1.ID.Features.TypeB = (byte) ContactFeatureType.Vertex;
    }

    /// <summary>
    /// Compute the collision manifold between two polygons.
    /// </summary>
    /// <param name="manifold">The manifold.</param>
    /// <param name="polyA">The poly A.</param>
    /// <param name="transformA">The transform A.</param>
    /// <param name="polyB">The poly B.</param>
    /// <param name="transformB">The transform B.</param>
    public void CollidePolygons(ref Manifold manifold, PolygonShape polyA, in Transform transformA,
        PolygonShape polyB, in Transform transformB)
    {
        manifold.PointCount = 0;
        var totalRadius = polyA.Radius + polyB.Radius;

        var edgeA = 0;
        var separationA = FindMaxSeparation(out edgeA, polyA, transformA, polyB, transformB);

        if (separationA > totalRadius)
            return;

        int edgeB = 0;
        float separationB = FindMaxSeparation(out edgeB, polyB, transformB, polyA, transformA);
        if (separationB > totalRadius)
            return;

        PolygonShape poly1; // reference polygon
        PolygonShape poly2; // incident polygon
        Transform xf1, xf2;
        int edge1; // reference edge
        bool flip;
        const float k_relativeTol = 0.98f;
        const float k_absoluteTol = 0.001f;

        if (separationB > k_relativeTol * separationA + k_absoluteTol)
        {
            poly1 = polyB;
            poly2 = polyA;
            xf1 = transformB;
            xf2 = transformA;
            edge1 = edgeB;
            manifold.Type = ManifoldType.FaceB;
            flip = true;
        }
        else
        {
            poly1 = polyA;
            poly2 = polyB;
            xf1 = transformA;
            xf2 = transformB;
            edge1 = edgeA;
            manifold.Type = ManifoldType.FaceA;
            flip = false;
        }

        Span<ClipVertex> incidentEdge = stackalloc ClipVertex[2];

        FindIncidentEdge(incidentEdge, poly1, xf1, edge1, poly2, xf2);

        int count1 = poly1.Vertices.Length;

        int iv1 = edge1;
        int iv2 = edge1 + 1 < count1 ? edge1 + 1 : 0;

        Vector2 v11 = poly1.Vertices[iv1];
        Vector2 v12 = poly1.Vertices[iv2];

        Vector2 localTangent = v12 - v11;
        localTangent = localTangent.Normalized;

        Vector2 localNormal = new Vector2(localTangent.Y, -localTangent.X);
        Vector2 planePoint = (v11 + v12) * 0.5f;

        Vector2 tangent = Transform.Mul(xf1.Quaternion2D, localTangent);

        float normalX = tangent.Y;
        float normalY = -tangent.X;

        v11 = Transform.Mul(xf1, v11);
        v12 = Transform.Mul(xf1, v12);

        // Face offset.
        float frontOffset = normalX * v11.X + normalY * v11.Y;

        // Side offsets, extended by polytope skin thickness.
        float sideOffset1 = -(tangent.X * v11.X + tangent.Y * v11.Y) + totalRadius;
        float sideOffset2 = tangent.X * v12.X + tangent.Y * v12.Y + totalRadius;

        // Clip incident edge against extruded edge1 side edges.
        Span<ClipVertex> clipPoints1 = stackalloc ClipVertex[2];

        // Clip to box side 1
        int np = ClipSegmentToLine(clipPoints1, incidentEdge, -tangent, sideOffset1, iv1);

        if (np < 2)
            return;

        Span<ClipVertex> clipPoints2 = stackalloc ClipVertex[2];
        // Clip to negative box side 1
        np = ClipSegmentToLine(clipPoints2, clipPoints1, tangent, sideOffset2, iv2);

        if (np < 2)
        {
            return;
        }

        // Now clipPoints2 contains the clipped points.
        manifold.LocalNormal = localNormal;
        manifold.LocalPoint = planePoint;

        int pointCount = 0;
        for (int i = 0; i < 2; ++i)
        {
            Vector2 value = clipPoints2[i].V;
            float separation = normalX * value.X + normalY * value.Y - frontOffset;

            if (separation <= totalRadius)
            {
                ref var cp = ref manifold.Points[pointCount];
                cp.LocalPoint = Transform.MulT(xf2, clipPoints2[i].V);
                cp.Id = clipPoints2[i].ID;

                if (flip)
                {
                    // Swap features
                    ContactFeature cf = cp.Id.Features;
                    cp.Id.Features.IndexA = cf.IndexB;
                    cp.Id.Features.IndexB = cf.IndexA;
                    cp.Id.Features.TypeA = cf.TypeB;
                    cp.Id.Features.TypeB = cf.TypeA;
                }

                pointCount++;
            }
        }

        manifold.PointCount = pointCount;
    }
}
