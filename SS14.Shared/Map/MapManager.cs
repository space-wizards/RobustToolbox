using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using SS14.Shared.Interfaces.Network;
using SS14.Shared.Network;
using SS14.Shared.Network.Messages;
using SFML.Graphics;
using SFML.System;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Maths;

namespace SS14.Shared.Map
{
    public class MapManager : IMapManager
    {
        private const uint MAP_INDEX = 0;
        private const ushort CHUNK_SIZE = 16;
        private const uint TILE_SIZE = 32;
        private bool _suppressOnTileChanged;

        public event TileChangedEventHandler TileChanged;
        public uint TileSize { get; }
        public static ushort ChunkSize => CHUNK_SIZE;

        private Dictionary<Vector2i, Chunk> Chunks { get; }

        private Dictionary<int, TileCollection> Grids;

        public MapManager()
        {
            TileSize = 32;
            Chunks = new Dictionary<Vector2i, Chunk>();
            Grids = new Dictionary<int, TileCollection>();
            _tileIndexer = new TileCollection(this);
        }

        public void Initialize()
        {
            NewMap();
        }

        #region Tile Enumerators

        // If `ignoreSpace` is false, this will return tiles in chunks that don't even exist.
        // This is to make the tile count predictable.  Is this appropriate behavior?
        public IEnumerable<TileRef> GetTilesIntersecting(FloatRect area, bool ignoreSpace)
        {
            int chunkLeft = (int)Math.Floor(area.Left / ChunkSize);
            int chunkTop = (int)Math.Floor(area.Top / ChunkSize);
            int chunkRight = (int)Math.Floor(area.Right() / ChunkSize);
            int chunkBottom = (int)Math.Floor(area.Bottom() / ChunkSize);
            for (int chunkY = chunkTop; chunkY <= chunkBottom; ++chunkY)
            {
                for (int chunkX = chunkLeft; chunkX <= chunkRight; ++chunkX)
                {
                    int xMin = 0;
                    int yMin = 0;
                    int xMax = 15;
                    int yMax = 15;

                    if (chunkX == chunkLeft)
                        xMin = Mod(Math.Floor(area.Left), ChunkSize);
                    if (chunkY == chunkTop)
                        yMin = Mod(Math.Floor(area.Top), ChunkSize);

                    if (chunkX == chunkRight)
                        xMax = Mod(Math.Floor(area.Right()), ChunkSize);
                    if (chunkY == chunkBottom)
                        yMax = Mod(Math.Floor(area.Bottom()), ChunkSize);

                    Chunk chunk;
                    if (!Chunks.TryGetValue(new Vector2i(chunkX, chunkY), out chunk))
                    {
                        if (ignoreSpace)
                            continue;

                        for (int y = yMin; y <= yMax; ++y)
                            for (int x = xMin; x <= xMax; ++x)
                                yield return new TileRef(this, MAP_INDEX, chunk, (uint) (chunkX * ChunkSize + x), (uint) (chunkY * ChunkSize + y));
                    }
                    else
                    {
                        for (int y = yMin; y <= yMax; ++y)
                        {
                            int i = y * ChunkSize + xMin;
                            for (int x = xMin; x <= xMax; ++x, ++i)
                            {
                                if (!ignoreSpace || chunk.Tiles[x,i].TileId != 0)
                                    yield return new TileRef(this, MAP_INDEX, chunk, (uint) (chunkX * ChunkSize + x), (uint) (chunkY * ChunkSize + y));
                            }
                        }
                    }
                }
            }
        }

        public IEnumerable<TileRef> GetGasTilesIntersecting(FloatRect area)
        {
            return GetTilesIntersecting(area, true).Where(t => t.Tile.TileDef.IsGasVolume);
        }

        public IEnumerable<TileRef> GetWallsIntersecting(FloatRect area)
        {
            return GetTilesIntersecting(area, true).Where(t => t.Tile.TileDef.IsWall);
        }

