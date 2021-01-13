using System;
using JetBrains.Annotations;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     All of the information needed to reference a tile in the game.
    /// </summary>
    [PublicAPI]
    public readonly struct TileRef : IEquatable<TileRef>
    {
        public static TileRef Zero => new(MapId.Nullspace, GridId.Invalid, Vector2i.Zero, Tile.Empty);

        /// <summary>
        ///     Identifier of the <see cref="MapManager.Map"/> this Tile belongs to.
        /// </summary>
        public readonly MapId MapIndex;

        /// <summary>
        ///     Identifier of the <see cref="MapGrid"/> this Tile belongs to.
        /// </summary>
        public readonly GridId GridIndex;

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
        /// <param name="mapId">Identifier of the map this tile belongs to.</param>
        /// <param name="gridId">Identifier of the grid this tile belongs to.</param>
        /// <param name="xIndex">Positional X index of this tile on the grid.</param>
        /// <param name="yIndex">Positional Y index of this tile on the grid.</param>
        /// <param name="tile">Actual data of this tile.</param>
        internal TileRef(MapId mapId, GridId gridId, int xIndex, int yIndex, Tile tile)
            : this(mapId, gridId, new Vector2i(xIndex, yIndex), tile) { }

        /// <summary>
        ///     Constructs a new instance of TileRef.
        /// </summary>
        /// <param name="mapId">Identifier of the map this tile belongs to.</param>
        /// <param name="gridId">Identifier of the grid this tile belongs to.</param>
        /// <param name="gridIndices">Positional indices of this tile on the grid.</param>
        /// <param name="tile">Actual data of this tile.</param>
        internal TileRef(MapId mapId, GridId gridId, Vector2i gridIndices, Tile tile)
        {
            MapIndex = mapId;
            GridIndex = gridId;
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
            return MapIndex.Equals(other.MapIndex)
                   && GridIndex.Equals(other.GridIndex)
                   && GridIndices.Equals(other.GridIndices)
                   && Tile.Equals(other.Tile);
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
                var hashCode = MapIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ GridIndex.GetHashCode();
                hashCode = (hashCode * 397) ^ GridIndices.GetHashCode();
                hashCode = (hashCode * 397) ^ Tile.GetHashCode();
                return hashCode;
            }
        }
    }
}
