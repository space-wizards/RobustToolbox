using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Shapes;

// Internal so people don't use it when it will have breaking changes very soon.
internal record struct Polygon : IPhysShape
{
    public static Polygon Empty = new(Box2.Empty);

    [DataField]
    public Vector2[] Vertices;

    public int VertexCount => Vertices.Length;

    public Vector2[] Normals;

    public Vector2 Centroid;

    public int ChildCount => 1;
    public float Radius { get; set; }
    public ShapeType ShapeType => ShapeType.Polygon;

    // Hopefully this one is short-lived for a few months
    public Polygon(IPhysShape shape) : this((PolygonShape) shape)
    {

    }

    public Polygon(PolygonShape polyShape)
    {
        Unsafe.SkipInit(out this);
        Vertices = new Vector2[polyShape.VertexCount];
        Normals = new Vector2[polyShape.Normals.Length];
        Radius = polyShape.Radius;
        Centroid = polyShape.Centroid;

        Array.Copy(polyShape.Vertices, Vertices, Vertices.Length);
        Array.Copy(polyShape.Normals, Normals, Vertices.Length);
    }

    public Polygon(Box2 aabb)
    {
        Unsafe.SkipInit(out this);
        Vertices = new Vector2[4];
        Normals = new Vector2[4];
        Radius = 0f;

        Vertices[0] = aabb.BottomLeft;
        Vertices[1] = aabb.BottomRight;
        Vertices[2] = aabb.TopRight;
        Vertices[3] = aabb.TopLeft;

        Normals[0] = new Vector2(0.0f, -1.0f);
        Normals[1] = new Vector2(1.0f, 0.0f);
        Normals[2] = new Vector2(0.0f, 1.0f);
        Normals[3] = new Vector2(-1.0f, 0.0f);

        Centroid = aabb.Center;
    }

    public Polygon(Box2Rotated bounds)
    {
        Unsafe.SkipInit(out this);
        Radius = 0f;
        Span<Vector2> verts = stackalloc Vector2[4];
        verts[0] = bounds.BottomLeft;
        verts[1] = bounds.BottomRight;
        verts[2] = bounds.TopRight;
        verts[3] = bounds.TopLeft;

        var hull = new PhysicsHull(verts, 4);
        Set(hull);

        Centroid = bounds.Center;
    }

    public Polygon(Vector2[] vertices)
    {
        Unsafe.SkipInit(out this);
        var hull = PhysicsHull.ComputeHull(vertices, vertices.Length);

        if (hull.Count < 3)
        {
            Vertices = Array.Empty<Vector2>();
            Normals = Array.Empty<Vector2>();
            return;
        }

        Vertices = vertices;
        Normals = new Vector2[vertices.Length];
        Set(hull);
        Centroid = ComputeCentroid(Vertices);
    }

    public static explicit operator Polygon(PolygonShape polyShape)
    {
        return new Polygon(polyShape);
    }

    private void Set(PhysicsHull hull)
    {
        DebugTools.Assert(hull.Count >= 3);
        var vertexCount = hull.Count;
        Array.Resize(ref Vertices, vertexCount);
        Array.Resize(ref Normals, vertexCount);

        for (var i = 0; i < vertexCount; i++)
        {
            Vertices[i] = hull.Points[i];
        }

        // Compute normals. Ensure the edges have non-zero length.
        for (var i = 0; i < vertexCount; i++)
        {
            var next = i + 1 < vertexCount ? i + 1 : 0;
            var edge = Vertices[next] - Vertices[i];
            DebugTools.Assert(edge.LengthSquared() > float.Epsilon * float.Epsilon);

            var temp = Vector2Helpers.Cross(edge, 1f);
            Normals[i] = temp.Normalized();
        }
    }

    private static Vector2 ComputeCentroid(Vector2[] vs)
    {
        var count = vs.Length;
        DebugTools.Assert(count >= 3);

        var c = new Vector2(0.0f, 0.0f);
        float area = 0.0f;

        // Get a reference point for forming triangles.
        // Use the first vertex to reduce round-off errors.
        var s = vs[0];

        const float inv3 = 1.0f / 3.0f;

        for (var i = 0; i < count; ++i)
        {
            // Triangle vertices.
            var p1 = vs[0] - s;
            var p2 = vs[i] - s;
            var p3 = i + 1 < count ? vs[i+1] - s : vs[0] - s;

            var e1 = p2 - p1;
            var e2 = p3 - p1;

            float D = Vector2Helpers.Cross(e1, e2);

            float triangleArea = 0.5f * D;
            area += triangleArea;

            // Area weighted centroid
            c += (p1 + p2 + p3) * triangleArea * inv3;
        }

        // Centroid
        DebugTools.Assert(area > float.Epsilon);
        c = c * (1.0f / area) + s;
        return c;
    }

    public Box2 ComputeAABB(Transform transform, int childIndex)
    {
        DebugTools.Assert(childIndex == 0);
        var lower = Transform.Mul(transform, Vertices[0]);
        var upper = lower;

        for (var i = 1; i < VertexCount; ++i)
        {
            var v = Transform.Mul(transform, Vertices[i]);
            lower = Vector2.Min(lower, v);
            upper = Vector2.Max(upper, v);
        }

        var r = new Vector2(Radius, Radius);
        return new Box2(lower - r, upper + r);
    }

    public bool Equals(IPhysShape? other)
    {
        if (other is not PolygonShape poly) return false;
        if (VertexCount != poly.VertexCount) return false;
        for (var i = 0; i < VertexCount; i++)
        {
            var vert = Vertices[i];
            if (!vert.Equals(poly.Vertices[i])) return false;
        }

        return true;
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(VertexCount, Vertices.AsSpan(0, VertexCount).ToArray(), Radius);
    }
}
