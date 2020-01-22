using System;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Axis Aligned rectangular box in world coordinates.
    ///     Uses a right-handed coordinate system. This means that X+ is to the right and Y+ up.
    /// </summary>
    [Serializable]
    public readonly struct Box2 : IEquatable<Box2>
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
        public float Height => Math.Abs(Bottom - Top);
        public Vector2 Size => new Vector2(Width, Height);
        public Vector2 Center => BottomLeft + Size / 2;

        /// <summary>
        ///     A 1x1 unit box with the origin centered.
        /// </summary>
        public static readonly Box2 UnitCentered = new Box2(-0.5f, -0.5f, 0.5f, 0.5f);

        public Box2(Vector2 bottomLeft, Vector2 topRight) : this(bottomLeft.X, bottomLeft.Y, topRight.X, topRight.Y)
        {
        }

        public Box2(float left, float bottom, float right, float top)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        public static Box2 FromDimensions(float left, float bottom, float width, float height)
        {
            return new Box2(left, bottom, left + width, bottom + height);
        }

        public static Box2 FromDimensions(Vector2 bottomLeft, Vector2 size)
        {
            return FromDimensions(bottomLeft.X, bottomLeft.Y, size.X, size.Y);
        }

        public static Box2 CenteredAround(Vector2 center, Vector2 size)
        {
            return FromDimensions(center - size / 2, size);
        }

        public bool Intersects(in Box2 other)
        {
            return other.Bottom <= this.Top && other.Top >= this.Bottom && other.Right >= this.Left &&
                   other.Left <= this.Right;
        }

        public Box2 Enlarged(float size)
        {
            return new Box2(Left - size, Bottom - size, Right + size, Top + size);
        }

        /// <summary>
        ///     Returns the intersection box created when two Boxes overlap.
        /// </summary>
        public Box2 Intersect(in Box2 other)
        {
            var left   = Math.Max(Left,   other.Left);
            var right  = Math.Min(Right,  other.Right);
            var bottom = Math.Max(Bottom, other.Bottom);
            var top    = Math.Min(Top,    other.Top);

            if (left <= right && bottom <= top)
                return new Box2(left, bottom, right, top);

            return new Box2();
        }

        /// <summary>
        ///     Returns the smallest rectangle that contains both of the rectangles.
        /// </summary>
        public Box2 Union(in Box2 other)
        {
            var left   = Math.Min(Left,   other.Left);
            var right  = Math.Max(Right,  other.Right);
            var bottom = Math.Min(Bottom, other.Bottom);
            var top    = Math.Max(Top,    other.Top);

            if (left <= right && bottom <= top)
                return new Box2(left, bottom, right, top);

            return new Box2();
        }

        public bool IsEmpty()
        {
            return FloatMath.CloseTo(Width, 0.0f) && FloatMath.CloseTo(Height, 0.0f);
        }

        public bool Encloses(in Box2 inner)
        {
            return this.Left < inner.Left && this.Bottom < inner.Bottom && this.Right > inner.Right &&
                   this.Top > inner.Top;
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
                ? point.Y >= Bottom ^ point.Y > Top
                : point.Y > Bottom ^ point.Y >= Top;
            return xOk && yOk;
        }

        /// <summary>
        ///     Uniformly scales the box by a given scalar.
        ///     This scaling is done such that the center of the resulting box is the same as this box.
        ///     i.e. it scales around the center of the box, just changing width/height.
        /// </summary>
        /// <param name="scalar">Value to scale the box by.</param>
        /// <returns>Scaled box.</returns>
        public Box2 Scale(float scalar)
        {
            if (scalar < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(scalar), scalar, "Scalar cannot be negative.");
            }

            var center = Center;
            var halfSize = Size / 2 * scalar;
            return new Box2(
                center - halfSize,
                center + halfSize);
        }

        /// <summary>Returns a Box2 translated by the given amount.</summary>
        public Box2 Translated(Vector2 point)
        {
            return new Box2(Left + point.X, Bottom + point.Y, Right + point.X, Top + point.Y);
        }

        public bool Equals(Box2 other)
        {
            return Left.Equals(other.Left) && Right.Equals(other.Right) && Top.Equals(other.Top) &&
                   Bottom.Equals(other.Bottom);
        }

        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            return obj is Box2 box2 && Equals(box2);
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
        public static bool operator ==(Box2 a, Box2 b)
        {
            return FloatMath.CloseTo(a.Bottom, b.Bottom) &&
                   FloatMath.CloseTo(a.Right, b.Right) &&
                   FloatMath.CloseTo(a.Top, b.Top) &&
                   FloatMath.CloseTo(a.Left, b.Left);
        }

        public static bool operator !=(Box2 a, Box2 b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return $"({Left}, {Bottom}, {Right}, {Top})";
        }
    }
}
