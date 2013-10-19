using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;
using BKSystem.IO;
using ClientInterfaces.Collision;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using ClientInterfaces.Resource;
using ClientInterfaces.State;
using ClientServices.State.States;
using ClientServices.Tiles;
using GorgonLibrary;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;

namespace ClientServices.Map
{
    public class MapManager : IMapManager
    {
        #region Variables

        private const int TileSpacing = 64; // Distance between tiles
        private readonly List<Vector2D> _cardinalList;

        private readonly ICollisionManager _collisionManager;
        private readonly ILightManager _lightManager;
        private readonly IResourceManager _resourceManager;
        private readonly Dictionary<byte, string> tileStringTable = new Dictionary<byte, string>();
        private bool _initialized;
        private bool _loaded;
        private int _mapHeight; // Number of tiles up the map
        private int _mapWidth; // Number of tiles across the map
        private RectangleTree<Tile> _tileArray;

        #endregion

        #region Events

        public event TileChangeEvent OnTileChanged;

        #endregion

        public MapManager(IResourceManager resourceManager, ILightManager lightManager,
                          ICollisionManager collisionManager)
        {
            _resourceManager = resourceManager;
            _lightManager = lightManager;
            _collisionManager = collisionManager;
            _mapHeight = 256;
            _mapWidth = 256;
            Init();

            _cardinalList = new List<Vector2D>
                                {
                                    new Vector2D(0, 0),
                                    new Vector2D(0, 1),
                                    new Vector2D(0, -1),
                                    new Vector2D(1, 0),
                                    new Vector2D(-1, 0),
                                    new Vector2D(1, 1),
                                    new Vector2D(-1, -1),
                                    new Vector2D(-1, 1),
                                    new Vector2D(1, -1)
                                };
        }

        #region Startup / Loading

        public void Init()
        {
            if (!_initialized)
            {
            }
            _initialized = true;
        }

        public bool LoadTileMap(NetIncomingMessage message)
        {
            int _mapWidth = message.ReadInt32();
            int _mapHeight = message.ReadInt32();


            _tileArray = new RectangleTree<Tile>(TilePos,
                                                 new Rectangle(0, 0,
                                                                 _mapWidth*TileSpacing, _mapHeight*TileSpacing));

            while (message.PositionInBytes < message.LengthBytes)
            {
                float posX = message.ReadFloat();
                float posY = message.ReadFloat();
                byte index = message.ReadByte();
                var state = (TileState)message.ReadByte();

                Tile newTile = GenerateNewTile(GetTileString(index), state, new Vector2D(posX, posY));
                _tileArray.Add(newTile);
            }

            foreach (Tile t in _tileArray.GetItems(new Rectangle(0, 0, _mapWidth * TileSpacing, _mapHeight * TileSpacing)))
            {
                t.surroundingTiles[0] = GetTileN(t.Position.X, t.Position.Y);
                t.surroundingTiles[1] = GetTileE(t.Position.X, t.Position.Y);
                t.surroundingTiles[2] = GetTileS(t.Position.X, t.Position.Y);
                t.surroundingTiles[3] = GetTileW(t.Position.X, t.Position.Y);
            }

            _loaded = true;
            return true;
        }

        #endregion

        #region Networking

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (MapMessage) message.ReadByte();
            switch (messageType)
            {
                case MapMessage.TurfUpdate:
                    HandleTurfUpdate(message);
                    break;
                case MapMessage.TurfAddDecal:
                    HandleTurfAddDecal(message);
                    break;
                case MapMessage.TurfRemoveDecal:
                    HandleTurfRemoveDecal(message);
                    break;
                case MapMessage.SendTileIndex:
                    HandleIndexUpdate(message);
                    break;
                case MapMessage.SendTileMap:
                    LoadTileMap(message);
                    break;
            }
        }

