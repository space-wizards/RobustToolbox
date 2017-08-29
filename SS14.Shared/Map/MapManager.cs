using System;
using System.Collections.Generic;
using System.Diagnostics;
using OpenTK;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace SS14.Shared.Map
{
    public partial class MapManager : IMapManager
    {
        private const int GridIndex = 0;
        private const ushort DefaultTileSize = 1;

        /// <inheritdoc />
        public void Initialize()
        {
            NetSetup();

            TileSize = DefaultTileSize;

            //create default grid.
            CreateGrid(DefaultGridId, 16);
        }

        /// <inheritdoc />
        public event TileChangedEventHandler OnTileChanged;

        /// <summary>
        ///     If you are only going to have one giant global grid, this is your gridId.
        /// </summary>
        public int DefaultGridId => GridIndex;

        /// <summary>
        ///     Should the OnTileChanged event be suppressed? This is useful for initially loading the map
        ///     so that you don't spam an event for each of the million station tiles.
        /// </summary>
        public bool SuppressOnTileChanged { get; set; }

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        public ushort TileSize { get; set; }

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

        #endregion

        #region GridAccess

        /// <summary>
        ///     Holds an indexed collection of map grids.
        /// </summary>
        private readonly Dictionary<int, MapGrid> _grids = new Dictionary<int, MapGrid>();

        /// <summary>
        ///     Creates a new empty grid with the given ID and optional chunk size. If a grid already
        ///     exists with the gridID, it is overwritten with the new grid.
        /// </summary>
        /// <param name="gridId">The id of the new grid to create.</param>
        /// <param name="chunkSize">Optional chunk size of the new grid.</param>
        /// <param name="snapSize">Optional size of the snap grid</param>
        /// <returns></returns>
        public IMapGrid CreateGrid(int gridId, ushort chunkSize = 32, float snapSize = 1, int mapID = 0)
        {
            var newGrid = new MapGrid(this, gridId, chunkSize, snapSize, mapID);
            _grids.Add(gridId, newGrid);
            return newGrid;
        }

        /// <summary>
        ///     Checks if a grid exists with the given ID.
        /// </summary>
        /// <param name="gridId">The ID of the grid to check.</param>
        /// <returns></returns>
        public bool GridExists(int gridId)
        {
            return _grids.ContainsKey(gridId);
        }

        /// <summary>
        ///     Gets the grid associated with the given grid ID. If the grid with the given ID does not exist, return null.
        /// </summary>
        /// <param name="gridId">The id of the grid to get.</param>
        /// <returns></returns>
        public IMapGrid GetGrid(int gridId)
        {
            MapGrid output;
            _grids.TryGetValue(gridId, out output);
            return output;
        }

        /// <summary>
        ///     Gets the grid associated with the given grid ID.
        /// </summary>
        /// <param name="gridId">The id of the grid to get.</param>
        /// <param name="mapGrid">The grid associated with the grid ID. If no grid exists, this is null.</param>
        /// <returns></returns>
        public bool TryGetGrid(int gridId, out IMapGrid mapGrid)
        {
            mapGrid = GetGrid(gridId);
            return mapGrid != null;
        }

        /// <summary>
        ///     Alias of IMapManager.GetGrid(IMapManager.DefaultGridId);
        /// </summary>
        /// <returns></returns>
        public IMapGrid GetDefaultGrid()
        {
            return GetGrid(DefaultGridId);
        }

        /// <summary>
        ///     Deletes the grid associated with the given grid ID.
        /// </summary>
        /// <param name="gridId">The grid to remove.</param>
        public void RemoveGrid(int gridId)
        {
            MapGrid output;
            if (!_grids.TryGetValue(gridId, out output))
                return;

            output.Dispose();
            _grids.Remove(gridId);
        }

        /// <summary>
        ///     Is there any grid at this position in the world?
        /// </summary>
        /// <param name="xWorld">The X coordinate in the world.</param>
        /// <param name="yWorld">The Y coordinate in the world.</param>
        /// <returns>True if there is any grid at the location.</returns>
        public bool IsGridAt(WorldCoordinates posWorld)
        {
            var pos = posWorld.Position;
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(pos))
                    return true;
            return false;
        }

        /// <summary>
        ///     Is the specified grid at this position in the world?
        /// </summary>
        /// <param name="xWorld">The X coordinate in the world.</param>
        /// <param name="yWorld">The Y coordinate in the world.</param>
        /// <param name="gridId">The grid id to find.</param>
        /// <returns></returns>
        public bool IsGridAt(WorldCoordinates worldPos, int gridId)
        {
            var pos = worldPos.Position;
            return _grids.TryGetValue(gridId, out MapGrid output) && output.AABBWorld.Contains(pos);
        }

        /// <summary>
        ///     Finds all of the grids at this position in the world.
        /// </summary>
        /// <param name="xWorld">The X coordinate in the world.</param>
        /// <param name="yWorld">The Y coordinate in the world.</param>
        /// <returns></returns>
        public IEnumerable<IMapGrid> FindGridsAt(WorldCoordinates worldPos)
        {
            var pos = worldPos.Position;
            var gridList = new List<MapGrid>();
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(pos))
                    gridList.Add(kvGrid.Value);
            return gridList;
        }

        /// <summary>
        ///     Finds the grid at this world coordinate
        /// </summary>
        /// <param name="xWorld">The X coordinate in the world.</param>
        /// <param name="yWorld">The Y coordinate in the world.</param>
        public bool TryFindGridAt(WorldCoordinates worldPos, out IMapGrid currentgrid)
        {
            var pos = worldPos.Position;
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(pos))
                {
                    currentgrid = kvGrid.Value;
                    return true;
                }
            currentgrid = null;
            return false;
        }

        /// <summary>
        ///     Finds the grid at this world coordinate
        /// </summary>
        /// <param name="WorldPos">The X coordinate in the world.</param>
        public bool TryFindGridAt(Vector2 worldPos, out IMapGrid currentgrid)
        {
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(worldPos))
                {
                    currentgrid = kvGrid.Value;
                    return true;
                }
            currentgrid = GetDefaultGrid();
            return false;
        }

        /// <summary>
        ///     Finds all of the grids at this position in the world.
        /// </summary>
        /// <param name="worldPos">The location of the tile in world coordinates.</param>
        /// <returns></returns>
        public IEnumerable<IMapGrid> FindGridsAt(Vector2 worldPos)
        {
            var gridList = new List<MapGrid>();
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Contains(worldPos))
                    gridList.Add(kvGrid.Value);
            return gridList;
        }

        /// <summary>
        ///     Finds all grids that intersect the rectangle in the world.
        /// </summary>
        /// <param name="worldArea">The are in world coordinates to search.</param>
        /// <returns></returns>
        public IEnumerable<IMapGrid> FindGridsIntersecting(Box2 worldArea)
        {
            var gridList = new List<MapGrid>();
            foreach (var kvGrid in _grids)
                if (kvGrid.Value.AABBWorld.Intersects(worldArea))
                    gridList.Add(kvGrid.Value);
            return gridList;
        }

        #endregion Networking

        #region File Operations

        public void SaveMap(string mapName)
        {
            throw new NotImplementedException();
#if _OLD
            mapName = Regex.Replace(mapName, @"-\d\d-\d\d_\d\d-\d\d-\d\d", ""); //Strip timestamp, same format as below
            DateTime date = DateTime.Now;
            mapName = String.Concat(mapName, date.ToString("-MM-dd_HH-mm-ss")); //Add timestamp, same format as above

            Logger.Log(string.Format("We are attempting to save map with name {0}", mapName));

            var badChars = Path.GetInvalidFileNameChars();
            if (mapName.Any(c => badChars.Contains(c)))
                throw new ArgumentException("Invalid characters in map name.", "mapName");

            string pathName = Path.Combine(Assembly.GetEntryAssembly().Location, @"..\Maps");
            Directory.CreateDirectory(pathName);

            string fileName = Path.GetFullPath(Path.Combine(pathName, mapName));

            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                var sw = new StreamWriter(fs);
                var bw = new BinaryWriter(fs);
                Logger.Log(string.Format("Saving map: \"{0}\" {1:N} Chunks", mapName, chunks.Count));

                sw.Write("SS14 Map File Version ");
                sw.Write((int)1); // Format version.  Who knows, it could come in handy.
                sw.Write("\r\n"); // Doing this instead of using WriteLine to keep things platform-agnostic.
                sw.Flush();

                // Tile definition mapping
                var tileDefManager = IoCManager.Resolve<ITileDefinitionManager>();
                bw.Write((int)tileDefManager.Count);
                for (int tileId = 0; tileId < tileDefManager.Count; ++tileId)
                {
                    bw.Write((ushort)tileId);
                    bw.Write((string)tileDefManager[tileId].Name);
                }

                // Map chunks
                bw.Write((int)Chunks.Count);
                foreach (var kvChunk in Chunks)
                {
                    bw.Write((int)kvChunk.Key.X);
                    bw.Write((int)kvChunk.Key.Y);

                    foreach (var tile in kvChunk.Value.Tiles)
                        bw.Write((uint)tile);
                }

                bw.Write("End of Map");
                bw.Flush();
            }

            Logger.Log("Done saving map.");
