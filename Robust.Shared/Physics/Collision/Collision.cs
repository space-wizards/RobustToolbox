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
        [Dependency] private readonly IConfigurationManager _configManager = default!;

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
        public static void GetPointStates(out PointState[] state1, out PointState[] state2, in Manifold manifold1, in Manifold manifold2)
        {
            state1 = new PointState[2];
            state2 = new PointState[2];

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
            cf.TypeB = (byte) ContactFeatureType.Vertex;

            Vector2 P, d;

            // Region A
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
                cf.TypeA = (byte) ContactFeatureType.Vertex;
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
                cf.TypeA = (byte) ContactFeatureType.Vertex;
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
            cf.TypeA = (byte) ContactFeatureType.Face;
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
            PolygonShape polygonB,
            in Transform xfB)
        {
            EPCollider collider = new(_configManager);
            collider.Collide(ref manifold, edgeA, xfA, polygonB, xfB);
        }

        private class EPCollider
        {
            private float _polygonRadius;
            private float _angularSlop;

            private TempPolygon _polygonB;

            Transform _xf;
            Vector2 _centroidB;
            Vector2 _v0, _v1, _v2, _v3;
            Vector2 _normal0, _normal1, _normal2;
            Vector2 _normal;
            Vector2 _lowerLimit, _upperLimit;
            float _radius;
            bool _front;

            internal EPCollider(IConfigurationManager configManager)
            {
                _polygonRadius = PhysicsConstants.PolygonRadius;
                _angularSlop = configManager.GetCVar(CVars.AngularSlop);
                _polygonB = new TempPolygon(configManager);
            }

            public void Collide(ref Manifold manifold, EdgeShape edgeA, in Transform xfA, PolygonShape polygonB,
                in Transform xfB)
            {
                // Algorithm:
                // 1. Classify v1 and v2
                // 2. Classify polygon centroid as front or back
                // 3. Flip normal if necessary
                // 4. Initialize normal range to [-pi, pi] about face normal
                // 5. Adjust normal range according to adjacent edges
                // 6. Visit each separating axes, only accept axes within the range
                // 7. Return if _any_ axis indicates separation
                // 8. Clip

                _xf = Transform.MulT(xfA, xfB);

                _centroidB = Transform.Mul(_xf, polygonB.Centroid);

                _v0 = edgeA.Vertex0;
                _v1 = edgeA.Vertex1;
                _v2 = edgeA.Vertex2;
                _v3 = edgeA.Vertex3;

                bool hasVertex0 = edgeA.HasVertex0;
                bool hasVertex3 = edgeA.HasVertex3;

                Vector2 edge1 = _v2 - _v1;
                edge1 = edge1.Normalized;
                _normal1 = new Vector2(edge1.Y, -edge1.X);
                float offset1 = Vector2.Dot(_normal1, _centroidB - _v1);
                float offset0 = 0.0f, offset2 = 0.0f;
                bool convex1 = false, convex2 = false;

                // Is there a preceding edge?
                if (hasVertex0)
                {
                    Vector2 edge0 = _v1 - _v0;
                    edge0 = edge0.Normalized;
                    _normal0 = new Vector2(edge0.Y, -edge0.X);
                    convex1 = Vector2.Cross(edge0, edge1) >= 0.0f;
                    offset0 = Vector2.Dot(_normal0, _centroidB - _v0);
                }

                // Is there a following edge?
                if (hasVertex3)
                {
                    Vector2 edge2 = _v3 - _v2;
                    edge2 = edge2.Normalized;
                    _normal2 = new Vector2(edge2.Y, -edge2.X);
                    convex2 = Vector2.Cross(edge1, edge2) > 0.0f;
                    offset2 = Vector2.Dot(_normal2, _centroidB - _v2);
                }

                // Determine front or back collision. Determine collision normal limits.
                if (hasVertex0 && hasVertex3)
                {
                    if (convex1 && convex2)
                    {
                        _front = offset0 >= 0.0f || offset1 >= 0.0f || offset2 >= 0.0f;
                        if (_front)
                        {
                            _normal = _normal1;
                            _lowerLimit = _normal0;
                            _upperLimit = _normal2;
                        }
                        else
                        {
                            _normal = -_normal1;
                            _lowerLimit = -_normal1;
                            _upperLimit = -_normal1;
                        }
                    }
                    else if (convex1)
                    {
                        _front = offset0 >= 0.0f || (offset1 >= 0.0f && offset2 >= 0.0f);
                        if (_front)
                        {
                            _normal = _normal1;
                            _lowerLimit = _normal0;
                            _upperLimit = _normal1;
                        }
                        else
                        {
                            _normal = -_normal1;
                            _lowerLimit = -_normal2;
                            _upperLimit = -_normal1;
                        }
                    }
                    else if (convex2)
                    {
                        _front = offset2 >= 0.0f || (offset0 >= 0.0f && offset1 >= 0.0f);
                        if (_front)
                        {
                            _normal = _normal1;
                            _lowerLimit = _normal1;
                            _upperLimit = _normal2;
                        }
                        else
                        {
                            _normal = -_normal1;
                            _lowerLimit = -_normal1;
                            _upperLimit = -_normal0;
                        }
                    }
                    else
                    {
                        _front = offset0 >= 0.0f && offset1 >= 0.0f && offset2 >= 0.0f;
                        if (_front)
                        {
                            _normal = _normal1;
                            _lowerLimit = _normal1;
                            _upperLimit = _normal1;
                        }
                        else
                        {
                            _normal = -_normal1;
                            _lowerLimit = -_normal2;
                            _upperLimit = -_normal0;
                        }
                    }
                }
                else if (hasVertex0)
                {
                    if (convex1)
                    {
                        _front = offset0 >= 0.0f || offset1 >= 0.0f;
                        if (_front)
                        {
                            _normal = _normal1;
                            _lowerLimit = _normal0;
                            _upperLimit = -_normal1;
                        }
                        else
                        {
                            _normal = -_normal1;
                            _lowerLimit = _normal1;
                            _upperLimit = -_normal1;
                        }
                    }
                    else
                    {
                        _front = offset0 >= 0.0f && offset1 >= 0.0f;
                        if (_front)
                        {
                            _normal = _normal1;
                            _lowerLimit = _normal1;
                            _upperLimit = -_normal1;
                        }
                        else
                        {
                            _normal = -_normal1;
                            _lowerLimit = _normal1;
                            _upperLimit = -_normal0;
                        }
                    }
                }
                else if (hasVertex3)
                {
                    if (convex2)
                    {
                        _front = offset1 >= 0.0f || offset2 >= 0.0f;
                        if (_front)
                        {
                            _normal = _normal1;
                            _lowerLimit = -_normal1;
                            _upperLimit = _normal2;
                        }
                        else
                        {
                            _normal = -_normal1;
                            _lowerLimit = -_normal1;
                            _upperLimit = _normal1;
                        }
                    }
                    else
                    {
                        _front = offset1 >= 0.0f && offset2 >= 0.0f;
                        if (_front)
                        {
                            _normal = _normal1;
                            _lowerLimit = -_normal1;
                            _upperLimit = _normal1;
                        }
                        else
                        {
                            _normal = -_normal1;
                            _lowerLimit = -_normal2;
                            _upperLimit = _normal1;
                        }
                    }
                }
                else
                {
                    _front = offset1 >= 0.0f;
                    if (_front)
                    {
                        _normal = _normal1;
                        _lowerLimit = -_normal1;
                        _upperLimit = -_normal1;
                    }
                    else
                    {
                        _normal = -_normal1;
                        _lowerLimit = _normal1;
                        _upperLimit = _normal1;
                    }
                }

                // Get polygonB in frameA
                _polygonB.Count = polygonB.Vertices.Length;
                for (var i = 0; i < polygonB.Vertices.Length; ++i)
                {
                    _polygonB.Vertices[i] = Transform.Mul(_xf, polygonB.Vertices[i]);
                    _polygonB.Normals[i] = Transform.Mul(_xf.Quaternion2D, polygonB.Normals[i]);
                }

                _radius = 2.0f * _polygonRadius;

                manifold.PointCount = 0;

                EPAxis edgeAxis = ComputeEdgeSeparation();

                // If no valid normal can be found than this edge should not collide.
                if (edgeAxis.Type == EPAxisType.Unknown)
                {
                    return;
                }

                if (edgeAxis.Separation > _radius)
                {
                    return;
                }

                EPAxis polygonAxis = ComputePolygonSeparation();
                if (polygonAxis.Type != EPAxisType.Unknown && polygonAxis.Separation > _radius)
                {
                    return;
                }

                // Use hysteresis for jitter reduction.
                const float k_relativeTol = 0.98f;
                const float k_absoluteTol = 0.001f;

                EPAxis primaryAxis;
                if (polygonAxis.Type == EPAxisType.Unknown)
                {
                    primaryAxis = edgeAxis;
                }
                else if (polygonAxis.Separation > k_relativeTol * edgeAxis.Separation + k_absoluteTol)
                {
                    primaryAxis = polygonAxis;
                }
                else
                {
                    primaryAxis = edgeAxis;
                }

                Span<ClipVertex> ie = stackalloc ClipVertex[2];
                ReferenceFace rf;
                if (primaryAxis.Type == EPAxisType.EdgeA)
                {
                    manifold.Type = ManifoldType.FaceA;

                    // Search for the polygon normal that is most anti-parallel to the edge normal.
                    int bestIndex = 0;
                    float bestValue = Vector2.Dot(_normal, _polygonB.Normals[0]);
                    for (int i = 1; i < _polygonB.Count; ++i)
                    {
                        float value = Vector2.Dot(_normal, _polygonB.Normals[i]);
                        if (value < bestValue)
                        {
                            bestValue = value;
                            bestIndex = i;
                        }
                    }

                    int i1 = bestIndex;
                    int i2 = i1 + 1 < _polygonB.Count ? i1 + 1 : 0;

                    ref var c0 = ref ie[0];
                    c0.V = _polygonB.Vertices[i1];
                    c0.ID.Features.IndexA = 0;
                    c0.ID.Features.IndexB = (byte) i1;
                    c0.ID.Features.TypeA = (byte) ContactFeatureType.Face;
                    c0.ID.Features.TypeB = (byte) ContactFeatureType.Vertex;

                    ref var c1 = ref ie[1];
                    c1.V = _polygonB.Vertices[i2];
                    c1.ID.Features.IndexA = 0;
                    c1.ID.Features.IndexB = (byte) i2;
                    c1.ID.Features.TypeA = (byte) ContactFeatureType.Face;
                    c1.ID.Features.TypeB = (byte) ContactFeatureType.Vertex;

                    if (_front)
                    {
                        rf.i1 = 0;
                        rf.i2 = 1;
                        rf.v1 = _v1;
                        rf.v2 = _v2;
                        rf.normal = _normal1;
                    }
                    else
                    {
                        rf.i1 = 1;
                        rf.i2 = 0;
                        rf.v1 = _v2;
                        rf.v2 = _v1;
                        rf.normal = -_normal1;
                    }
                }
                else
                {
                    manifold.Type = ManifoldType.FaceB;
                    ref var c0 = ref ie[0];
                    c0.V = _v1;
                    c0.ID.Features.IndexA = 0;
                    c0.ID.Features.IndexB = (byte) primaryAxis.Index;
                    c0.ID.Features.TypeA = (byte) ContactFeatureType.Vertex;
                    c0.ID.Features.TypeB = (byte) ContactFeatureType.Face;

                    ref var c1 = ref ie[1];
                    c1.V = _v2;
                    c1.ID.Features.IndexA = 0;
                    c1.ID.Features.IndexB = (byte) primaryAxis.Index;
                    c1.ID.Features.TypeA = (byte) ContactFeatureType.Vertex;
                    c1.ID.Features.TypeB = (byte) ContactFeatureType.Face;

                    rf.i1 = primaryAxis.Index;
                    rf.i2 = rf.i1 + 1 < _polygonB.Count ? rf.i1 + 1 : 0;
                    rf.v1 = _polygonB.Vertices[rf.i1];
                    rf.v2 = _polygonB.Vertices[rf.i2];
                    rf.normal = _polygonB.Normals[rf.i1];
                }

                rf.sideNormal1 = new Vector2(rf.normal.Y, -rf.normal.X);
                rf.sideNormal2 = -rf.sideNormal1;
                rf.sideOffset1 = Vector2.Dot(rf.sideNormal1, rf.v1);
                rf.sideOffset2 = Vector2.Dot(rf.sideNormal2, rf.v2);

                // Clip incident edge against extruded edge1 side edges.
                Span<ClipVertex> clipPoints1 = stackalloc ClipVertex[2];

                // Clip to box side 1
                var np = ClipSegmentToLine(clipPoints1, ie, rf.sideNormal1, rf.sideOffset1, rf.i1);

                if (np < 2)
                {
                    return;
                }

                Span<ClipVertex> clipPoints2 = stackalloc ClipVertex[2];
                // Clip to negative box side 1
                np = ClipSegmentToLine(clipPoints2, clipPoints1, rf.sideNormal2, rf.sideOffset2, rf.i2);

                // TODO: Max manifold points
                if (np < 2)
                {
                    return;
                }

                // Now clipPoints2 contains the clipped points.
                if (primaryAxis.Type == EPAxisType.EdgeA)
                {
                    manifold.LocalNormal = rf.normal;
                    manifold.LocalPoint = rf.v1;
                }
                else
                {
                    manifold.LocalNormal = polygonB.Normals[rf.i1];
                    manifold.LocalPoint = polygonB.Vertices[rf.i1];
                }

                int pointCount = 0;

                for (int i = 0; i < 2; ++i)
                {
                    float separation = Vector2.Dot(rf.normal, clipPoints2[i].V - rf.v1);

                    if (separation <= _radius)
                    {
                        ref var cp = ref manifold.Points[pointCount];

                        if (primaryAxis.Type == EPAxisType.EdgeA)
                        {
                            cp.LocalPoint = Transform.MulT(_xf, clipPoints2[i].V);
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

            private EPAxis ComputeEdgeSeparation()
            {
                EPAxis axis;
                axis.Type = EPAxisType.EdgeA;
                axis.Index = _front ? 0 : 1;
                axis.Separation = float.MaxValue;

                for (int i = 0; i < _polygonB.Count; ++i)
                {
                    float s = Vector2.Dot(_normal, _polygonB.Vertices[i] - _v1);
                    if (s < axis.Separation)
                    {
                        axis.Separation = s;
                    }
                }

                return axis;
            }

            private EPAxis ComputePolygonSeparation()
            {
                EPAxis axis;
                axis.Type = EPAxisType.Unknown;
                axis.Index = -1;
                axis.Separation = float.MinValue;

                Vector2 perp = new Vector2(-_normal.Y, _normal.X);

                for (int i = 0; i < _polygonB.Count; ++i)
                {
                    Vector2 n = -_polygonB.Normals[i];

                    float s1 = Vector2.Dot(n, _polygonB.Vertices[i] - _v1);
                    float s2 = Vector2.Dot(n, _polygonB.Vertices[i] - _v2);
                    float s = Math.Min(s1, s2);

                    if (s > _radius)
                    {
                        // No collision
                        axis.Type = EPAxisType.EdgeB;
                        axis.Index = i;
                        axis.Separation = s;
                        return axis;
                    }

                    // Adjacency
                    if (Vector2.Dot(n, perp) >= 0.0f)
                    {
                        if (Vector2.Dot(n - _upperLimit, _normal) < -_angularSlop)
                        {
                            continue;
                        }
                    }
                    else
                    {
                        if (Vector2.Dot(n - _lowerLimit, _normal) < -_angularSlop)
                        {
                            continue;
                        }
                    }

                    if (s > axis.Separation)
                    {
                        axis.Type = EPAxisType.EdgeB;
                        axis.Index = i;
                        axis.Separation = s;
                    }
                }

                return axis;
            }
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
                manifold.LocalNormal.X = manifold.LocalNormal.X * factor;
                manifold.LocalNormal.Y = manifold.LocalNormal.Y * factor;
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
                manifold.LocalNormal.X = manifold.LocalNormal.X * factor;
                manifold.LocalNormal.Y = manifold.LocalNormal.Y * factor;
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
    /// This holds polygon B expressed in frame A.
    /// </summary>
    public class TempPolygon
    {
        public Vector2[] Vertices;
        public Vector2[] Normals;
        public int Count;

        public TempPolygon(IConfigurationManager configManager)
        {
            var maxVerts = configManager.GetCVar(CVars.MaxPolygonVertices);
            Vertices = new Vector2[maxVerts];
            Normals = new Vector2[maxVerts];
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
