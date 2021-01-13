using System;
using JetBrains.Annotations;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     This structure contains the data for an individual Tile in a <c>MapGrid</c>.
    /// </summary>
    [PublicAPI, Serializable]
    public readonly struct Tile : IEquatable<Tile>
    {
        /// <summary>
        ///     Internal type ID of this tile.
        /// </summary>
        public readonly ushort TypeId;

        /// <summary>
        ///     Optional per-tile data of this tile.
        /// </summary>
        public readonly ushort Data;

        /// <summary>
        ///     An empty tile that can be compared against.
        /// </summary>
        public static readonly Tile Empty = new(0);

        /// <summary>
        ///     Is this tile space (empty)?
        /// </summary>
        public bool IsEmpty => TypeId == 0;

        /// <summary>
        ///     Creates a new instance of a grid tile.
        /// </summary>
        /// <param name="typeId">Internal type ID.</param>
        /// <param name="data">Optional per-tile data.</param>
        public Tile(ushort typeId, ushort data = 0)
        {
            TypeId = typeId;
            Data = data;
        }

        /// <summary>
        ///     Explicit conversion of <c>Tile</c> to <c>uint</c> . This should only
        ///     be used in special cases like serialization. Do NOT use this in
        ///     content.
        /// </summary>
        public static explicit operator uint(Tile tile)
        {
            return ((uint)tile.TypeId << 16) | tile.Data;
        }

        /// <summary>
        ///     Explicit conversion of <c>uint</c> to <c>Tile</c> . This should only
        ///     be used in special cases like serialization. Do NOT use this in
        ///     content.
        /// </summary>
        public static explicit operator Tile(uint tile)
        {
            return new(
                (ushort)(tile >> 16),
                (ushort)tile
            );
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(Tile a, Tile b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(Tile a, Tile b)
        {
            return !a.Equals(b);
        }

        /// <summary>
        /// Generates String representation of this Tile.
        /// </summary>
        /// <returns>String representation of this Tile.</returns>
        public override string ToString()
        {
            return $"Tile {TypeId}, {Data}";
        }

        /// <inheritdoc />
        public bool Equals(Tile other)
        {
            return TypeId == other.TypeId && Data == other.Data;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is Tile other && Equals(other);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                return (TypeId.GetHashCode() * 397) ^ Data.GetHashCode();
            }
        }
    }
}
