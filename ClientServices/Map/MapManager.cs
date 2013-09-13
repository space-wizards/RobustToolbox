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
        //private Tile[][] _tileArray; // The array holding all the tiles that make up the map
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

        private Rectangle TilePos(Tile T)
        {
            return new Rectangle((int)(T.Position.X), (int)(T.Position.Y), (int)(TileSpacing), (int)(TileSpacing));
        }

        private ITile GetITileAt(Point p)
        {
            return (Tile)_tileArray.GetItems(p).FirstOrDefault();
        }

        public ITile GetITileAt(int X, int Y)
        {
            return GetITileAt(new Point(X, Y));
        }

        private Tile GetTileAt(Point p)
        {
                return (Tile)_tileArray.GetItems(p).FirstOrDefault();
        }

        private Tile GetTileAt(int X, int Y)
        {
            return GetTileAt(new Point(X, Y));
        }


        public bool LoadTileMap(NetIncomingMessage message)
        {
            var _mapLoadWidth = message.ReadInt32();
            var _mapLoadHeight = message.ReadInt32();

            //_tileArray = new Tile[_mapHeight][];

            _tileArray = new RectangleTree<Tile>(TilePos,
                                                 new Rectangle(-(_mapWidth/2)*TileSpacing, -(_mapHeight/2)*TileSpacing,
                                                                 _mapWidth*TileSpacing, _mapHeight*TileSpacing));

            /*for (int i = 0; i < _mapHeight; i++)
            {
                _tileArray[i] = new Tile[_mapWidth];
            }*/

            for (int x = 0; x < _mapLoadWidth; x++)
            {
                for (int y = 0; y < _mapLoadHeight; y++)
                {
                    int posX = x*TileSpacing;
                    int posY = y*TileSpacing;

                    byte index = message.ReadByte();
                    var state = (TileState) message.ReadByte();

                    Tile created = GenerateNewTile(GetTileString(index), state, new Vector2D(posX, posY));
                    _tileArray.Add(created);
                }
            }

            for (int x = 0; x < _mapLoadWidth; x++)
            {
                for (int y = 0; y < _mapLoadWidth; y++)
                {
                    Tile T = GetTileAt(new Point(x * TileSpacing, y * TileSpacing));
                    /*if (T == null)
                        continue;*/
                    if (T.ConnectSprite) //Was wall check.
                    {
                        byte i = SetSprite(x, y);
                    }
                    if (y > 0)
                    {
                        T.surroundingTiles[0] = GetTileAt(x * TileSpacing, (y - 1) * TileSpacing); //north
                    }
                    if (x < _mapWidth - 1)
                    {
                        T.surroundingTiles[1] = GetTileAt((x + 1) * TileSpacing, y * TileSpacing); //east
                    }
                    if (y < _mapHeight - 1)
                    {
                        T.surroundingTiles[2] = GetTileAt(x * TileSpacing, (y + 1) * TileSpacing); //south
                    }
                    if (x > 0)
                    {
                        T.surroundingTiles[3] = GetTileAt((x - 1) * TileSpacing, y * TileSpacing); //west
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
                            //GetTileAt(x * TileSpacing, y * TileSpacing).SetAtmosDisplay((GasType) i, amount);
                        }
                    }

                    /*        _tileArray[y][x].SetAtmosDisplay(decompressed[r]);
                    r++;
                    _tileArray[y][x].SetAtmosDisplay(decompressed[r]);
                    r++;
                    _tileArray[y][x].SetAtmosDisplay(decompressed[r]);
                    r++;*/
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
            int x = message.ReadInt32();
            int y = message.ReadInt32();
            var type = (DecalType) message.ReadByte();

            GetTileAt(x * TileSpacing,y * TileSpacing).AddDecal(type);
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
            short x = message.ReadInt16();
            short y = message.ReadInt16();
            string tileStr = GetTileString(message.ReadByte());
            var state = (TileState) message.ReadByte();

            Tile t = GetTileAt(x * TileSpacing, y * TileSpacing);

            if (t == null)
            {
                t = GenerateNewTile(tileStr, state, new Vector2D(x*TileSpacing, y*TileSpacing));
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

        // Returns the position of a tile in the tileArray from world coordinates
        // Returns -1,-1 if an invalid position was passed in.
        public Vector2D GetTileArrayPositionFromWorldPosition(float x, float z)
        {
            if (x < 0 || z < 0)
                return new Vector2D(-1, -1);
            if (x > _mapWidth*TileSpacing || z > _mapWidth*TileSpacing)
                return new Vector2D(-1, -1);

            var xPos = (int) Math.Floor(x/TileSpacing);
            var zPos = (int) Math.Floor(z/TileSpacing);

            return new Vector2D(xPos, zPos);
        }

        public Point GetTileArrayPositionFromWorldPosition(Vector2D pos)
        {
            if (pos.X < 0 || pos.Y < 0)
                return new Point(-1, -1);
            if (pos.X > _mapWidth*TileSpacing || pos.Y > _mapWidth*TileSpacing)
                return new Point(-1, -1);

            var xPos = (int) Math.Floor(pos.X/TileSpacing);
            var yPos = (int) Math.Floor(pos.Y/TileSpacing);

            return new Point(xPos, yPos);
        }

        public Type GetTileTypeFromWorldPosition(Vector2D pos)
        {
            Vector2D arrayPosition = GetTileArrayPositionFromWorldPosition(pos.X, pos.Y);
            if (arrayPosition.X < 0 || arrayPosition.Y < 0)
            {
                return null;
            }

            return GetTileTypeFromArrayPosition((int) arrayPosition.X, (int) arrayPosition.Y);
        }

        /// <summary>
        /// Get Tile from World Position.
        /// </summary>
        public ITile GetITileAt(Vector2D worldPos)
        {
            return GetTileAt((int)worldPos.X, (int)worldPos.Y);
        }

        /// <summary>
        /// Get Tile from Array Position.
        /// </summary>
        /*public ITile GetTileAt(int arrayX, int arrayY)
        {
            if (arrayX < 0 || arrayY < 0 || arrayX >= _mapWidth || arrayY >= _mapHeight) return null;
            return _tileArray[arrayY][arrayX];
        }*/

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

            if (GetTileAt(x * TileSpacing, (y - 1) * TileSpacing) != null && GetTileAt(x * TileSpacing, (y - 1) * TileSpacing).ConnectSprite) // N
            {
                i += 1;
            }
            if (GetTileAt((x + 1) * TileSpacing, y * TileSpacing) != null && GetTileAt((x + 1) * TileSpacing, y * TileSpacing).ConnectSprite) // E
            {
                i += 2;
            }
            if (GetTileAt(x * TileSpacing, (y + 1) * TileSpacing) != null && GetTileAt(x * TileSpacing, (y + 1) * TileSpacing).ConnectSprite) // S
            {
                i += 4;
            }
            if (GetTileAt((x - 1) * TileSpacing, y * TileSpacing) != null && GetTileAt((x - 1) * TileSpacing, y * TileSpacing).ConnectSprite) // W
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
            return new Size(_mapWidth*TileSpacing, _mapHeight*TileSpacing);
        }

        public Type GetTileTypeFromWorldPosition(float x, float y)
        {
            return GetTileTypeFromWorldPosition(new Vector2D(x, y));
        }

        public Type GetTileTypeFromArrayPosition(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _mapWidth || y >= _mapHeight)
            {
                return null;
            }

            return GetTileAt(x * TileSpacing, y * TileSpacing).GetType();
        }

        public Tile GenerateNewTile(string typeName, TileState state, Vector2D pos)
        {
            var p = new Point((int)pos.X / TileSpacing, (int)pos.Y / TileSpacing);

            Type tileType = Type.GetType("ClientServices.Tiles." + typeName, false);

            if (tileType == null) throw new ArgumentException("Invalid Tile Type specified : '" + typeName + "' .");

            var created = (Tile) Activator.CreateInstance(tileType, state, pos, p);

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
                OnTileChanged(t.TilePosition, t.Position);

            t.surroundDirs = SetSprite(t.TilePosition.X, t.TilePosition.Y);
            foreach (Tile T in t.surroundingTiles)
            {
                if (T == null) continue;
                T.surroundDirs = SetSprite(T.TilePosition.X, T.TilePosition.Y);
            }
        }

        #endregion
    }
}