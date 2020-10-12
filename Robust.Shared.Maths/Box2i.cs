using System;

namespace Robust.Shared.Maths
{
    [Serializable]
    public struct Box2i : IEquatable<Box2i>
    {
        public int Left;
        public int Right;
        public int Top;
        public int Bottom;

        public readonly Vector2i BottomRight => new Vector2i(Right, Bottom);
        public readonly Vector2i TopLeft => new Vector2i(Left, Top);
        public readonly Vector2i TopRight => new Vector2i(Right, Top);
        public readonly Vector2i BottomLeft => new Vector2i(Left, Bottom);
        public readonly int Width => Math.Abs(Right - Left);
        public readonly int Height => Math.Abs(Top - Bottom);
        public readonly Vector2i Size => new Vector2i(Width, Height);

        public Box2i(Vector2i bottomLeft, Vector2i topRight) : this(bottomLeft.X, bottomLeft.Y, topRight.X, topRight.Y)
        {
        }

        public Box2i(int left, int bottom, int right, int top)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public static Box2i FromDimensions(int left, int bottom, int width, int height)
        {
            return new Box2i(left, bottom, left + width, bottom + height);
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

        /// <summary>Returns a UIBox2 translated by the given amount.</summary>
        public readonly Box2i Translated(Vector2i point)
        {
            return new Box2i(Left + point.X, Bottom + point.Y, Right + point.X, Top + point.Y);
        }

        /// <summary>
        ///     Returns the smallest rectangle that contains both of the rectangles.
        /// </summary>
        public readonly Box2i Union(in Box2i other)
        {
            var left = Math.Min(Left, other.Left);
            var right = Math.Max(Right, other.Right);
            var bottom = Math.Min(Bottom, other.Bottom);
            var top = Math.Max(Top, other.Top);

            if (left <= right && bottom <= top)
                return new Box2i(left, bottom, right, top);

            return new Box2i();
        }

        // override object.Equals
        public override readonly bool Equals(object? obj)
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
        public override readonly int GetHashCode()
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

        public override readonly string ToString()
        {
            return $"({Left}, {Bottom}, {Right}, {Top})";
        }
    }
}
