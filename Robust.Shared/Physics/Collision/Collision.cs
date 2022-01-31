/*
* Farseer Physics Engine:
* Copyright (c) 2012 Ian Qvist
*
* Original source Box2D:
* Copyright (c) 2006-2011 Erin Catto http://www.box2d.org
*
* This software is provided 'as-is', without any express or implied
* warranty.  In no event will the authors be held liable for any damages
* arising from the use of this software.
* Permission is granted to anyone to use this software for any purpose,
* including commercial applications, and to alter it and redistribute it
* freely, subject to the following restrictions:
* 1. The origin of this software must not be misrepresented; you must not
* claim that you wrote the original software. If you use this software
* in a product, an acknowledgment in the product documentation would be
* appreciated but is not required.
* 2. Altered source versions must be plainly marked as such, and must not be
* misrepresented as being the original software.
* 3. This notice may not be removed or altered from any source distribution.
*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using Robust.Shared.Configuration;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Collision
{
    internal interface IManifoldManager
    {
        bool TestOverlap(IPhysShape shapeA, int indexA, IPhysShape shapeB, int indexB, in Transform xfA,
            in Transform xfB);

        void CollideCircles(ref Manifold manifold, PhysShapeCircle circleA, in Transform xfA,
            PhysShapeCircle circleB, in Transform xfB);

        void CollideEdgeAndCircle(ref Manifold manifold, EdgeShape edgeA, in Transform transformA,
            PhysShapeCircle circleB, in Transform transformB);

        void CollideEdgeAndPolygon(ref Manifold manifold, EdgeShape edgeA, in Transform xfA,
            PolygonShape polygonB, in Transform xfB);

        void CollidePolygonAndCircle(ref Manifold manifold, PolygonShape polygonA, in Transform xfA,
            PhysShapeCircle circleB, in Transform xfB);

        void CollidePolygons(ref Manifold manifold, PolygonShape polyA, in Transform transformA,
            PolygonShape polyB, in Transform transformB);

        void CollideAabbAndPolygon(ref Manifold manifold, PhysShapeAabb aabbA, in Transform transformA,
            PolygonShape polyB, in Transform transformB);

        void CollideAabbAndCircle(ref Manifold manifold, PhysShapeAabb aabbA, in Transform transformA,
            PhysShapeCircle circleB, in Transform transformB);

        void CollideAabbs(ref Manifold manifold, PhysShapeAabb aabbA, in Transform transformA,
            PhysShapeAabb aabbB, in Transform transformB);
    }

    /// <summary>
    ///     Handles several collision features: Generating contact manifolds, testing shape overlap,
    /// </summary>
    internal sealed class CollisionManager : IManifoldManager
    {
        /*
         * Farseer had this as a static class with a ThreadStatic DistanceInput
         */

        /// <summary>
        /// Test overlap between the two shapes.
        /// </summary>
        /// <param name="shapeA">The first shape.</param>
        /// <param name="indexA">The index for the first shape.</param>
        /// <param name="shapeB">The second shape.</param>
        /// <param name="indexB">The index for the second shape.</param>
        /// <param name="xfA">The transform for the first shape.</param>
        /// <param name="xfB">The transform for the seconds shape.</param>
        /// <returns></returns>
        bool IManifoldManager.TestOverlap(IPhysShape shapeA, int indexA, IPhysShape shapeB, int indexB,
            in Transform xfA, in Transform xfB)
        {
            // TODO: Make this a struct.
            var input = new DistanceInput();

            input.ProxyA.Set(shapeA, indexA);
            input.ProxyB.Set(shapeB, indexB);
            input.TransformA = xfA;
            input.TransformB = xfB;
            input.UseRadii = true;

            DistanceManager.ComputeDistance(out var output, out _, input);

            return output.Distance < 10.0f * float.Epsilon;
        }

        /// <summary>
        ///     Used for debugging contact points.
        /// </summary>
        /// <param name="state1"></param>
        /// <param name="state2"></param>
        /// <param name="manifold1"></param>
        /// <param name="manifold2"></param>
        public static void GetPointStates(ref PointState[] state1, ref PointState[] state2, in Manifold manifold1,
            in Manifold manifold2)
        {
            // Detect persists and removes.
            for (int i = 0; i < manifold1.PointCount; ++i)
            {
                ContactID id = manifold1.Points[i].Id;

                state1[i] = PointState.Remove;

                for (int j = 0; j < manifold2.PointCount; ++j)
                {
                    if (manifold2.Points[j].Id.Key == id.Key)
                    {
                        state1[i] = PointState.Persist;
                        break;
                    }
                }
            }

            // Detect persists and adds.
            for (int i = 0; i < manifold2.PointCount; ++i)
            {
                ContactID id = manifold2.Points[i].Id;

                state2[i] = PointState.Add;

                for (int j = 0; j < manifold1.PointCount; ++j)
                {
                    if (manifold1.Points[j].Id.Key == id.Key)
                    {
                        state2[i] = PointState.Persist;
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// Compute contact points for edge versus circle.
        /// This accounts for edge connectivity.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="edgeA">The edge A.</param>
        /// <param name="transformA">The transform A.</param>
        /// <param name="circleB">The circle B.</param>
        /// <param name="transformB">The transform B.</param>
        public void CollideEdgeAndCircle(ref Manifold manifold, EdgeShape edgeA, in Transform transformA,
            PhysShapeCircle circleB, in Transform transformB)
        {
            manifold.PointCount = 0;

            // Compute circle in frame of edge
            Vector2 Q = Transform.MulT(transformA, Transform.Mul(transformB, circleB.Position));

            Vector2 A = edgeA.Vertex1, B = edgeA.Vertex2;
            Vector2 e = B - A;

            // Barycentric coordinates
            float u = Vector2.Dot(e, B - Q);
            float v = Vector2.Dot(e, Q - A);

            float radius = edgeA.Radius + circleB.Radius;

            ContactFeature cf;
            cf.IndexB = 0;
            cf.TypeB = (byte)ContactFeatureType.Vertex;

            Vector2 P, d;

            // Region A
            /*
            if (v <= 0.0f)
            {
                P = A;
                d = Q - P;
                float dd = Vector2.Dot(d, d);
                if (dd > radius * radius)
                {
                    return;
                }


                // Is there an edge connected to A?
                if (edgeA.HasVertex0)
                {
                    Vector2 A1 = edgeA.Vertex0;
                    Vector2 B1 = A;
                    Vector2 e1 = B1 - A1;
                    float u1 = Vector2.Dot(e1, B1 - Q);

                    // Is the circle in Region AB of the previous edge?
                    if (u1 > 0.0f)
                    {
                        return;
                    }
                }

                cf.IndexA = 0;
                cf.TypeA = (byte)ContactFeatureType.Vertex;
                manifold.PointCount = 1;
                manifold.Type = ManifoldType.Circles;
                manifold.LocalNormal = Vector2.Zero;
                manifold.LocalPoint = P;
                ref var mp = ref manifold.Points[0];
                mp.Id.Key = 0;
                mp.Id.Features = cf;
                mp.LocalPoint = circleB.Position;
                return;
            }

            // Region B
            if (u <= 0.0f)
            {
                P = B;
                d = Q - P;
                float dd = Vector2.Dot(d, d);
                if (dd > radius * radius)
                {
                    return;
                }

                // Is there an edge connected to B?
                if (edgeA.HasVertex3)
                {
                    Vector2 B2 = edgeA.Vertex3;
                    Vector2 A2 = B;
                    Vector2 e2 = B2 - A2;
                    float v2 = Vector2.Dot(e2, Q - A2);

                    // Is the circle in Region AB of the next edge?
                    if (v2 > 0.0f)
                    {
                        return;
                    }
                }

                cf.IndexA = 1;
                cf.TypeA = (byte)ContactFeatureType.Vertex;
                manifold.PointCount = 1;
                manifold.Type = ManifoldType.Circles;
                manifold.LocalNormal = Vector2.Zero;
                manifold.LocalPoint = P;
                ref var mp = ref manifold.Points[0];
                mp.Id.Key = 0;
                mp.Id.Features = cf;
                mp.LocalPoint = circleB.Position;
                return;
            }
            */

            // Region AB
            float den = Vector2.Dot(e, e);
            DebugTools.Assert(den > 0.0f);
            P = (A * u + B * v) * (1.0f / den);
            d = Q - P;
            float dd2 = Vector2.Dot(d, d);
            if (dd2 > radius * radius)
            {
                return;
            }

            Vector2 n = new Vector2(-e.Y, e.X);
            if (Vector2.Dot(n, Q - A) < 0.0f)
            {
                n = new Vector2(-n.X, -n.Y);
            }

            n = n.Normalized;

            cf.IndexA = 0;
            cf.TypeA = (byte)ContactFeatureType.Face;
            manifold.PointCount = 1;
            manifold.Type = ManifoldType.FaceA;
            manifold.LocalNormal = n;
            manifold.LocalPoint = A;
            ref var mp2 = ref manifold.Points[0];
            mp2.Id.Key = 0;
            mp2.Id.Features = cf;
            mp2.LocalPoint = circleB.Position;
        }

        public void CollideCircles(ref Manifold manifold, PhysShapeCircle circleA, in Transform xfA,
            PhysShapeCircle circleB,
            in Transform xfB)
        {
            manifold.PointCount = 0;

            Vector2 pA = Transform.Mul(xfA, circleA.Position);
            Vector2 pB = Transform.Mul(xfB, circleB.Position);

            Vector2 d = pB - pA;
            float distSqr = Vector2.Dot(d, d);
            float radius = circleA.Radius + circleB.Radius;
            if (distSqr > radius * radius)
            {
                return;
            }

            manifold.Type = ManifoldType.Circles;
            manifold.LocalPoint = circleA.Position;
            manifold.LocalNormal = Vector2.Zero;
            manifold.PointCount = 1;

            ref var p0 = ref manifold.Points[0];

            p0.LocalPoint = Vector2.Zero; // Also here
            p0.Id.Key = 0;
        }

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
            edge1 = edge1.Normalized;

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
		        edge0 = edge0.Normalized;
		        var normal0 = new Vector2(edge0.Y, -edge0.X);
		        bool convex1 = Vector2.Cross(edge0, edge1) >= 0.0f;

		        var edge2 = edgeA.Vertex3 - v2;
		        edge2 = edge2.Normalized;
		        var normal2 = new Vector2(edge2.Y, -edge2.X);
		        bool convex2 = Vector2.Cross(edge1, edge2) >= 0.0f;

		        const float sinTol = 0.1f;
		        bool side1 = Vector2.Dot(primaryAxis.Normal, edge1) <= 0.0f;

		        // Check Gauss Map
		        if (side1)
		        {
			        if (convex1)
			        {
				        if (Vector2.Cross(primaryAxis.Normal, normal0) > sinTol)
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
				        if (Vector2.Cross(normal2, primaryAxis.Normal) > sinTol)
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
	        ReferenceFace refFace = new();

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
	        for (var i = 0; i < 2; ++i)
	        {
		        float separation;

		        separation = Vector2.Dot(refFace.normal, clipPoints2[i].V - refFace.v1);

		        if (separation <= radius)
		        {
			        ref var cp = ref manifold.Points[pointCount];

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

        /// <summary>
        /// Compute the collision manifold between a polygon and a circle.
        /// </summary>
        /// <param name="manifold">The manifold.</param>
        /// <param name="polygonA">The polygon A.</param>
        /// <param name="xfA">The transform of A.</param>
        /// <param name="circleB">The circle B.</param>
        /// <param name="xfB">The transform of B.</param>
        public void CollidePolygonAndCircle(ref Manifold manifold, PolygonShape polygonA, in Transform xfA,
            PhysShapeCircle circleB, in Transform xfB)
        {
            manifold.PointCount = 0;

            // Compute circle position in the frame of the polygon.
            Vector2 c = Transform.Mul(xfB, circleB.Position);
            Vector2 cLocal = Transform.MulT(xfA, c);

            // Find the min separating edge.
            int normalIndex = 0;
            float separation = float.MinValue;
            float radius = polygonA.Radius + circleB.Radius;
            int vertexCount = polygonA.Vertices.Length;

            for (int i = 0; i < vertexCount; ++i)
            {
                Vector2 value1 = polygonA.Normals[i];
                Vector2 value2 = cLocal - polygonA.Vertices[i];
                float s = value1.X * value2.X + value1.Y * value2.Y;

                if (s > radius)
                {
                    // Early out.
                    return;
                }

                if (s > separation)
                {
                    separation = s;
                    normalIndex = i;
                }
            }

            // Vertices that subtend the incident face.
            int vertIndex1 = normalIndex;
            int vertIndex2 = vertIndex1 + 1 < vertexCount ? vertIndex1 + 1 : 0;
            Vector2 v1 = polygonA.Vertices[vertIndex1];
            Vector2 v2 = polygonA.Vertices[vertIndex2];

            // If the center is inside the polygon ...
            if (separation < float.Epsilon)
            {
                manifold.PointCount = 1;
                manifold.Type = ManifoldType.FaceA;
                manifold.LocalNormal = polygonA.Normals[normalIndex];
                manifold.LocalPoint = (v1 + v2) * 0.5f;

                ref var p0 = ref manifold.Points[0];

                p0.LocalPoint = circleB.Position;
                p0.Id.Key = 0;

                return;
            }

            // Compute barycentric coordinates
            float u1 = (cLocal.X - v1.X) * (v2.X - v1.X) + (cLocal.Y - v1.Y) * (v2.Y - v1.Y);
            float u2 = (cLocal.X - v2.X) * (v1.X - v2.X) + (cLocal.Y - v2.Y) * (v1.Y - v2.Y);

            if (u1 <= 0.0f)
            {
                float r = (cLocal.X - v1.X) * (cLocal.X - v1.X) + (cLocal.Y - v1.Y) * (cLocal.Y - v1.Y);
                if (r > radius * radius)
                {
                    return;
                }

                manifold.PointCount = 1;
                manifold.Type = ManifoldType.FaceA;
                manifold.LocalNormal = cLocal - v1;
                float factor = 1f /
                               MathF.Sqrt(manifold.LocalNormal.X * manifold.LocalNormal.X +
                                          manifold.LocalNormal.Y * manifold.LocalNormal.Y);
                manifold.LocalNormal.X *= factor;
                manifold.LocalNormal.Y *= factor;
                manifold.LocalPoint = v1;

                ref var p0b = ref manifold.Points[0];

                p0b.LocalPoint = circleB.Position;
                p0b.Id.Key = 0;
            }
            else if (u2 <= 0.0f)
            {
                float r = (cLocal.X - v2.X) * (cLocal.X - v2.X) + (cLocal.Y - v2.Y) * (cLocal.Y - v2.Y);
                if (r > radius * radius)
                {
                    return;
                }

                manifold.PointCount = 1;
                manifold.Type = ManifoldType.FaceA;
                manifold.LocalNormal = cLocal - v2;
                float factor = 1f /
                               (float)
                               Math.Sqrt(manifold.LocalNormal.X * manifold.LocalNormal.X +
                                         manifold.LocalNormal.Y * manifold.LocalNormal.Y);
                manifold.LocalNormal.X *= factor;
                manifold.LocalNormal.Y *= factor;
                manifold.LocalPoint = v2;

                ref var p0c = ref manifold.Points[0];

                p0c.LocalPoint = circleB.Position;
                p0c.Id.Key = 0;
            }
            else
            {
                Vector2 faceCenter = (v1 + v2) * 0.5f;
                Vector2 value1 = cLocal - faceCenter;
                Vector2 value2 = polygonA.Normals[vertIndex1];
                float separation2 = value1.X * value2.X + value1.Y * value2.Y;
                if (separation2 > radius)
                {
                    return;
                }

                manifold.PointCount = 1;
                manifold.Type = ManifoldType.FaceA;
                manifold.LocalNormal = polygonA.Normals[vertIndex1];
                manifold.LocalPoint = faceCenter;

                ref var p0d = ref manifold.Points[0];

                p0d.LocalPoint = circleB.Position;
                p0d.Id.Key = 0;
            }
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

        // TODO: Uhh optimise these because holy fuck dey expensive. I didn't use for now because we can just convert to a poly quicker.
        // Probably copy Acruid's implementation though you need to make it return a box2d manifold instead.
        // Also I tried using the AABBandcircle one but it didn't seem to work well.
        public void CollideAabbAndPolygon(ref Manifold manifold, PhysShapeAabb aabbA, in Transform transformA, PolygonShape polyB,
            in Transform transformB)
        {
            CollidePolygons(ref manifold, (PolygonShape) aabbA, transformA, polyB, transformB);
        }

        public void CollideAabbAndCircle(ref Manifold manifold, PhysShapeAabb aabbA, in Transform transformA, PhysShapeCircle circleB,
            in Transform transformB)
        {
            // TODO: Either port Acruid's or use Randy Gaul's or something. Big gains
            CollidePolygonAndCircle(ref manifold, (PolygonShape) aabbA, transformA, circleB, transformB);
        }

        public void CollideAabbs(ref Manifold manifold, PhysShapeAabb aabbA, in Transform transformA, PhysShapeAabb aabbB,
            in Transform transformB)
        {
            CollidePolygons(ref manifold, (PolygonShape) aabbA, transformA, (PolygonShape) aabbB, transformB);
        }

        /// <summary>
        ///     Clipping for contact manifolds.
        /// </summary>
        /// <param name="vOut">The v out.</param>
        /// <param name="vIn">The v in.</param>
        /// <param name="normal">The normal.</param>
        /// <param name="offset">The offset.</param>
        /// <param name="vertexIndexA">The vertex index A.</param>
        /// <returns></returns>
        private static int ClipSegmentToLine(Span<ClipVertex> vOut, Span<ClipVertex> vIn, Vector2 normal,
            float offset, int vertexIndexA)
        {
            ClipVertex v0 = vIn[0];
            ClipVertex v1 = vIn[1];

            // Start with no output points
            int numOut = 0;

            // Calculate the distance of end points to the line
            float distance0 = normal.X * v0.V.X + normal.Y * v0.V.Y - offset;
            float distance1 = normal.X * v1.V.X + normal.Y * v1.V.Y - offset;

            // If the points are behind the plane
            if (distance0 <= 0.0f)
                vOut[numOut++] = v0;

            if (distance1 <= 0.0f)
                vOut[numOut++] = v1;

            // If the points are on different sides of the plane
            if (distance0 * distance1 < 0.0f)
            {
                // Find intersection point of edge and plane
                var interp = distance0 / (distance0 - distance1);

                ref var cv = ref vOut[numOut];

                cv.V.X = v0.V.X + interp * (v1.V.X - v0.V.X);
                cv.V.Y = v0.V.Y + interp * (v1.V.Y - v0.V.Y);

                // VertexA is hitting edgeB.
                cv.ID.Features.IndexA = (byte) vertexIndexA;
                cv.ID.Features.IndexB = v0.ID.Features.IndexB;
                cv.ID.Features.TypeA = (byte) ContactFeatureType.Vertex;
                cv.ID.Features.TypeB = (byte) ContactFeatureType.Face;

                ++numOut;
            }

            return numOut;
        }

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

            Debug.Assert(0 <= edge1 && edge1 < poly1.Vertices.Length);

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
    }

    /// <summary>
    /// This structure is used to keep track of the best separating axis.
    /// </summary>
    public struct EPAxis
    {
        public int Index;
        public float Separation;
        public EPAxisType Type;
        public Vector2 Normal;
    }

    /// <summary>
    /// Reference face used for clipping
    /// </summary>
    public struct ReferenceFace
    {
        public int i1, i2;

        public Vector2 v1, v2;

        public Vector2 normal;

        public Vector2 sideNormal1;
        public float sideOffset1;

        public Vector2 sideNormal2;
        public float sideOffset2;
    }

    public enum EPAxisType : byte
    {
        Unknown,
        EdgeA,
        EdgeB,
    }
}
