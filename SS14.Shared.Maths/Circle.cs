using System;

namespace SS14.Shared.Maths
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
        public readonly Vector2 Position;

        /// <summary>
        ///     Radius of the circle.
        /// </summary>
        public readonly float Radius;

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
        public bool Contains(Vector2 point)
        {
            var dx = System.Math.Abs(point.X - Position.X);
            var dy = System.Math.Abs(point.Y - Position.Y);
            var r = Radius;

            return (dx * dx + dy * dy <= r * r);
        }

        /// <summary>
        ///     Checks if this circle intersects with another circle.
        /// </summary>
        public bool Intersects(Circle circle)
        {
            var dx = Position.X - circle.Position.X;
            var dy = Position.Y - circle.Position.Y;

            return System.Math.Sqrt(dx * dx + dy * dy) < Radius + circle.Radius;
        }

        /// <summary>
        ///     Checks if this circle intersects with a box.
        /// </summary>
        public bool Intersects(Box2 aabb)
        {
            var dx = Position.X - System.Math.Max(aabb.Left, System.Math.Min(Position.X, aabb.Left + aabb.Height));
            var dy = Position.Y - System.Math.Max(aabb.Top, System.Math.Min(Position.Y, aabb.Top + aabb.Height));
            var r = Radius;

            return (dx * dx + dy * dy <= r * r);
        }

        /// <inheritdoc />
        public bool Equals(Circle other)
        {
            return Position.Equals(other.Position) && Radius.Equals(other.Radius);
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Circle circle && Equals(circle);
        }

        /// <inheritdoc />
        public override int GetHashCode()
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
    }
}
