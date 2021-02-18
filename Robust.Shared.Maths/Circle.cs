using System;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Represents a circle with a 2D position and a radius.
    /// </summary>
    [Serializable]
    public struct Circle : IEquatable<Circle>
    {
        /// <summary>
        ///     Position of the circle in 2D space.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        ///     Radius of the circle.
        /// </summary>
        public float Radius;

        /// <summary>
        ///     Constructs an instance of this struct.
        /// </summary>
        public Circle(Vector2 position, float radius)
        {
            Position = position;
            Radius = radius;
        }

        /// <summary>
        ///     Checks if this circle contains a point.
        /// </summary>
        public readonly bool Contains(Vector2 point)
        {
            return Contains(point.X, point.Y);
        }

        /// <summary>
        ///     Checks if this circle intersects with another circle.
        /// </summary>
        public readonly bool Intersects(Circle circle)
        {
            var dx = Position.X - circle.Position.X;
            var dy = Position.Y - circle.Position.Y;
            var sumR = Radius + circle.Radius;

            return dx * dx + dy * dy < sumR * sumR;
        }

        /// <summary>
        ///     Checks if this circle intersects with a box.
        /// </summary>
        public readonly bool Intersects(Box2 box)
        {
            // Construct the point in / on the box nearest to the center of the circle.
            float closestX = MathHelper.Median(box.Left, box.Right, Position.X);
            float closestY = MathHelper.Median(box.Bottom, box.Top, Position.Y);

            // Check if the circle contains that point.
            return Contains(closestX, closestY);
        }

        private readonly bool Contains(float x, float y)
        {
            var dx = Position.X - x;
            var dy = Position.Y - y;

            var d2 = dx * dx + dy * dy;
            var r2 = Radius * Radius;

            // Instead of d2 <= r2, use MathHelper.CloseTo to allow for some tolerance.
            return (d2 < r2) || MathHelper.CloseTo(d2, r2);
        }

        /// <inheritdoc />
        public readonly bool Equals(Circle other)
        {
            return Position.Equals(other.Position) && Radius.Equals(other.Radius);
        }

        /// <inheritdoc />
        public override readonly bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Circle circle && Equals(circle);
        }

        /// <inheritdoc />
        public override readonly int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ Radius.GetHashCode();
            }
        }

        public static bool operator ==(Circle a, Circle b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Circle a, Circle b)
        {
            return !(a == b);
        }

        public override readonly string ToString()
        {
            return $"Circle ({Position.X}, {Position.Y}), {Radius} r";
        }
    }
}
