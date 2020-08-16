using System;

namespace Robust.Shared.Maths
{
    [Serializable]
    public readonly struct UIBox2i : IEquatable<UIBox2i>
    {
        public readonly int Left;
        public readonly int Right;
        public readonly int Top;
        public readonly int Bottom;

        public Vector2i BottomRight => new Vector2i(Right, Bottom);
        public Vector2i TopLeft => new Vector2i(Left, Top);
        public Vector2i TopRight => new Vector2i(Right, Top);
        public Vector2i BottomLeft => new Vector2i(Left, Bottom);
        public int Width => Math.Abs(Right - Left);
        public int Height => Math.Abs(Top - Bottom);
        public Vector2i Size => new Vector2i(Width, Height);

        public UIBox2i(Vector2i topLeft, Vector2i bottomRight) : this(topLeft.X, topLeft.Y, bottomRight.X, bottomRight.Y)
        {
        }

        public UIBox2i(int left, int top, int right, int bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public static UIBox2i FromDimensions(int left, int top, int width, int height)
        {
            return new UIBox2i(left, top, left + width, top + height);
        }

        public static UIBox2i FromDimensions(Vector2i position, Vector2i size)
        {
            return FromDimensions(position.X, position.Y, size.X, size.Y);
        }

        public bool Contains(int x, int y)
        {
            return Contains(new Vector2i(x, y));
        }

        public bool Contains(Vector2i point, bool closedRegion = true)
        {
            var xOk = closedRegion
                ? point.X >= Left ^ point.X > Right
                : point.X > Left ^ point.X >= Right;
            var yOk = closedRegion
                ? point.Y >= Top ^ point.Y > Bottom
                : point.Y > Top ^ point.Y >= Bottom;
            return xOk && yOk;
        }

        /// <summary>Returns a UIBox2 translated by the given amount.</summary>
        public UIBox2i Translated(Vector2i point)
        {
            return new UIBox2i(Left + point.X, Top + point.Y, Right + point.X, Bottom + point.Y);
        }

        /// <summary>
        ///     Calculates the "intersection" of this and another box.
        ///     Basically, the smallest region that fits in both boxes.
        /// </summary>
        /// <param name="other">The box to calculate the intersection with.</param>
        /// <returns>
        ///     <c>null</c> if there is no intersection, otherwise the smallest region that fits in both boxes.
        /// </returns>
        public UIBox2i? Intersection(in UIBox2i other)
        {
            if (!Intersects(other))
            {
                return null;
            }
            return new UIBox2i(
                Vector2i.ComponentMax(TopLeft, other.TopLeft),
                Vector2i.ComponentMin(BottomRight, other.BottomRight));
        }

        public bool Intersects(in UIBox2i other)
        {
            return other.Bottom >= this.Top && other.Top <= this.Bottom && other.Right >= this.Left &&
                   other.Left <= this.Right;
        }

        // override object.Equals
        public override bool Equals(object? obj)
        {
            if (obj is UIBox2i box)
            {
                return Equals(box);
            }

            return false;
        }

        public bool Equals(UIBox2i other)
        {
            return other.Left == Left && other.Right == Right && other.Bottom == Bottom && other.Top == Top;
        }

        // override object.GetHashCode
        public override int GetHashCode()
        {
            var code = Left.GetHashCode();
            code = (code * 929) ^ Right.GetHashCode();
            code = (code * 929) ^ Top.GetHashCode();
            code = (code * 929) ^ Bottom.GetHashCode();
            return code;
        }

        public static explicit operator UIBox2i(UIBox2 box)
        {
            return new UIBox2i((int) box.Left, (int) box.Top, (int) box.Right, (int) box.Bottom);
        }

        public static implicit operator UIBox2(UIBox2i box)
        {
            return new UIBox2(box.Left, box.Top, box.Right, box.Bottom);
        }

        public static UIBox2i operator +(UIBox2i box, (int lo, int to, int ro, int bo) offsets)
        {
            var (lo, to, ro, bo) = offsets;

            return new UIBox2i(box.Left + lo, box.Top + to, box.Right + ro, box.Bottom + bo);
        }

        public override string ToString()
        {
            return $"({Left}, {Top}, {Right}, {Bottom})";
        }
    }
}
