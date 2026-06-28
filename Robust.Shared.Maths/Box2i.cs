using System;
using System.Diagnostics.Contracts;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths;

[Serializable]
[StructLayout(LayoutKind.Explicit)]
public struct Box2i : IEquatable<Box2i>, ISpanFormattable
{
    public static Box2i Empty => new();

    [FieldOffset(sizeof(int) * 0)] internal int _left;
    [FieldOffset(sizeof(int) * 1)] internal int _bottom;
    [FieldOffset(sizeof(int) * 2)] internal int _right;
    [FieldOffset(sizeof(int) * 3)] internal int _top;

    [FieldOffset(sizeof(int) * 0)] internal Vector2i _bottomLeft;
    [FieldOffset(sizeof(int) * 2)] internal Vector2i _topRight;

    public int Left
    {
        readonly get => _left;
        set
        {
            if (value > _right)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Left cannot be greater than Right.");

            _left = value;
        }
    }

    public int Bottom
    {
        readonly get => _bottom;
        set
        {
            if (value > _top)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Bottom cannot be greater than Top.");

            _bottom = value;
        }
    }

    public int Right
    {
        readonly get => _right;
        set
        {
            if (value < _left)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Right cannot be less than Left.");

            _right = value;
        }
    }

    public int Top
    {
        readonly get => _top;
        set
        {
            if (value < _bottom)
                throw new ArgumentOutOfRangeException(nameof(value), value, "Top cannot be less than Bottom.");

            _top = value;
        }
    }

    public Vector2i BottomLeft
    {
        readonly get => _bottomLeft;
        set
        {
            if (value.X > _right)
                throw new ArgumentOutOfRangeException(nameof(value), value, "BottomLeft.X cannot be greater than Right.");

            if (value.Y > _top)
                throw new ArgumentOutOfRangeException(nameof(value), value, "BottomLeft.Y cannot be greater than Top.");

            _bottomLeft = value;
        }
    }

    public Vector2i TopRight
    {
        readonly get => _topRight;
        set
        {
            if (value.X < _left)
                throw new ArgumentOutOfRangeException(nameof(value), value, "TopRight.X cannot be less than Left.");

            if (value.Y < _bottom)
                throw new ArgumentOutOfRangeException(nameof(value), value, "TopRight.Y cannot be less than Bottom.");

            _topRight = value;
        }
    }

    public readonly Vector2i BottomRight => new(Right, Bottom);

    public readonly Vector2i TopLeft => new(Left, Top);

    public readonly int Width => _right - _left;

    public readonly int Height => _top - _bottom;

    public readonly Vector2i Size => new(Width, Height);

    public readonly int Area => Width * Height;
    public readonly Vector2 Center => new Vector2(_left + _right, _bottom + _top) / 2f;

    private static void Validate(int left, int bottom, int right, int top)
    {
        if (left > right)
            throw new ArgumentException("Left cannot be greater than Right.", nameof(left));

        if (bottom > top)
            throw new ArgumentException("Bottom cannot be greater than Top.", nameof(bottom));
    }

    public Box2i(Vector2i bottomLeft, Vector2i topRight)
    {
        Unsafe.SkipInit(out this);

        Validate(bottomLeft.X, bottomLeft.Y, topRight.X, topRight.Y);

        _bottomLeft = bottomLeft;
        _topRight = topRight;
    }

    public Box2i(int left, int bottom, int right, int top)
    {
        Unsafe.SkipInit(out this);

        Validate(left, bottom, right, top);

        _left = left;
        _right = right;
        _top = top;
        _bottom = bottom;
    }

    /// <summary>
    /// Creates a Box2i with no bounds validation applied, use at your own risk.
    /// </summary>
    internal static Box2i DangerousCreate(int left, int bottom, int right, int top)
    {
        Unsafe.SkipInit(out Box2i box);
        box._left = left;
        box._right = right;
        box._top = top;
        box._bottom = bottom;
        return box;
    }

    [Pure]
    public static Box2i FromDimensions(int left, int bottom, int width, int height)
    {
        return new Box2i(left, bottom, left + width, bottom + height);
    }

    [Pure]
    public static Box2i FromDimensions(Vector2i position, Vector2i size)
    {
        return FromDimensions(position.X, position.Y, size.X, size.Y);
    }

    [Pure]
    public static Box2i FromTwoPoints(Vector2i a, Vector2i b)
    {
        return new Box2i(Vector2i.ComponentMin(a, b), Vector2i.ComponentMax(a, b));
    }

    [Pure]
    public readonly bool Contains(int x, int y)
    {
        return Contains(new Vector2i(x, y));
    }

