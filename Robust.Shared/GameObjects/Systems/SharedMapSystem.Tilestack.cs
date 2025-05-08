using System.Collections.Generic;
using System.Linq;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    private void InitializeTilestacking()
    {
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
    }

    /// <summary>
    /// Deletes the tilestack if the tile is manually changed.
    /// </summary>
    private void OnTileChanged(ref TileChangedEvent ev)
    {
        DeleteTilestack(ev.ChunkIndex, ev.Entity.Owner, ev.Entity.Comp);
    }

    // TODO remove this
    public bool TryTilestack(Vector2i gridIndices, EntityUid gridUid, out List<Tile>? tilestack)
    {
        if (!TryComp<TilestackMapGridComponent>(gridUid, out var comp))
        {
            tilestack = null;
            return false;
        }
        var found = comp.Data.TryGetValue(gridIndices, out var tileStack);
        tilestack = tileStack;
        return found;
    }

    /// <summary>
    /// Adds the tile on top of the current tile in the tilestack or simply sets the tile.
    /// </summary>
    public void AddLayer(Vector2i gridIndices, EntityUid gridUid, MapGridComponent grid, Tile newTile)
    {
        var chunkIndices = GridTileToChunkIndices(gridUid, grid, gridIndices);
        TryGetChunk(gridUid, grid, gridIndices, out var chunk);
        chunk?.AddToTilestack((ushort)chunkIndices.X, (ushort)chunkIndices.Y, newTile);
    }

    /// <summary>
    /// Removes the top tile from the tilestack, does nothing if there is no tilestack.
    /// </summary>
    public void RemoveLayer(Vector2i gridIndices, EntityUid gridUid, MapGridComponent grid)
    {
        var chunkIndices = GridTileToChunkIndices(gridUid, grid, gridIndices);
        TryGetChunk(gridUid, grid, gridIndices, out var chunk);
        var tilestack = chunk?.GetTilestack((ushort)chunkIndices.X, (ushort)chunkIndices.Y);
        if (tilestack?.Count == 0 || tilestack == null)
            return;
        chunk?.RemoveFromTilestack((ushort)chunkIndices.X, (ushort)chunkIndices.Y, tilestack.Last());
    }

    /// <summary>
    /// Removes the tilestack at the position entirely.
    /// </summary>
    public void DeleteTilestack(Vector2i gridIndices, EntityUid gridUid, MapGridComponent grid)
    {
        var chunkIndices = GridTileToChunkIndices(gridUid, grid, gridIndices);
        TryGetChunk(gridUid, grid, gridIndices, out var chunk);
        chunk?.DeleteTilestack((ushort)chunkIndices.X, (ushort)chunkIndices.Y);
    }
}
