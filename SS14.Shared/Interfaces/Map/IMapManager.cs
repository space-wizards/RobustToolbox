using System;
using System.Collections.Generic;
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
    public delegate void TileChangedEventHandler(TileRef tileRef, Tile oldTile);

    public delegate void GridEventHandler(MapId mapId, GridId gridId);

    /// <summary>
    ///     This manages all of the grids in the world.
    /// </summary>
    public interface IMapManager
    {
        /// <summary>
        ///     The default <see cref="IMap" /> that is always available. Equivalent to SS13 Null space.
        /// </summary>
        IMap DefaultMap { get; }

        IEnumerable<IMap> GetAllMaps();

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        bool SuppressOnTileChanged { get; set; }

        /// <summary>
        ///     Starts up the map system.
        /// </summary>
        void Initialize();

        IMap CreateMap(MapId mapID, bool overwrite = false);

        bool MapExists(MapId mapID);

        IMap GetMap(MapId mapID);

        bool TryGetMap(MapId mapID, out IMap map);

        void SendMap(INetChannel channel);

        void DeleteMap(MapId mapID);

        /// <summary>
        ///     A tile is being modified.
        /// </summary>
        event EventHandler<TileChangedEventArgs> TileChanged;

        event GridEventHandler OnGridCreated;

        event GridEventHandler OnGridRemoved;

        /// <summary>
        ///     A Grid was modified.
        /// </summary>
        event EventHandler<GridChangedEventArgs> GridChanged;

        /// <summary>
        ///     A new map has been created.
        /// </summary>
        event EventHandler<MapEventArgs> MapCreated;

        /// <summary>
        ///     An existing map has been destroyed.
        /// </summary>
        event EventHandler<MapEventArgs> MapDestroyed;
    }
}
