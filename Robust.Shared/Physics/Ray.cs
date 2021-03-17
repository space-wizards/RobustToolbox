using System;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     A representation of a 2D ray.
    /// </summary>
    [Serializable]
    public struct Ray : IEquatable<Ray>
    {
        /// <summary>
        ///     Specifies the starting point of the ray.
        /// </summary>
        public Vector2 Position;

        /// <summary>
        ///     Specifies the direction the ray is pointing.
        /// </summary>
        public Vector2 Direction;

        /// <summary>
        ///     Creates a new instance of a Ray.
        /// </summary>
        /// <param name="position">Starting position of the ray.</param>
        /// <param name="direction">Unit direction vector that the ray is pointing.</param>
        public Ray(Vector2 position, Vector2 direction)
        {
            Position = position;
            Direction = direction;

            DebugTools.Assert(MathHelper.CloseTo(Direction.LengthSquared, 1));

        }

        #region Intersect Tests

        public readonly bool Intersects(Box2 box, out float distance, out Vector2 hitPos)
        {
            hitPos = Vector2.Zero;
            distance = 0;

            var tmin = 0.0f; // set to -FLT_MAX to get first hit on line
            var tmax = float.MaxValue; // set to max distance ray can travel (for segment)
            const float epsilon = 1.0E-07f;

            // X axis slab
            {
                if (MathF.Abs(Direction.X) < epsilon)
                {
                    // ray is parallel to this slab, it will never hit unless ray is inside box
                    if (Position.X < MathF.Min(box.Left, box.Right) || Position.X > MathF.Max(box.Left, box.Right))
                    {
                        return false;
                    }
                }

                // calculate intersection t value of ray with near and far plane of slab
                var ood = 1.0f / Direction.X;
                var t1 = (MathF.Min(box.Left, box.Right) - Position.X) * ood;
                var t2 = (MathF.Max(box.Left, box.Right) - Position.X) * ood;

                // Make t1 be the intersection with near plane, t2 with far plane
                if (t1 > t2)
                    MathHelper.Swap(ref t1, ref t2);

                // Compute the intersection of slab intersection intervals
                tmin = MathF.Max(t1, tmin);
                tmax = MathF.Min(t2, tmax); // Is this Min (SE) or Max(Textbook)

                // Exit with no collision as soon as slab intersection becomes empty
                if (tmin > tmax)
                {
                    return false;
                }
            }

            // Y axis slab
            {
                if (MathF.Abs(Direction.Y) < epsilon)
                {
                    // ray is parallel to this slab, it will never hit unless ray is inside box
                    if (Position.Y < MathF.Min(box.Top, box.Bottom) || Position.Y > MathF.Max(box.Top, box.Bottom))
                    {
                        return false;
                    }
                }

                // calculate intersection t value of ray with near and far plane of slab
                var ood = 1.0f / Direction.Y;
                var t1 = (MathF.Min(box.Top, box.Bottom) - Position.Y) * ood;
                var t2 = (MathF.Max(box.Top, box.Bottom) - Position.Y) * ood;

                // Make t1 be the intersection with near plane, t2 with far plane
                if (t1 > t2)
                    MathHelper.Swap(ref t1, ref t2);

                // Compute the intersection of slab intersection intervals
                tmin = MathF.Max(t1, tmin);
                tmax = MathF.Min(t2, tmax); // Is this Min (SE) or Max(Textbook)

                // Exit with no collision as soon as slab intersection becomes empty
                if (tmin > tmax)
                {
                    return false;
                }
            }

            // Ray intersects all slabs. Return point and intersection t value
            hitPos = Position + Direction * tmin;
            distance = tmin;
            return true;
        }

        #endregion

        #region Equality

        /// <summary>
        ///     Determines if this Ray and another Ray are equivalent.
        /// </summary>
        /// <param name="other">Ray to compare to.</param>
        public readonly bool Equals(Ray other)
        {
            return Position.Equals(other.Position) && Direction.Equals(other.Direction);
        }

        /// <summary>
        ///     Determines if this ray and another object is equivalent.
        /// </summary>
        /// <param name="obj">Object to compare to.</param>
        public override readonly bool Equals(object? obj)
        {
            if (obj is null) return false;
            return obj is Ray ray && Equals(ray);
        }

        /// <summary>
        ///     Calculates the hash code of this Ray.
        /// </summary>
        public override readonly int GetHashCode()
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
