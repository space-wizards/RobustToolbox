using System;
using Robust.Shared.Maths;

namespace Robust.Shared.Physics
{
    /// <summary>
    ///     Holds the mass data computed for a shape
    /// </summary>
    public struct MassData : IEquatable<MassData>
    {
        /// <summary>
        ///     The area of the shape
        /// </summary>
        public float Area { get; set; }

        /// <summary>
        ///     The position of the shape's centroid relative to the shape's origin.
        /// </summary>
        public Vector2 Centroid { get; set; }

        /// <summary>
        ///     The rotational inertia of the shape about the local origin.
        /// </summary>
        public float Inertia { get; set; }

        /// <summary>
        ///     The mass of the shape, usually in kilograms.
        /// </summary>
        public float Mass { get; set; }

        public static bool operator ==(MassData left, MassData right)
        {
            return (Math.Abs(left.Area - right.Area) < float.Epsilon &&
                    Math.Abs(left.Mass - right.Mass) < float.Epsilon &&
                    left.Centroid == right.Centroid &&
                    Math.Abs(left.Inertia - right.Inertia) < float.Epsilon);
        }

        public static bool operator !=(MassData left, MassData right)
        {
            return !(left == right);
        }

        public bool Equals(MassData other)
        {
            return this == other;
        }

        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;

            if (obj.GetType() != typeof(MassData))
                return false;

            return Equals((MassData)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int result = Area.GetHashCode();
                result = (result * 397) ^ Centroid.GetHashCode();
                result = (result * 397) ^ Inertia.GetHashCode();
                result = (result * 397) ^ Mass.GetHashCode();
                return result;
            }
        }
    }
}
