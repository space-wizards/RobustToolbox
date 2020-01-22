using System;
using System.Runtime.InteropServices;

namespace Robust.Shared.Maths
{
    /// <summary>
    ///     Represents a float vector with two components (x, y).
    /// </summary>
    [StructLayout(LayoutKind.Sequential)]
    [Serializable]
    public readonly struct Vector2 : IEquatable<Vector2>, IApproxEquatable<Vector2>
    {
        /// <summary>
        ///     The X component of the vector.
        /// </summary>
        public readonly float X;

        /// <summary>
        ///     The Y component of the vector.
        /// </summary>
        public readonly float Y;

        /// <summary>
        ///     A zero length vector.
        /// </summary>
        public static readonly Vector2 Zero = new Vector2(0, 0);

        /// <summary>
        ///     A vector with all components set to 1.
        /// </summary>
        public static readonly Vector2 One = new Vector2(1, 1);

        /// <summary>
        ///     A unit vector pointing in the +X direction.
        /// </summary>
        public static readonly Vector2 UnitX = new Vector2(1, 0);

        /// <summary>
        ///     A unit vector pointing in the +Y direction.
        /// </summary>
        public static readonly Vector2 UnitY = new Vector2(0, 1);

        /// <summary>
        ///     Construct a vector from its coordinates.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public Vector2(float x, float y)
        {
            X = x;
            Y = y;
        }

        /// <summary>
        ///     Gets the length (magnitude) of the vector.
        /// </summary>
#if NETCOREAPP
        public float Length => MathF.Sqrt(LengthSquared);
#else
        public float Length => (float) Math.Sqrt(LengthSquared);
#endif

        /// <summary>
        ///     Gets the squared length of the vector.
        /// </summary>
        public float LengthSquared => X * X + Y * Y;

        /// <summary>
        ///     Returns a new, normalized, vector.
        /// </summary>
        /// <returns></returns>
        public Vector2 Normalized
        {
            get
            {
                var length = Length;
                return new Vector2(X / length, Y / length);
            }
        }

        public Vector2 Rounded()
        {
            return new Vector2((float) Math.Round(X), (float) Math.Round(Y));
        }

        /// <summary>
        ///     Subtracts a vector from another, returning a new vector.
        /// </summary>
        /// <param name="a">Vector to subtract from.</param>
        /// <param name="b">Vector to subtract with.</param>
        public static Vector2 operator -(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X - b.X, a.Y - b.Y);
        }

        /// <summary>
        ///     Subtracts a scalar with each component of a vector, returning a new vecotr..
        /// </summary>
        /// <param name="a">Vector to subtract from.</param>
        /// <param name="b">Scalar to subtract with.</param>
        public static Vector2 operator -(Vector2 a, float b)
        {
            return new Vector2(a.X - b, a.Y - b);
        }

        /// <summary>
        /// Negates a vector.
        /// </summary>
        public static Vector2 operator -(Vector2 vec)
        {
            return new Vector2(-vec.X, -vec.Y);
        }

        /// <summary>
        ///     Adds two vectors together, returning a new vector with the components of each added together.
        /// </summary>
        public static Vector2 operator +(Vector2 a, Vector2 b)
        {
            return new Vector2(a.X + b.X, a.Y + b.Y);
        }

        /// <summary>
        ///     Adds a scalar to each component of a vector, returning a new vector.
        /// </summary>
        public static Vector2 operator +(Vector2 a, float b)
        {
            return new Vector2(a.X + b, a.Y + b);
        }

        /// <summary>
        ///     Multiply a vector by a scale by multiplying the individual components.
        /// </summary>
        /// <param name="vec">The vector to multiply.</param>
        /// <param name="scale">The scale to multiply with.</param>
        /// <returns>A new vector.</returns>
        public static Vector2 operator *(Vector2 vec, float scale)
        {
            return new Vector2(vec.X * scale, vec.Y * scale);
        }

        /// <summary>
        ///     Multiplies a vector's components corresponding to a vector scale.
        /// </summary>
        public static Vector2 operator *(Vector2 vec, Vector2 scale)
        {
            return new Vector2(vec.X * scale.X, vec.Y * scale.Y);
        }

        /// <summary>
        ///     Divide a vector by a scale by dividing the individual components.
        /// </summary>
        /// <param name="vec">The vector to divide.</param>
        /// <param name="scale">The scale to divide by.</param>
        /// <returns>A new vector.</returns>
        public static Vector2 operator /(Vector2 vec, float scale)
        {
            return new Vector2(vec.X / scale, vec.Y / scale);
        }

        /// <summary>
        ///     Divides a vector's components corresponding to a vector scale.
        /// </summary>
        public static Vector2 operator /(Vector2 vec, Vector2 scale)
        {
            return new Vector2(vec.X / scale.X, vec.Y / scale.Y);
        }

