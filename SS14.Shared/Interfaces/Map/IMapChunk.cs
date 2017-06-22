using System.Collections.Generic;
using SS14.Shared.Map;

namespace SS14.Shared.Interfaces.Map
{
    /// <summary>
    /// This is a square 'chunk' of a MapGrid. 
    /// </summary>
    public interface IMapChunk : IEnumerable<TileRef>
    {
        /// <summary>
        ///     The number of tiles per side of the square chunk.
        /// </summary>
        ushort ChunkSize { get; }

        /// <summary>
        ///     The supported version of the chunk format.
        /// </summary>
        uint Version { get; }

        /// <summary>
        ///     The X index of this chunk.
        /// </summary>
        int X { get; }

        /// <summary>
        ///     The Y index of this chunk.
        /// </summary>
        int Y { get; }

        /// <summary>
        ///     Returns the tile at the given indices. The tile indices are relative locations to the
        ///     chunk origin, NOT the grid origin.
        /// </summary>
        /// <param name="xTile">The X tile index relative to the chunk.</param>
        /// <param name="yTile">The Y tile index relative to the chunk.</param>
        /// <returns>A reference to a tile.</returns>
        TileRef GetTile(ushort xTile, ushort yTile);

        /// <summary>
        ///     Returns all of the tiles in the chunk.
        /// </summary>
        /// <param name="ignoreEmpty">Will empty (space) tiles be added to the collection?</param>
        /// <returns></returns>
        IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true);

        /// <summary>
        ///     Replaces a single tile inside of the chunk.
        /// </summary>
        /// <param name="xChunkTile">The X tile index relative to the chunk.</param>
        /// <param name="yChunkTile">The Y tile index relative to the chunk.</param>
        /// <param name="tile">The new tile to insert.</param>
        void SetTile(ushort xChunkTile, ushort yChunkTile, Tile tile);

        /// <summary>
        ///     Transforms Tile indices relative to the grid into tile indices relative to this chunk.
        /// </summary>
        /// <param name="gridTile">Tile indices relative to the grid.</param>
        /// <returns>Tile indices relative to this chunk.</returns>
        MapGrid.Indices GridTileToChunkTile(MapGrid.Indices gridTile);
    }
}