    [Pure]
    public readonly bool Contains(in Box2i inner)
        => Left <= inner.Left
           && Bottom <= inner.Bottom
           && Right >= inner.Right
           && Top >= inner.Top;

    [Pure]
    public readonly bool Contains(Vector2i point, bool closedRegion = true)
    {
        var xOk = closedRegion
            ? point.X >= Left ^ point.X > Right
            : point.X > Left ^ point.X >= Right;
        var yOk = closedRegion
            ? point.Y >= Bottom ^ point.Y > Top
            : point.Y > Bottom ^ point.Y >= Top;
        return xOk && yOk;
    }

    /// <summary>
    /// Unlike Contains this assumes the Vector2i occupies an entire tile so we need the point to the top-right of it for consideration.
    /// </summary>
    [Pure]
    public readonly bool ContainsTile(Vector2i tile, bool closedRegion = true)
    {
        if (closedRegion)
        {
            return tile.X >= Left
                   && tile.X + 1 <= Right
                   && tile.Y >= Bottom
                   && tile.Y + 1 <= Top;
        }

        return tile.X > Left
               && tile.X + 1 < Right
               && tile.Y > Bottom
               && tile.Y + 1 < Top;
    }

    [Pure]
    public readonly bool IsEmpty()
    {
        return Bottom >= Top || Left >= Right;
    }

    /// <summary>Returns a UIBox2 translated by the given amount.</summary>
    [Pure]
    public readonly Box2i Translated(Vector2i point)
    {
        return new Box2i(Left + point.X, Bottom + point.Y, Right + point.X, Top + point.Y);
    }

    /// <summary>
    ///     Returns the smallest rectangle that contains both of the rectangles.
    /// </summary>
    [Pure]
    public readonly Box2i Union(in Box2i other)
    {
        var botLeft = Vector2i.ComponentMin(BottomLeft, other.BottomLeft);
        var topRight = Vector2i.ComponentMax(TopRight, other.TopRight);

        if (botLeft.X <= topRight.X && botLeft.Y <= topRight.Y)
            return new Box2i(botLeft, topRight);

        return new Box2i();
    }

    /// <summary>
    /// Unions the box2i with the specified Vector2i.
    /// </summary>
    /// <remarks>
    /// Union treating other as a single point and not an entire tile.
    /// </remarks>
    [Pure]
    public readonly Box2i Union(in Vector2i other)
    {
        if (Contains(other))
            return this;

        var botLeft = Vector2i.ComponentMin(BottomLeft, other);
        var topRight = Vector2i.ComponentMax(TopRight, other);

        return new Box2i(botLeft, topRight);
    }

    /// <summary>
    /// Unions the box2i with the specified Vector2i.
    /// </summary>
    /// <remarks>
    /// Union treating other as an entire tile and not a single point.
    /// </remarks>
    [Pure]
    public readonly Box2i UnionTile(in Vector2i other)
    {
        if (ContainsTile(other))
            return this;

        var botLeft = Vector2i.ComponentMin(BottomLeft, other);
        var topRight = Vector2i.ComponentMax(TopRight, other + Vector2i.One);

        return new Box2i(botLeft, topRight);
    }

    // override object.Equals
    public readonly override bool Equals(object? obj)
    {
        if (obj is Box2i box)
        {
            return Equals(box);
        }

        return false;
    }

    public readonly bool Equals(Box2i other)
    {
        return other.Left == Left && other.Right == Right && other.Bottom == Bottom && other.Top == Top;
    }

    // override object.GetHashCode
    public readonly override int GetHashCode()
    {
        var code = Left.GetHashCode();
        code = (code * 929) ^ Right.GetHashCode();
        code = (code * 929) ^ Top.GetHashCode();
        code = (code * 929) ^ Bottom.GetHashCode();
        return code;
    }

    public static explicit operator Box2i(Box2 box)
    {
        return new Box2i((int) box.Left, (int) box.Bottom, (int) box.Right, (int) box.Top);
    }

    public static implicit operator Box2(Box2i box)
    {
        return new Box2(box.Left, box.Bottom, box.Right, box.Top);
    }

    public readonly override string ToString()
    {
        return $"({Left}, {Bottom}, {Right}, {Top})";
    }

    /// <summary>
    ///     Compares two objects for equality by value.
    /// </summary>
    public static bool operator ==(Box2i a, Box2i b)
    {
        return a.Equals(b);
    }

    public static bool operator !=(Box2i a, Box2i b)
    {
        return !a.Equals(b);
    }

    public readonly string ToString(string? format, IFormatProvider? formatProvider)
    {
        return ToString();
    }

