using System;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

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
	    var c = Transform.Mul(xfB, circleB.Position);
	    var cLocal = Transform.MulT(xfA, c);

	    // Find the min separating edge.
	    int normalIndex = 0;
	    float separation = float.MinValue;
	    float radius = polygonA.Radius + circleB.Radius;
	    int vertexCount = polygonA.VertexCount;
	    var vertices = polygonA.Vertices;
	    var normals = polygonA.Normals;

	    for (int i = 0; i < vertexCount; ++i)
	    {
		    float s = Vector2.Dot(normals[i], cLocal - vertices[i]);

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
	    Vector2 v1 = vertices[vertIndex1];
	    Vector2 v2 = vertices[vertIndex2];

	    // If the center is inside the polygon ...
	    if (separation < float.Epsilon)
	    {
		    manifold.PointCount = 1;
		    manifold.Type = ManifoldType.FaceA;
		    manifold.LocalNormal = normals[normalIndex];
		    manifold.LocalPoint = (v1 + v2) * 0.5f;

            ref var p0 = ref manifold.Points[0];

		    p0.LocalPoint = circleB.Position;
		    p0.Id.Key = 0;
		    return;
	    }

	    // Compute barycentric coordinates
	    float u1 = Vector2.Dot(cLocal - v1, v2 - v1);
	    float u2 = Vector2.Dot(cLocal - v2, v1 - v2);
	    if (u1 <= 0.0f)
        {
            var sergal = (cLocal - v1);

		    if (sergal.LengthSquared > radius * radius)
		    {
			    return;
		    }

		    manifold.PointCount = 1;
		    manifold.Type = ManifoldType.FaceA;
		    manifold.LocalNormal = (cLocal - v1).Normalized;
            manifold.LocalPoint = v1;

            ref var p0 = ref manifold.Points[0];

		    p0.LocalPoint = circleB.Position;
		    p0.Id.Key = 0;
	    }
	    else if (u2 <= 0.0f)
        {
            var sergal = (cLocal - v2);

		    if (sergal.LengthSquared > radius * radius)
		    {
			    return;
		    }

		    manifold.PointCount = 1;
		    manifold.Type = ManifoldType.FaceA;
		    manifold.LocalNormal = (cLocal - v2).Normalized;
            manifold.LocalPoint = v2;

            ref var p0 = ref manifold.Points[0];

		    p0.LocalPoint = circleB.Position;
		    p0.Id.Key = 0;
	    }
	    else
	    {
		    Vector2 faceCenter = (v1 + v2) * 0.5f;
		    float s = Vector2.Dot(cLocal - faceCenter, normals[vertIndex1]);
		    if (s > radius)
		    {
			    return;
		    }

		    manifold.PointCount = 1;
		    manifold.Type = ManifoldType.FaceA;
		    manifold.LocalNormal = normals[vertIndex1];
		    manifold.LocalPoint = faceCenter;

            ref var p0 = ref manifold.Points[0];

		    p0.LocalPoint = circleB.Position;
            p0.Id.Key = 0;
	    }
    }
}
