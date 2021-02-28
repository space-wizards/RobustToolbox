using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics.Collision
{
    /// <summary>
    /// A rectangle that is always axis-aligned.
    /// </summary>
    [Serializable]
    internal readonly struct AlignedRectangle : IEquatable<AlignedRectangle>
    {
        /// <summary>
        /// Center point of the rectangle in world space.
        /// </summary>
        public readonly Vector2 Center;

        /// <summary>
        /// Half of the total width and height of the rectangle.
        /// </summary>
        public readonly Vector2 HalfExtents;

        /// <summary>
        ///     A 1x1 unit rectangle with the origin centered on the world origin.
        /// </summary>
        public static readonly AlignedRectangle UnitCentered = new(new Vector2(0.5f, 0.5f));

        /// <summary>
        ///     The lower X coordinate of the left edge of the box.
        /// </summary>
        public float Left => Center.X - HalfExtents.X;

        /// <summary>
        ///     The higher X coordinate of the right edge of the box.
        /// </summary>
        public float Right => Center.X + HalfExtents.X;

        /// <summary>
        ///     The lower Y coordinate of the top edge of the box.
        /// </summary>
        public float Bottom => Center.Y + HalfExtents.Y;

        /// <summary>
        ///     The higher Y coordinate of the bottom of the box.
        /// </summary>
        public float Top => Center.Y + HalfExtents.Y;

        public AlignedRectangle(Box2 box)
        {
            var halfWidth = box.Width / 2;
            var halfHeight = box.Height / 2;

            HalfExtents = new Vector2(halfWidth, halfHeight);
            Center = new Vector2(box.Left + halfWidth, box.Height + halfHeight);
        }

        public AlignedRectangle(Vector2 halfExtents)
        {
            Center = default;
            HalfExtents = halfExtents;
        }

        public AlignedRectangle(Vector2 center, Vector2 halfExtents)
        {
            Center = center;
            HalfExtents = halfExtents;
        }

        /// <summary>
        /// Given a point, returns the closest point to it inside the box.
        /// </summary>
        public Vector2 ClosestPoint(in Vector2 position)
        {
            // clamp the point to the border of the box
            var cx = MathHelper.Clamp(position.X, Left, Right);
            var cy = MathHelper.Clamp(position.Y, Bottom, Top);

            return new Vector2(cx, cy);
        }

        #region Equality members

        public bool Equals(AlignedRectangle other)
        {
            return Center.Equals(other.Center) && HalfExtents.Equals(other.HalfExtents);
        }

        public override bool Equals(object? obj)
        {
            return obj is AlignedRectangle other && Equals(other);
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Center, HalfExtents);
        }

        public static bool operator ==(AlignedRectangle left, AlignedRectangle right) {
            return left.Equals(right);
        }

        public static bool operator !=(AlignedRectangle left, AlignedRectangle right) {
            return !left.Equals(right);
        }

        #endregion

        /// <summary>
        /// Returns the string representation of this object.
        /// </summary>
        public override string ToString()
        {
            return $"({Left}, {Bottom}, {Right}, {Top})";
        }
    }
}
