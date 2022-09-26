using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Utility;
#pragma warning disable CS0618

namespace Robust.Shared.Map
{
    /// <summary>
    ///     All of the information needed to reference a tile in the game.
    /// </summary>
    [PublicAPI]
    public readonly struct TileRef : IEquatable<TileRef>, ISpanFormattable
    {
        public static TileRef Zero => new(EntityUid.Invalid, Vector2i.Zero, Tile.Empty);

        /// <summary>
        ///     Grid Entity this Tile belongs to.
        /// </summary>
        public readonly EntityUid GridUid;

        /// <summary>
        ///     Positional indices of this tile on the grid.
        /// </summary>
        public readonly Vector2i GridIndices;

        /// <summary>
        ///     Actual data of this Tile.
        /// </summary>
        public readonly Tile Tile;

        /// <summary>
        ///     Constructs a new instance of TileRef.
        /// </summary>
        /// <param name="gridId">Identifier of the grid this tile belongs to.</param>
        /// <param name="gridUid">Identifier of the grid entity this tile belongs to.</param>
        /// <param name="xIndex">Positional X index of this tile on the grid.</param>
        /// <param name="yIndex">Positional Y index of this tile on the grid.</param>
        /// <param name="tile">Actual data of this tile.</param>
        internal TileRef(EntityUid gridUid, int xIndex, int yIndex, Tile tile)
            : this(gridUid, new Vector2i(xIndex, yIndex), tile) { }

        /// <summary>
        ///     Constructs a new instance of TileRef.
        /// </summary>
        /// <param name="gridId">Identifier of the grid this tile belongs to.</param>
        /// <param name="gridUid">Identifier of the grid entity this tile belongs to.</param>
        /// <param name="gridIndices">Positional indices of this tile on the grid.</param>
        /// <param name="tile">Actual data of this tile.</param>
        internal TileRef(EntityUid gridUid, Vector2i gridIndices, Tile tile)
        {
            GridUid = gridUid;
            GridIndices = gridIndices;
            Tile = tile;
        }

        /// <summary>
        ///     Grid index on the X axis.
        /// </summary>
        public int X => GridIndices.X;

        /// <summary>
        ///     Grid index on the Y axis.
        /// </summary>
        public int Y => GridIndices.Y;

        /// <inheritdoc />
        public override string ToString()
        {
            return $"TileRef: {X},{Y} ({Tile})";
        }

        public string ToString(string? format, IFormatProvider? formatProvider)
        {
            return ToString();
        }

        public bool TryFormat(
            Span<char> destination,
            out int charsWritten,
            ReadOnlySpan<char> format,
            IFormatProvider? provider)
        {
            return FormatHelpers.TryFormatInto(
                destination,
                out charsWritten,
                $"TileRef: {X},{Y} ({Tile})");
        }

        /// <inheritdoc />
        public bool Equals(TileRef other)
        {
            return GridUid.Equals(other.GridUid) &&
                   GridIndices.Equals(other.GridIndices) &&
                   Tile.Equals(other.Tile);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is TileRef other && Equals(other);
        }

        /// <summary>
        ///     Check for equality by value between two objects.
        /// </summary>
        public static bool operator ==(TileRef a, TileRef b)
        {
            return a.Equals(b);
        }

        /// <summary>
        ///     Check for inequality by value between two objects.
        /// </summary>
        public static bool operator !=(TileRef a, TileRef b)
        {
            return !a.Equals(b);
        }

        /// <inheritdoc />
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = GridUid.GetHashCode();
                hashCode = (hashCode * 397) ^ GridIndices.GetHashCode();
                hashCode = (hashCode * 397) ^ Tile.GetHashCode();
                return hashCode;
            }
        }
    }
}