        public void HandleAtmosDisplayUpdate(NetIncomingMessage message)
        {
            if (!_loaded)
                return;
            //Length of records in bits
            int lengthBits = message.ReadInt32();
            int lengthBytes = message.ReadInt32();
            var records = new byte[lengthBytes];
            message.ReadBytes(records, 0, lengthBytes);
            byte[] decompressed = Decompress(records);
            var recordStream = new BitStream(lengthBits);
            int bitsWritten = 0;
            for (int i = 0; i < decompressed.Length; i++)
            {
                int toWrite = 8;
                if (toWrite > lengthBits - bitsWritten)
                    toWrite = lengthBits - bitsWritten;
                recordStream.Write(decompressed[i], 0, toWrite);
                bitsWritten += toWrite;
            }

            int typesCount = Enum.GetValues(typeof (GasType)).Length;
            recordStream.Position = 0;
            int types = 0;
            byte amount = 0;
            for (int x = 0; x < _mapWidth; x++)
            {
                for (int y = 0; y < _mapHeight; y++)
                {
                    recordStream.Read(out types, 0, typesCount);

                    for (int i = typesCount - 1; i >= 0; i--)
                    {
                        if ((types & (1 << i)) == (1 << i))
                        {
                            recordStream.Read(out amount, 0, 4);
                            Tile t = GetTileAt(x * TileSpacing, y * TileSpacing);
                            if (t == null)
                                continue;
                            t.SetAtmosDisplay((GasType) i, amount);
                        }
                    }
                }
            }

            var gameScreen = IoCManager.Resolve<IStateManager>().CurrentState as GameScreen;
            gameScreen.RecalculateScene();
        }

        private void HandleIndexUpdate(NetIncomingMessage message)
        {
            tileStringTable.Clear();

            byte indexCount = message.ReadByte();

            for (int i = 0; i < indexCount; i++)
            {
                byte currIndex = message.ReadByte();
                string currStr = message.ReadString();
                tileStringTable.Add(currIndex, currStr);
            }
        }

        private void HandleTurfRemoveDecal(NetIncomingMessage message)
        {
            throw new NotImplementedException();
        }

        private void HandleTurfAddDecal(NetIncomingMessage message)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            var type = (DecalType) message.ReadByte();

