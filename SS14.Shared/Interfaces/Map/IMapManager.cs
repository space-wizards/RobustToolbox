using System.Collections.Generic;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Map;
using SS14.Shared.Maths;
using OpenTK;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Shared.Interfaces.Map
{
    /// <summary>
    ///     Event delegate for the OnTileChanged event.
    /// </summary>
    /// <param name="gridId">The ID of the grid being changed.</param>
    /// <param name="tileRef">A reference to the new tile being inserted.</param>
    /// <param name="oldTile">The old tile that is being replaced.</param>
    public delegate void TileChangedEventHandler(TileRef tileRef, Tile oldTile);

    public delegate void GridEventHandler(int mapId, int gridId);

    /// <summary>
    ///     This manages all of the grids in the world.
    /// </summary>
    public interface IMapManager
    {
        void UnregisterMap(int mapID);

        IMap CreateMap(int mapID);

        IMap GetMap(int mapID);

        IEnumerable<IMap> GetAllMaps();

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        bool SuppressOnTileChanged { get; set; }

        /// <summary>
        ///     A tile is being modified.
        /// </summary>
        event TileChangedEventHandler OnTileChanged;

        event GridEventHandler OnGridCreated;

        event GridEventHandler OnGridRemoved;

        /// <summary>
        ///     Starts up the map system.
        /// </summary>
        void Initialize();

        void SendMap(INetChannel channel);

        bool TryGetMap(int mapID, out IMap map);
    }
}
