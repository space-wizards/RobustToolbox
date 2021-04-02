namespace Robust.Shared.Map
{
    /// <summary>
    /// Invoked when a grid is altered.
    /// </summary>
    /// <param name="mapId">Passed to the delegate given it may no longer be retrievable.</param>
    /// <param name="gridId">The index of the grid being changed.</param>
    public delegate void GridEventHandler(MapId mapId, GridId gridId);
}
