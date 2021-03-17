using System;
using System.Runtime.InteropServices;
using System.Text.Json.Serialization;
using Newtonsoft.Json;

namespace Robust.Shared.Maths
{
    [JsonObject(MemberSerialization.Fields)]
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct Vector2u : IEquatable<Vector2u>
    {
        /// <summary>
        /// The X component of the Vector2i.
        /// </summary>
        [JsonInclude] public uint X;

        /// <summary>
        /// The Y component of the Vector2i.
        /// </summary>
        [JsonInclude] public uint Y;

        /// <summary>
        /// Construct a vector from its coordinates.
        /// </summary>
        /// <param name="x">X coordinate</param>
        /// <param name="y">Y coordinate</param>
        public Vector2u(uint x, uint y)
        {
            X = x;
            Y = y;
        }

        public readonly void Deconstruct(out uint x, out uint y)
        {
            x = X;
            y = Y;
        }

        /// <summary>
        /// Compare a vector to another vector and check if they are equal.
        /// </summary>
        /// <param name="other">Other vector to check.</param>
        /// <returns>True if the two vectors are equal.</returns>
        public readonly bool Equals(Vector2u other)
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
            return obj is Vector2u vec && Equals(vec);
        }

        /// <summary>
        /// Returns the hash code for this instance.
        /// </summary>
        /// <returns>A unique hash code for this instance.</returns>
        public override readonly int GetHashCode()
        {
            unchecked
            {
                return ((int) X * 397) ^ (int) Y;
            }
        }

        public static Vector2u operator /(Vector2u vector, uint divider)
        {
            return new(vector.X / divider, vector.Y / divider);
        }

        public static implicit operator Vector2(Vector2u vector)
        {
            return new(vector.X, vector.Y);
        }

        public static explicit operator Vector2u(Vector2 vector)
        {
            return new((uint) vector.X, (uint) vector.Y);
        }

        public static explicit operator Vector2i(Vector2u vector)
        {
            return new((int) vector.X, (int) vector.Y);
        }
    }
}
