using System.Buffers;
using System.Numerics;
using Microsoft.Extensions.ObjectPool;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    /// <summary>
    /// Gets a polygon with pooled arrays backing it.
    /// </summary>
    internal Polygon GetPooled(Box2 box)
    {
        var vertices = ArrayPool<Vector2>.Shared.Rent(4);
        var normals = ArrayPool<Vector2>.Shared.Rent(4);
        var centroid = box.Center;

        vertices[0] = box.BottomLeft;
        vertices[1] = box.BottomRight;
        vertices[2] = box.TopRight;
        vertices[3] = box.TopLeft;

        normals[0] = new Vector2(0.0f, -1.0f);
        normals[1] = new Vector2(1.0f, 0.0f);
        normals[2] = new Vector2(0.0f, 1.0f);
        normals[3] = new Vector2(-1.0f, 0.0f);

        return new Polygon(vertices, normals, centroid, 4);
    }

    internal Polygon GetPooled(Box2Rotated box)
    {
        var vertices = ArrayPool<Vector2>.Shared.Rent(4);
        var normals = ArrayPool<Vector2>.Shared.Rent(4);
        var centroid = box.Center;

        vertices[0] = box.BottomLeft;
        vertices[1] = box.BottomRight;
        vertices[2] = box.TopRight;
        vertices[3] = box.TopLeft;

        var polygon = new Polygon(vertices, normals, centroid, 4);
        polygon.CalculateNormals(normals, 4);

        return polygon;
    }

    internal void ReturnPooled(Polygon polygon)
    {
        ArrayPool<Vector2>.Shared.Return(polygon.Vertices);
        ArrayPool<Vector2>.Shared.Return(polygon.Normals);
    }
}
