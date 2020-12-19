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
    public struct Vector4i : IEquatable<Vector4i>
    {
        public static readonly Vector4i Zero = (0, 0, 0, 0);
        public static readonly Vector4i One = (1, 1, 1, 1);

        /// <summary>
        /// The X component of the Vector4i.
        /// </summary>
        public int X;

        /// <summary>
        /// The Y component of the Vector4i.
        /// </summary>
        public int Y;

        /// <summary>
        /// The Z component of the Vector4i.
        /// </summary>
        public int Z;

        /// <summary>
        /// The W component of the Vector4i.
        /// </summary>
        public int W;

        /// <summary>
        /// Construct a vector from its coordinates.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        /// <param name="z">Z coordinate</param>
        /// <param name="w">W coordinate</param>
        public Vector4i(int x, int y, int z, int w)
        {
            X = x;
            Y = y;
            Z = z;
            W = w;
        }

        public static Vector4i ComponentMax(Vector4i a, Vector4i b)
        {
            return new(Math.Max(a.X, b.X), Math.Max(a.Y, b.Y), Math.Max(a.Z, b.Z), Math.Max(a.W, b.W));
        }

        public static Vector4i ComponentMin(Vector4i a, Vector4i b)
        {
            return new(Math.Min(a.X, b.X), Math.Min(a.Y, b.Y), Math.Min(a.Z, b.Z), Math.Min(a.W, b.W));
        }

        /// <summary>
        /// Compare a vector to another vector and check if they are equal.
        /// </summary>
        /// <param name="other">Other vector to check.</param>
        /// <returns>True if the two vectors are equal.</returns>
        public readonly bool Equals(Vector4i other)
        {
            return X == other.X && Y == other.Y && Z == other.Z && W == other.W;
        }

        /// <summary>
        /// Compare a vector to an object and check if they are equal.
        /// </summary>
        /// <param name="obj">Other object to check.</param>
        /// <returns>True if Object and vector are equal.</returns>
        public override readonly bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            return obj is Vector4i vector && Equals(vector);
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
            hc.Add(W);
            return hc.ToHashCode();
        }

        public static Vector4i operator -(Vector4i a, Vector4i b)
        {
            return new(a.X - b.X, a.Y - b.Y, a.Z - b.Z, a.W - b.W);
        }

        public static Vector4i operator -(Vector4i a, int b)
        {
            return new(a.X - b, a.Y - b, a.Z - b, a.W - b);
        }

        public static Vector4i operator -(Vector4i a)
        {
            return new(-a.X, -a.Y, -a.Z, -a.W);
        }

        public static Vector4i operator +(Vector4i a, Vector4i b)
        {
            return new(a.X + b.X, a.Y + b.Y, a.Z + b.Z, a.W + b.W);
        }

        public static Vector4i operator +(Vector4i a, int b)
        {
            return new(a.X + b, a.Y + b, a.Z + b, a.W + b);
        }

        public static Vector4i operator *(Vector4i a, Vector4i b)
        {
            return new(a.X * b.X, a.Y * b.Y, a.Z * b.Z, a.W * b.W);
        }

        public static Vector4i operator *(Vector4i a, int scale)
        {
            return new(a.X * scale, a.Y * scale, a.Z * scale, a.W * scale);
        }

        public static Vector4 operator *(Vector4i a, float scale)
        {
            return new(a.X * scale, a.Y * scale, a.Z * scale, a.W * scale);
        }

        public static Vector4i operator /(Vector4i a, Vector4i b)
        {
            return new(a.X / b.X, a.Y / b.Y, a.Z / b.Z, a.W / b.W);
        }

        public static Vector4i operator /(Vector4i a, int scale)
        {
            return new(a.X / scale, a.Y / scale, a.Z / scale, a.W / scale);
        }

        public static Vector4 operator /(Vector4i a, float scale)
        {
            return new(a.X / scale, a.Y / scale, a.Z / scale, a.W / scale);
        }

        public static bool operator ==(Vector4i a, Vector4i b)
        {
            return a.Equals(b);
        }

        public static bool operator !=(Vector4i a, Vector4i b)
        {
            return !a.Equals(b);
        }

        public readonly void Deconstruct(out int x, out int y, out int z, out int w)
        {
            x = X;
            y = Y;
            z = Z;
            w = W;
        }

        public static implicit operator Vector4(Vector4i vector)
        {
            return new(vector.X, vector.Y, vector.Z, vector.W);
        }

        public static explicit operator Vector4i(Vector4 vector)
        {
            return new((int) vector.X, (int) vector.Y, (int) vector.Z, (int) vector.W);
        }

        public static implicit operator Vector4i((int x, int y, int z, int w) tuple)
        {
            var (x, y, z, w) = tuple;
            return new Vector4i(x, y, z, w);
        }

        /// <summary>
        ///     Returns a string that represents the current Vector4i.
        /// </summary>
        public override readonly string ToString()
        {
            return $"({X}, {Y}, {Z}, {W})";
        }
    }
}
