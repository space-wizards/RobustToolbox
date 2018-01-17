using System;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Map;

namespace SS14.Shared.Interfaces.Map
{
    /// <summary>
    ///     This manages all of the grids in the world.
    /// </summary>
    public interface IMapManager
    {
        /// <summary>
        ///     The default <see cref="IMap" /> that is always available. Equivalent to SS13 Null space.
        /// </summary>
        IMap DefaultMap { get; }

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
