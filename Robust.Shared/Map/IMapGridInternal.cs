using System.Collections.Generic;
using Robust.Shared.Maths;

namespace Robust.Shared.Map
{
    internal interface IMapGridInternal : IMapGrid
    {
        /// <summary>
        ///     The total number of chunks contained on this grid.
        /// </summary>
        int ChunkCount { get; }

        /// <summary>
        ///     Returns the chunk at the given indices. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="xIndex">The X index of the chunk in this grid.</param>
        /// <param name="yIndex">The Y index of the chunk in this grid.</param>
        /// <returns>The existing or new chunk.</returns>
        MapChunk GetChunk(int xIndex, int yIndex);

        /// <summary>
        /// Removes the chunk with the specified origin.
        /// </summary>
        void RemoveChunk(Vector2i origin);

        /// <summary>
        ///     Returns the chunk at the given indices. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="chunkIndices">The indices of the chunk in this grid.</param>
        /// <returns>The existing or new chunk.</returns>
        MapChunk GetChunk(Vector2i chunkIndices);

        /// <summary>
        /// Returns whether a chunk exists with the specified indices.
        /// </summary>
        bool HasChunk(Vector2i chunkIndices);

        /// <summary>
        ///     Returns all chunks in this grid. This will not generate new chunks.
        /// </summary>
        /// <returns>All chunks in the grid.</returns>
        IReadOnlyDictionary<Vector2i, MapChunk> GetMapChunks();

        /// <summary>
        ///     Returns all the <see cref="MapChunk"/> intersecting the worldAABB.
        /// </summary>
        void GetMapChunks(Box2 worldAABB, out MapGrid.ChunkEnumerator enumerator);

        /// <summary>
        ///     Returns all the <see cref="MapChunk"/> intersecting the rotated world box.
        /// </summary>
        void GetMapChunks(Box2Rotated worldArea, out MapGrid.ChunkEnumerator enumerator);

        /// <summary>
        /// Regenerates the chunk local bounds of this chunk.
        /// </summary>
        void RegenerateCollision(MapChunk mapChunk);

        /// <summary>
        /// Calculate the world space AABB for this chunk.
        /// </summary>
        Box2 CalcWorldAABB(MapChunk mapChunk);

        /// <summary>
        ///     Returns the tile at the given chunk indices.
        /// </summary>
        /// <param name="mapChunk"></param>
        /// <param name="xIndex">The X tile index relative to the chunk origin.</param>
        /// <param name="yIndex">The Y tile index relative to the chunk origin.</param>
        /// <returns>A reference to a tile.</returns>
        TileRef GetTileRef(MapChunk mapChunk, ushort xIndex, ushort yIndex);
    }
}
