using BKSystem.IO;
using SS14.Client.Graphics.CluwneLib;
using SS14.Shared.Maths;
using Lidgren.Network;
using SS14.Client.Interfaces.Collision;
using SS14.Client.Interfaces.Lighting;
using SS14.Client.Interfaces.Map;
using SS14.Client.Interfaces.Resource;
using SS14.Client.Interfaces.State;
using SS14.Client.Services.State.States;
using SS14.Client.Services.Tiles;
using SS14.Shared;
using SS14.Shared.IoC;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Linq;

namespace SS14.Client.Services.Map
{
    public class MapManager : IMapManager
    {
        #region Variables

        private const int TileSpacing = 64; // Distance between tiles
        private const int wallThickness = 24;
        private readonly List<Vector2> _cardinalList;

        private readonly ICollisionManager _collisionManager;
        private readonly ILightManager _lightManager;
        private readonly IResourceManager _resourceManager;
        private readonly Dictionary<byte, string> tileStringTable = new Dictionary<byte, string>();
        private bool _initialized;
        private bool _loaded;
        private int _mapHeight; // Number of tiles up the map
        private int _mapWidth; // Number of tiles across the map
        private QuadTree<Tile> _groundArray;
        private QuadTree<Tile> _wallArray;

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

            _cardinalList = new List<Vector2>
                                {
                                    new Vector2(0, 0),
                                    new Vector2(0, 1),
                                    new Vector2(0, -1),
                                    new Vector2(1, 0),
                                    new Vector2(-1, 0),
                                    new Vector2(1, 1),
                                    new Vector2(-1, -1),
                                    new Vector2(-1, 1),
                                    new Vector2(1, -1)
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


            _groundArray = new QuadTree<Tile>(new SizeF(2*TileSpacing, 2*TileSpacing), 4);
            _wallArray = new QuadTree<Tile>(new SizeF(2 * TileSpacing, 2 * TileSpacing), 4);

            while (message.PositionInBytes < message.LengthBytes)
            {
                float posX = message.ReadFloat();
                float posY = message.ReadFloat();
                byte index = message.ReadByte();
                var state = (TileState)message.ReadByte();
                string name = GetTileString(index);
                Direction dir = Direction.North;
                if (name == "Wall")
                {
                    dir = (Direction)message.ReadByte();
                }
                Tile newTile = GenerateNewTile(GetTileString(index), state, new Vector2(posX, posY), dir);
                AddTile(newTile);
            }

            foreach (Wall w in GetAllWallIn(new RectangleF(0, 0, _mapWidth * TileSpacing, _mapHeight * TileSpacing)))
            {
                w.SetSprite();
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
            if (lengthBytes == 0)
            {
                return;
            }
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
                            Tile t = (Tile)GetFloorAt(new Vector2(x * TileSpacing, y * TileSpacing));
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

            Tile t = (Tile)GetAllTilesAt(new Vector2(x, y)).FirstOrDefault();
            t.AddDecal(type);
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
            Direction dir = Direction.North;
            if (tileStr == "Wall") dir = (Direction)message.ReadByte();

            Tile t = (Tile)GetTypeAt(tileStr, new Vector2(x, y));
            if (t != null && t._dir == dir)
            {
                RemoveTile(t);
                if (t.GetType().GetInterface("ICollidable") != null)
                    _collisionManager.RemoveCollidable((ICollidable)t);
            }
            t = GenerateNewTile(tileStr, state, new Vector2(x, y), dir);
            AddTile(t);
            t.SetSprite();
            TileChanged(t);
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

        public Tile GetFloorN(float x, float y)
        {
            return (Tile)GetFloorAt(new Vector2(x, y - TileSpacing));
        }

        public Tile GetFloorE(float x, float y)
        {
            return (Tile)GetFloorAt(new Vector2(x + TileSpacing, y));
        }

        public Tile GetFloorS(float x, float y)
        {
            return (Tile)GetFloorAt(new Vector2(x, y + TileSpacing));
        }

        public Tile GetFloorW(float x, float y)
        {
            return (Tile)GetFloorAt(new Vector2(x - TileSpacing, y));
        }

        public void AddTile(Tile t)
        {
            if (t.GetType().Name == "Wall")
            {
                _wallArray.Insert(t);
                SetSpritesAround(t);
            }
            else
            {
                _groundArray.Insert(t);
            }
        }

        private void RemoveTile(Tile t)
        {
            if (t.GetType().Name == "Wall")
            {
                _wallArray.Remove(t);
                SetSpritesAround(t);
            }
            else
            {
                _groundArray.Remove(t);
            }
        }

        private Rectangle TilePos(Tile T)
        {
            return new Rectangle((int)(T.Position.X), (int)(T.Position.Y), TileSpacing, TileSpacing);
        }

        public ITile[] GetAllTilesIn(RectangleF area)
        {
            List<Tile> tiles = _groundArray.Query(area);
            tiles.AddRange(_wallArray.Query(area));
            return tiles.ToArray();
        }

        public ITile[] GetAllFloorIn(RectangleF Area)
        {
            return _groundArray.Query(Area).ToArray();
        }

        public ITile[] GetAllWallIn(RectangleF Area)
        {
            return _wallArray.Query(Area).ToArray();
        }

        public ITile GetWallAt(Vector2 pos)
        {
            return GetAllWallIn(new RectangleF(pos.X, pos.Y, 2f, 2f)).FirstOrDefault();
        }

        public ITile GetFloorAt(Vector2 pos)
        {
            return GetAllFloorIn(new RectangleF(pos.X, pos.Y, 2f, 2f)).FirstOrDefault();
        }

        public ITile[] GetAllTilesAt(Vector2 pos)
        {
            return GetAllTilesIn(new RectangleF(pos.X, pos.Y, 2f, 2f));
        }

        public ITile GetTypeAt(Type type, Vector2 pos)
        {
            ITile[] tiles = GetAllTilesAt(pos);
            return tiles.FirstOrDefault(x => x.GetType() == type);
        }

        public ITile GetTypeAt(string type, Vector2 pos)
        {
            return GetTypeAt(Type.GetType("SS14.Client.Services.Tiles." + type, false), pos);
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

        public int GetTileSpacing()
        {
            return TileSpacing;
        }

        public int GetWallThickness()
        {
            return wallThickness;
        }

        public int GetMapWidth()
        {
            return _mapWidth;
        }

        public int GetMapHeight()
        {
            return _mapHeight;
        }

        public Tile GenerateNewTile(string typeName, TileState state, Vector2 pos, Direction dir = Direction.North)
        {
            Type tileType = Type.GetType("SS14.Client.Services.Tiles." + typeName, false);

            if (tileType == null) throw new ArgumentException("Invalid Tile Type specified : '" + typeName + "' .");
            RectangleF rect = new RectangleF();
            Tile created;
            if (typeName != "Wall")
            {
                rect = new RectangleF(pos.X, pos.Y, TileSpacing, TileSpacing);
            }
            else
            {
                if (dir == Direction.North)
                {
                    rect = new RectangleF(pos.X, pos.Y, wallThickness, TileSpacing);
                }
                else
                {
                    rect = new RectangleF(pos.X, pos.Y, TileSpacing, wallThickness);
                }
            }

            if (typeName == "Wall")
            {
                created = (Tile)Activator.CreateInstance(tileType, state, rect, dir);
            }
            else
            {
                created = (Tile)Activator.CreateInstance(tileType, state, rect);
            }

            created.Initialize();

            if (tileType.GetInterface("ICollidable") != null)
                _collisionManager.AddCollidable((ICollidable) created);

            return created;
        }

        #endregion

        #region Quick collision checks

        public bool IsSolidTile(Vector2 worldPos)
        {
            var tile = (Tile) GetWallAt(worldPos);
            if (tile == null) return false;
            return true;
        }

        #endregion

        #region Shutdown

        public void Shutdown()
        {
            _groundArray = null;
            _initialized = false;
            _loaded = false;
        }

        #endregion

        #region Event Handling

        private void TileChanged(Tile t)
        {
            if (OnTileChanged != null)
                OnTileChanged(t.Position);
        }

        private void SetSpritesAround(Tile t)
        {
            Tile[] tiles = (Tile[])GetAllWallIn(new RectangleF(t.Position.X - (TileSpacing / 2f), t.Position.Y - (TileSpacing / 2f), TileSpacing * 2, TileSpacing * 2));

            foreach (Tile u in tiles)
            {
                if(u != t)
                    u.SetSprite();
            }
        }

        

        #endregion
    }
}