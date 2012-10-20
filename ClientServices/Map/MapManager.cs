using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO.Compression;
using ClientInterfaces.Collision;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using ClientInterfaces.Resource;
using ClientInterfaces.State;
using ClientServices.State.States;
using ClientServices.Tiles;
using SS13.IoC;
using SS13_Shared;
using System.IO;
using Lidgren.Network;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientInterfaces;
using System.Linq;
using System.Reflection;

namespace ClientServices.Map
{
    public class MapManager : IMapManager
    {
        #region Variables
        private Tile[][] _tileArray; // The array holding all the tiles that make up the map
        private int _mapWidth; // Number of tiles across the map
        private int _mapHeight; // Number of tiles up the map
        private const int TileSpacing = 64; // Distance between tiles

        private readonly List<Vector2D> _cardinalList;

        private bool _loaded;
        private bool _initialized;

        Dictionary<byte, string> tileStringTable = new Dictionary<byte, string>();

        private readonly IResourceManager _resourceManager;
        private readonly ILightManager _lightManager;
        private readonly ICollisionManager _collisionManager;
        #endregion

        #region Events
        public event TileChangeEvent OnTileChanged;
        #endregion

        public MapManager(IResourceManager resourceManager, ILightManager lightManager, ICollisionManager collisionManager)
        {
            _resourceManager = resourceManager;
            _lightManager = lightManager;
            _collisionManager = collisionManager;

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

        public bool LoadTileMap(NetIncomingMessage message)
        {
            _mapWidth = message.ReadInt32();
            _mapHeight = message.ReadInt32();

            _tileArray = new Tile[_mapHeight][];

            for (int i = 0; i < _mapHeight; i++)
            {
                _tileArray[i] = new Tile[_mapWidth];
            }

            for (int x = 0; x < _mapWidth; x++)
            {
                for (int y = 0; y < _mapHeight; y++)
                {
                    var posX = x * TileSpacing;
                    var posY = y * TileSpacing;

                    byte index = message.ReadByte();
                    TileState state = (TileState)message.ReadByte();

                    Tile created = GenerateNewTile(GetTileString(index), state, new Vector2D(posX, posY));
                    _tileArray[y][x] = created;
                }
            }

            for (var x = 0; x < _mapWidth; x++)
            {
                for (var y = 0; y < _mapHeight; y++)
                {
                    if (_tileArray[y][x].ConnectSprite) //Was wall check.
                    {
                        var i = SetSprite(x, y);
                    }
                    if (y > 0)
                    {
                        _tileArray[y][x].surroundingTiles[0] = _tileArray[y - 1][x]; //north
                    }
                    if (x < _mapWidth - 1)
                    {
                        _tileArray[y][x].surroundingTiles[1] = _tileArray[y][x + 1]; //east
                    }
                    if (y < _mapHeight - 1)
                    {
                        _tileArray[y][x].surroundingTiles[2] = _tileArray[y + 1][x]; //south
                    }
                    if (x > 0)
                    {
                        _tileArray[y][x].surroundingTiles[3] = _tileArray[y][x - 1]; //west
                    }

                }
            }

            _loaded = true;
            return true;
        }
        #endregion

        #region Networking
        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (MapMessage)message.ReadByte();
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
            int x = message.ReadInt32();
            int y = message.ReadInt32();
            var type = (DecalType)message.ReadByte();

            _tileArray[y][x].AddDecal(type);
        }

        static byte[] Decompress(byte[] gzip)
        {
            // Create a GZIP stream with decompression mode.
            // ... Then create a buffer and write into while reading from the GZIP stream.
            using (GZipStream stream = new GZipStream(new MemoryStream(gzip), CompressionMode.Decompress))
            {
                const int size = 4096;
                byte[] buffer = new byte[size];
                using (MemoryStream memory = new MemoryStream())
                {
                    int count = 0;
                    do
                    {
                        count = stream.Read(buffer, 0, size);
                        if (count > 0)
                        {
                            memory.Write(buffer, 0, count);
                        }
                    }
                    while (count > 0);
                    return memory.ToArray();
                }
            }
        }

