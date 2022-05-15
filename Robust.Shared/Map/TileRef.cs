using System;
using JetBrains.Annotations;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     All of the information needed to reference a tile in the game.
    /// </summary>
    [PublicAPI]
    public readonly struct TileRef : IEquatable<TileRef>
    {
        public static TileRef Zero => new(GridId.Invalid, EntityUid.Invalid, Vector2i.Zero, Tile.Empty);

        /// <summary>
        ///     Identifier of the <see cref="MapGrid"/> this Tile belongs to.
        /// </summary>
        public readonly GridId GridIndex;

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
        internal TileRef(GridId gridId, EntityUid gridUid, int xIndex, int yIndex, Tile tile)
            : this(gridId, gridUid, new Vector2i(xIndex, yIndex), tile) { }

        /// <summary>
        ///     Constructs a new instance of TileRef.
        /// </summary>
        /// <param name="gridId">Identifier of the grid this tile belongs to.</param>
        /// <param name="gridUid">Identifier of the grid entity this tile belongs to.</param>
        /// <param name="gridIndices">Positional indices of this tile on the grid.</param>
        /// <param name="tile">Actual data of this tile.</param>
        internal TileRef(GridId gridId, EntityUid gridUid, Vector2i gridIndices, Tile tile)
        {
            GridIndex = gridId;
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

        /// <inheritdoc />
        public bool Equals(TileRef other)
        {
            return GridIndex.Equals(other.GridIndex) &&
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
                var hashCode = GridIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ GridIndices.GetHashCode();
                hashCode = (hashCode * 397) ^ Tile.GetHashCode();
                return hashCode;
            }
        }
    }
}
