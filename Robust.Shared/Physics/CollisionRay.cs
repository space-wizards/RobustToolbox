using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     A representation of a 2D ray.
    /// </summary>
    [Serializable]
    public readonly struct CollisionRay : IEquatable<CollisionRay> {

        private readonly Ray _ray;

        private readonly int _collisionMask;

        /// <summary>
        ///     Specifies the starting point of the ray.
        /// </summary>
        public Vector2 Position => _ray.Position;

        /// <summary>
        ///     Specifies the direction the ray is pointing.
        /// </summary>
        public Vector2 Direction => _ray.Direction;

        public int CollisionMask => _collisionMask;

        /// <summary>
        ///     Creates a new instance of a Ray.
        /// </summary>
        /// <param name="position">Starting position of the ray.</param>
        /// <param name="direction">Unit direction vector that the ray is pointing.</param>
        public CollisionRay(Vector2 position, Vector2 direction, int collisionMask)
        {
            _ray = new Ray(position, direction);
            _collisionMask = collisionMask;

        }

        #region Intersect Tests

        public bool Intersects(Box2 box, out float distance, out Vector2 hitPos)
            => _ray.Intersects(box, out distance, out hitPos);

        #endregion

        #region Equality

        /// <summary>
        ///     Determines if this Ray and another Ray are equivalent.
        /// </summary>
        /// <param name="other">Ray to compare to.</param>
        public bool Equals(CollisionRay other)
        {
            return Position.Equals(other.Position) && Direction.Equals(other.Direction);
        }

        /// <summary>
        ///     Determines if this ray and another object is equivalent.
        /// </summary>
        /// <param name="obj">Object to compare to.</param>
        public override bool Equals(object? obj)
        {
            if (obj is null) return false;
            return obj is CollisionRay ray && Equals(ray);
        }

        /// <summary>
        ///     Calculates the hash code of this Ray.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (Position.GetHashCode() * 397) ^ Direction.GetHashCode();
            }
        }

        /// <summary>
        ///     Determines if two instances of Ray are equal.
        /// </summary>
        /// <param name="a">Ray on the left side of the operator.</param>
        /// <param name="b">Ray on the right side of the operator.</param>
        public static bool operator ==(CollisionRay a, CollisionRay b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Determines if two instances of Ray are not equal.
        /// </summary>
        /// <param name="a">Ray on the left side of the operator.</param>
        /// <param name="b">Ray on the right side of the operator.</param>
        public static bool operator !=(CollisionRay a, CollisionRay b)
        {
            return !(a == b);
        }

        #endregion

        public static implicit operator Ray(CollisionRay a)
            => a._ray;

    }
}
