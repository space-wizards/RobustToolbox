using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;

namespace Robust.Shared.Maths
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    public struct Vector2i : IEquatable<Vector2i>
    {
        public static readonly Vector2i Zero = (0, 0);
        public static readonly Vector2i One = (1, 1);

        /// <summary>
        /// The X component of the Vector2i.
        /// </summary>
        [JsonInclude] public int X;

        /// <summary>
        /// The Y component of the Vector2i.
        /// </summary>
        [JsonInclude] public int Y;

        /// <summary>
        /// Construct a vector from its coordinates.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public Vector2i(int x, int y)
        {
            X = x;
            Y = y;
        }

        public static Vector2i ComponentMax(Vector2i a, Vector2i b)
        {
            return new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y));
        }

        public static Vector2i ComponentMin(Vector2i a, Vector2i b)
        {
            return new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y));
        }

        /// <summary>
        /// Compare a vector to another vector and check if they are equal.
        /// </summary>
        /// <param name="other">Other vector to check.</param>
        /// <returns>True if the two vectors are equal.</returns>
        public readonly bool Equals(Vector2i other)
        {
            return X == other.X && Y == other.Y;
        }

        /// <summary>
        /// Compare a vector to an object and check if they are equal.
        /// </summary>
        /// <param name="obj">Other object to check.</param>
        /// <returns>True if Object and vector are equal.</returns>
        public override readonly bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Vector2i vector && Equals(vector);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A unique hash code for this instance.</returns>
        public override readonly int GetHashCode()
        {
            unchecked
            {
                return (X * 397) ^ Y;
            }
        }

        public static Vector2i operator -(Vector2i a, Vector2i b)
        {
            return new(a.X - b.X, a.Y - b.Y);
        }

        public static Vector2i operator -(Vector2i a, int b)
        {
            return new(a.X - b, a.Y - b);
        }

        public static Vector2i operator -(Vector2i a)
        {
            return new(-a.X, -a.Y);
        }

        public static Vector2i operator +(Vector2i a, Vector2i b)
        {
            return new(a.X + b.X, a.Y + b.Y);
        }

        public static Vector2i operator +(Vector2i a, int b)
        {
            return new(a.X + b, a.Y + b);
        }

        public static Vector2i operator *(Vector2i a, Vector2i b)
        {
            return new(a.X * b.X, a.Y * b.Y);
        }

        public static Vector2i operator *(Vector2i a, int scale)
        {
            return new(a.X * scale, a.Y * scale);
        }

        public static Vector2 operator *(Vector2i a, float scale)
        {
            return new(a.X * scale, a.Y * scale);
        }

        public static Vector2i operator /(Vector2i a, Vector2i b)
        {
            return new(a.X / b.X, a.Y / b.Y);
        }

        public static Vector2i operator /(Vector2i a, int scale)
        {
            return new(a.X / scale, a.Y / scale);
        }

        public static Vector2 operator /(Vector2i a, float scale)
        {
            return new(a.X / scale, a.Y / scale);
        }

        public static bool operator ==(Vector2i a, Vector2i b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector2i a, Vector2i b)
        {
            return !a.Equals(b);
        }

        public readonly void Deconstruct(out int x, out int y)
        {
            x = X;
            y = Y;
        }

        public static implicit operator Vector2(Vector2i vector)
        {
            return new(vector.X, vector.Y);
        }

        public static explicit operator Vector2i(Vector2 vector)
        {
            return new((int) vector.X, (int) vector.Y);
        }

        public static implicit operator Vector2i((int x, int y) tuple)
        {
            var (x, y) = tuple;
            return new Vector2i(x, y);
        }

        /// <summary>
        ///     Returns a string that represents the current Vector2i.
        /// </summary>
        public override readonly string ToString()
        {
            return $"({X}, {Y})";
        }
    }
}
