using System.Collections.Generic;
using SS14.Shared.Map;

namespace SS14.Shared.Interfaces.Map
{
    public interface IMapChunk : IEnumerable<TileRef>
    {
        /// <summary>
        ///     The number of tiles per side of the square chunk.
        /// </summary>
        uint Size { get; }

        /// <summary>
        ///     The supported version of the chunk format.
        /// </summary>
        uint Version { get; }

        /// <summary>
        ///     The X index of this chunk.
        /// </summary>
        int X { get; }

        /// <summary>
        ///     The Y index of this chunk;
        /// </summary>
        int Y { get; }

        TileRef GetTile(uint xTile, uint yTile);

        /// <summary>
        ///     Returns all tiles in the chunk, in row-major order [xTileIndex, yTileIndex].
        /// </summary>
        /// <returns>All tiles in the chunk.</returns>
        IEnumerable<TileRef> GetAllTiles();

        /// <summary>
        /// Replaces a single tile inside of the chunk.
        /// </summary>
        /// <param name="xTileIndex">The X index of the location inside the chunk.</param>
        /// <param name="yTileIndex">The Y index of the location inside the chunk.</param>
        /// <param name="tile">The new tile to insert.</param>
        void SetTile(uint xTileIndex, uint yTileIndex, Tile tile);

        /// <summary>
        /// Modifies a single tile inside of the chunk.
        /// </summary>
        /// <param name="xTileIndex">The X index of the location inside the chunk.</param>
        /// <param name="yTileIndex">The Y index of the location inside the chunk.</param>
        /// <param name="tileId">The new internal ID of the tile.</param>
        /// <param name="tileData">The new data of the tile.</param>
        void SetTile(uint xTileIndex, uint yTileIndex, ushort tileId, ushort tileData);
    }
}
