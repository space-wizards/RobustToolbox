using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Collision.Shapes;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics.Shapes;

/// <summary>
/// Polygon backed by FixedArray4 to be smaller.
/// Useful for internal ops where the inputs are boxes to avoid the additional padding.
/// </summary>
internal record struct SlimPolygon : IPhysShape
{
    public Vector2[] Vertices => _vertices.AsSpan[..VertexCount].ToArray();

    public Vector2[] Normals => _normals.AsSpan[..VertexCount].ToArray();

    [DataField]
    public FixedArray4<Vector2> _vertices;

    public FixedArray4<Vector2> _normals;

    public Vector2 Centroid;

    public byte VertexCount => 4;

    public int ChildCount => 1;
    public float Radius { get; set; } = PhysicsConstants.PolygonRadius;
    public ShapeType ShapeType => ShapeType.Polygon;

    public SlimPolygon(Box2 box)
    {
        Unsafe.SkipInit(out this);
        Radius = 0f;

        _vertices._00 = box.BottomLeft;
        _vertices._01 = box.BottomRight;
        _vertices._02 = box.TopRight;
        _vertices._03 = box.TopLeft;

        _normals._00 = new Vector2(0.0f, -1.0f);
        _normals._01 = new Vector2(1.0f, 0.0f);
        _normals._02 = new Vector2(0.0f, 1.0f);
        _normals._03 = new Vector2(-1.0f, 0.0f);
        Centroid = box.Center;
    }

    /// <summary>
    /// Construct polygon by applying a transformation to a rotated box while simultaneously computing the bounding box.
    /// </summary>
    public SlimPolygon(in Box2 box, in Matrix3x2 transform, out Box2 aabb)
    {
        Unsafe.SkipInit(out this);
        Radius = 0f;

        transform.TransformBox(box, out var x, out var y);

        var tmp = SimdHelpers.GetAABB(x, y);
        aabb = Unsafe.As<Vector128<float>, Box2>(ref tmp);

        if (Sse.IsSupported)
        {
            var span = MemoryMarshal.Cast<Vector2, Vector128<float>>(_vertices.AsSpan);
            span[0]  = Sse.UnpackLow(x, y);
            span[1] = Sse.UnpackHigh(x, y);
        }
        else
        {
            _vertices._00 = new Vector2(x[0], y[0]);
            _vertices._01 = new Vector2(x[1], y[1]);
            _vertices._02 = new Vector2(x[2], y[2]);
            _vertices._03 = new Vector2(x[3], y[3]);
        }

        Centroid = (_vertices._00 + _vertices._02) / 2;

        // TODO SIMD
        // Probably use a special case for SlimPolygon
        Polygon.CalculateNormals(_vertices.AsSpan, _normals.AsSpan, 4);
    }

    /// <summary>
    /// Construct polygon by applying a transformation to a rotated box while simultaneously computing the bounding box.
    /// </summary>
    public SlimPolygon(in Box2Rotated box, in Matrix3x2 transform, out Box2 aabb)
        : this(in box.Box, box.Transform * transform, out aabb)
    {
    }

    public SlimPolygon(in Box2Rotated box)
    {
        Unsafe.SkipInit(out this);
        Radius = 0f;

        box.GetVertices(out var x, out var y);

        if (Sse.IsSupported)
        {
            var span = MemoryMarshal.Cast<Vector2, Vector128<float>>(_vertices.AsSpan);
            span[0] = Sse.UnpackLow(x, y);
            span[1] = Sse.UnpackHigh(x, y);
        }
        else
        {
            _vertices._00 = new Vector2(x[0], y[0]);
            _vertices._01 = new Vector2(x[1], y[1]);
            _vertices._02 = new Vector2(x[2], y[2]);
            _vertices._03 = new Vector2(x[3], y[3]);
        }

        Centroid = (_vertices._00 + _vertices._02) / 2;

        // TODO SIMD
        // Probably use a special case for SlimPolygon
        Polygon.CalculateNormals(_vertices.AsSpan, _normals.AsSpan, 4);
    }

    public Box2 ComputeAABBSlow(Transform transform)
    {
        // This is just Polygon.ComputeAABB
        DebugTools.Assert(VertexCount > 0);
        var verts = _vertices.AsSpan;
        var lower = Transform.Mul(transform, verts[0]);
        var upper = lower;

        for (var i = 1; i < VertexCount; ++i)
        {
            var v = Transform.Mul(transform, verts[i]);
            lower = Vector2.Min(lower, v);
            upper = Vector2.Max(upper, v);
        }

        var r = new Vector2(Radius, Radius);
        return new Box2(lower - r, upper + r);
    }

    public Box2 ComputeAABBSse(Transform transform)
    {
        var span = MemoryMarshal.Cast<Vector2, Vector128<float>>(_vertices.AsSpan);
        // Span = [x0, y0, x1, y1], [x2, y2, x3, y3]

        var polyX = Sse.Shuffle(span[0], span[1], 0b_10_00_10_00);
        var polyY = Sse.Shuffle(span[0], span[1], 0b_11_01_11_01);
        // polyX = [x0, x1, x2, x3], polyY = [y0, y1, y2, y3]

        SimdHelpers.Transform(transform, polyX, polyY, out var x, out var y);
        var lbrt = SimdHelpers.GetAABB(x, y);

        // Next we enlarge the bounds by th radius. i.e, box.Enlarged(R);
        // TODO is this even needed for SlimPoly? Is the radius ever set to non-zero?
        var zero = Vector128<float>.Zero;
        var r = Vector128.Create(Radius);
        lbrt = lbrt - Sse.MoveLowToHigh(r, zero) + Sse.MoveHighToLow(r, zero);
        // lbrt = lbrt - [R, R, 0, 0] + [0, 0, R, R]

        return Unsafe.As<Vector128<float>, Box2>(ref lbrt);
    }

    public Box2 ComputeAABB(Transform transform, int childIndex)
    {
        DebugTools.Assert(childIndex == 0);
        return Sse.IsSupported
            ? ComputeAABBSse(transform)
            : ComputeAABBSlow(transform);
    }

    public bool Equals(SlimPolygon other)
    {
        return Radius.Equals(other.Radius) && _vertices.AsSpan[..VertexCount].SequenceEqual(other._vertices.AsSpan[..VertexCount]);
    }

    public readonly override int GetHashCode()
    {
        return HashCode.Combine(_vertices, _normals, Centroid, Radius);
    }

    public bool Equals(IPhysShape? other)
    {
        if (other is Polygon poly)
        {
            return poly.Equals(this);
        }

        return other is SlimPolygon slim && Equals(slim);
    }
}
