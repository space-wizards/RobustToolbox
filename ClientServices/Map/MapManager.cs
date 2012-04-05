using System;
using System.Collections.Generic;
using System.Drawing;
using ClientInterfaces.Collision;
using ClientInterfaces.Lighting;
using ClientInterfaces.Map;
using ClientInterfaces.Resource;
using ClientServices.Map.Tiles.Floor;
using ClientServices.Map.Tiles.Wall;
using SS13_Shared;
using System.IO;
using Lidgren.Network;
using GorgonLibrary;
using GorgonLibrary.Graphics;
using ClientInterfaces;
using ClientServices.Map.Tiles;

namespace ClientServices.Map
{
    public class MapManager : IMapManager
    {
        #region Variables
        private Tile[,] _tileArray; // The array holding all the tiles that make up the map
        private int _mapWidth; // Number of tiles across the map (must be a multiple of StaticGeoSize)
        private int _mapHeight; // Number of tiles up the map (must be a multiple of StaticGeoSize)
        private const int TileSpacing = 64; // Distance between tiles
        private Dictionary<string, Sprite> _tileSprites;
        private const string FloorSpriteName = "floor_texture";
        private const string WallTopSpriteName = "wall_texture";
        private const string WallSideSpriteName = "wall_side";
        private readonly List<Vector2D> _cardinalList;
        private static readonly PortalInfo[] Portal = new PortalInfo[4];
        private Point _lastVisPoint;

        private bool _needVisUpdate;
        private bool _loaded;
        private bool _initialized;

        private readonly IResourceManager _resourceManager;
        private readonly ILightManager _lightManager;
        private readonly ICollisionManager _collisionManager;
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

            Portal[0] = new PortalInfo(1, 1, 1, -1, 1, 0); // East
            Portal[1] = new PortalInfo(-1, 1, 1, 1, 0, 1); // South
            Portal[2] = new PortalInfo(-1, -1, -1, 1, -1, 0); // West
            Portal[3] = new PortalInfo(1, -1, -1, -1, 0, -1); // North
            
            _lastVisPoint = new Point(0, 0);
        }
        
        #region Startup / Loading
        public void Init()
        {
            if (!_initialized)
            {
                _tileSprites = new Dictionary<string, Sprite>
                                   {
                                       {FloorSpriteName, _resourceManager.GetSprite(FloorSpriteName)},
                                       {WallSideSpriteName, _resourceManager.GetSprite(WallSideSpriteName)}
                                   };
                for (var i = 0; i < 16; i++)
                {
                    _tileSprites.Add(WallTopSpriteName + i, _resourceManager.GetSprite(WallTopSpriteName + i));
                }
                _tileSprites.Add("space_texture", _resourceManager.GetSprite("space_texture"));
            }
            _initialized = true;
        }


        public bool LoadNetworkedMap(TileType[,] networkedArray, TileState[,] networkedStates, int mapWidth, int mapHeight)
        {
           
            _mapWidth = mapWidth;
            _mapHeight = mapHeight;

            _tileArray = new Tile[_mapWidth, _mapHeight];

            //loadingText = "Building Map...";
            //loadingPercent = 0;

            float maxElements = (_mapHeight * _mapWidth);
            float oneElement = 100f / maxElements;
            float currCount = 0;

            for (var y = 0; y < _mapHeight; y++)
            {
                for (var x = 0; x < _mapWidth; x++)
                {
                    var posX = x * TileSpacing;
                    var posY = y * TileSpacing;
                    var state = networkedStates[x, y];
                    switch (networkedArray[x, y])
                    {
                        case TileType.Wall:
                            _tileArray[x, y] = GenerateNewTile(TileType.Wall,  state, new Vector2D(posX, posY));
                            break;
                        case TileType.Floor:
                            _tileArray[x, y] = GenerateNewTile(TileType.Floor, state, new Vector2D(posX, posY));
                            break;
                        case TileType.Space:
                            _tileArray[x, y] = GenerateNewTile(TileType.Space, state, new Vector2D(posX, posY));
                            break;
                    }
                    currCount += oneElement;
                    if (currCount >= 1)
                    {
                        //loadingPercent += maxElements > 100 ? 1 : oneElement;
                        currCount = 0;
                    }
                }
            }

            for (var x = 0; x < _mapWidth; x++)
            {
                for (var y = 0; y < _mapHeight; y++)
                {
                    if (_tileArray[x, y].TileType == TileType.Wall)
                    {
                        var i = SetSprite(x, y);
                        _tileArray[x, y].SetSprites(_tileSprites[WallTopSpriteName+i], _tileSprites[WallSideSpriteName], i);
                    }
                    if (y > 0)
                    {
                        _tileArray[x, y].surroundingTiles[0] = _tileArray[x, y - 1]; //north
                    }
                    if (x < _mapWidth - 1)
                    {
                        _tileArray[x, y].surroundingTiles[1] = _tileArray[x + 1, y]; //east
                    }
                    if (y < _mapHeight - 1)
                    {
                        _tileArray[x, y].surroundingTiles[2] = _tileArray[x, y + 1]; //south
                    }
                    if (x > 0)
                    {
                        _tileArray[x, y].surroundingTiles[3] = _tileArray[x - 1, y]; //west
                    }

                }
            }

            _loaded = true;
            return true;
        }