#endif
        }

        public bool LoadMap(string mapName)
        {
            var defManager = IoCManager.Resolve<ITileDefinitionManager>();

            NewDefaultMap(this, defManager, DefaultGridId);

            return true;
#if _OLD
            SuppressOnTileChanged = true;
            try
            {
                var badChars = Path.GetInvalidFileNameChars();
                if (mapName.Any(c => badChars.Contains(c)))
                    throw new ArgumentException("Invalid characters in map name.", "mapName");

                string fileName = Path.GetFullPath(Path.Combine(Assembly.GetEntryAssembly().Location, @"..\Maps", mapName));

                if (!File.Exists(fileName))
                    return false;

                using (var fs = new FileStream(fileName, FileMode.Open))
                {
                    var sr = new StreamReader(fs);
                    var br = new BinaryReader(fs);
                    Logger.Log(string.Format("Loading map: \"{0}\"", mapName));

                    var versionString = sr.ReadLine();
                    if (!versionString.StartsWith("SS14 Map File Version "))
                        return false;

                    fs.Seek(versionString.Length + 2, SeekOrigin.Begin);

                    int formatVersion;
                    if (!Int32.TryParse(versionString.Substring(22), out formatVersion))
                        return false;

                    if (formatVersion != 1)
                        return false; // Unsupported version.

                    var tileDefMgr = IoCManager.Resolve<ITileDefinitionManager>();

                    //Dictionary<ushort, ITileDefinition> tileMapping = new Dictionary<ushort, ITileDefinition>();
                    int tileDefCount = br.ReadInt32();
                    for (int i = 0; i < tileDefCount; ++i)
                    {
                        ushort tileId = br.ReadUInt16();
                        string tileName = br.ReadString();
                        //tileMapping[tileId] = tileDefMgr[tileName];
                    }

                    int chunkCount = br.ReadInt32();
                    for (int i = 0; i < chunkCount; ++i)
                    {
                        int cx = br.ReadInt32() * ChunkSize;
                        int cy = br.ReadInt32() * ChunkSize;

                        for (int y = cy; y < cy + ChunkSize; ++y)
                        for (int x = cx; x < cx + ChunkSize; ++x)
                        {
                            Tile tile = (Tile)br.ReadUInt32();
                            Tiles[x, y] = tile;
                        }
                    }

                    string ending = br.ReadString();
                    Debug.Assert(ending == "End of Map");
                }

                Logger.Log("Done loading map.");
                return true;
            }
            finally
            {
                SuppressOnTileChanged = false;
            }
#endif
        }

        //TODO: This whole method should be removed once file loading/saving works, and replaced with a 'Demo' map.
        /// <summary>
        ///     Generates 'Demo' grid and inserts it into the map manager.
        /// </summary>
        /// <param name="mapManager">The map manager to work with.</param>
        /// <param name="defManager">The definition manager to work with.</param>
        /// <param name="gridId">The ID of the grid to generate and insert into the map manager.</param>
        private static void NewDefaultMap(IMapManager mapManager, ITileDefinitionManager defManager, int gridId)
        {
            mapManager.SuppressOnTileChanged = true;
            try
            {
                Logger.Log("Cannot find map. Generating blank map.", LogLevel.Warning);
                var floor = defManager["Floor"].TileId;
                var wall = defManager["Wall"].TileId;

                Debug.Assert(floor > 0);
                Debug.Assert(wall > 0);

                var grid = mapManager.GetGrid(gridId) ?? mapManager.CreateGrid(gridId);

                for (var y = -32; y <= 32; ++y)
                for (var x = -32; x <= 32; ++x)
                    if (Math.Abs(x) == 32 || Math.Abs(y) == 32 || Math.Abs(x) == 5 && Math.Abs(y) < 5 || Math.Abs(y) == 7 && Math.Abs(x) < 3)
                        grid.SetTile(x, y, new Tile(wall));
                    else
                        grid.SetTile(x, y, new Tile(floor));
            }
            finally
            {
                mapManager.SuppressOnTileChanged = false;
            }
        }

        #endregion
    }
}
