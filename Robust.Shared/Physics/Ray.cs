using Robust.Shared.Map;
using Robust.Shared.Utility;
using System;

namespace Robust.Shared.Maths
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
        public readonly Vector2 Start;

        /// <summary>
        ///     Specifies the direction the ray is pointing.
        /// </summary>
        public readonly Vector2 End;

        public readonly Vector2 Direction => (End - Start).Normalized;

        /// <summary>
        ///     Creates a new instance of a Ray.
        /// </summary>
        /// <param name="start">Starting position of the ray.</param>
        /// <param name="direction">Unit direction vector that the ray is pointing.</param>
        /// <param name="distance"></param>
        public Ray(Vector2 start, Vector2 direction, float distance)
        {
            DebugTools.Assert(distance > 0f);
            Start = start;
            DebugTools.Assert(MathHelper.CloseTo(direction.LengthSquared, 1));
            End = start + direction * distance;
        }

        public Ray(Vector2 start, Vector2 end)
        {
            Start = start;
            End = end;
            DebugTools.Assert((end - start).LengthSquared > 0f);
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
                    if (Start.X < MathF.Min(box.Left, box.Right) || Start.X > MathF.Max(box.Left, box.Right))
                    {
                        return false;
                    }
                }

                // calculate intersection t value of ray with near and far plane of slab
                var ood = 1.0f / Direction.X;
                var t1 = (MathF.Min(box.Left, box.Right) - Start.X) * ood;
                var t2 = (MathF.Max(box.Left, box.Right) - Start.X) * ood;

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
                    if (Start.Y < MathF.Min(box.Top, box.Bottom) || Start.Y > MathF.Max(box.Top, box.Bottom))
                    {
                        return false;
                    }
                }

                // calculate intersection t value of ray with near and far plane of slab
                var ood = 1.0f / Direction.Y;
                var t1 = (MathF.Min(box.Top, box.Bottom) - Start.Y) * ood;
                var t2 = (MathF.Max(box.Top, box.Bottom) - Start.Y) * ood;

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
            hitPos = Start + Direction * tmin;
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
            return Start.Equals(other.Start) && Direction.Equals(other.Direction);
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
                return (Start.GetHashCode() * 397) ^ Direction.GetHashCode();
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
