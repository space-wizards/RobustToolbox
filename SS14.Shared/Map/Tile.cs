
namespace SS14.Shared.Map
{
    public struct Tile
    {
        /// <summary>
        ///     Internal type ID of this tile.
        /// </summary>
        public ushort TileId { get; }

        /// <summary>
        ///     Optional per-tile data of this tile.
        /// </summary>
        public ushort Data { get; }

        /// <summary>
        ///     Is this tile space (empty)?
        /// </summary>
        public bool IsEmpty => TileId == 0;

        /// <summary>
        ///     Creates a new instance of a grid tile.
        /// </summary>
        /// <param name="tileId">Internal type ID.</param>
        /// <param name="data">Optional per-tile data.</param>
        public Tile(ushort tileId, ushort data = 0)
        {
            TileId = tileId;
            Data = data;
        }

        public static explicit operator uint(Tile tile)
        {
            return ((uint) tile.TileId << 16) | tile.Data;
        }


        public static explicit operator Tile(uint tile)
        {
            return new Tile(
                (ushort) (tile >> 16),
                (ushort) tile
            );
        }

        public override bool Equals(object obj)
        {
            return obj is Tile && this == (Tile) obj;
        }

        public override int GetHashCode()
        {
            return ((uint) this).GetHashCode();
        }

        public static bool operator ==(Tile a, Tile b)
        {
            return a.TileId == b.TileId && a.Data == b.Data;
        }

        public static bool operator !=(Tile a, Tile b)
        {
            return !(a == b);
        }

        public override string ToString()
        {
            return $"Tile {TileId}, {Data}";
        }
    }
}
