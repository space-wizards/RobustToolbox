using System;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Map;

namespace SS14.Shared.Interfaces.Map
{
    /// <summary>
    ///     Event delegate for the OnTileChanged event.
    /// </summary>
    /// <param name="gridId">The ID of the grid being changed.</param>
    /// <param name="tileRef">A reference to the new tile being inserted.</param>
    /// <param name="oldTile">The old tile that is being replaced.</param>
    public delegate void TileChangedEventHandler(GridId gridId, TileRef tileRef, Tile oldTile);

    /// <summary>
    ///     This manages all of the grids in the world.
    /// </summary>
    public interface IMapManager
    {
        /// <summary>
        ///     The default <see cref="IMap"/> that is always available. Equivalent to SS13 Null space.
        /// </summary>
        IMap DefaultMap { get; }

        void UnregisterMap(MapId mapID);

        IMap CreateMap(MapId mapID, bool overwrite = false);

        IMap GetMap(MapId mapID);

        bool MapExists(MapId mapID);

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        bool SuppressOnTileChanged { get; set; }

        /// <summary>
        ///     A tile is being modified.
        /// </summary>
        event TileChangedEventHandler OnTileChanged;

        /// <summary>
        ///     A new map has been created.
        /// </summary>
        event EventHandler<MapEventArgs> MapCreated;

        /// <summary>
        ///     An existing map has been destroyed.
        /// </summary>
        event EventHandler<MapEventArgs> MapDestroyed;

        /// <summary>
        ///     Starts up the map system.
        /// </summary>
        void Initialize();

        void SendMap(INetChannel channel);

        bool TryGetMap(MapId mapID, out IMap map);
    }
}
