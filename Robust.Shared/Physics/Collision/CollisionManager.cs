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
using System.Numerics;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.Shared.Physics.Collision;

/// <summary>
///     Handles several collision features: Generating contact manifolds, testing shape overlap,
/// </summary>
internal sealed partial class CollisionManager : IManifoldManager
{
    /*
     * Farseer had this as a static class with a ThreadStatic DistanceInput
     */

    private ObjectPool<EdgeShape> _edgePool =
        new DefaultObjectPool<EdgeShape>(new DefaultPooledObjectPolicy<EdgeShape>());

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
        var points1 = manifold1.Points.AsSpan;
        var points2 = manifold2.Points.AsSpan;

        for (int i = 0; i < manifold1.PointCount; ++i)
        {
            var id = points1[i].Id;

            state1[i] = PointState.Remove;

            for (int j = 0; j < manifold2.PointCount; ++j)
            {
                if (points2[j].Id.Key == id.Key)
                {
                    state1[i] = PointState.Persist;
                    break;
                }
            }
        }

        // Detect persists and adds.
        for (int i = 0; i < manifold2.PointCount; ++i)
        {
            var id = points2[i].Id;

            state2[i] = PointState.Add;

            for (var j = 0; j < manifold1.PointCount; ++j)
            {
                if (points1[j].Id.Key == id.Key)
                {
                    state2[i] = PointState.Persist;
                    break;
                }
            }
        }
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

    public EdgeShape GetContactEdge()
    {
        return _edgePool.Get();
    }

    public void ReturnEdge(EdgeShape edge)
    {
        _edgePool.Return(edge);
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
