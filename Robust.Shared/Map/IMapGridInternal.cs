using System.Collections.Generic;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    internal interface IMapGridInternal : IMapGrid
    {
        GameTick LastTileModifiedTick { get; }

        GameTick CurTick { get; }

        /// <summary>
        ///     The total number of chunks contained on this grid.
        /// </summary>
        int ChunkCount { get; }

        GameTick LastAnchoredModifiedTick { get; }

        void NotifyTileChanged(in TileRef tileRef, in Tile oldTile);

        /// <summary>
        /// Notifies the grid that an anchored entity is dirty.
        /// </summary>
        /// <param name="pos">Position of the entity in local tile indices.</param>
        void AnchoredEntDirty(Vector2i pos);

        void UpdateAABB();

        /// <summary>
        ///     Returns the chunk at the given indices. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="xIndex">The X index of the chunk in this grid.</param>
        /// <param name="yIndex">The Y index of the chunk in this grid.</param>
        /// <returns>The existing or new chunk.</returns>
        IMapChunkInternal GetChunk(int xIndex, int yIndex);

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
        IMapChunkInternal GetChunk(Vector2i chunkIndices);

        /// <summary>
        /// Returns whether a chunk exists with the specified indices.
        /// </summary>
        bool HasChunk(Vector2i chunkIndices);

        /// <summary>
        ///     Returns all chunks in this grid. This will not generate new chunks.
        /// </summary>
        /// <returns>All chunks in the grid.</returns>
        IReadOnlyDictionary<Vector2i, IMapChunkInternal> GetMapChunks();

        /// <summary>
        ///     Returns all the <see cref="IMapChunkInternal"/> intersecting the worldAABB.
        /// </summary>
        IEnumerable<IMapChunkInternal> GetMapChunks(Box2 worldAABB);

        IEnumerable<IMapChunkInternal> GetMapChunks(Box2Rotated worldArea);
    }
}
