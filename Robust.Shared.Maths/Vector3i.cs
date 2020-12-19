using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Newtonsoft.Json;

namespace Robust.Shared.Maths
{
    [JsonObject(MemberSerialization.Fields)]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    // ReSharper disable once InconsistentNaming
    public struct Vector3i : IEquatable<Vector3i>
    {
        public static readonly Vector3i Zero = (0, 0, 0);
        public static readonly Vector3i One = (1, 1, 1);

        /// <summary>
        /// The X component of the Vector3i.
        /// </summary>
        public int X;

        /// <summary>
        /// The Y component of the Vector3i.
        /// </summary>
        public int Y;

        /// <summary>
        /// The Z component of the Vector3i.
        /// </summary>
        public int Z;

        /// <summary>
        /// Construct a vector from its coordinates.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        public Vector3i(int x, int y, int z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public static Vector3i ComponentMax(Vector3i a, Vector3i b)
        {
            return new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z));
        }

        public static Vector3i ComponentMin(Vector3i a, Vector3i b)
        {
            return new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z));
        }

        /// <summary>
        /// Compare a vector to another vector and check if they are equal.
        /// </summary>
        /// <param name="other">Other vector to check.</param>
        /// <returns>True if the two vectors are equal.</returns>
        public readonly bool Equals(Vector3i other)
        {
            return X == other.X && Y == other.Y && Z == other.Z;
        }

        /// <summary>
        /// Compare a vector to an object and check if they are equal.
        /// </summary>
        /// <param name="obj">Other object to check.</param>
        /// <returns>True if Object and vector are equal.</returns>
        public override readonly bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Vector3i vector && Equals(vector);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A unique hash code for this instance.</returns>
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override readonly int GetHashCode()
        {
            var hc = new HashCode();
            hc.Add(X);
            hc.Add(Y);
            hc.Add(Z);
            return hc.ToHashCode();
        }

        public static Vector3i operator -(Vector3i a, Vector3i b)
        {
            return new(a.X - b.X, a.Y - b.Y, a.Z - b.Z);
        }

        public static Vector3i operator -(Vector3i a, int b)
        {
            return new(a.X - b, a.Y - b, a.Z - b);
        }

        public static Vector3i operator -(Vector3i a)
        {
            return new(-a.X, -a.Y, -a.Z);
        }

        public static Vector3i operator +(Vector3i a, Vector3i b)
        {
            return new(a.X + b.X, a.Y + b.Y, a.Z + b.Z);
        }

        public static Vector3i operator +(Vector3i a, int b)
        {
            return new(a.X + b, a.Y + b, a.Z + b);
        }

        public static Vector3i operator *(Vector3i a, Vector3i b)
        {
            return new(a.X * b.X, a.Y * b.Y, a.Z * b.Z);
        }

        public static Vector3i operator *(Vector3i a, int scale)
        {
            return new(a.X * scale, a.Y * scale, a.Z * scale);
        }

        public static Vector3 operator *(Vector3i a, float scale)
        {
            return new(a.X * scale, a.Y * scale, a.Z * scale);
        }

        public static Vector3i operator /(Vector3i a, Vector3i b)
        {
            return new(a.X / b.X, a.Y / b.Y, a.Z / b.Z);
        }

        public static Vector3i operator /(Vector3i a, int scale)
        {
            return new(a.X / scale, a.Y / scale, a.Z / scale);
        }

        public static Vector3 operator /(Vector3i a, float scale)
        {
            return new(a.X / scale, a.Y / scale, a.Z / scale);
        }

        public static bool operator ==(Vector3i a, Vector3i b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector3i a, Vector3i b)
        {
            return !a.Equals(b);
        }

        public readonly void Deconstruct(out int x, out int y, out int z)
        {
            x = X;
            y = Y;
            z = Z;
        }

        public static implicit operator Vector3(Vector3i vector)
        {
            return new(vector.X, vector.Y, vector.Z);
        }

        public static explicit operator Vector3i(Vector3 vector)
        {
            return new((int) vector.X, (int) vector.Y, (int) vector.Z);
        }

        public static implicit operator Vector3i((int x, int y, int z) tuple)
        {
            var (x, y, z) = tuple;
            return new Vector3i(x, y, z);
        }

        /// <summary>
        ///     Returns a string that represents the current Vector3i.
        /// </summary>
        public override readonly string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
