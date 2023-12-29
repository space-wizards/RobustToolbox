using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths
{
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct Box2i : IEquatable<Box2i>, ISpanFormattable
    {
        public static Box2i Empty => new();

        [FieldOffset(sizeof(int) * 0)] public int Left;
        [FieldOffset(sizeof(int) * 1)] public int Bottom;
        [FieldOffset(sizeof(int) * 2)] public int Right;
        [FieldOffset(sizeof(int) * 3)] public int Top;

        [FieldOffset(sizeof(int) * 0)] public Vector2i BottomLeft;
        [FieldOffset(sizeof(int) * 2)] public Vector2i TopRight;

        public readonly Vector2i BottomRight => new(Right, Bottom);
        public readonly Vector2i TopLeft => new(Left, Top);
        public readonly int Width => Math.Abs(Right - Left);
        public readonly int Height => Math.Abs(Top - Bottom);
        public readonly Vector2i Size => new(Width, Height);

        public readonly int Area => Width * Height;
        public readonly Vector2 Center => Size / 2f + BottomLeft;

        public Box2i(Vector2i bottomLeft, Vector2i topRight)
        {
            Unsafe.SkipInit(out this);

            BottomLeft = bottomLeft;
            TopRight = topRight;
        }

        public Box2i(int left, int bottom, int right, int top)
        {
            Unsafe.SkipInit(out this);

            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public static Box2i FromDimensions(int left, int bottom, int width, int height)
        {
            return new(left, bottom, left + width, bottom + height);
        }

        public static Box2i FromDimensions(Vector2i position, Vector2i size)
        {
            return FromDimensions(position.X, position.Y, size.X, size.Y);
        }

        public readonly bool Contains(int x, int y)
        {
            return Contains(new Vector2i(x, y));
        }

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
        public readonly bool ContainsTile(Vector2i tile, bool closedRegion = true)
        {
            var xOk = closedRegion
                ? tile.X >= Left ^ tile.X + 1 > Right
                : tile.X > Left ^ tile.X + 1 >= Right;
            var yOk = closedRegion
                ? tile.Y >= Bottom ^ tile.Y + 1 > Top
                : tile.Y > Bottom ^ tile.Y + 1 >= Top;
            return xOk && yOk;
        }

        public readonly bool IsEmpty()
        {
            return Bottom == Top || Left == Right;
        }

        /// <summary>Returns a UIBox2 translated by the given amount.</summary>
        public readonly Box2i Translated(Vector2i point)
        {
            return new(Left + point.X, Bottom + point.Y, Right + point.X, Top + point.Y);
        }

        /// <summary>
        ///     Returns the smallest rectangle that contains both of the rectangles.
        /// </summary>
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
            return new((int) box.Left, (int) box.Bottom, (int) box.Right, (int) box.Top);
        }

        public static implicit operator Box2(Box2i box)
        {
            return new(box.Left, box.Bottom, box.Right, box.Top);
        }

        public readonly override string ToString()
        {
            return $"({Left}, {Bottom}, {Right}, {Top})";
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
        public Box2i Scale(int scalar)
        {
            return new Box2i(
                Left * scalar,
                Bottom * scalar,
                Right * scalar,
                Top * scalar);
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
}
