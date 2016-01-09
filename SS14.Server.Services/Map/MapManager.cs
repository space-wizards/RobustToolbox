using Lidgren.Network;
using SFML.Graphics;
using SFML.System;
using SS14.Server.Interfaces.Map;
using SS14.Server.Interfaces.Network;
using SS14.Server.Services.Log;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.ServerEnums;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace SS14.Server.Services.Map
{
    public class MapManager : IMapManager
    {
        private Dictionary<Vector2i, Chunk> chunks = new Dictionary<Vector2i, Chunk>();
        private static readonly int ChunkSize = Chunk.ChunkSize;

        public event TileChangedEventHandler TileChanged;
        private bool suppressNetworkUpdatesOnTileChanged = false;

        public MapManager()
        {
            tileIndexer = new TileCollection(this);
            NewMap();
        }

        public int TileSize
        {
            get { return 32; }
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
                    if (!chunks.TryGetValue(new Vector2i(chunkX, chunkY), out chunk))
                    {
                        if (ignoreSpace)
                            continue;
                        else
                            for (int y = yMin; y <= yMax; ++y)
                                for (int x = xMin; x <= xMax; ++x)
                                    yield return new TileRef(this,
                                        chunkX * ChunkSize + x,
                                        chunkY * ChunkSize + y);
                    }
                    else
                    {
                        for (int y = yMin; y <= yMax; ++y)
                        {
                            int i = y * ChunkSize + xMin;
                            for (int x = xMin; x <= xMax; ++x, ++i)
                            {
                                if (!ignoreSpace || chunk.Tiles[i].TileId != 0)
                                    yield return new TileRef(this,
                                        chunkX * ChunkSize + x,
                                        chunkY * ChunkSize + y,
                                        chunk, i);
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
            foreach (var pair in chunks)
            {
                int i = 0;
                for (int y = 0; y < ChunkSize; ++y)
                {
                    for (int x = 0; x < ChunkSize; ++x, ++i)
                    {
                        if (pair.Value.Tiles[i].TileId != 0)
                            yield return new TileRef(this,
                                pair.Key.X * ChunkSize + x,
                                pair.Key.Y * ChunkSize + y,
                                pair.Value, i);
                    }
                }
            }
        }

        #endregion

        #region Indexers

        public TileRef GetTileRef(Vector2f pos)
        {
            return GetTileRef((int)Math.Floor(pos.X), (int)Math.Floor(pos.Y));
        }
        public TileRef GetTileRef(int x, int y)
        {
            Vector2i chunkPos = new Vector2i(
                (int)Math.Floor((float)x / ChunkSize),
                (int)Math.Floor((float)y / ChunkSize)
            );
            Chunk chunk;
            if (chunks.TryGetValue(chunkPos, out chunk))
                return new TileRef(this, x, y, chunk,
                    (y - chunkPos.Y * ChunkSize) * ChunkSize + (x - chunkPos.X * ChunkSize));
            else
                return new TileRef(this, x, y);
        }

        private TileCollection tileIndexer;
        public ITileCollection Tiles { get { return tileIndexer; } }

        public sealed class TileCollection : ITileCollection
        {
            private readonly MapManager mm;

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
                    Vector2i chunkPos = new Vector2i(
                        (int)Math.Floor((float)x / ChunkSize),
                        (int)Math.Floor((float)y / ChunkSize)
                    );
                    Chunk chunk;
                    if (mm.chunks.TryGetValue(chunkPos, out chunk))
                        return chunk.Tiles[(y - chunkPos.Y * ChunkSize) * ChunkSize + (x - chunkPos.X * ChunkSize)];
                    else
                        return default(Tile); // SPAAAAAAAAAAAAAACE!!!
                }
                set
                {
                    Vector2i chunkPos = new Vector2i(
                        (int)Math.Floor((float)x / ChunkSize),
                        (int)Math.Floor((float)y / ChunkSize)
                    );
                    Chunk chunk;
                    if (!mm.chunks.TryGetValue(chunkPos, out chunk))
                    {
                        if (value.IsSpace)
                            return;
                        else
                            mm.chunks[chunkPos] = chunk = new Chunk();
                    }

                    int index = (y - chunkPos.Y * ChunkSize) * ChunkSize + (x - chunkPos.X * ChunkSize);
                    Tile oldTile = chunk.Tiles[index];
                    if (oldTile == value)
                        return;

                    chunk.Tiles[index] = value;
                    var tileRef = new TileRef(mm, x, y, chunk, index);

                    if (mm.TileChanged != null)
                        mm.TileChanged(tileRef, oldTile);

                    if (!mm.suppressNetworkUpdatesOnTileChanged)
                        mm.NetworkUpdateTile(tileRef);
                }
            }
        }

        #endregion

        #region Networking

        public NetOutgoingMessage CreateMapMessage(MapMessage messageType)
        {
            NetOutgoingMessage message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            message.Write((byte)NetMessage.MapMessage);
            message.Write((byte)messageType);
            return message;
        }

        public void SendMap(NetConnection connection)
        {
            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map");
            NetOutgoingMessage mapMessage = CreateMapMessage(MapMessage.SendTileMap);

            mapMessage.Write((int)1); // Format version.  Who knows, it could come in handy.

            // Tile definition mapping
            var tileDefManager = IoCManager.Resolve<ITileDefinitionManager>();
            mapMessage.Write((int)tileDefManager.Count);
            for (int tileId = 0; tileId < tileDefManager.Count; ++tileId)
                mapMessage.Write((string)tileDefManager[tileId].Name);

            // Map chunks
            mapMessage.Write((int)chunks.Count);
            foreach (var chunk in chunks)
            {
                mapMessage.Write((int)chunk.Key.X);
                mapMessage.Write((int)chunk.Key.Y);

                foreach (var tile in chunk.Value.Tiles)
                    mapMessage.Write((uint)tile);
            }

            IoCManager.Resolve<ISS14NetServer>().SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map finished with message size: " +
                           mapMessage.LengthBytes + " bytes");
        }

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (MapMessage)message.ReadByte();
            switch (messageType)
            {
                case MapMessage.TurfClick:
                    //HandleTurfClick(message);
                    break;
                default:
                    break;
            }
        }

        /*
        private void HandleTurfClick(NetIncomingMessage message)
        {
            // Who clicked and on what tile.
            Atom.Atom clicker = SS13Server.Singleton.playerManager.GetSessionByConnection(message.SenderConnection).attachedAtom;
            short x = message.ReadInt16();
            short y = message.ReadInt16();

            if (Vector2.Distance(clicker.position, new Vector2(x * tileSpacing + (tileSpacing / 2), y * tileSpacing + (tileSpacing / 2))) > 96)
            {
                return; // They were too far away to click us!
            }
            bool Update = false;
            if (IsSaneArrayPosition(x, y))
            {
                Update = tileArray[x, y].ClickedBy(clicker);
                if (Update)
                {
                    if (tileArray[x, y].tileState == TileState.Dead)
                    {
                        Tiles.Atmos.GasCell g = tileArray[x, y].gasCell;
                        Tiles.Tile t = GenerateNewTile(x, y, tileArray[x, y].tileType);
                        tileArray[x, y] = t;
                        tileArray[x, y].gasCell = g;
                    }
                    NetworkUpdateTile(x, y);
                }
            }
        }*/ // TODO HOOK ME BACK UP WITH ENTITY SYSTEM

        public void NetworkUpdateTile(TileRef tile)
        {
            NetOutgoingMessage message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            message.Write((byte)NetMessage.MapMessage);
            message.Write((byte)MapMessage.TurfUpdate);

            message.Write((int)tile.X);
            message.Write((int)tile.Y);
            message.Write((uint)tile.Tile);
            IoCManager.Resolve<ISS14NetServer>().SendToAll(message);
        }

        #endregion

        #region File Operations

        public void SaveMap(string mapName)
        {
            var badChars = Path.GetInvalidFileNameChars();
            if (mapName.Any(c => badChars.Contains(c)))
                throw new ArgumentException("Invalid characters in map name.", "mapName");

            string fileName = Path.GetFullPath(Path.Combine(System.Reflection.Assembly.GetEntryAssembly().Location, @"..\Maps", mapName));

            using (var fs = new FileStream(fileName, FileMode.Create))
            {
                var sw = new StreamWriter(fs);
                var bw = new BinaryWriter(fs);
                LogManager.Log(string.Format("Saving map: \"{0}\" {1:N} Chunks", mapName, chunks.Count));

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
                bw.Write((int)chunks.Count);
                foreach (var chunk in chunks)
                {
                    bw.Write((int)chunk.Key.X);
                    bw.Write((int)chunk.Key.Y);

                    foreach (var tile in chunk.Value.Tiles)
                        bw.Write((uint)tile);
                }

                bw.Write("End of Map");
                bw.Flush();
            }

            LogManager.Log("Done saving map.");
        }

        public bool LoadMap(string mapName)
        {
            suppressNetworkUpdatesOnTileChanged = true;
            try
            {
                var badChars = Path.GetInvalidFileNameChars();
                if (mapName.Any(c => badChars.Contains(c)))
                    throw new ArgumentException("Invalid characters in map name.", "mapName");

                string fileName = Path.GetFullPath(Path.Combine(System.Reflection.Assembly.GetEntryAssembly().Location, @"..\Maps", mapName));

                if (!File.Exists(fileName))
                    return false;

                using (var fs = new FileStream(fileName, FileMode.Open))
                {
                    var sr = new StreamReader(fs);
                    var br = new BinaryReader(fs);
                    LogManager.Log(string.Format("Loading map: \"{0}\"", mapName));

                    var versionString = sr.ReadLine();
                    if (!versionString.StartsWith("SS14 Map File Version "))
                        return false;

                    fs.Seek(versionString.Length + 2, SeekOrigin.Begin);

                    int formatVersion;
                    if (!int.TryParse(versionString.Substring(22), out formatVersion))
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
                                this.Tiles[x, y] = tile;
                            }
                    }

                    string ending = br.ReadString();
                    Debug.Assert(ending == "End of Map");
                }

                LogManager.Log("Done loading map.");
                return true;
            }
            finally
            {
                suppressNetworkUpdatesOnTileChanged = false;
            }
        }

        private void NewMap()
        {
            suppressNetworkUpdatesOnTileChanged = true;
            try
            {
                LogManager.Log("Cannot find map. Generating blank map.", LogLevel.Warning);
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
                suppressNetworkUpdatesOnTileChanged = false;
            }
        }

        #endregion

        // An actual modulus implementation, because apparently % is not modulus.  Srsly
        // Should probably stick this in some static class.
        [System.Diagnostics.DebuggerStepThrough]
        private static int Mod(double n, int d)
        {
            return (int)(n - (int)Math.Floor(n / d) * d);
        }

    }
}