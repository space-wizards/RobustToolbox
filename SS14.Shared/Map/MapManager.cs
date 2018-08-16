using System;
using System.Collections.Generic;
using System.Linq;
using SS14.Shared.Enums;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using SS14.Shared.Network.Messages;

namespace SS14.Shared.Map
{
    /// <inheritdoc />
    public partial class MapManager : IMapManager, IPostInjectInit
    {
        /// <inheritdoc />
        public IMap DefaultMap => GetMap(MapId.Nullspace);

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

        private MapId HighestMapID = MapId.Nullspace;
        private GridId HighestGridID = GridId.Nullspace;

        /// <summary>
        ///     Holds an indexed collection of map grids.
        /// </summary>
        private readonly Dictionary<MapId, Map> _maps = new Dictionary<MapId, Map>();

        private readonly Dictionary<GridId, MapGrid> _grids = new Dictionary<GridId, MapGrid>();

        public void PostInject()
        {
            CreateMap(MapId.Nullspace, GridId.Nullspace);
        }

        /// <inheritdoc />
        public void Initialize()
        {
            _netManager.RegisterNetMessage<MsgMap>(MsgMap.NAME, HandleNetworkMessage);
            _netManager.RegisterNetMessage<MsgMapReq>(MsgMapReq.NAME, message => SendMap(message.MsgChannel));
        }

        public void Startup()
        {
            _gridsToReceive = -1;
            _gridsReceived = 0;
        }

        public void Shutdown()
        {
        }

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

            if (_netManager.IsClient)
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

            _netManager.ServerSendToAll(message);
        }


        /// <inheritdoc />
        public void DeleteMap(MapId mapID)
        {
            if (!_maps.TryGetValue(mapID, out var map))
            {
                throw new InvalidOperationException($"Attempted to delete nonexistant map '{mapID}'");
            }

            // grids are cached because Delete modifies collection
            foreach (var grid in map.GetAllGrids().ToList())
            {
                DeleteGrid(grid.Index);
            }

            MapDestroyed?.Invoke(this, new MapEventArgs(_maps[mapID]));
            _maps.Remove(mapID);

            if (_netManager.IsClient)
                return;

            var msg = _netManager.CreateNetMessage<MsgMap>();

            msg.MessageType = MapMessage.DeleteMap;
            msg.MapIndex = mapID;

            _netManager.ServerSendToAll(msg);
        }

        /// <inheritdoc />
        public IMap CreateMap(MapId? mapID = null, GridId? defaultGridID = null)
        {
            if (defaultGridID != null && GridExists(defaultGridID.Value))
            {
                throw new InvalidOperationException($"Grid '{defaultGridID}' already exists.");
            }
            MapId actualID;
            if (mapID != null)
            {
                actualID = mapID.Value;
            }
            else
            {
                actualID = new MapId(HighestMapID.Value + 1);
            }

            if (MapExists(actualID))
            {
                throw new InvalidOperationException($"A map with ID {actualID} already exists");
            }

            if (HighestMapID.Value < actualID.Value)
            {
                HighestMapID = actualID;
            }

            var newMap = new Map(this, actualID);
            _maps.Add(actualID, newMap);
            MapCreated?.Invoke(this, new MapEventArgs(newMap));
            newMap.DefaultGrid = CreateGrid(newMap.Index, defaultGridID);

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
            if (_maps.TryGetValue(mapID, out var mapinterface))
            {
                map = mapinterface;
                return true;
            }
            map = null;
            return false;
        }

        public IEnumerable<IMap> GetAllMaps()
        {
            return _maps.Values;
        }

        public IMapGrid CreateGrid(MapId currentMapID, GridId? gridID = null, ushort chunkSize = 16, float snapSize = 1)
        {
            var map = _maps[currentMapID];

            GridId actualID;
            if (gridID != null)
            {
                actualID = gridID.Value;
            }
            else
            {
                actualID = new GridId(HighestGridID.Value + 1);
            }

            if (GridExists(actualID))
            {
                throw new InvalidOperationException($"A map with ID {actualID} already exists");
            }

            if (HighestGridID.Value < actualID.Value)
            {
                HighestGridID = actualID;
            }

            var grid = new MapGrid(this, actualID, chunkSize, snapSize, currentMapID);
            _grids.Add(actualID, grid);
            map.AddGrid(grid);
            OnGridCreated?.Invoke(actualID);
            return grid;
        }

        public IMapGrid GetGrid(GridId gridID)
        {
            return _grids[gridID];
        }

        public bool TryGetGrid(GridId gridId, out IMapGrid grid)
        {
            if (_grids.TryGetValue(gridId, out var gridinterface))
            {
                grid = gridinterface;
                return true;
            }
            grid = null;
            return false;
        }

        public bool GridExists(GridId gridID)
        {
            return _grids.ContainsKey(gridID);
        }

        public void DeleteGrid(GridId gridID)
        {
            var grid = _grids[gridID];
            var map = (Map)grid.Map;

            grid.Dispose();
            map.RemoveGrid(grid);
            _grids.Remove(grid.Index);

            OnGridRemoved?.Invoke(gridID);
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

        public IReadOnlyCollection<(int x, int y, Tile tile)> Modified { get; }

        /// <summary>
        ///     Creates a new instance of this class.
        /// </summary>
        public GridChangedEventArgs(IMapGrid grid, IReadOnlyCollection<(int x, int y, Tile tile)> modified)
        {
            Grid = grid;
            Modified = modified;
        }
    }
}