        public void SaveMap()
        {
            const string fileName = "SavedMap";

            var fs = new FileStream(fileName, FileMode.Create);
            var sw = new StreamWriter(fs);

            sw.WriteLine(_mapWidth);
            sw.WriteLine(_mapHeight);

            for (int y = 0; y < _mapHeight; y++)
            {
                for (int x = 0; x < _mapWidth; x++)
                {
                    sw.WriteLine(_tileArray[x, y].name);
                }
            }

            sw.Close();
            fs.Close();
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

            _tileArray[x, y].AddDecal(type);
        }

        public void HandleAtmosDisplayUpdate(NetIncomingMessage message)
        {
            if (!_loaded)
                return;
            var count = message.ReadInt32();
            var records = new List<AtmosRecord>();
            for (var i = 1; i <= count; i++)
            {
                records.Add(new AtmosRecord(message.ReadInt32(), message.ReadInt32(), message.ReadByte()));
            }

            foreach (var record in records)
            {
                _tileArray[record.X, record.Y].SetAtmosDisplay(record.Display);
            }
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
            var type = (TileType)message.ReadByte();
            var state = (TileState)message.ReadByte();

            if (_tileArray[x, y] == null)
            {
                GenerateNewTile(type, state, new Vector2D(x * TileSpacing, y * TileSpacing));
            }
            else
            {
                if (_tileArray[x, y].TileType != type)
                {
                    var surroundTiles = _tileArray[x, y].surroundingTiles;
                    var lightList = _tileArray[x, y].tileLights.ToArray();
                    _tileArray[x, y] = GenerateNewTile(type, state, new Vector2D(x * TileSpacing, y * TileSpacing));
                    _tileArray[x, y].surroundingTiles = surroundTiles;
                    foreach (var T in _tileArray[x, y].surroundingTiles)
                    {
                        T.surroundDirs = SetSprite(T.TilePosition.X, T.TilePosition.Y);
                    }
                    _needVisUpdate = true;
                }
                else if (_tileArray[x, y].tileState != state)
                {
                    _tileArray[x, y].tileState = state;
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

        public TileType GetTileTypeFromWorldPosition(float x, float z)
        {
            var arrayPosition = GetTileArrayPositionFromWorldPosition(x, z);
            if (arrayPosition.X < 0 || arrayPosition.Y < 0)
            {
                return TileType.None;
            }

            return GetTileTypeFromArrayPosition((int)arrayPosition.X, (int)arrayPosition.Y);
        }

        public TileType GetTileTypeFromWorldPosition(Vector2D pos)
        {
            Vector2D arrayPosition = GetTileArrayPositionFromWorldPosition(pos.X, pos.Y);
            if (arrayPosition.X < 0 || arrayPosition.Y < 0)
            {
                return TileType.None;
            }

            return GetTileTypeFromArrayPosition((int)arrayPosition.X, (int)arrayPosition.Y);
        }

        public TileType GetTileTypeFromArrayPosition(int x, int y)
        {
            if (x < 0 || y < 0 || x >= _mapWidth || y >= _mapHeight)
            {
                return TileType.None;
            }

            return _tileArray[x, y].TileType;
        }

        public ITile GetTileAt(Vector2D pos)
        {
            if (pos.X < 0 || pos.Y < 0) return null;
            var p = GetTileArrayPositionFromWorldPosition(pos);
            return _tileArray[p.X, p.Y];
        }

        public ITile GetTileAt(int x, int y)
        {
            if (x < 0 || y < 0) return null;
            return _tileArray[x, y];
        }

        // Changes a tile based on its array position (get from world
        // coordinates using GetTileFromWorldPosition(int, int). Returns true if successful.
        public bool ChangeTile(Vector2D arrayPosition, TileType newType)
        {
            var x = (int)arrayPosition.X;
            var z = (int)arrayPosition.Y;

            if (x < 0 || z < 0)
                return false;
            if (x > _mapWidth || z > _mapWidth)
                return false;
            var pos = _tileArray[x, z].Position;
            //Tile tile = GenerateNewTile(newType, pos);

            /*if (tile == null)
            {
                return false;
            }

            tileArray[x, z] = tile;*/
            return true;
        }

        public bool ChangeTile(int x, int z, TileType newType)
        {
            var pos = new Vector2D(x, z);
            return ChangeTile(pos, newType);
        }

        public Tile GenerateNewTile(TileType type, TileState state, Vector2D pos)
        {
            var p = new Point((int) Math.Floor(pos.X / TileSpacing), (int) Math.Floor(pos.Y / TileSpacing));

            switch (type)
            {
                case TileType.Space:
                    return new Space(_tileSprites["space_texture"], state, TileSpacing, pos, p, _lightManager, _resourceManager);
                case TileType.Floor:
                    return new Floor(_tileSprites[FloorSpriteName], state, TileSpacing, pos, p, _lightManager, _resourceManager);
                case TileType.Wall:
                    var wall = new Wall(_tileSprites[WallTopSpriteName + "0"], _tileSprites[WallSideSpriteName], state, TileSpacing, pos, p, _lightManager, _resourceManager);
                    _collisionManager.AddCollidable(wall);
                    return wall;
                default:
                    return null;
            }
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

            if (GetTileTypeFromArrayPosition(x, y - 1) == TileType.Wall) // N
            {
                i += 1;
            }
            if (GetTileTypeFromArrayPosition(x + 1, y) == TileType.Wall) // E
            {
                i += 2;
            }
            if (GetTileTypeFromArrayPosition(x, y + 1) == TileType.Wall) // S
            {
                i += 4;
            }
            if (GetTileTypeFromArrayPosition(x - 1, y) == TileType.Wall) // W
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

        /*public List<System.Drawing.RectangleF> GetSurroundingAABB(Vector2D pos)
        {
            List<System.Drawing.RectangleF> AABBList = new List<System.Drawing.RectangleF>();
            Vector2D tilePos = GetTileArrayPositionFromWorldPosition(pos.X, pos.Y);

            foreach (Vector2D dir in cardinalList)
            {
                Vector2D checkPos = pos + dir;
                if (GetTileTypeFromArrayPosition((int)checkPos.X, (int)checkPos.Y) == TileType.Wall)
                {
                    System.Drawing.RectangleF AABB = GetAABB(checkPos);
                    if (AABB != null)
                    {
                        AABBList.Add(AABB);
                    }
                }
            }

            return AABB;
        }*/

        #endregion

        #region Quick collision checks

        public bool IsSolidTile(Vector2D pos)
        {
            var tile = GetTileTypeFromWorldPosition(pos);

            if (tile == null) return false; //Hack. This happens when its outside the map.

            switch (tile)
            {
                case TileType.None:
                    return false;
                case TileType.Wall:
                    return true;
                case TileType.Space:
                case TileType.Floor:
                    return false;
                default:
                    return false;
            }
        }

        public bool CheckCollision(Vector2D pos)
        {
            var tile = GetTileTypeFromWorldPosition(pos);

            switch (tile)
            {
                case TileType.None:
                    return false;
                case TileType.Wall:
                    return true;
                case TileType.Space:
                case TileType.Floor:
                    return true;
                default:
                    return false;
            }
        }

        public TileType GetObjectTypeAt(Vector2D pos)
        {
            return GetTileTypeFromWorldPosition(pos);
        }

        public bool IsFloorUnder(Vector2D pos)
        {
            return GetTileTypeFromWorldPosition(pos) == TileType.Floor;
        }

        #endregion

        #region Shutdown
        public void Shutdown()
        {
            _tileArray = null;
            _tileSprites = null;
            _initialized = false;
        }
        #endregion

        #region Visibility

        struct PortalInfo
        {
            // offset of portal's left corner relative to square center (doubled coordinates):
            public readonly int Lx;
            public readonly int Ly;
            // offset of portal's right corner relative to square center (doubled coordinates):
            public readonly int Rx;
            public readonly int Ry;
            // offset of neighboring cell relative to this cell's coordinates (not doubled):
            public readonly int Nx;
            public readonly int Ny;

            public PortalInfo(int lx, int ly, int rx, int ry, int nx, int ny)
            {
                Lx = lx;
                Ly = ly;
                Rx = rx;
                Ry = ry;
                Nx = nx;
                Ny = ny;
            }
        }

        #region Helper methods
        bool IsSightBlocked(int x, int y)
        {
            if (_tileArray[x, y].TileType == TileType.Wall || _tileArray[x, y].sightBlocked)
            {
                return true;
            }
            return false;
        }

        void ClearVisibility()
        {
            for (var x = 0; x < _mapWidth; x++)
            {
                for (var y = 0; y < _mapHeight; y++)
                {
                    _tileArray[x, y].Visible = false;
                }
            }
        }

        public void SetAllVisible()
        {
            for (var x = 0; x < _mapWidth; x++)
            {
                for (var y = 0; y < _mapHeight; y++)
                {
                    _tileArray[x, y].Visible = true;
                }
            }
        }

        void SetVisible(int x, int y)
        {
            _tileArray[x, y].Visible = true;
        }
        #endregion

        public void ComputeVisibility(int viewerX, int viewerY)
        {
            ClearVisibility();
            for (var i = 0; i < 4; ++i)
            {
                ComputeVisibility
                (
                    viewerX, viewerY,
                    viewerX, viewerY,
                    Portal[i].Lx, Portal[i].Ly,
                    Portal[i].Rx, Portal[i].Ry
                );
            }
        }
        
        bool a_right_of_b(int ax, int ay, int bx, int by)
        {
            return ax * by > ay * bx;
        }
        
        void ComputeVisibility(int viewerX, int viewerY, int targetX, int targetY, int ldx, int ldy, int rdx, int rdy)
        {
            if (targetX > viewerX + 15 || targetX < viewerX - 15)
                return;
            if (targetY > viewerY + 15 || targetY < viewerY - 15)
                return;
            // Abort if we are out of bounds.
            if (targetX < 0 || targetX >= _mapWidth)
                return;
            if (targetY < 0 || targetY >= _mapHeight)
                return;

            // This square is visible.
            SetVisible(targetX, targetY);

            // A solid target square blocks all further visibility through it.
            if (IsSightBlocked(targetX, targetY))
                return;

            // Target square center position relative to viewer:
            int dx = 2 * (targetX - viewerX);
            int dy = 2 * (targetY - viewerY);

            for (int i = 0; i < 4; ++i)
            {
                // Relative positions of the portal's left and right endpoints:
                int pldx = dx + Portal[i].Lx;
                int pldy = dy + Portal[i].Ly;
                int prdx = dx + Portal[i].Rx;
                int prdy = dy + Portal[i].Ry;

                // Clip portal against current view frustum:
                int cldx, cldy;
                if (a_right_of_b(ldx, ldy, pldx, pldy))
                {
                    cldx = ldx;
                    cldy = ldy;
                }
                else
                {
                    cldx = pldx;
                    cldy = pldy;
                }
                int crdx, crdy;
                if (a_right_of_b(rdx, rdy, prdx, prdy))
                {
                    crdx = prdx;
                    crdy = prdy;
                }
                else
                {
                    crdx = rdx;
                    crdy = rdy;
                }

                // If we can see through the clipped portal, recurse through it.
                if (a_right_of_b(crdx, crdy, cldx, cldy))
                {
                    ComputeVisibility
                    (
                        viewerX, viewerY,
                        targetX + Portal[i].Nx, targetY + Portal[i].Ny,
                        cldx, cldy,
                        crdx, crdy
                    );
                }
            }
        }

        public Point GetLastVisiblePoint()
        {
            return _lastVisPoint;
        }

        public void SetLastVisiblePoint(Point point)
        {
            _lastVisPoint = point;
        }

        public bool NeedVisibilityUpdate()
        {
            return _needVisUpdate;
        }


#endregion

    }
}