        // Unlike GetAllTilesIn(...), this skips non-existant chunks.
        // It also does not return chunks in order.
        public IEnumerable<TileRef> GetAllTiles()
        {
            foreach (var pair in Chunks)
            {
                int i = 0;
                for (int y = 0; y < ChunkSize; ++y)
                {
                    for (int x = 0; x < ChunkSize; ++x, ++i)
                    {
                        if (pair.Value.Tiles[x,y].TileId != 0)
                            yield return new TileRef(this, MAP_INDEX, pair.Value, (uint) (pair.Key.X * ChunkSize + x), (uint) (pair.Key.Y * ChunkSize + y));
                    }
                }
            }
        }

        #endregion Tile Enumerators

        #region Indexers

        private readonly TileCollection _tileIndexer;
        public ITileCollection Tiles => _tileIndexer;

        /// <summary>
        /// Returns the tileRef at a given world position.
        /// </summary>
        /// <param name="posWorld">The position of the tile in world space.</param>
        /// <returns>The tileRef at the position.</returns>
        public TileRef GetTileRef(Vector2f posWorld)
        {
            return GetTileRef((int)Math.Floor(posWorld.X), (int)Math.Floor(posWorld.Y));
        }

        public TileRef GetTileRef(int x, int y)
        {
            Vector2i chunkPos = new Vector2i(
                (int)Math.Floor((float)x / ChunkSize),
                (int)Math.Floor((float)y / ChunkSize)
            );
            Chunk chunk;
            if (Chunks.TryGetValue(chunkPos, out chunk))
                return new TileRef(this, MAP_INDEX, chunk, (uint) x, (uint) y);
            else
                return new TileRef(this, MAP_INDEX, chunk, (uint) x, (uint) y);
        }

        public uint GetChunkCount(uint mapIndex)
        {
            throw new NotImplementedException();
        }

        public IMapChunk GetChunk(uint mapIndex, Vector2i posChunk)
        {
            throw new NotImplementedException();
        }

        public IMapChunk GetChunk(uint mapIndex, int xChunk, int yChunk)
        {
            throw new NotImplementedException();
        }

        public Tile GetTile(uint mapIndex, float xWorld, float yWorld)
        {
            throw new NotImplementedException();
        }

        public void SetTile(uint mapIndex, float xWorld, float yWorld, Tile tile)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Returns all chunks inside of a grid.
        /// </summary>
        /// <param name="mapIndex">The index of the grid to access.</param>
        /// <returns>An enumeration of all the chunks.</returns>
        public IEnumerable<IMapChunk> GetMapChunks(uint mapIndex)
        {
            throw new NotImplementedException();
        }
        
        /// <summary>
        /// Transforms world coordinates, local to the grid origin, into chunk indices.
        /// </summary>
        /// <param name="posWorld">The world position to transform.</param>
        /// <returns>Chunk indices of the world position.</returns>
        public Vector2i WorldToChunkIndices(Vector2f posWorld)
        {
            var chunkSpace = posWorld / (CHUNK_SIZE * TILE_SIZE);

            // casting truncates the floats
            return new Vector2i((int) chunkSpace.X, (int) chunkSpace.Y);
        }

        /// <summary>
        /// Transforms a local world position into local tile indices.
        /// </summary>
        /// <param name="posWorld"></param>
        /// <returns></returns>
        public Vector2i WorldToTileIndices(Vector2f posWorld)
        {
            var tileSpace = posWorld / TILE_SIZE;
            
            // casting truncates the floats
            return new Vector2i((int)tileSpace.X, (int)tileSpace.Y);
        }

        /// <summary>
        /// Transform Tile indices to Chunk indices.
        /// </summary>
        /// <param name="posTile"></param>
        /// <returns></returns>
        public Vector2i TileToChunkIndices(Vector2i posTile)
        {
            var chunkSpace = posTile / CHUNK_SIZE;
            
            return new Vector2i(chunkSpace.X, chunkSpace.Y);
        }

        /// <summary>
        /// Transforms a local world position to a local tile position.
        /// </summary>
        /// <param name="posWorld"></param>
        /// <returns></returns>
        public Vector2f WorldToTile(Vector2f posWorld)
        {
            return posWorld / TILE_SIZE;
        }

        private sealed class TileCollection : ITileCollection
        {
            private readonly MapManager mm;
            public Dictionary<Vector2i, Chunk> Chunks { get; }