            GetTileAt(x, y).AddDecal(type);
        }

        private static byte[] Decompress(byte[] gzip)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (var stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                var buffer = new byte[size];
                using (var memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    } while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        private void HandleTurfUpdate(NetIncomingMessage message)
        {
            float x = message.ReadFloat();
            float y = message.ReadFloat();
            string tileStr = GetTileString(message.ReadByte());
            var state = (TileState) message.ReadByte();

            Tile t = GetTileAt(x, y);

            if (t == null)
            {
                t = GenerateNewTile(tileStr, state, new Vector2D(x, y));
                _tileArray.Add(t);
                TileChanged(t);
            }
            else
            {
                if (t.GetType().Name != tileStr) //This is ugly beep boop. Fix this later.
                {
                    Tile[] surroundTiles = t.surroundingTiles;

                    if (t.GetType().GetInterface("ICollidable") != null)
                        _collisionManager.RemoveCollidable((ICollidable) t);
                    _tileArray.Remove(t);
                    t = GenerateNewTile(tileStr, state, new Vector2D(x*TileSpacing, y*TileSpacing));
                    t.surroundingTiles = surroundTiles;
                    if (t.surroundingTiles[0] != null) t.surroundingTiles[0].surroundingTiles[2] = t;
                    if (t.surroundingTiles[1] != null) t.surroundingTiles[1].surroundingTiles[3] = t;
                    if (t.surroundingTiles[2] != null) t.surroundingTiles[2].surroundingTiles[0] = t;
                    if (t.surroundingTiles[3] != null) t.surroundingTiles[3].surroundingTiles[1] = t;

                    _tileArray.Add(t);
                    TileChanged(t);
                }
                else if (t.tileState != state)
                {
                    t.tileState = state;
                    TileChanged(t);
                }
            }
        }

        private struct AtmosRecord
        {
            public readonly byte Display;
            public readonly int X;
            public readonly int Y;

            public AtmosRecord(int x, int y, byte display)
            {
                X = x;
                Y = y;
                Display = display;
            }
        }

        #endregion

        #region Tile helper functions

        public Tile GetTileN(float x, float y)
        {
            return GetTileAt(x, y - TileSpacing);
        }

        public Tile GetTileE(float x, float y)
        {
            return GetTileAt(x + TileSpacing, y);
        }
        public Tile GetTileS(float x, float y)
        {
            return GetTileAt(x, y + TileSpacing);
        }
        public Tile GetTileW(float x, float y)
        {
            return GetTileAt(x - TileSpacing, y);
        }

        private Rectangle TilePos(Tile T)
        {
            return new Rectangle((int)(T.Position.X), (int)(T.Position.Y), (int)(TileSpacing), (int)(TileSpacing));
        }

        public ITile[] GetITilesIn(RectangleF area)
        {
            return _tileArray.GetItems(new Rectangle((int)area.X, (int)area.Y, (int)area.Width, (int)area.Height));
        }

        private ITile GetITileAt(Point p)
        {
            return (Tile)_tileArray.GetItems(p).FirstOrDefault();
        }

        private Tile GetTileAt(Point p)
        {
            return (Tile)_tileArray.GetItems(p).FirstOrDefault();
        }

        private Tile GetTileAt(float x, float y)
        {
            return (Tile)_tileArray.GetItems(new Point((int)x, (int)y)).FirstOrDefault();
        }

        public ITile GetITileAt(Vector2D worldPos)
        {
            return GetTileAt((int)worldPos.X, (int)worldPos.Y);
        }

        public byte GetTileIndex(string typeName)
        {
            if (tileStringTable.Values.Any(x => x.ToLowerInvariant() == typeName.ToLowerInvariant()))
                return tileStringTable.First(x => x.Value.ToLowerInvariant() == typeName.ToLowerInvariant()).Key;
            else throw new ArgumentNullException("tileStringTable", "Can not find '" + typeName + "' type.");
        }

        public string GetTileString(byte index)
        {
            string typeStr = (from a in tileStringTable
                              where a.Key == index
                              select a.Value).First();

            return typeStr;
        }

        // Where do we have tiles around us?
        // 0 = None
        // 1 = North
        // 2 = East
        // 4 = South
        // 8 = West
        // So if we have one N and S, we return (N + S) or (1 + 4), so 5.
        public byte SetSprite(Vector2D position)
        {
            byte i = 0;

            if (GetTileAt(position.X, position.Y - TileSpacing) != null && GetTileAt(position.X, position.Y - TileSpacing).ConnectSprite) // N
            {
                i += 1;
            }
            if (GetTileAt(position.X + TileSpacing, position.Y) != null && GetTileAt(position.X + TileSpacing, position.Y).ConnectSprite) // E
            {
                i += 2;
            }
            if (GetTileAt(position.X, position.Y + TileSpacing) != null && GetTileAt(position.X, position.Y + TileSpacing).ConnectSprite) // S
            {
                i += 4;
            }
            if (GetTileAt(position.X - TileSpacing, position.Y) != null && GetTileAt(position.X - TileSpacing, position.Y).ConnectSprite) // W
            {
                i += 8;
            }

            return i;
        }

        public int GetTileSpacing()
        {
            return TileSpacing;
        }

        public int GetMapWidth()
        {
            return _mapWidth;
        }

        public int GetMapHeight()
        {
            return _mapHeight;
        }

        public Tile GenerateNewTile(string typeName, TileState state, Vector2D pos)
        {

            Type tileType = Type.GetType("ClientServices.Tiles." + typeName, false);

            if (tileType == null) throw new ArgumentException("Invalid Tile Type specified : '" + typeName + "' .");

            var created = (Tile) Activator.CreateInstance(tileType, state, pos);

            if (tileType.GetInterface("ICollidable") != null)
                _collisionManager.AddCollidable((ICollidable) created);

            return created;
        }

        #endregion

        #region Quick collision checks

        public bool IsSolidTile(Vector2D worldPos)
        {
            var tile = (Tile) GetITileAt(worldPos);
            if (tile == null) return false;
            return tile.IsSolidTile();
        }

        #endregion

        #region Shutdown

        public void Shutdown()
        {
            _tileArray = null;
            _initialized = false;
            _loaded = false;
        }

        #endregion

        #region Event Handling

        private void TileChanged(Tile t)
        {
            if (OnTileChanged != null)
                OnTileChanged(t.Position);

            t.surroundDirs = SetSprite(t.Position);
            foreach (Tile T in t.surroundingTiles)
            {
                if (T == null) continue;
                T.surroundDirs = SetSprite(T.Position);
            }
        }

        #endregion
    }
}