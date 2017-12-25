using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;
using Vector2 = SS14.Shared.Maths.Vector2;

namespace SS14.Shared.Map
{
    public partial class MapManager : IMapManager
    {
        public const int NULLSPACE = 0;
        public const int DEFAULTGRID = 0;
        private const ushort DefaultTileSize = 1;

        /// <inheritdoc />
        public void Initialize()
        {
            NetSetup();
            CreateMap(MapManager.NULLSPACE);
        }

        /// <inheritdoc />
        public event TileChangedEventHandler OnTileChanged;

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
        public void RaiseOnTileChanged(int gridId, TileRef tileRef, Tile oldTile)
        {
            if (SuppressOnTileChanged)
                return;

            OnTileChanged?.Invoke(gridId, tileRef, oldTile);
        }

        #region Networking

        #endregion Networking

        #region MapAccess

        /// <summary>
        ///     Holds an indexed collection of map grids.
        /// </summary>
        private readonly Dictionary<int, Map> _Maps = new Dictionary<int, Map>();

        public virtual void UnregisterMap(int mapID)
        {
            if (_Maps.ContainsKey(mapID))
            {
                _Maps.Remove(mapID);
            }
            else
            {
                Logger.Warning("Attempted to unregister nonexistent map");
            }
        }

        public virtual IMap CreateMap(int mapID)
        {
            var newMap = new Map(this, mapID);
            _Maps.Add(mapID, newMap);
            return newMap;
        }

        public IMap GetMap(int mapID)
        {
            return _Maps[mapID];
        }

        public bool TryGetMap(int mapID, out IMap map)
        {
            if (_Maps.ContainsKey(mapID))
            {
                map = _Maps[mapID];
                return true;
            }
            map = null;
            return false;
        }

        public IEnumerable<IMap> GetAllMaps()
        {
            foreach(var kmap in _Maps)
            {
                yield return kmap.Value;
            }
        }

        #endregion MapAccess
    }
}
