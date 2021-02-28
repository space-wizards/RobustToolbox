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
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Collision
{
    /// <summary>
    /// A distance proxy is used by the GJK algorithm.
    /// It encapsulates any shape.
    /// </summary>
    internal sealed class DistanceProxy
    {
        internal float Radius;
        internal List<Vector2> Vertices = new();

        // GJK using Voronoi regions (Christer Ericson) and Barycentric coordinates.

        /// <summary>
        /// Initialize the proxy using the given shape. The shape
        /// must remain in scope while the proxy is in use.
        /// </summary>
        /// <param name="shape">The shape.</param>
        /// <param name="index">The index.</param>
        public void Set(IPhysShape shape, int index)
        {
            Vertices.Clear();

            switch (shape.ShapeType)
            {
                case ShapeType.Aabb:
                    var aabb = (PhysShapeAabb) shape;
                    var bounds = aabb.LocalBounds;

                    Vertices.Add(bounds.BottomRight);
                    Vertices.Add(bounds.TopRight);
                    Vertices.Add(bounds.TopLeft);
                    Vertices.Add(bounds.BottomLeft);

                    Radius = aabb.Radius;

                    break;
                case ShapeType.Circle:
                    PhysShapeCircle circle = (PhysShapeCircle) shape;
                    // TODO: Circle's position offset to entity, someday.
                    Vertices.Add(Vector2.Zero);
                    Radius = circle.Radius;
                    break;

                case ShapeType.Polygon:
                    // TODO: When manifold building gets fixed replace it back with a cast.
                    var polygon = new PolygonShape(shape);

                    foreach (var vert in polygon.Vertices)
                    {
                        Vertices.Add(vert);
                    }

                    Radius = polygon.Radius;
                    break;

                case ShapeType.Chain:
                    throw new NotImplementedException();
                    /*
                    ChainShape chain = (ChainShape) shape;
                    Debug.Assert(0 <= index && index < chain.Vertices.Count);
                    Vertices.Clear();
                    Vertices.Add(chain.Vertices[index]);
                    Vertices.Add(index + 1 < chain.Vertices.Count ? chain.Vertices[index + 1] : chain.Vertices[0]);

                    Radius = chain.Radius;
                    */
                    break;

                case ShapeType.Edge:
                    EdgeShape edge = (EdgeShape) shape;

                    Vertices.Add(edge.Vertex1);
                    Vertices.Add(edge.Vertex2);

                    Radius = edge.Radius;
                    break;
                case ShapeType.Rectangle:
                    var rectangle = (PhysShapeRect) shape;
                    var calcedBounds = rectangle.CachedBounds;

                    Vertices.Add(calcedBounds.BottomRight);
                    Vertices.Add(calcedBounds.TopRight);
                    Vertices.Add(calcedBounds.TopLeft);
                    Vertices.Add(calcedBounds.BottomLeft);

                    Radius = rectangle.Radius;
                    break;
                default:
                    throw new InvalidOperationException($"Invalid shapetype specified {shape.ShapeType}");
            }
        }

        /// <summary>
        /// Get the supporting vertex index in the given direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public int GetSupport(Vector2 direction)
        {
            int bestIndex = 0;
            float bestValue = Vector2.Dot(Vertices[0], direction);
            for (int i = 1; i < Vertices.Count; ++i)
            {
                float value = Vector2.Dot(Vertices[i], direction);
                if (value > bestValue)
                {
                    bestIndex = i;
                    bestValue = value;
                }
            }

            return bestIndex;
        }

        /// <summary>
        /// Get the supporting vertex in the given direction.
        /// </summary>
        /// <param name="direction">The direction.</param>
        /// <returns></returns>
        public Vector2 GetSupportVertex(Vector2 direction)
        {
            int bestIndex = 0;
            float bestValue = Vector2.Dot(Vertices[0], direction);
            for (int i = 1; i < Vertices.Count; ++i)
            {
                float value = Vector2.Dot(Vertices[i], direction);
                if (value > bestValue)
                {
                    bestIndex = i;
                    bestValue = value;
                }
            }

            return Vertices[bestIndex];
        }
    }

    /// <summary>
    /// Output for Distance.ComputeDistance().
    /// </summary>
    public struct DistanceOutput
    {
        public float Distance;

        /// <summary>
        /// Number of GJK iterations used
        /// </summary>
        public int Iterations;

        /// <summary>
        /// Closest point on shapeA
        /// </summary>
        public Vector2 PointA;

        /// <summary>
        /// Closest point on shapeB
        /// </summary>
        public Vector2 PointB;
    }

    internal struct SimplexVertex
    {
        /// <summary>
        /// Barycentric coordinate for closest point
        /// </summary>
        public float A;

        /// <summary>
        /// wA index
        /// </summary>
        public int IndexA;

        /// <summary>
        /// wB index
        /// </summary>
        public int IndexB;

        /// <summary>
        /// wB - wA
        /// </summary>
        public Vector2 W;

        /// <summary>
        /// Support point in proxyA
        /// </summary>
        public Vector2 WA;

        /// <summary>
        /// Support point in proxyB
        /// </summary>
        public Vector2 WB;
    }

    internal class Simplex
    {
        // Made it a class from a struct as it seemed silly to be a struct considering it's being mutated constantly.

        internal int Count;
        internal readonly SimplexVertex[] V = new SimplexVertex[3];

        internal void ReadCache(ref SimplexCache cache, DistanceProxy proxyA, ref Transform transformA, DistanceProxy proxyB, ref Transform transformB)
        {
            DebugTools.Assert(cache.Count <= 3);

            // Copy data from cache.
            Count = cache.Count;
            for (int i = 0; i < Count; ++i)
            {
                SimplexVertex v = V[i];
                unsafe
                {
                    v.IndexA = cache.IndexA[i];
                    v.IndexB = cache.IndexB[i];
                }

                Vector2 wALocal = proxyA.Vertices[v.IndexA];
                Vector2 wBLocal = proxyB.Vertices[v.IndexB];
                v.WA = Transform.Mul(transformA, wALocal);
                v.WB = Transform.Mul(transformB, wBLocal);
                v.W = v.WB - v.WA;
                v.A = 0.0f;
                V[i] = v;
            }

            // Compute the new simplex metric, if it is substantially different than
            // old metric then flush the simplex.
            if (Count > 1)
            {
                float metric1 = cache.Metric;
                float metric2 = GetMetric();
                if (metric2 < 0.5f * metric1 || 2.0f * metric1 < metric2 || metric2 < float.Epsilon)
                {
                    // Reset the simplex.
                    Count = 0;
                }
            }

            // If the cache is empty or invalid ...
            if (Count == 0)
            {
                SimplexVertex v = V[0];
                v.IndexA = 0;
                v.IndexB = 0;
                Vector2 wALocal = proxyA.Vertices[0];
                Vector2 wBLocal = proxyB.Vertices[0];
                v.WA = Transform.Mul(transformA, wALocal);
                v.WB = Transform.Mul(transformB, wBLocal);
                v.W = v.WB - v.WA;
                v.A = 1.0f;
                V[0] = v;
                Count = 1;
            }
        }

        internal void WriteCache(ref SimplexCache cache)
        {
            cache.Metric = GetMetric();
            cache.Count = (UInt16)Count;
            for (var i = 0; i < Count; ++i)
            {
                unsafe
                {
                    cache.IndexA[i] = (byte) (V[i].IndexA);
                    cache.IndexB[i] = (byte) (V[i].IndexB);
                }
            }
        }

        internal Vector2 GetSearchDirection()
        {
            switch (Count)
            {
                case 1:
                    return -V[0].W;

                case 2:
                    {
                        Vector2 e12 = V[1].W - V[0].W;
                        float sgn = Vector2.Cross(e12, -V[0].W);
                        if (sgn > 0.0f)
                        {
                            // Origin is left of e12.
                            return new Vector2(-e12.Y, e12.X);
                        }
                        else
                        {
                            // Origin is right of e12.
                            return new Vector2(e12.Y, -e12.X);
                        }
                    }

                default:
                    Debug.Assert(false);
                    return Vector2.Zero;
            }
        }

        internal Vector2 GetClosestPoint()
        {
            switch (Count)
            {
                case 0:
                    Debug.Assert(false);
                    return Vector2.Zero;

                case 1:
                    return V[0].W;

                case 2:
                    return V[0].W * V[0].A + V[1].W * V[1].A;

                case 3:
                    return Vector2.Zero;

                default:
                    Debug.Assert(false);
                    return Vector2.Zero;
            }
        }

        internal void GetWitnessPoints(out Vector2 pA, out Vector2 pB)
        {
            switch (Count)
            {
                case 0:
                    pA = Vector2.Zero;
                    pB = Vector2.Zero;
                    Debug.Assert(false);
                    break;

                case 1:
                    pA = V[0].WA;
                    pB = V[0].WB;
                    break;

                case 2:
                    pA = V[0].WA * V[0].A + V[1].WA * V[1].A;
                    pB = V[0].WB * V[0].A + V[1].WB * V[1].A;
                    break;

                case 3:
                    pA = V[0].WA * V[0].A + V[1].WA * V[1].A + V[2].WA * V[2].A;
                    pB = pA;
                    break;

                default:
                    throw new Exception();
            }
        }

        internal float GetMetric()
        {
            switch (Count)
            {
                case 0:
                    Debug.Assert(false);
                    return 0.0f;
                case 1:
                    return 0.0f;

                case 2:
                    return (V[0].W - V[1].W).Length;

                case 3:
                    return Vector2.Cross(V[1].W - V[0].W, V[2].W - V[0].W);

                default:
                    Debug.Assert(false);
                    return 0.0f;
            }
        }

        // Solve a line segment using barycentric coordinates.
        //
        // p = a1 * w1 + a2 * w2
        // a1 + a2 = 1
        //
        // The vector from the origin to the closest point on the line is
        // perpendicular to the line.
        // e12 = w2 - w1
        // dot(p, e) = 0
        // a1 * dot(w1, e) + a2 * dot(w2, e) = 0
        //
        // 2-by-2 linear system
        // [1      1     ][a1] = [1]
        // [w1.e12 w2.e12][a2] = [0]
        //
        // Define
        // d12_1 =  dot(w2, e12)
        // d12_2 = -dot(w1, e12)
        // d12 = d12_1 + d12_2
        //
        // Solution
        // a1 = d12_1 / d12
        // a2 = d12_2 / d12

        internal void Solve2()
        {
            Vector2 w1 = V[0].W;
            Vector2 w2 = V[1].W;
            Vector2 e12 = w2 - w1;

            // w1 region
            float d12_2 = -Vector2.Dot(w1, e12);
            if (d12_2 <= 0.0f)
            {
                // a2 <= 0, so we clamp it to 0
                SimplexVertex v0 = V[0];
                v0.A = 1.0f;
                V[0] = v0;
                Count = 1;
                return;
            }

            // w2 region
            float d12_1 = Vector2.Dot(w2, e12);
            if (d12_1 <= 0.0f)
            {
                // a1 <= 0, so we clamp it to 0
                SimplexVertex v1 = V[1];
                v1.A = 1.0f;
                V[1] = v1;
                Count = 1;
                V[0] = V[1];
                return;
            }

            // Must be in e12 region.
            float inv_d12 = 1.0f / (d12_1 + d12_2);
            SimplexVertex v0_2 = V[0];
            SimplexVertex v1_2 = V[1];
            v0_2.A = d12_1 * inv_d12;
            v1_2.A = d12_2 * inv_d12;
            V[0] = v0_2;
            V[1] = v1_2;
            Count = 2;
        }

        // Possible regions:
        // - points[2]
        // - edge points[0]-points[2]
        // - edge points[1]-points[2]
        // - inside the triangle
        internal void Solve3()
        {
            Vector2 w1 = V[0].W;
            Vector2 w2 = V[1].W;
            Vector2 w3 = V[2].W;

            // Edge12
            // [1      1     ][a1] = [1]
            // [w1.e12 w2.e12][a2] = [0]
            // a3 = 0
            Vector2 e12 = w2 - w1;
            float w1e12 = Vector2.Dot(w1, e12);
            float w2e12 = Vector2.Dot(w2, e12);
            float d12_1 = w2e12;
            float d12_2 = -w1e12;

            // Edge13
            // [1      1     ][a1] = [1]
            // [w1.e13 w3.e13][a3] = [0]
            // a2 = 0
            Vector2 e13 = w3 - w1;
            float w1e13 = Vector2.Dot(w1, e13);
            float w3e13 = Vector2.Dot(w3, e13);
            float d13_1 = w3e13;
            float d13_2 = -w1e13;

            // Edge23
            // [1      1     ][a2] = [1]
            // [w2.e23 w3.e23][a3] = [0]
            // a1 = 0
            Vector2 e23 = w3 - w2;
            float w2e23 = Vector2.Dot(w2, e23);
            float w3e23 = Vector2.Dot(w3, e23);
            float d23_1 = w3e23;
            float d23_2 = -w2e23;

            // Triangle123
            float n123 = Vector2.Cross(e12, e13);

            float d123_1 = n123 * Vector2.Cross(w2, w3);
            float d123_2 = n123 * Vector2.Cross(w3, w1);
            float d123_3 = n123 * Vector2.Cross(w1, w2);

            // w1 region
            if (d12_2 <= 0.0f && d13_2 <= 0.0f)
            {
                SimplexVertex v0_1 = V[0];
                v0_1.A = 1.0f;
                V[0] = v0_1;
                Count = 1;
                return;
            }

            // e12
            if (d12_1 > 0.0f && d12_2 > 0.0f && d123_3 <= 0.0f)
            {
                float inv_d12 = 1.0f / (d12_1 + d12_2);
                SimplexVertex v0_2 = V[0];
                SimplexVertex v1_2 = V[1];
                v0_2.A = d12_1 * inv_d12;
                v1_2.A = d12_2 * inv_d12;
                V[0] = v0_2;
                V[1] = v1_2;
                Count = 2;
                return;
            }

            // e13
            if (d13_1 > 0.0f && d13_2 > 0.0f && d123_2 <= 0.0f)
            {
                float inv_d13 = 1.0f / (d13_1 + d13_2);
                SimplexVertex v0_3 = V[0];
                SimplexVertex v2_3 = V[2];
                v0_3.A = d13_1 * inv_d13;
                v2_3.A = d13_2 * inv_d13;
                V[0] = v0_3;
                V[2] = v2_3;
                Count = 2;
                V[1] = V[2];
                return;
            }

            // w2 region
            if (d12_1 <= 0.0f && d23_2 <= 0.0f)
            {
                SimplexVertex v1_4 = V[1];
                v1_4.A = 1.0f;
                V[1] = v1_4;
                Count = 1;
                V[0] = V[1];
                return;
            }

            // w3 region
            if (d13_1 <= 0.0f && d23_1 <= 0.0f)
            {
                SimplexVertex v2_5 = V[2];
                v2_5.A = 1.0f;
                V[2] = v2_5;
                Count = 1;
                V[0] = V[2];
                return;
            }

            // e23
            if (d23_1 > 0.0f && d23_2 > 0.0f && d123_1 <= 0.0f)
            {
                float inv_d23 = 1.0f / (d23_1 + d23_2);
                SimplexVertex v1_6 = V[1];
                SimplexVertex v2_6 = V[2];
                v1_6.A = d23_1 * inv_d23;
                v2_6.A = d23_2 * inv_d23;
                V[1] = v1_6;
                V[2] = v2_6;
                Count = 2;
                V[0] = V[2];
                return;
            }

            // Must be in triangle123
            float inv_d123 = 1.0f / (d123_1 + d123_2 + d123_3);
            SimplexVertex v0_7 = V[0];
            SimplexVertex v1_7 = V[1];
            SimplexVertex v2_7 = V[2];
            v0_7.A = d123_1 * inv_d123;
            v1_7.A = d123_2 * inv_d123;
            v2_7.A = d123_3 * inv_d123;
            V[0] = v0_7;
            V[1] = v1_7;
            V[2] = v2_7;
            Count = 3;
        }
    }
}
