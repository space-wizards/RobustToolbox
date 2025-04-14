using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;
using Robust.Shared.Utility;

namespace Robust.Shared.GameObjects;

public abstract partial class SharedMapSystem
{
    private void InitializeTilestacking()
    {
        SubscribeLocalEvent<GridInitializeEvent>(AfterGridInit);
        SubscribeLocalEvent<TileChangedEvent>(OnTileChanged);
    }

    private void AfterGridInit(GridInitializeEvent ev)
    {
        EnsureComp<TilestackMapGridComponent>(ev.EntityUid);
    }

    /// <summary>
    ///     Deletes the tilestack if the tile is manually changed.
    /// </summary>
    private void OnTileChanged(ref TileChangedEvent ev)
    {
        DeleteTilestack(ev.ChunkIndex, ev.Entity.Owner, ev.Entity.Comp);
    }

    /// <summary>
    ///     Gets a list of tiles under the current tile, returns true if succeeds.
    /// </summary>
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
    ///     Adds the tile on top of the current tile in the tilestack or simply sets the tile.
    /// </summary>
    public void AddLayer(Vector2i gridIndices, EntityUid gridUid, MapGridComponent grid, Tile newTile)
    {
        var curTile = GetTileRef(gridUid, grid, gridIndices);
        SetTile(gridUid, grid, gridIndices, newTile);
        if (!TryTilestack(gridIndices, gridUid, out var tilestack))
            return;
        tilestack!.Add(curTile.Tile);
    }

    /// <summary>
    ///     Removes the top tile from the tilestack, does nothing if there is no tilestack.
    /// </summary>
    public void RemoveLayer(Vector2i gridIndices, EntityUid gridUid, MapGridComponent grid)
    {
        if (!TryTilestack(gridIndices, gridUid, out var tilestack) || !TryComp<TilestackMapGridComponent>(gridUid, out var comp))
            return;
        SetTile(gridUid, grid, gridIndices, tilestack![^1]);
        tilestack.RemoveAt(tilestack.Count - 1);
        if (tilestack.Count == 0)
            comp.Data.Remove(gridIndices);
    }

    /// <summary>
    ///     Removes the tilestack at the position entirely.
    /// </summary>
    public void DeleteTilestack(Vector2i gridIndices, EntityUid gridUid, MapGridComponent grid)
    {
        if (!TryComp<TilestackMapGridComponent>(gridUid, out var comp))
            return;
        comp.Data.Remove(gridIndices);
    }
}
