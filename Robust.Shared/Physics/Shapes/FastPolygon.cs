using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Shapes;

internal struct FastPolygon : IPhysShape
{
    public FixedArray4<Vector2> Vertices;
    public FixedArray4<Vector2> Normals;

    public Vector2 Centroid;

    public int ChildCount => 1;
    public float Radius { get; set; }
    public ShapeType ShapeType => ShapeType.Polygon;

    public FastPolygon(Box2 box)
    {
        Unsafe.SkipInit(out this);

        Vertices._00 = box.BottomLeft;
        Vertices._01 = box.BottomRight;
        Vertices._02 = box.TopRight;
        Vertices._03 = box.TopLeft;

        Normals._00 = new Vector2(0.0f, -1.0f);
        Normals._01 = new Vector2(1.0f, 0.0f);
        Normals._02 = new Vector2(0.0f, 1.0f);
        Normals._03 = new Vector2(-1.0f, 0.0f);
    }

    public Box2 ComputeAABB(Transform transform, int childIndex)
    {
        throw new NotImplementedException();
    }

    public bool Equals(FastPolygon other)
    {
        return Radius.Equals(other.Radius) && Vertices.SequenceEqual(other.Vertices);
    }

    public bool Equals(IPhysShape? other)
    {
        throw new NotImplementedException();
    }
}
