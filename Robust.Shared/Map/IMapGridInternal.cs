using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    internal interface IMapGridInternal : IMapGrid
    {
        GameTick LastModifiedTick { get; }

        GameTick CurTick { get; }

        Box2 LocalBounds { get; }

        /// <summary>
        ///     The total number of chunks contained on this grid.
        /// </summary>
        int ChunkCount { get; }

        void NotifyTileChanged(in TileRef tileRef, in Tile oldTile);

        /// <summary>
        ///     Returns the chunk at the given indices. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="xIndex">The X index of the chunk in this grid.</param>
        /// <param name="yIndex">The Y index of the chunk in this grid.</param>
        /// <returns>The existing or new chunk.</returns>
        IMapChunkInternal GetChunk(int xIndex, int yIndex);

        /// <summary>
        ///     Returns the chunk at the given indices. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="chunkIndices">The indices of the chunk in this grid.</param>
        /// <returns>The existing or new chunk.</returns>
        IMapChunkInternal GetChunk(MapIndices chunkIndices);

        /// <summary>
        ///     Returns all chunks in this grid. This will not generate new chunks.
        /// </summary>
        /// <returns>All chunks in the grid.</returns>
        IReadOnlyDictionary<MapIndices, IMapChunkInternal> GetMapChunks();
    }
}