        /// <summary>
        ///     Return a vector made up of the smallest components of the provided vectors.
        /// </summary>
        public static Vector2 ComponentMin(Vector2 a, Vector2 b)
        {
            return new Vector2(
                a.X < b.X ? a.X : b.X,
                a.Y < b.Y ? a.Y : b.Y
            );
        }

        /// <summary>
        ///     Return a vector made up of the largest components of the provided vectors.
        /// </summary>
        public static Vector2 ComponentMax(Vector2 a, Vector2 b)
        {
            return new Vector2(
                a.X > b.X ? a.X : b.X,
                a.Y > b.Y ? a.Y : b.Y
            );
        }

        /// <summary>
        ///     Returns the vector with the smallest magnitude. If both have equal magnitude, <paramref name="b" /> is selected.
        /// </summary>
        public static Vector2 MagnitudeMin(Vector2 a, Vector2 b)
        {
            return a.LengthSquared < b.LengthSquared ? a : b;
        }

        /// <summary>
        ///     Returns the vector with the largest magnitude. If both have equal magnitude, <paramref name="a" /> is selected.
        /// </summary>
        public static Vector2 MagnitudeMax(Vector2 a, Vector2 b)
        {
            return a.LengthSquared >= b.LengthSquared ? a : b;
        }

        /// <summary>
        ///     Clamps the components of a vector to minimum and maximum vectors.
        /// </summary>
        /// <param name="vector">The vector to clamp.</param>
        /// <param name="min">The lower bound vector.</param>
        /// <param name="max">The upper bound vector.</param>
        public static Vector2 Clamp(Vector2 vector, Vector2 min, Vector2 max)
        {
            return new Vector2(
                vector.X.Clamp(min.X, max.X),
                vector.Y.Clamp(min.Y, max.Y)
            );
        }

        /// <summary>
        ///     Calculates the dot product of two vectors.
        /// </summary>
        public static float Dot(Vector2 a, Vector2 b)
        {
            return a.X * b.X + a.Y * b.Y;
        }

        /// <summary>
        ///     Linearly interpolates two vectors so make a mix based on a factor.
        /// </summary>
        /// <returns>
        ///     a when factor=0, b when factor=1, a linear interpolation between the two otherwise.
        /// </returns>
        public static Vector2 Lerp(Vector2 a, Vector2 b, float factor)
        {
            return new Vector2(
                factor * (b.X - a.X) + a.X,
                factor * (b.Y - a.Y) + a.Y
            );
        }

        public static Vector2 LerpClamped(in Vector2 a, in Vector2 b, float factor)
        {
            if (factor <= 0)
                return a;

            if (factor >= 1)
                return b;

            return Lerp(a, b, factor);
        }

        public static Vector2 InterpolateCubic(Vector2 preA, Vector2 a, Vector2 b, Vector2 postB, float t)
        {
            return a +
                   (b - preA + (preA * 2.0f - a * 5.0f + b * 4.0f - postB + ((a - b) * 3.0f + postB - preA) * t) * t) *
                   t * 0.5f;
        }

        public void Deconstruct(out float x, out float y)
        {
            x = X;
            y = Y;
        }

        public static implicit operator Vector2((float x, float y) tuple)
        {
            var (x, y) = tuple;
            return new Vector2(x, y);
        }

        /// <summary>
        ///     Returns a string that represents the current Vector2.
        /// </summary>
        public override string ToString()
        {
            return $"({X}, {Y})";
        }

        public static bool operator ==(Vector2 a, Vector2 b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector2 a, Vector2 b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        ///     Compare a vector to another vector and check if they are equal.
        /// </summary>
        /// <param name="other">Other vector to check.</param>
        /// <returns>True if the two vectors are equal.</returns>
        public bool Equals(Vector2 other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        ///     Compare a vector to an object and check if they are equal.
        /// </summary>
        /// <param name="obj">Other object to check.</param>
        /// <returns>True if Object and vector are equal.</returns>
        public override bool Equals(object? obj)
        {
            return obj is Vector2 vec && Equals(vec);
        }

        /// <summary>
        ///     Returns the hash code for this instance.
        /// </summary>
        /// <returns>A unique hash code for this instance.</returns>
        public override int GetHashCode()
        {
            unchecked
            {
                return (X.GetHashCode() * 397) ^ Y.GetHashCode();
            }
        }

        public bool EqualsApprox(Vector2 other)
        {
            return FloatMath.CloseTo(X, other.X) && FloatMath.CloseTo(Y, other.Y);
        }

        public bool EqualsApprox(Vector2 other, double tolerance)
        {
            return FloatMath.CloseTo(X, other.X, tolerance) && FloatMath.CloseTo(Y, other.Y, tolerance);
        }
    }
}
