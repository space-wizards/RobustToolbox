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

    public delegate void GridEventHandler(GridId gridId);

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

        void Shutdown();
        void Startup();

        /// <summary>
        ///     Creates a new map.
        /// </summary>
        /// <param name="mapID">
        ///     If provided, the new map will use this ID. If not provided, a new ID will be selected automatically.
        /// </param>
        /// <param name="defaultGapID">
        ///     If provided, the new map will use this grid ID as default grid. If not provided, a new ID will be selected automatically.
        /// </param>
        /// <returns>The new map.</returns>
        /// <exception cref="InvalidOperationException">
        ///     Throw if an explicit ID for the map or default grid is passed and a map or grid with the specified ID already exists, respectively.
        /// </exception>
        IMap CreateMap(MapId? mapID = null, GridId? defaultGridID = null);

        /// <summary>
        ///     Check whether a map with specified ID exists.
        /// </summary>
        /// <param name="mapID">The map ID to check existance of.</param>
        /// <returns>True if the map exists, false otherwise.</returns>
        bool MapExists(MapId mapID);

        IMap GetMap(MapId mapID);

        bool TryGetMap(MapId mapID, out IMap map);

        void SendMap(INetChannel channel);

        void DeleteMap(MapId mapID);

        IMapGrid CreateGrid(MapId currentMapID, GridId? gridID = null, ushort chunkSize = 16, float snapSize = 1);
        IMapGrid GetGrid(GridId gridID);
        bool TryGetGrid(GridId gridId, out IMapGrid grid);
        bool GridExists(GridId gridID);
        void DeleteGrid(GridId gridID);

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
