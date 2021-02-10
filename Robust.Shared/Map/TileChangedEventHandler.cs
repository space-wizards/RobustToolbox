namespace Robust.Shared.Map
{
    /// <summary>
    ///     Event delegate for the OnTileChanged event.
    /// </summary>
    /// <param name="gridId">The ID of the grid being changed.</param>
    /// <param name="tileRef">A reference to the new tile being inserted.</param>
    /// <param name="oldTile">The old tile that is being replaced.</param>
    public delegate void TileChangedEventHandler(TileRef tileRef, Tile oldTile);
}