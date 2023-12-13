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
    internal bool SetChunkTile(EntityUid uid, MapGridComponent grid, MapChunk chunk, ushort xIndex, ushort yIndex, Tile tile)
    {
        if (!chunk.TrySetTile(xIndex, yIndex, tile, out var oldTile, out var shapeChanged))
            return false;

        var tileIndices = new Vector2i(xIndex, yIndex);
        OnTileModified(uid, grid, chunk, tileIndices, tile, oldTile, shapeChanged);
        return true;
    }
}
