using System;
using System.Collections.Generic;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Log;

namespace SS14.Shared.Map
{
    public partial class MapManager : IMapManager
    {
        private const ushort DefaultTileSize = 1;

        /// <inheritdoc />
        public IMap DefaultMap => GetMap(MapId.Nullspace);

        /// <inheritdoc />
        public void Initialize()
        {
            NetSetup();
            CreateMap(MapId.Nullspace);
        }

        /// <inheritdoc />
        public event TileChangedEventHandler OnTileChanged;

        public event EventHandler<MapEventArgs> MapCreated;
        public event EventHandler<MapEventArgs> MapDestroyed;

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        public bool SuppressOnTileChanged { get; set; }

        /// <summary>
        ///     Raises the OnTileChanged event.
        /// </summary>
        /// <param name="gridId">The ID of the grid that was modified.</param>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile that got replaced.</param>
        public void RaiseOnTileChanged(GridId gridId, TileRef tileRef, Tile oldTile)
        {
            if (SuppressOnTileChanged)
                return;

            OnTileChanged?.Invoke(gridId, tileRef, oldTile);
        }

        #region MapAccess

        /// <summary>
        ///     Holds an indexed collection of map grids.
        /// </summary>
        private readonly Dictionary<MapId, Map> _maps = new Dictionary<MapId, Map>();

        public void UnregisterMap(MapId mapID)
        {
            if (_maps.ContainsKey(mapID))
            {
                BroadcastUnregisterMap(mapID);
                MapDestroyed?.Invoke(this, new MapEventArgs(_maps[mapID]));
                _maps.Remove(mapID);
            }
            else
            {
                Logger.Warning("[MAP] Attempted to unregister nonexistent map.");
            }
        }
        
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

            BroadcastCreateMap(newMap);

            return newMap;
        }

        public IMap GetMap(MapId mapID)
        {
            return _maps[mapID];
        }

        public bool MapExists(MapId mapID)
        {
            return _maps.ContainsKey(mapID);
        }

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

        public IEnumerable<IMap> GetAllMaps()
        {
            foreach(var kmap in _maps)
            {
                yield return kmap.Value;
            }
        }

        #endregion MapAccess
    }

    public class MapEventArgs : EventArgs
    {
        public IMap Map { get; }

        public MapEventArgs(IMap map)
        {
            Map = map;
        }
    }
}
