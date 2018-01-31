using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Network.Messages;

namespace SS14.Shared.Map
{
    /// <inheritdoc />
    public partial class MapManager : IMapManager
    {
        /// <inheritdoc />
        public IMap DefaultMap => GetMap(MapId.Nullspace);

        /// <inheritdoc />
        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgMap>(MsgMap.NAME, message => HandleNetworkMessage((MsgMap)message));
            CreateMap(MapId.Nullspace);
        }

        /// <inheritdoc />
        public event EventHandler<TileChangedEventArgs> TileChanged;

        public event GridEventHandler OnGridCreated;

        public event GridEventHandler OnGridRemoved;

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        /// <inheritdoc />
        public event EventHandler<GridChangedEventArgs> GridChanged;

        /// <inheritdoc />
        public event EventHandler<MapEventArgs> MapCreated;

        /// <inheritdoc />
        public event EventHandler<MapEventArgs> MapDestroyed;

        /// <inheritdoc />
        public bool SuppressOnTileChanged { get; set; }

        /// <summary>
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        public void RaiseOnTileChanged(TileRef tileRef, Tile oldTile)
        {
            if (SuppressOnTileChanged)
                return;

            TileChanged?.Invoke(this, new TileChangedEventArgs(tileRef, oldTile));

            if(_netManager.IsClient)
                return;

            var message = _netManager.CreateNetMessage<MsgMap>();

            message.MessageType = MapMessage.TurfUpdate;
            message.SingleTurf = new MsgMap.Turf
            {
                X = tileRef.X,
                Y = tileRef.Y,
                Tile = (uint)tileRef.Tile
            };
            message.GridIndex = tileRef.LocalPos.GridID;
            message.MapIndex = tileRef.LocalPos.MapID;

            _netManager.ServerSendToAll(message);
        }

        public void RaiseOnGridCreated(int mapId, int gridId)
        {
            OnGridCreated?.Invoke(mapId, gridId);
        }

        public void RaiseOnGridRemoved(int mapId, int gridId)
        {
            OnGridRemoved?.Invoke(mapId, gridId);
        }

        /// <summary>
        ///     Holds an indexed collection of map grids.
        /// </summary>
        private readonly Dictionary<MapId, Map> _maps = new Dictionary<MapId, Map>();

        /// <inheritdoc />
        public void DeleteMap(MapId mapID)
        {
            if (!_maps.ContainsKey(mapID))
            {
                Logger.Warning("[MAP] Attempted to delete nonexistent map.");
                return;
            }

            MapDestroyed?.Invoke(this, new MapEventArgs(_maps[mapID]));
            _maps.Remove(mapID);

            if(_netManager.IsClient)
                return;

            var msg = _netManager.CreateNetMessage<MsgMap>();

            msg.MessageType = MapMessage.DeleteMap;
            msg.MapIndex = mapID;

            _netManager.ServerSendToAll(msg);
        }

        /// <inheritdoc />
        public IMap CreateMap(MapId mapID, bool overwrite = false)
        {
            if(!overwrite && _maps.ContainsKey(mapID))
            {
                Logger.Warning("[MAP] Attempted to overwrite existing map.");
                return null;
            }

            var newMap = new Map(this, mapID);
            _maps.Add(mapID, newMap);
            MapCreated?.Invoke(this, new MapEventArgs(newMap));

            if (_netManager.IsClient)
                return newMap;

            var msg = _netManager.CreateNetMessage<MsgMap>();

            msg.MessageType = MapMessage.CreateMap;
            msg.MapIndex = newMap.Index;

            _netManager.ServerSendToAll(msg);

            return newMap;
        }

        /// <inheritdoc />
        public IMap GetMap(MapId mapID)
        {
            return _maps[mapID];
        }

        /// <inheritdoc />
        public bool MapExists(MapId mapID)
        {
            return _maps.ContainsKey(mapID);
        }

        /// <inheritdoc />
        public bool TryGetMap(MapId mapID, out IMap map)
        {
            if (_maps.ContainsKey(mapID))
            {
                map = _maps[mapID];
                return true;
            }
            map = null;
            return false;
        }

        private IEnumerable<IMap> GetAllMaps()
        {
            return _maps.Select(kvMap => kvMap.Value);
        }
    }

    /// <summary>
    ///     Arguments for when a map is created or deleted locally ore remotely.
    /// </summary>
    public class MapEventArgs : EventArgs
    {
        /// <summary>
        ///     Map that is being modified.
        /// </summary>
        public IMap Map { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public MapEventArgs(IMap map)
        {
            Map = map;
        }
    }

    /// <summary>
    ///     Arguments for when a tile is changed locally or remotely.
    /// </summary>
    public class TileChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     New tile that replaced the old one.
        /// </summary>
        public TileRef NewTile { get; }

        /// <summary>
        ///     Old tile that was replaced.
        /// </summary>
        public Tile OldTile { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public TileChangedEventArgs(TileRef newTile, Tile oldTile)
        {
            NewTile = newTile;
            OldTile = oldTile;
        }
    }

    /// <summary>
    ///     Arguments for when a Grid is changed locally or remotely.
    /// </summary>
    public class GridChangedEventArgs : EventArgs
    {
        /// <summary>
        ///     Grid being changed.
        /// </summary>
        public IMapGrid Grid { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public GridChangedEventArgs(IMapGrid grid)
        {
            Grid = grid;
        }
    }
}