    public readonly bool TryFormat(
        Span<char> destination,
        out int charsWritten,
        ReadOnlySpan<char> format,
        IFormatProvider? provider)
    {
        return FormatHelpers.TryFormatInto(
            destination,
            out charsWritten,
            $"({Left}, {Bottom}, {Right}, {Top})");
    }

    /// <summary>
    /// Multiplies each side of the box by the scalar.
    /// </summary>
    [Pure]
    public readonly Box2i Scale(int scalar)
    {
        return new Box2i(
            Left * scalar,
            Bottom * scalar,
            Right * scalar,
            Top * scalar);
    }

    [Pure]
    public readonly bool Intersects(in Box2i other)
    {
        return other._bottom <= _top
               && other._top >= _bottom
               && other._right >= _left
               && other._left <= _right;
    }

    [Pure]
    public readonly Box2i Enlarged(int size)
    {
        return new Box2i(Left - size, Bottom - size, Right + size, Top + size);
    }

    /// <summary>
    ///     Returns the intersection box created when two boxes overlap.
    /// </summary>
    [Pure]
    public readonly Box2i Intersect(in Box2i other)
    {
        var bottomLeft = Vector2i.ComponentMax(BottomLeft, other.BottomLeft);
        var topRight = Vector2i.ComponentMin(TopRight, other.TopRight);

        if (bottomLeft.X <= topRight.X && bottomLeft.Y <= topRight.Y)
            return new Box2i(bottomLeft, topRight);

        return new Box2i();
    }

    [Pure]
    public readonly bool IsValid()
    {
        return Right >= Left && Top >= Bottom;
    }

    [Pure]
    public readonly bool Encloses(in Box2i inner)
    {
        return Left < inner.Left && Bottom < inner.Bottom && Right > inner.Right && Top > inner.Top;
    }

    /// <summary>
    ///     Returns this box enlarged to also contain the specified position.
    /// </summary>
    [Pure]
    public readonly Box2i ExtendToContain(Vector2i vec)
    {
        return new Box2i(Vector2i.ComponentMin(BottomLeft, vec), Vector2i.ComponentMax(TopRight, vec));
    }

    /// <summary>
    /// Given a point, returns the closest point to it inside the box.
    /// </summary>
    [Pure]
    public readonly Vector2i ClosestPoint(in Vector2i position)
    {
        return new Vector2i(
            MathHelper.Clamp(position.X, Left, Right),
            MathHelper.Clamp(position.Y, Bottom, Top));
    }

    public static int Perimeter(in Box2i box)
        => (box.Width + box.Height) * 2;

    public static int UnionPerimeter(in Box2i a, in Box2i b)
    {
        var left = Math.Min(a._left, b._left);
        var bottom = Math.Min(a._bottom, b._bottom);
        var right = Math.Max(a._right, b._right);
        var top = Math.Max(a._top, b._top);

        return 2 * ((right - left) + (top - bottom));
    }

    [Pure]
    public static Box2i Union(Box2i a, Box2i b)
    {
        return new Box2i(
            Vector2i.ComponentMin(a.BottomLeft, b.BottomLeft),
            Vector2i.ComponentMax(a.TopRight, b.TopRight));
    }

    [Pure]
    public static Box2i Union(in Vector2i a, in Vector2i b)
    {
        return FromTwoPoints(a, b);
    }
}

/// <summary>
/// Iterates neighbouring tiles to a box2i.
/// </summary>
public struct Box2iEdgeEnumerator
{
    private readonly bool _corners;
    private readonly Box2i _box;
    private readonly int _offset;
    private int _x;
    private int _y;

    public Box2iEdgeEnumerator(Box2i box, bool corners, int offset = 1)
    {
        _box = box;
        _corners = corners;
        _x = _box.Left - offset;
        _y = _box.Bottom - offset;
        _offset = offset;
    }

    public bool MoveNext(out Vector2i index)
    {
        for (var x = _x; x < _box.Right + _offset; x++)
        {
            for (var y = _y; y < _box.Top + _offset; y++)
            {
                if (x != _box.Left - _offset &&
                    x != _box.Right + (_offset - 1) &&
                    y != _box.Bottom - _offset &&
                    y != _box.Top + (_offset - 1))
                {
                    continue;
                }

                if (!_corners &&
                    (x == _box.Left - _offset && (y == _box.Bottom - _offset || y == _box.Top + (_offset - 1)) ||
                     x == _box.Right && (y == _box.Bottom - _offset || y == _box.Top + (_offset - 1))))
                {
                    continue;
                }

                _x = x;
                _y = y + 1;

                if (_y == _box.Top + _offset)
                {
                    _x++;
                    _y = _box.Bottom - _offset;
                }

                index = new Vector2i(x, y);
                return true;
            }
        }

        index = default;
        return false;
    }
}