        public void HandleAtmosDisplayUpdate(NetIncomingMessage message)
        {
            if (!_loaded)
                return;
            /*var count = message.ReadInt32();
            var records = new List<AtmosRecord>();
            for (var i = 1; i <= count; i++)
            {
                records.Add(new AtmosRecord(message.ReadInt32(), message.ReadInt32(), message.ReadByte()));
            }

            foreach (var record in records)
            {
                _tileArray[record.X][record.Y].SetAtmosDisplay(record.Display);
            }*/
            var length = message.ReadInt32();
            var records = new byte[length];
            message.ReadBytes(records, 0, length);
            var decompressed = Decompress(records);

            var r = 0;
            for(var x = 0;x < _mapWidth;x++)
            {
                for(var y = 0;y<_mapHeight;y++)
                {
                    _tileArray[y][x].SetAtmosDisplay(decompressed[r]);
                    r++;
                    _tileArray[y][x].SetAtmosDisplay(decompressed[r]);
                    r++;
                    _tileArray[y][x].SetAtmosDisplay(decompressed[r]);
                    r++;
                }
            }

            var gameScreen = IoCManager.Resolve<IStateManager>().CurrentState as GameScreen;
            gameScreen.RecalculateScene();

        }

        private struct AtmosRecord
        {
            public readonly int X;
            public readonly int Y;
            public readonly byte Display;

            public AtmosRecord(int x, int y, byte display)
            {
                X = x;
                Y = y;
                Display = display;
            }
        }

        private void HandleTurfUpdate(NetIncomingMessage message)
        {
            var x = message.ReadInt16();
            var y = message.ReadInt16();
            var tileStr = GetTileString(message.ReadByte());
            var state = (TileState)message.ReadByte();

            var t = _tileArray[y][x];

            if (t == null)
            {
                t = GenerateNewTile(tileStr, state, new Vector2D(x * TileSpacing, y * TileSpacing));
                _tileArray[y][x] = t;
                TileChanged(_tileArray[y][x]);
            }
            else
            {
                if (t.GetType().Name != tileStr) //This is ugly beep boop. Fix this later.
                {
                    var surroundTiles = t.surroundingTiles;

                    if (t.GetType().GetInterface("ICollidable") != null)
                        _collisionManager.RemoveCollidable((ICollidable)t);

                    t = GenerateNewTile(tileStr, state, new Vector2D(x * TileSpacing, y * TileSpacing));
                    t.surroundingTiles = surroundTiles;
                    if(t.surroundingTiles[0] != null) t.surroundingTiles[0].surroundingTiles[2] = t;
                    if(t.surroundingTiles[1] != null) t.surroundingTiles[1].surroundingTiles[3] = t;
                    if(t.surroundingTiles[2] != null) t.surroundingTiles[2].surroundingTiles[0] = t;
                    if(t.surroundingTiles[3] != null) t.surroundingTiles[3].surroundingTiles[1] = t;

                    _tileArray[y][x] = t;
                    TileChanged(_tileArray[y][x]);
                }
                else if (t.tileState != state)
                {
                    t.tileState = state;
                    TileChanged(t);
                }
            }
        }

        #endregion

        #region Tile helper functions
        // Returns the position of a tile in the tileArray from world coordinates
        // Returns -1,-1 if an invalid position was passed in.
        public Vector2D GetTileArrayPositionFromWorldPosition(float x, float z)
        {
            if (x < 0 || z < 0)
                return new Vector2D(-1, -1);
            if (x > _mapWidth * TileSpacing || z > _mapWidth * TileSpacing)
                return new Vector2D(-1, -1);

            var xPos = (int)Math.Floor(x / TileSpacing);
            var zPos = (int)Math.Floor(z / TileSpacing);

            return new Vector2D(xPos, zPos);
        }

