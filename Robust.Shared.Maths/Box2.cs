using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Runtime.Intrinsics;
using JetBrains.Annotations;
using Robust.Shared.Utility;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Axis Aligned rectangular box in world coordinates.
    ///     Uses a right-handed coordinate system. This means that X+ is to the right and Y+ up.
    /// </summary>
    [Serializable]
    [StructLayout(LayoutKind.Explicit)]
    public struct Box2 : IEquatable<Box2>, IApproxEquatable<Box2>, ISpanFormattable
    {
        /// <summary>
        ///     The X coordinate of the left edge of the box.
        /// </summary>
        [FieldOffset(sizeof(float) * 0)] public float Left;

        /// <summary>
        ///     The Y coordinate of the bottom of the box.
        /// </summary>
        [FieldOffset(sizeof(float) * 1)] public float Bottom;

        /// <summary>
        ///     The X coordinate of the right edge of the box.
        /// </summary>
        [FieldOffset(sizeof(float) * 2)] public float Right;

        /// <summary>
        ///     The Y coordinate of the top edge of the box.
        /// </summary>
        [FieldOffset(sizeof(float) * 3)] public float Top;

        [NonSerialized]
        [FieldOffset(sizeof(float) * 0)] public Vector2 BottomLeft;

        [NonSerialized]
        [FieldOffset(sizeof(float) * 2)] public Vector2 TopRight;

        [NonSerialized]
        [FieldOffset(sizeof(float) * 0)] public System.Numerics.Vector4 AsVector4;

        public readonly Vector2 BottomRight
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Right, Bottom);
        }

        public readonly Vector2 TopLeft
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Left, Top);
        }

        public readonly float Width
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathF.Abs(Right - Left);
        }

        public readonly float Height
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathF.Abs(Bottom - Top);
        }

        public readonly Vector2 Size
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(Width, Height);
        }

        /// <summary>
        /// Returns the highest of width or height.
        /// </summary>
        public readonly float MaxDimension
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => MathF.Max(Height, Width);
        }

        public readonly Vector2 Center
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => BottomLeft + Size * .5f;
        }

        public readonly Vector2 Extents
        {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => (TopRight - BottomLeft) * 0.5f;
        }

        public static Box2 Empty = new Box2();

        /// <summary>
        ///     A 1x1 unit box with the origin centered.
        /// </summary>
        public static readonly Box2 UnitCentered = new(-0.5f, -0.5f, 0.5f, 0.5f);

        public Box2(Vector2 bottomLeft, Vector2 topRight)
        {
            Unsafe.SkipInit(out this);

            BottomLeft = bottomLeft;
            TopRight = topRight;
        }

        public Box2(float left, float bottom, float right, float top)
        {
            Unsafe.SkipInit(out this);

            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public static Box2 FromDimensions(float left, float bottom, float width, float height)
        {
            return new(left, bottom, left + width, bottom + height);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public static Box2 FromDimensions(Vector2 bottomLeft, Vector2 size)
        {
            return FromDimensions(bottomLeft.X, bottomLeft.Y, size.X, size.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public static Box2 CenteredAround(Vector2 center, Vector2 size)
        {
            return FromDimensions(center - size / 2, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public static Box2 CentredAroundZero(Vector2 size)
        {
            return FromDimensions(-size / 2, size);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public static Box2 FromTwoPoints(Vector2 a, Vector2 b)
        {
            var min = Vector2.Min(a, b);
            var max = Vector2.Max(a, b);

            return new Box2(min, max);
        }

        public readonly bool HasNan()
        {
            return Vector128.EqualsAny(AsVector4.AsVector128(), Vector128.Create(float.NaN));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly bool Intersects(in Box2 other)
        {
            return other.Bottom <= this.Top && other.Top >= this.Bottom && other.Right >= this.Left &&
                   other.Left <= this.Right;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly Box2 Enlarged(float size)
        {
            return new(Left - size, Bottom - size, Right + size, Top + size);
        }

        /// <summary>
        ///     Returns the intersection box created when two Boxes overlap.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly Box2 Intersect(in Box2 other)
        {
            var ourLeftBottom = BottomLeft;
            var ourRightTop = TopRight;
            var otherLeftBottom = other.BottomLeft;
            var otherRightTop = other.TopRight;

            var max = Vector2.Max(ourLeftBottom, otherLeftBottom);
            var min = Vector2.Min(ourRightTop, otherRightTop);

            if (max.X <= min.X && max.Y <= min.Y)
                return new Box2(max.X, max.Y, min.X, min.Y);

            return new Box2();
        }

        /// <summary>
        ///     Returns how much two Boxes overlap from 0 to 1.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly float IntersectPercentage(in Box2 other)
        {
            var surfaceIntersect = Area(Intersect(other));

            return surfaceIntersect / (Area(this) + Area(other) - surfaceIntersect);
        }

        /// <summary>
        ///     Returns the smallest rectangle that contains both of the rectangles.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly Box2 Union(in Box2 other)
        {
            var ourLeftBottom = BottomLeft;
            var otherLeftBottom = other.BottomLeft;
            var ourRightTop = TopRight;
            var otherRightTop = other.TopRight;

            var leftBottom = Vector2.Min(ourLeftBottom, otherLeftBottom);
            var rightTop = Vector2.Max(ourRightTop, otherRightTop);

            if (leftBottom.X <= rightTop.X && leftBottom.Y <= rightTop.Y)
                return new Box2(leftBottom.X, leftBottom.Y, rightTop.X, rightTop.Y);

            return new Box2();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly bool IsEmpty()
        {
            return MathHelper.CloseToPercent(Width, 0.0f) && MathHelper.CloseToPercent(Height, 0.0f);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly bool Encloses(in Box2 inner)
        {
            return this.Left < inner.Left && this.Bottom < inner.Bottom && this.Right > inner.Right &&
                   this.Top > inner.Top;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly bool Contains(in Box2 inner)
            => Left <= inner.Left
               && Bottom <= inner.Bottom
               && Right >= inner.Right
               && Top >= inner.Top;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly bool Contains(float x, float y)
        {
            return Contains(new Vector2(x, y));
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly bool Contains(Vector2 point, bool closedRegion = true)
        {
            var xOk = closedRegion
                ? point.X >= Left ^ point.X > Right
                : point.X > Left ^ point.X >= Right;
            var yOk = closedRegion
                ? point.Y >= Bottom ^ point.Y > Top
                : point.Y > Bottom ^ point.Y >= Top;
            return xOk && yOk;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly Box2 Rounded(int digits)
        {
            return new Box2(MathF.Round(Left, digits), MathF.Round(Bottom, digits), MathF.Round(Right, digits),
                MathF.Round(Top, digits));
        }

        /// <summary>
        ///     Uniformly scales the box by a given scalar.
        ///     This scaling is done such that the center of the resulting box is the same as this box.
        ///     i.e. it scales around the center of the box, just changing width/height.
        /// </summary>
        /// <param name="scalar">Value to scale the box by.</param>
        /// <returns>Scaled box.</returns>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly Box2 Scale(float scalar)
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly Box2 Scale(Vector2 scale)
        {
            var center = Center;
            var halfSize = (Size / 2) * scale;
            return new Box2(
                center - halfSize,
                center + halfSize);
        }

        /// <summary>Returns a Box2 translated by the given amount.</summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly Box2 Translated(Vector2 point)
        {
            return new(Left + point.X, Bottom + point.Y, Right + point.X, Top + point.Y);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly bool Equals(Box2 other)
        {
            return Left.Equals(other.Left) && Right.Equals(other.Right) && Top.Equals(other.Top) &&
                   Bottom.Equals(other.Bottom);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override bool Equals(object? obj)
        {
            if (obj is null) return false;
            return obj is Box2 box2 && Equals(box2);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public readonly override int GetHashCode()
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
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator ==(Box2 a, Box2 b)
        {
            return MathHelper.CloseToPercent(a.Bottom, b.Bottom) &&
                   MathHelper.CloseToPercent(a.Right, b.Right) &&
                   MathHelper.CloseToPercent(a.Top, b.Top) &&
                   MathHelper.CloseToPercent(a.Left, b.Left);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool operator !=(Box2 a, Box2 b)
        {
            return !(a == b);
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

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Area(in Box2 box)
            => box.Width * box.Height;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static float Perimeter(in Box2 box)
            => (box.Width + box.Height) * 2;

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public static Box2 Union(in Vector2 a, in Vector2 b)
        {
            var vecA = new Vector2(a.X, a.Y);
            var vecB = new Vector2(b.X, b.Y);

            var min = Vector2.Min(vecA, vecB);
            var max = Vector2.Max(vecA, vecB);

            return new Box2(min.X, min.Y, max.X, max.Y);
        }

        /// <summary>
        ///     Returns this box enlarged to also contain the specified position.
        /// </summary>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        [Pure]
        public readonly Box2 ExtendToContain(Vector2 vec)
        {
            var leftBottom = new Vector2(Left, Bottom);
            var rightTop = new Vector2(Right, Top);
            var vector = new Vector2(vec.X, vec.Y);

            var min = Vector2.Min(vector, leftBottom);
            var max = Vector2.Max(vector, rightTop);

            return new Box2(min.X, min.Y, max.X, max.Y);
        }

        /// <summary>
        /// Given a point, returns the closest point to it inside the box.
        /// </summary>
        [Pure]
        public readonly Vector2 ClosestPoint(in Vector2 position)
        {
            // clamp the point to the border of the box
            var cx = MathHelper.Clamp(position.X, Left, Right);
            var cy = MathHelper.Clamp(position.Y, Bottom, Top);

            return new Vector2(cx, cy);
        }

        public bool EqualsApprox(Box2 other)
        {
            return MathHelper.CloseToPercent(Left, other.Left)
                   && MathHelper.CloseToPercent(Bottom, other.Bottom)
                   && MathHelper.CloseToPercent(Right, other.Right)
                   && MathHelper.CloseToPercent(Top, other.Top);
        }

        public bool EqualsApprox(Box2 other, double tolerance)
        {
            return MathHelper.CloseToPercent(Left, other.Left, tolerance)
                   && MathHelper.CloseToPercent(Bottom, other.Bottom, tolerance)
                   && MathHelper.CloseToPercent(Right, other.Right, tolerance)
                   && MathHelper.CloseToPercent(Top, other.Top, tolerance);
        }
    }
}
