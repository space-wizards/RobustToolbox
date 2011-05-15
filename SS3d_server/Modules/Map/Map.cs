using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Media;

using SS3D_shared;
using SS3D_shared.HelperClasses;

namespace SS3d_server.Modules.Map
{
    public class Map
    {
        #region Variables
        private BaseTile[,] tileArray;
        private int mapWidth;
        private int mapHeight;
        private string[,] nameArray;
        public int tileSpacing = 16;
        private int wallHeight = 40; // This must be the same as defined in the MeshManager.
        #endregion

        public Map()
        {

        }

        #region Startup
        public bool InitMap(string mapName)
        {
            if (!LoadMap(mapName))
            {
                return false;
            }

            ParseNameArray();

            return true;
        }
        #endregion

        #region Map loading/sending
        private bool LoadMap(string filename)
        {
            if (!File.Exists(filename))
            {
                return false;
            }

            FileStream fs = new FileStream(filename, FileMode.Open);
            StreamReader sr = new StreamReader(fs);

            mapWidth = int.Parse(sr.ReadLine());
            mapHeight = int.Parse(sr.ReadLine());

            nameArray = new string[mapWidth, mapHeight];

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    nameArray[x, y] = sr.ReadLine();
                }
            }

            sr.Close();
            fs.Close();

            return true;
        }

        private void ParseNameArray()
        {
            tileArray = new BaseTile[mapWidth, mapHeight];

            for (int z = 0; z < mapHeight; z++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    switch (nameArray[x, z])
                    {
                        case "wall":
                            tileArray[x, z] = new Wall();
                            break;
                        case "floor":
                            tileArray[x, z] = new Floor();
                            break;
                        case "space":
                            tileArray[x, z] = new Space();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        public TileType[,] GetMapForSending()
        {
            TileType[,] mapObjectType = new TileType[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    mapObjectType[x, z] = tileArray[x, z].TileType;
                }
            }

            return mapObjectType;

        }
        #endregion

        #region Map altering
        public bool ChangeTile(int x, int z, TileType newType)
        {
            if (x < 0 || z < 0)
                return false;
            if (x > mapWidth || z > mapWidth)
                return false;

            BaseTile tile = GenerateNewTile(newType);

            tileArray[x, z] = tile;
            return true;
        }

        public BaseTile GenerateNewTile(TileType type)
        {
            switch (type)
            {
                case TileType.Space:
                    Space space = new Space();
                    return space;
                case TileType.Floor:
                    Floor floor = new Floor();
                    return floor;
                case TileType.Wall:
                    Wall wall = new Wall();
                    return wall;
                default:
                    return null;
            }
        }
        #endregion


        public int GetMapWidth()
        {
            return mapWidth;
        }

        public int GetMapHeight()
        {
            return mapHeight;
        }


        #region Tile helper function
        public Point GetTileArrayPositionFromWorldPosition(double x, double z)
        {
            
            if (x < 0 || z < 0)
                return new Point(-1, -1);
            if (x >= mapWidth * tileSpacing || z >= mapWidth * tileSpacing)
                return new Point(-1, -1);

            // We use floor here, because even if we're at pos 10.999999, we're still on tile 10 in the array.
            int xPos = (int)System.Math.Floor(x / tileSpacing);
            int zPos = (int)System.Math.Floor(z / tileSpacing);

            return new Point(xPos, zPos);
        }

        public Point GetTileArrayPositionFromWorldPosition(Vector3 pos)
        {
            return GetTileArrayPositionFromWorldPosition(pos.X, pos.Z);
        }

        public TileType GetObjectTypeFromWorldPosition(float x, float z)
        {
            Point arrayPosition = GetTileArrayPositionFromWorldPosition(x, z);
            if (arrayPosition.x < 0 || arrayPosition.y < 0)
            {
                return TileType.None;
            }
            else
            {
                return GetObjectTypeFromArrayPosition((int)arrayPosition.x, (int)arrayPosition.y);
            }
        }


        private TileType GetObjectTypeFromWorldPosition(Vector3 pos)
        {
            Point arrayPosition = GetTileArrayPositionFromWorldPosition(pos.X, pos.Z);
            if (arrayPosition.x < 0 || arrayPosition.y < 0)
            {
                return TileType.None;
            }
            else
            {
                return GetObjectTypeFromArrayPosition((int)arrayPosition.x, (int)arrayPosition.y);
            }
        }

        private TileType GetObjectTypeFromArrayPosition(int x, int z)
        {
            if (x < 0 || z < 0 || x >= mapWidth || z >= mapHeight)
            {
                return TileType.None;
            }
            else
            {
                return tileArray[x, z].TileType;
            }
        }
        #endregion


        #region BASIC collision things

        public bool CheckCollision(Vector3 pos)
        {
            TileType tile = GetObjectTypeFromWorldPosition(pos);

            if (tile == TileType.None)
            {
                return false;
            }
            else if (tile == TileType.Wall && pos.Y <= wallHeight)
            {
                return true;
            }
            else if ((tile == TileType.Floor || tile == TileType.Space) && pos.Y <= 0)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public Vector3 GetPointAboveTileAt(Vector3 pos)
        {
            TileType tile = GetObjectTypeFromWorldPosition(pos);

            if (tile == TileType.None)
            {
                return pos;
            }
            else if (tile == TileType.Wall)
            {
                return new Vector3(pos.X, 40f, pos.Z);
            }
            else if ((tile == TileType.Floor || tile == TileType.Space) && pos.Y < 0)
            {
                return new Vector3(pos.X, 0, pos.Z);
            }
            else
            {
                return pos;
            }
        }

        public double GetHeightAboveTileAt(Vector3 pos)
        {
            TileType tile = GetObjectTypeFromWorldPosition(pos);

            if (tile == TileType.None)
            {
                return pos.Y;
            }
            else if (tile == TileType.Wall)
            {
                return wallHeight;
            }
            else if ((tile == TileType.Floor || tile == TileType.Space))
            {
                return 0;
            }
            else
            {
                return pos.Y;
            }
        }

        public TileType GetObjectTypeAt(Vector3 pos)
        {
            return GetObjectTypeFromWorldPosition(pos);
        }

        public bool IsFloorUnder(Vector3 pos)
        {
            if (GetObjectTypeFromWorldPosition(pos) == TileType.Floor)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        #endregion
    }
}
