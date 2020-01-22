using System;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Axis Aligned rectangular box in screen coordinates.
    ///     Uses a left-handed coordinate system. This means that X+ is to the right and Y+ down.
    /// </summary>
    [Serializable]
    public readonly struct UIBox2 : IEquatable<UIBox2>
    {
        /// <summary>
        ///     The X coordinate of the left edge of the box.
        /// </summary>
        public readonly float Left;

        /// <summary>
        ///     The X coordinate of the right edge of the box.
        /// </summary>
        public readonly float Right;

        /// <summary>
        ///     The Y coordinate of the top edge of the box.
        /// </summary>
        public readonly float Top;

        /// <summary>
        ///     The Y coordinate of the bottom of the box.
        /// </summary>
        public readonly float Bottom;

        public Vector2 BottomRight => new Vector2(Right, Bottom);
        public Vector2 TopLeft => new Vector2(Left, Top);
        public Vector2 TopRight => new Vector2(Right, Top);
        public Vector2 BottomLeft => new Vector2(Left, Bottom);
        public float Width => Math.Abs(Right - Left);
        public float Height => Math.Abs(Top - Bottom);
        public Vector2 Size => new Vector2(Width, Height);
        public Vector2 Center => TopLeft + Size / 2;

        public UIBox2(Vector2 leftTop, Vector2 rightBottom) : this(leftTop.X, leftTop.Y, rightBottom.X, rightBottom.Y)
        {
        }

        public UIBox2(float left, float top, float right, float bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public static UIBox2 FromDimensions(float left, float top, float width, float height)
        {
            return new UIBox2(left, top, left + width, top + height);
        }

        public static UIBox2 FromDimensions(Vector2 leftTopPosition, Vector2 size)
        {
            return FromDimensions(leftTopPosition.X, leftTopPosition.Y, size.X, size.Y);
        }

        public bool Intersects(UIBox2 other)
        {
            return other.Bottom >= this.Top && other.Top <= this.Bottom && other.Right >= this.Left &&
                   other.Left <= this.Right;
        }

        public bool IsEmpty()
        {
            return FloatMath.CloseTo(Width, 0.0f) && FloatMath.CloseTo(Height, 0.0f);
        }

        public bool Encloses(UIBox2 inner)
        {
            return this.Left < inner.Left && this.Bottom > inner.Bottom && this.Right > inner.Right &&
                   this.Top < inner.Top;
        }

        public bool Contains(float x, float y)
        {
            return Contains(new Vector2(x, y));
        }

        public bool Contains(Vector2 point, bool closedRegion = true)
        {
            var xOk = closedRegion
                ? point.X >= Left ^ point.X > Right
                : point.X > Left ^ point.X >= Right;
            var yOk = closedRegion
                ? point.Y >= Top ^ point.Y > Bottom
                : point.Y > Top ^ point.Y >= Bottom;
            return xOk && yOk;
        }

        /// <summary>
        ///     Uniformly scales the box by a given scalar.
        ///     This scaling is done such that the center of the resulting box is the same as this box.
        ///     i.e. it scales around the center of the box, just changing width/height.
        /// </summary>
        /// <param name="scalar">Value to scale the box by.</param>
        /// <returns>Scaled box.</returns>
        public UIBox2 Scale(float scalar)
        {
            if (scalar < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scalar), scalar, "Scalar cannot be negative.");
            }

            var center = Center;
            var halfSize = Size / 2 * scalar;
            return new UIBox2(
                center - halfSize,
                center + halfSize);
        }

        /// <summary>Returns a UIBox2 translated by the given amount.</summary>
        public UIBox2 Translated(Vector2 point)
        {
            return new UIBox2(Left + point.X, Top + point.Y, Right + point.X, Bottom + point.Y);
        }

        public bool Equals(UIBox2 other)
        {
            return Left.Equals(other.Left) && Right.Equals(other.Right) && Top.Equals(other.Top) &&
                   Bottom.Equals(other.Bottom);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            return obj is UIBox2 box2 && Equals(box2);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = Left.GetHashCode();
                hashCode = (hashCode * 397) ^ Right.GetHashCode();
                hashCode = (hashCode * 397) ^ Top.GetHashCode();
                hashCode = (hashCode * 397) ^ Bottom.GetHashCode();
                return hashCode;
            }
        }

        /// <summary>
        ///     Compares two objects for equality by value.
        /// </summary>
        public static bool operator ==(UIBox2 a, UIBox2 b)
        {
            return FloatMath.CloseTo(a.Bottom, b.Bottom) &&
                   FloatMath.CloseTo(a.Right, b.Right) &&
                   FloatMath.CloseTo(a.Top, b.Top) &&
                   FloatMath.CloseTo(a.Left, b.Left);
        }

        public static bool operator !=(UIBox2 a, UIBox2 b)
        {
            return !(a == b);
        }

        public static UIBox2 operator +(UIBox2 box, (float lo, float to, float ro, float bo) offsets)
        {
            var (lo, to, ro, bo) = offsets;

            return new UIBox2(box.Left + lo, box.Top + to, box.Right + ro, box.Bottom + bo);
        }

        public override string ToString()
        {
            return $"({Left}, {Top}, {Right}, {Bottom})";
        }
    }
}
