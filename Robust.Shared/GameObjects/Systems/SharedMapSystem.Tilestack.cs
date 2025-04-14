using System.Collections.Generic;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Maths;

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
    ///     Deletes the tilestack if the tile is empty.
    /// </summary>
    private void OnTileChanged(ref TileChangedEvent ev)
    {
        return;
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
}
