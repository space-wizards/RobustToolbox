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
using System.Diagnostics;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Physics.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Collision;

/// <summary>
/// A distance proxy is used by the GJK algorithm.
/// It encapsulates any shape.
/// </summary>
internal ref struct DistanceProxy
{
    internal float Radius;
    internal ReadOnlySpan<Vector2> Vertices;
    internal FixedArray2<Vector2> Buffer;

    // GJK using Voronoi regions (Christer Ericson) and Barycentric coordinates.

    internal DistanceProxy(ReadOnlySpan<Vector2> vertices, float radius)
    {
        Vertices = vertices;
        Radius = radius;
    }

    /// <summary>
    /// Initialize the proxy using the given shape. The shape
    /// must remain in scope while the proxy is in use.
    /// </summary>
    /// <param name="shape">The shape.</param>
    internal void Set<T>(T shape, int index) where T : IPhysShape
    {
        switch (shape.ShapeType)
        {
            case ShapeType.Circle:
                var circle = Unsafe.As<PhysShapeCircle>(shape);
                Buffer._00 = circle.Position;
                Vertices = Buffer.AsSpan[..1];
                Radius = circle.Radius;
                break;

            case ShapeType.Polygon:
                if (shape is Polygon poly)
                {
                    Span<Vector2> verts = new Vector2[poly.VertexCount];
                    poly._vertices.AsSpan[..poly.VertexCount].CopyTo(verts);
                    Vertices = verts;
                    Radius = poly.Radius;
                }
                else if (shape is SlimPolygon fast)
                {
                    Span<Vector2> verts = new Vector2[fast.VertexCount];
                    fast._vertices.AsSpan[..fast.VertexCount].CopyTo(verts);
                    Vertices = verts;
                    Radius = fast.Radius;
                }
                else
                {
                    var polyShape = Unsafe.As<PolygonShape>(shape);
                    Vertices = polyShape.Vertices.AsSpan()[..polyShape.VertexCount];
                    Radius = polyShape.Radius;
                }

                break;

            case ShapeType.Chain:
                var chain = Unsafe.As<ChainShape>(shape);
                Debug.Assert(0 <= index && index < chain.Vertices.Length);

                Buffer._00 = chain.Vertices[index];
                Buffer._01 = index + 1 < chain.Vertices.Length ? chain.Vertices[index + 1] : chain.Vertices[0];
                Vertices = Buffer.AsSpan;

                Radius = chain.Radius;
                break;
            case ShapeType.Edge:
                var edge = Unsafe.As<EdgeShape>(shape);

                Buffer._00 = edge.Vertex1;
                Buffer._01 = edge.Vertex2;
                Vertices = Buffer.AsSpan;

                Radius = edge.Radius;
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
        for (int i = 1; i < Vertices.Length; ++i)
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
        for (int i = 1; i < Vertices.Length; ++i)
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

    internal static DistanceProxy MakeProxy(ReadOnlySpan<Vector2> vertices, int count, float radius )
    {
        count = Math.Min(count, PhysicsConstants.MaxPolygonVertices);
        var proxy = new DistanceProxy(vertices[..count], radius);
        return proxy;
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

internal struct Simplex
{
    // Made it a class from a struct as it seemed silly to be a struct considering it's being mutated constantly.

    internal int Count;
    // Normally 3 but fixed size buffers don't support structs and this stack-only anyways.
    internal FixedArray4<SimplexVertex> V;

    internal void ReadCache(ref SimplexCache cache, DistanceProxy proxyA, in Transform transformA, DistanceProxy proxyB, in Transform transformB)
    {
        DebugTools.Assert(cache.Count <= 3);

        // Copy data from cache.
        Count = cache.Count;
        var vSpan = V.AsSpan;
        for (int i = 0; i < Count; ++i)
        {
            ref SimplexVertex v = ref vSpan[i];
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
            ref SimplexVertex v = ref V._00;
            v.IndexA = 0;
            v.IndexB = 0;
            Vector2 wALocal = proxyA.Vertices[0];
            Vector2 wBLocal = proxyB.Vertices[0];
            v.WA = Transform.Mul(transformA, wALocal);
            v.WB = Transform.Mul(transformB, wBLocal);
            v.W = v.WB - v.WA;
            v.A = 1.0f;
            Count = 1;
        }
    }

    internal void WriteCache(ref SimplexCache cache)
    {
        cache.Metric = GetMetric();
        cache.Count = (UInt16)Count;
        var vSpan = V.AsSpan;
        for (var i = 0; i < Count; ++i)
        {
            unsafe
            {
                cache.IndexA[i] = (byte) (vSpan[i].IndexA);
                cache.IndexB[i] = (byte) (vSpan[i].IndexB);
            }
        }
    }

    internal Vector2 GetSearchDirection()
    {
        switch (Count)
        {
            case 1:
                return -V._00.W;

            case 2:
            {
                Vector2 e12 = V._01.W - V._00.W;
                float sgn = Vector2Helpers.Cross(e12, -V._00.W);
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

    public static Vector2 Weight2( float a1, Vector2 w1, float a2, Vector2 w2 )
    {
        return new Vector2(a1 * w1.X + a2 * w2.X, a1 * w1.Y + a2 * w2.Y);
    }

    public static Vector2 Weight3(float a1, Vector2 w1, float a2, Vector2 w2, float a3, Vector2 w3 )
    {
        return new Vector2(a1 * w1.X + a2 * w2.X + a3 * w3.X, a1 * w1.Y + a2 * w2.Y + a3 * w3.Y);
    }

    internal Vector2 GetClosestPoint()
    {
        switch (Count)
        {
            case 0:
                Debug.Assert(false);
                return Vector2.Zero;

            case 1:
                return V._00.W;

            case 2:
                return V._00.W * V._00.A + V._01.W * V._01.A;

            case 3:
                return Vector2.Zero;

            default:
                Debug.Assert(false);
                return Vector2.Zero;
        }
    }

    public static Vector2 ComputeSimplexClosestPoint(Simplex s)
    {
        switch (s.Count)
        {
            case 0:
                DebugTools.Assert(false);
                return Vector2.Zero;

            case 1:
                return s.V._00.W;

            case 2:
                return Weight2(s.V._00.A, s.V._00.W, s.V._01.A, s.V._01.W);

            case 3:
                return Vector2.Zero;

            default:
                DebugTools.Assert(false);
                return Vector2.Zero;
        }
    }

    public static void ComputeSimplexWitnessPoints(ref Vector2 a, ref Vector2 b, Simplex s)
    {
        switch (s.Count)
        {
            case 0:
                DebugTools.Assert(false);
                break;

            case 1:
                a = s.V._00.WA;
                b = s.V._00.WB;
                break;

            case 2:
                a = Weight2(s.V._00.A, s.V._00.WA, s.V._01.A, s.V._01.WA);
                b = Weight2(s.V._00.A, s.V._00.WB, s.V._01.A, s.V._01.WB);
                break;

            case 3:
                a = Weight3(s.V._00.A, s.V._00.WA, s.V._01.A, s.V._01.WA, s.V._02.A, s.V._02.WA);
                // TODO_ERIN why are these not equal?
                //*b = b2Weight3(s->v1.a, s->v1.wB, s->v2.a, s->v2.wB, s->v3.a, s->v3.wB);
                b = a;
                break;

            default:
                DebugTools.Assert(false);
                break;
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
    public static void SolveSimplex2(ref Simplex s)
    {
	    var w1 = s.V._00.W;
	    var w2 = s.V._01.W;
	    var e12 = Vector2.Subtract(w2, w1);

	    // w1 region
	    float d12_2 = -Vector2.Dot(w1, e12);
	    if (d12_2 <= 0.0f)
	    {
		    // a2 <= 0, so we clamp it to 0
		    s.V._00.A = 1.0f;
		    s.Count = 1;
		    return;
	    }

	    // w2 region
	    float d12_1 = Vector2.Dot(w2, e12);
	    if (d12_1 <= 0.0f)
	    {
		    // a1 <= 0, so we clamp it to 0
		    s.V._01.A = 1.0f;
		    s.Count = 1;
		    s.V._00 = s.V._01;
		    return;
	    }

	    // Must be in e12 region.
	    float inv_d12 = 1.0f / ( d12_1 + d12_2 );
	    s.V._00.A = d12_1 * inv_d12;
	    s.V._01.A = d12_2 * inv_d12;
	    s.Count = 2;
    }

    public static void SolveSimplex3(ref Simplex s)
    {
	    var w1 = s.V._00.W;
        var w2 = s.V._01.W;
        var w3 = s.V._02.W;

	    // Edge12
	    // [1      1     ][a1] = [1]
	    // [w1.e12 w2.e12][a2] = [0]
	    // a3 = 0
	    var e12 = Vector2.Subtract(w2, w1);
	    float w1e12 = Vector2.Dot(w1, e12);
	    float w2e12 = Vector2.Dot(w2, e12);
	    float d12_1 = w2e12;
	    float d12_2 = -w1e12;

	    // Edge13
	    // [1      1     ][a1] = [1]
	    // [w1.e13 w3.e13][a3] = [0]
	    // a2 = 0
	    var e13 = Vector2.Subtract(w3, w1);
	    float w1e13 = Vector2.Dot(w1, e13);
	    float w3e13 = Vector2.Dot(w3, e13);
	    float d13_1 = w3e13;
	    float d13_2 = -w1e13;

	    // Edge23
	    // [1      1     ][a2] = [1]
	    // [w2.e23 w3.e23][a3] = [0]
	    // a1 = 0
	    var e23 = Vector2.Subtract(w3, w2);
	    float w2e23 = Vector2.Dot(w2, e23);
	    float w3e23 = Vector2.Dot(w3, e23);
	    float d23_1 = w3e23;
	    float d23_2 = -w2e23;

	    // Triangle123
	    float n123 = Vector2Helpers.Cross(e12, e13);

	    float d123_1 = n123 * Vector2Helpers.Cross(w2, w3);
	    float d123_2 = n123 * Vector2Helpers.Cross(w3, w1);
	    float d123_3 = n123 * Vector2Helpers.Cross(w1, w2);

	    // w1 region
	    if (d12_2 <= 0.0f && d13_2 <= 0.0f)
	    {
		    s.V._00.A = 1.0f;
		    s.Count = 1;
		    return;
	    }

	    // e12
	    if (d12_1 > 0.0f && d12_2 > 0.0f && d123_3 <= 0.0f)
	    {
		    float inv_d12 = 1.0f / ( d12_1 + d12_2 );
		    s.V._00.A = d12_1 * inv_d12;
		    s.V._01.A = d12_2 * inv_d12;
		    s.Count = 2;
		    return;
	    }

	    // e13
	    if (d13_1 > 0.0f && d13_2 > 0.0f && d123_2 <= 0.0f)
	    {
		    float inv_d13 = 1.0f / ( d13_1 + d13_2 );
		    s.V._00.A = d13_1 * inv_d13;
		    s.V._02.A = d13_2 * inv_d13;
		    s.Count = 2;
		    s.V._01 = s.V._02;
		    return;
	    }

	    // w2 region
	    if (d12_1 <= 0.0f && d23_2 <= 0.0f)
	    {
		    s.V._01.A = 1.0f;
		    s.Count = 1;
		    s.V._00 = s.V._01;
		    return;
	    }

	    // w3 region
	    if (d13_1 <= 0.0f && d23_1 <= 0.0f)
	    {
		    s.V._02.A = 1.0f;
		    s.Count = 1;
		    s.V._00 = s.V._02;
		    return;
	    }

	    // e23
	    if (d23_1 > 0.0f && d23_2 > 0.0f && d123_1 <= 0.0f)
	    {
		    float inv_d23 = 1.0f / ( d23_1 + d23_2 );
		    s.V._01.A = d23_1 * inv_d23;
		    s.V._02.A = d23_2 * inv_d23;
		    s.Count = 2;
		    s.V._00 = s.V._02;
		    return;
	    }

	    // Must be in triangle123
	    float inv_d123 = 1.0f / (d123_1 + d123_2 + d123_3);
	    s.V._00.A = d123_1 * inv_d123;
	    s.V._01.A = d123_2 * inv_d123;
	    s.V._02.A = d123_3 * inv_d123;
	    s.Count = 3;
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
                pA = V._00.WA;
                pB = V._00.WB;
                break;

            case 2:
                pA = V._00.WA * V._00.A + V._01.WA * V._01.A;
                pB = V._00.WB * V._00.A + V._01.WB * V._01.A;
                break;

            case 3:
                pA = V._00.WA * V._00.A + V._01.WA * V._01.A + V._02.WA * V._02.A;
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
                return (V._00.W - V._01.W).Length();

            case 3:
                return Vector2Helpers.Cross(V._01.W - V._00.W, V._02.W - V._00.W);

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
        Vector2 w1 = V._00.W;
        Vector2 w2 = V._01.W;
        Vector2 e12 = w2 - w1;

        // w1 region
        float d12_2 = -Vector2.Dot(w1, e12);
        if (d12_2 <= 0.0f)
        {
            // a2 <= 0, so we clamp it to 0
            SimplexVertex v0 = V._00;
            v0.A = 1.0f;
            V._00 = v0;
            Count = 1;
            return;
        }

        // w2 region
        float d12_1 = Vector2.Dot(w2, e12);
        if (d12_1 <= 0.0f)
        {
            // a1 <= 0, so we clamp it to 0
            SimplexVertex v1 = V._01;
            v1.A = 1.0f;
            V._01 = v1;
            Count = 1;
            V._00 = V._01;
            return;
        }

        // Must be in e12 region.
        float inv_d12 = 1.0f / (d12_1 + d12_2);
        SimplexVertex v0_2 = V._00;
        SimplexVertex v1_2 = V._01;
        v0_2.A = d12_1 * inv_d12;
        v1_2.A = d12_2 * inv_d12;
        V._00 = v0_2;
        V._01 = v1_2;
        Count = 2;
    }

    // Possible regions:
    // - points[2]
    // - edge points[0]-points[2]
    // - edge points[1]-points[2]
    // - inside the triangle
    internal void Solve3()
    {
        Vector2 w1 = V._00.W;
        Vector2 w2 = V._01.W;
        Vector2 w3 = V._02.W;

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
        float n123 = Vector2Helpers.Cross(e12, e13);

        float d123_1 = n123 * Vector2Helpers.Cross(w2, w3);
        float d123_2 = n123 * Vector2Helpers.Cross(w3, w1);
        float d123_3 = n123 * Vector2Helpers.Cross(w1, w2);

        // w1 region
        if (d12_2 <= 0.0f && d13_2 <= 0.0f)
        {
            SimplexVertex v0_1 = V._00;
            v0_1.A = 1.0f;
            V._00 = v0_1;
            Count = 1;
            return;
        }

        // e12
        if (d12_1 > 0.0f && d12_2 > 0.0f && d123_3 <= 0.0f)
        {
            float inv_d12 = 1.0f / (d12_1 + d12_2);
            SimplexVertex v0_2 = V._00;
            SimplexVertex v1_2 = V._01;
            v0_2.A = d12_1 * inv_d12;
            v1_2.A = d12_2 * inv_d12;
            V._00 = v0_2;
            V._01 = v1_2;
            Count = 2;
            return;
        }

        // e13
        if (d13_1 > 0.0f && d13_2 > 0.0f && d123_2 <= 0.0f)
        {
            float inv_d13 = 1.0f / (d13_1 + d13_2);
            SimplexVertex v0_3 = V._00;
            SimplexVertex v2_3 = V._02;
            v0_3.A = d13_1 * inv_d13;
            v2_3.A = d13_2 * inv_d13;
            V._00 = v0_3;
            V._02 = v2_3;
            Count = 2;
            V._01 = V._02;
            return;
        }

        // w2 region
        if (d12_1 <= 0.0f && d23_2 <= 0.0f)
        {
            SimplexVertex v1_4 = V._01;
            v1_4.A = 1.0f;
            V._01 = v1_4;
            Count = 1;
            V._00 = V._01;
            return;
        }

        // w3 region
        if (d13_1 <= 0.0f && d23_1 <= 0.0f)
        {
            SimplexVertex v2_5 = V._02;
            v2_5.A = 1.0f;
            V._02 = v2_5;
            Count = 1;
            V._00 = V._02;
            return;
        }

        // e23
        if (d23_1 > 0.0f && d23_2 > 0.0f && d123_1 <= 0.0f)
        {
            float inv_d23 = 1.0f / (d23_1 + d23_2);
            SimplexVertex v1_6 = V._01;
            SimplexVertex v2_6 = V._02;
            v1_6.A = d23_1 * inv_d23;
            v2_6.A = d23_2 * inv_d23;
            V._01 = v1_6;
            V._02 = v2_6;
            Count = 2;
            V._00 = V._02;
            return;
        }

        // Must be in triangle123
        float inv_d123 = 1.0f / (d123_1 + d123_2 + d123_3);
        SimplexVertex v0_7 = V._00;
        SimplexVertex v1_7 = V._01;
        SimplexVertex v2_7 = V._02;
        v0_7.A = d123_1 * inv_d123;
        v1_7.A = d123_2 * inv_d123;
        v2_7.A = d123_3 * inv_d123;
        V._00 = v0_7;
        V._01 = v1_7;
        V._02 = v2_7;
        Count = 3;
    }
}
