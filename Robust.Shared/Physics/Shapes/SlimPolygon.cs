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

        // TODO why was Centroid not set?
        Centroid = box.Center;
    }

    /// <summary>
    /// Construct polygon by applying a transformation to a box.
    /// </summary>
    internal SlimPolygon(in Box2 box, in Matrix3x2 transform)
    {
        Unsafe.SkipInit(out this);
        Radius = 0f;

        transform.TransformBox(box, out var x, out var y);

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

        Polygon.CalculateNormals(_vertices.AsSpan, _normals.AsSpan, 4);

        // Get midpoint between opposite corners
        Centroid = (_vertices._00 + _vertices._02)/2;
    }

    public SlimPolygon(in Box2Rotated box, in Matrix3x2 transform) : this(in box.Box,  box.Transform * transform)
    {
    }

    public SlimPolygon(in Box2Rotated bounds) :  this(in bounds.Box, bounds.Transform)
    {
    }

    public Box2 ComputeAABB(Transform transform, int childIndex)
    {
        // TODO PHYSICS PERF
        // This should probably be simdified if its used a lot.
        // i.e., something like Box2's matrix transformation code.

        DebugTools.Assert(VertexCount > 0);
        DebugTools.Assert(childIndex == 0);
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
