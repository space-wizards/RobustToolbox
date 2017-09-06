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
        public const int NULLSPACE = 0;
        private const int GridIndex = 0;
        private const ushort DefaultTileSize = 1;

        /// <inheritdoc />
        public void Initialize()
        {
            NetSetup();
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

        public IMap CreateMap(int mapID)
        {
            var newMap = new Map(this, mapID);
            _Maps.Add(mapID, newMap);
            return newMap;
        }

        public IMap GetMap(int mapID)
        {
            return _Maps[mapID];
        }

        #endregion MapAccess

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

            NewDefaultMap(this, defManager, 1);

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

                var map = mapManager.CreateMap(1);
                var grid = map.GetGrid(gridId) ?? map.CreateGrid(gridId);

                for (var y = -32; y <= 32; ++y)
                for (var x = -32; x <= 32; ++x)
                    if (Math.Abs(x) == 32 || Math.Abs(y) == 32 || Math.Abs(x) == 5 && Math.Abs(y) < 5 || Math.Abs(y) == 7 && Math.Abs(x) < 3)
                        grid.SetTile(new LocalCoordinates(x,y,grid), new Tile(wall)); //TODO: Fix this
                    else
                        grid.SetTile(new LocalCoordinates(x,y,grid), new Tile(floor)); //TODO: Fix this
            }
            finally
            {
                mapManager.SuppressOnTileChanged = false;
            }
        }

        #endregion
    }
}