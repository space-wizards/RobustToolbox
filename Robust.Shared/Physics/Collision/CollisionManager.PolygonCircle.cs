using System;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;

namespace Robust.Shared.Physics.Collision;

internal sealed partial class CollisionManager
{
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
}
