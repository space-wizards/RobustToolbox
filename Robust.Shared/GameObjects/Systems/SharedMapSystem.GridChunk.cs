using System;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    /// <summary>
    ///     Replaces a single tile inside of the chunk.
    /// </summary>
    /// <param name="xIndex">The X tile index relative to the chunk.</param>
    /// <param name="yIndex">The Y tile index relative to the chunk.</param>
    /// <param name="tile">The new tile to insert.</param>
    internal void SetChunkTile(EntityUid uid, MapGridComponent grid, MapChunk chunk, ushort xIndex, ushort yIndex, Tile tile)
    {
        if (xIndex >= grid.ChunkSize)
            throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

        if (yIndex >= grid.ChunkSize)
            throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

        // same tile, no point to continue
        if (chunk.Tiles[xIndex, yIndex] == tile)
            return;

        var oldTile = chunk.Tiles[xIndex, yIndex];
        var oldFilledTiles = chunk.FilledTiles;

        if (oldTile.IsEmpty != tile.IsEmpty)
        {
            if (oldTile.IsEmpty)
            {
                chunk.FilledTiles += 1;
            }
            else
            {
                chunk.FilledTiles -= 1;
            }
        }

        var shapeChanged = oldFilledTiles != chunk.FilledTiles;
        DebugTools.Assert(chunk.FilledTiles >= 0);

        chunk.Tiles[xIndex, yIndex] = tile;

        var tileIndices = new Vector2i(xIndex, yIndex);

        OnTileModified(uid, grid, chunk, tileIndices, tile, oldTile, shapeChanged);
    }
}
