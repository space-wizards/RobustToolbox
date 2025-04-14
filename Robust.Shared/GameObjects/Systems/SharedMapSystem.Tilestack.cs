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

    private void AfterGridInit(ref GridInitializeEvent ev)
    {
        EnsureComp<TilestackMapGridComponent>(ev.EntityUid);
    }

    /// <summary>
    ///     Deletes the tilestack if the tile is empty.
    /// </summary>
    private void OnTileChanged(ref TileChangedEvent ev)
    {
        if (ev.NewTile.Tile.IsEmpty)
            RemoveTilestack(ev.NewTile, false);
    }

    public bool HasTilestack(TileRef tileRef)
    {
        return HasTilestack(tileRef.GridIndices, tileRef.GridUid);
    }

    /// <summary>
    ///     Checks if the tile has a stored tilestack.
    /// </summary>
    /// <param name="gridIndices">Positional indices of this tile on the grid.</param>
    /// <param name="gridUid">Identifier of the grid entity this tile belongs to.</param>
    public bool HasTilestack(Vector2i gridIndices, EntityUid gridUid)
    {
        if (!TryComp<TilestackMapGridComponent>(gridUid, out var comp))
            return false;
        return comp.Data.ContainsKey(gridIndices);
    }

    public void AddTileLayer(Tile newTile, Vector2i gridIndices, EntityUid gridUid)
    {
        if (!HasComp<MapGridComponent>(gridUid))
            return;
        var tileRef = GetTileRef(gridUid!, gridIndices);
        AddTileLayer(newTile, tileRef);
    }

    /// <summary>
    ///     Tilestack-respecting way of adding a tile.
    /// </summary>
    /// <param name="newTile">A tile that you're placing.</param>
    /// <param name="oldTile">TileRef where you're placing it.</param>
    public void AddTileLayer(Tile newTile, TileRef oldTile)
    {
        if (!HasComp<MapGridComponent>(oldTile.GridUid) || !TryComp<TilestackMapGridComponent>(oldTile.GridUid, out var comp))
            return;
        comp.Data[oldTile.GridIndices].Add(oldTile.Tile);
        SetTile(oldTile.GridUid!, oldTile.GridIndices, newTile);
    }

    public void RemoveTileLayer(TileRef tileRef)
    {
        RemoveTileLayer(tileRef.GridIndices, tileRef.GridUid);
    }

    /// <summary>
    ///     Tilestack-respecting way of prying a tile.
    /// </summary>
    /// <param name="gridIndices">Positional indices of this tile on the grid.</param>
    /// <param name="gridUid">Identifier of the grid entity this tile belongs to.</param>
    public void RemoveTileLayer(Vector2i gridIndices, EntityUid gridUid)
    {
        if (!HasComp<MapGridComponent>(gridUid) || !TryComp<TilestackMapGridComponent>(gridUid, out var comp))
            return;
        SetTile(gridUid!, gridIndices, comp.Data[gridIndices].Pop());
        // delete empty tilestack
        if (comp.Data[gridIndices].Count == 0)
            comp.Data.Remove(gridIndices);
    }

    public void RemoveTilestack(TileRef tileRef, bool removeTile = true)
    {
        RemoveTilestack(tileRef.GridIndices, tileRef.GridUid, removeTile);
    }

    /// <summary>
    ///     Tilestack-respecting way of removing a tile completely.
    /// </summary>
    /// <param name="gridIndices">Positional indices of this tile on the grid.</param>
    /// <param name="gridUid">Identifier of the grid entity this tile belongs to.</param>
    /// <param name="removeTile">If true, the top tile gets removed too.</param>
    public void RemoveTilestack(Vector2i gridIndices, EntityUid gridUid, bool removeTile=true)
    {
        if (!HasComp<MapGridComponent>(gridUid) || !TryComp<TilestackMapGridComponent>(gridUid, out var comp))
            return;
        comp.Data.Remove(gridIndices);
        if (removeTile)
            SetTile(gridUid!, gridIndices, Tile.Empty);
    }
}
