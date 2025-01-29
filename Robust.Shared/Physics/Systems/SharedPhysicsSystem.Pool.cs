using System.Buffers;
using System.Numerics;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Shapes;

namespace Robust.Shared.Physics.Systems;

public abstract partial class SharedPhysicsSystem
{
    private ArrayPool<Vector2> _vectorPool = ArrayPool<Vector2>.Create(4, 128);

    /// <summary>
    /// Gets a polygon with pooled arrays backing it.
    /// </summary>
    internal Polygon GetPooled(Box2 box)
    {
        var vertices = _vectorPool.Rent(4);
        var normals = _vectorPool.Rent(4);
        var centroid = box.Center;

        vertices[0] = box.BottomLeft;
        vertices[1] = box.BottomRight;
        vertices[2] = box.TopRight;
        vertices[3] = box.TopLeft;

        normals[0] = new Vector2(0.0f, -1.0f);
        normals[1] = new Vector2(1.0f, 0.0f);
        normals[2] = new Vector2(0.0f, 1.0f);
        normals[3] = new Vector2(-1.0f, 0.0f);

        return new Polygon(vertices, normals, centroid);
    }

    internal Polygon GetPooled(Box2Rotated box)
    {
        var vertices = _vectorPool.Rent(4);
        var normals = _vectorPool.Rent(4);
        var centroid = box.Center;

        vertices[0] = box.BottomLeft;
        vertices[1] = box.BottomRight;
        vertices[2] = box.TopRight;
        vertices[3] = box.TopLeft;

        var polygon = new Polygon(vertices, normals, centroid);
        polygon.CalculateNormals(normals, 4);

        return polygon;
    }

    internal void ReturnPooled(Polygon polygon)
    {
        _vectorPool.Return(polygon.Vertices);
        _vectorPool.Return(polygon.Normals);
    }
}