            internal TileCollection(MapManager mm)
            {
                this.mm = mm;
            }

            public Tile this[Vector2f pos]
            {
                get
                {
                    return this[(int)Math.Floor(pos.X), (int)Math.Floor(pos.Y)];
                }
                set
                {
                    this[(int)Math.Floor(pos.X), (int)Math.Floor(pos.Y)] = value;
                }
            }

            public Tile this[int x, int y]
            {
                get
                {
                    /*
                    Vector2i chunkPos = new Vector2i(
                        (int)Math.Floor((float)x / ChunkSize),
                        (int)Math.Floor((float)y / ChunkSize)
                    );
                    Chunk chunk;
                    if (mm.Chunks.TryGetValue(chunkPos, out chunk))
                        return chunk.Tiles[(y - chunkPos.Y * ChunkSize) * ChunkSize + (x - chunkPos.X * ChunkSize)];
                    else
                        return default(Tile); // SPAAAAAAAAAAAAAACE!!!
                        */
                    return default(Tile);
                }
                set
                {
                    /*
                    Vector2i chunkPos = new Vector2i(
                        (int)Math.Floor((float)x / ChunkSize),
                        (int)Math.Floor((float)y / ChunkSize)
                    );
                    Chunk chunk;
                    if (!mm.Chunks.TryGetValue(chunkPos, out chunk))
                    {
                        if (value.IsSpace)
                            return;
                        else
                            mm.Chunks[chunkPos] = chunk = new Chunk();
                    }

                    int index = (y - chunkPos.Y * ChunkSize) * ChunkSize + (x - chunkPos.X * ChunkSize);
                    Tile oldTile = chunk.Tiles[index];
                    if (oldTile == value)
                        return;

                    chunk.Tiles[index] = value;
                    var tileRef = new TileRef(mm, x, y, chunk, index);

                    mm.RaiseOnTileChanged(tileRef, oldTile);
                    */
                }
            }
        }

        #endregion Networking

        #region File Operations

        public void SaveMap(string mapName)
        {
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
        }

        public bool LoadMap(string mapName)
        {
            _suppressOnTileChanged = true;
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

                    Dictionary<ushort, ITileDefinition> tileMapping = new Dictionary<ushort, ITileDefinition>();
                    int tileDefCount = br.ReadInt32();
                    for (int i = 0; i < tileDefCount; ++i)
                    {
                        ushort tileId = br.ReadUInt16();
                        string tileName = br.ReadString();
                        tileMapping[tileId] = tileDefMgr[tileName];
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
                _suppressOnTileChanged = false;
            }
        }

        private void NewMap()
        {
            _suppressOnTileChanged = true;
            try
            {
                Logger.Log("Cannot find map. Generating blank map.", LogLevel.Warning);
                ushort floor = IoCManager.Resolve<ITileDefinitionManager>()["Floor"].TileId;
                ushort wall = IoCManager.Resolve<ITileDefinitionManager>()["Wall"].TileId;

                Debug.Assert(floor > 0); // This whole method should be removed once tiles become data driven.
                Debug.Assert(wall > 0);

                for (int y = -32; y <= 32; ++y)
                for (int x = -32; x <= 32; ++x)
                    if (Math.Abs(x) == 32 || Math.Abs(y) == 32 || (Math.Abs(x) == 5 && Math.Abs(y) < 5) || (Math.Abs(y) == 7 && Math.Abs(x) < 3))
                        Tiles[x, y] = new Tile(wall);
                    else
                        Tiles[x, y] = new Tile(floor);
            }
            finally
            {
                _suppressOnTileChanged = false;
            }
        }

        #endregion File Operations

        // An actual modulus implementation, because apparently % is not modulus.  Seriously
        // Should probably stick this in some static class.
        [DebuggerStepThrough]
        private static int Mod(double n, uint d)
        {
            return (int)(n - (int)Math.Floor(n / d) * d);
        }
        private void RaiseOnTileChanged(TileRef tileRef, Tile oldTile)
        {
            if(_suppressOnTileChanged)
                return;

            TileChanged?.Invoke(tileRef, oldTile);
        }
    }
}
