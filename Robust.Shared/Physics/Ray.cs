using Robust.Shared.Map;
using Robust.Shared.Utility;
using System;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     A representation of a 2D ray.
    /// </summary>
    [Serializable]
    public readonly struct Ray : IEquatable<Ray>
    {
        private readonly Vector2 _position;
        private readonly Vector2 _direction;
        private readonly int _collisionMask;

        /// <summary>
        ///     Specifies the starting point of the ray.
        /// </summary>
        public Vector2 Position => _position;

        /// <summary>
        ///     Specifies the direction the ray is pointing.
        /// </summary>
        public Vector2 Direction => _direction;

        public int CollisionMask => _collisionMask;

        /// <summary>
        ///     Creates a new instance of a Ray.
        /// </summary>
        /// <param name="position">Starting position of the ray.</param>
        /// <param name="direction">Unit direction vector that the ray is pointing.</param>
        public Ray(Vector2 position, Vector2 direction, int collisionMask)
        {
            _position = position;
            _direction = direction;
            _collisionMask = collisionMask;

            DebugTools.Assert(FloatMath.CloseTo(_direction.LengthSquared, 1));

        }

        #region Intersect Tests

        public bool Intersects(Box2 box, out float distance, out Vector2 hitPos)
        {
            hitPos = Vector2.Zero;
            distance = 0;

            var tmin = 0.0f; // set to -FLT_MAX to get first hit on line
            var tmax = float.MaxValue; // set to max distance ray can travel (for segment)
            const float epsilon = 1.0E-07f;

            // X axis slab
            {
                if (Math.Abs(_direction.X) < epsilon)
                {
                    // ray is parallel to this slab, it will never hit unless ray is inside box
                    if (_position.X < Math.Min(box.Left, box.Right) || _position.X > Math.Max(box.Left, box.Right))
                    {
                        return false;
                    }
                }

                // calculate intersection t value of ray with near and far plane of slab
                var ood = 1.0f / _direction.X;
                var t1 = (Math.Min(box.Left, box.Right) - _position.X) * ood;
                var t2 = (Math.Max(box.Left, box.Right) - _position.X) * ood;

                // Make t1 be the intersection with near plane, t2 with far plane
                if (t1 > t2)
                    MathHelper.Swap(ref t1, ref t2);

                // Compute the intersection of slab intersection intervals
                tmin = Math.Max(t1, tmin);
                tmax = Math.Min(t2, tmax); // Is this Min (SE) or Max(Textbook)

                // Exit with no collision as soon as slab intersection becomes empty
                if (tmin > tmax)
                {
                    return false;
                }
            }

            // Y axis slab
            {
                if (Math.Abs(_direction.Y) < epsilon)
                {
                    // ray is parallel to this slab, it will never hit unless ray is inside box
                    if (_position.Y < Math.Min(box.Top, box.Bottom) || _position.Y > Math.Max(box.Top, box.Bottom))
                    {
                        return false;
                    }
                }

                // calculate intersection t value of ray with near and far plane of slab
                var ood = 1.0f / _direction.Y;
                var t1 = (Math.Min(box.Top, box.Bottom) - _position.Y) * ood;
                var t2 = (Math.Max(box.Top, box.Bottom) - _position.Y) * ood;

                // Make t1 be the intersection with near plane, t2 with far plane
                if (t1 > t2)
                    MathHelper.Swap(ref t1, ref t2);

                // Compute the intersection of slab intersection intervals
                tmin = Math.Max(t1, tmin);
                tmax = Math.Min(t2, tmax); // Is this Min (SE) or Max(Textbook)

                // Exit with no collision as soon as slab intersection becomes empty
                if (tmin > tmax)
                {
                    return false;
                }
            }

            // Ray intersects all slabs. Return point and intersection t value
            hitPos = _position + _direction * tmin;
            distance = tmin;
            return true;
        }

        #endregion

        #region Equality

        /// <summary>
        ///     Determines if this Ray and another Ray are equivalent.
        /// </summary>
        /// <param name="other">Ray to compare to.</param>
        public bool Equals(Ray other)
        {
            return _position.Equals(other._position) && _direction.Equals(other._direction);
        }

        /// <summary>
        ///     Determines if this ray and another object is equivalent.
        /// </summary>
        /// <param name="obj">Object to compare to.</param>
        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            return obj is Ray ray && Equals(ray);
        }

        /// <summary>
        ///     Calculates the hash code of this Ray.
        /// </summary>
        public override int GetHashCode()
        {
            unchecked
            {
                return (_position.GetHashCode() * 397) ^ _direction.GetHashCode();
            }
        }

        /// <summary>
        ///     Determines if two instances of Ray are equal.
        /// </summary>
        /// <param name="a">Ray on the left side of the operator.</param>
        /// <param name="b">Ray on the right side of the operator.</param>
        public static bool operator ==(Ray a, Ray b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Determines if two instances of Ray are not equal.
        /// </summary>
        /// <param name="a">Ray on the left side of the operator.</param>
        /// <param name="b">Ray on the right side of the operator.</param>
        public static bool operator !=(Ray a, Ray b)
        {
            return !(a == b);
        }

        #endregion
    }
}