        public Point GetTileArrayPositionFromWorldPosition(Vector2D pos)
        {
            if (pos.X < 0 || pos.Y < 0)
                return new Point(-1, -1);
            if (pos.X > _mapWidth * TileSpacing || pos.Y > _mapWidth * TileSpacing)
                return new Point(-1, -1);

            var xPos = (int)Math.Floor(pos.X / TileSpacing);
            var yPos = (int)Math.Floor(pos.Y / TileSpacing);

            return new Point(xPos, yPos);
        }

        public Type GetTileTypeFromWorldPosition(float x, float y)
        {
            return GetTileTypeFromWorldPosition(new Vector2D(x, y));
        }

        public Type GetTileTypeFromWorldPosition(Vector2D pos)
        {
            Vector2D arrayPosition = GetTileArrayPositionFromWorldPosition(pos.X, pos.Y);
            if (arrayPosition.X < 0 || arrayPosition.Y < 0)
            {
                return null;
            }

            return GetTileTypeFromArrayPosition((int)arrayPosition.X, (int)arrayPosition.Y);
        }

        public Type GetTileTypeFromArrayPosition(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _mapWidth || y >= _mapHeight)
            {
                return null;
            }

            return _tileArray[y][x].GetType();
        }

        /// <summary>
        /// Get Tile from World Position.
        /// </summary>
        public ITile GetTileAt(Vector2D worldPos)
        {
            var p = GetTileArrayPositionFromWorldPosition(worldPos);
            if (p.X < 0 || p.Y < 0 || p.X >= _mapWidth || p.Y >= _mapHeight) return null;
            return _tileArray[p.Y][p.X];
        }

        /// <summary>
        /// Get Tile from Array Position.
        /// </summary>
        public ITile GetTileAt(int arrayX, int arrayY)
        {
            if (arrayX < 0 || arrayY < 0 || arrayX >= _mapWidth || arrayY >= _mapHeight) return null;
            return _tileArray[arrayY][arrayX];
        }

        public Tile GenerateNewTile(string typeName, TileState state, Vector2D pos)
        {
            var p = new Point((int)Math.Floor(pos.X / TileSpacing), (int)Math.Floor(pos.Y / TileSpacing));

            Type tileType = Type.GetType("ClientServices.Tiles." + typeName, false);

            if (tileType == null) throw new ArgumentException("Invalid Tile Type specified : '" + typeName + "' .");

            Tile created = (Tile)Activator.CreateInstance(tileType, state, pos, p);

            if (tileType.GetInterface("ICollidable") != null)
                _collisionManager.AddCollidable((ICollidable)created);

            return created;
        }

        // Where do we have tiles around us?
        // 0 = None
        // 1 = North
        // 2 = East
        // 4 = South
        // 8 = West
        // So if we have one N and S, we return (N + S) or (1 + 4), so 5.

        public byte SetSprite(int x, int y)
        {
            byte i = 0;

            if (GetTileAt(x, y - 1) != null && GetTileAt(x, y - 1).ConnectSprite) // N
            {
                i += 1;
            }
            if (GetTileAt(x + 1, y) != null && GetTileAt(x + 1, y).ConnectSprite) // E
            {
                i += 2;
            }
            if (GetTileAt(x, y + 1) != null && GetTileAt(x, y + 1).ConnectSprite) // S
            {
                i += 4;
            }
            if (GetTileAt(x - 1, y) != null && GetTileAt(x - 1, y).ConnectSprite) // W
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

        public Size GetMapSizeWorld()
        {
            return new Size(_mapWidth * TileSpacing, _mapHeight * TileSpacing);
        }
        #endregion

        #region Quick collision checks
        public bool IsSolidTile(Vector2D worldPos)
        {
            var tile = (Tile)GetTileAt(worldPos);
            if (tile == null) return false;
            if (tile.GetType().GetInterface("ICollidable") != null)
                return true;
            else return false;
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
            if(OnTileChanged != null)
                OnTileChanged(t.TilePosition, t.Position);

            t.surroundDirs = SetSprite(t.TilePosition.X, t.TilePosition.Y);
            foreach(Tile T in t.surroundingTiles)
            {
                if (T == null) continue;
                T.surroundDirs = SetSprite(T.TilePosition.X, T.TilePosition.Y);
            }
        }
        #endregion
    }
}
