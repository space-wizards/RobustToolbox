using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Media;
using SS3d_server.Tiles;
using Lidgren.Network;

using SS3D_shared;
using SS3D_shared.HelperClasses;

namespace SS3d_server.Modules.Map
{
    public class Map
    {
        #region Variables
        private Tile[,] tileArray;
        SS3DNetserver netServer;
        private int mapWidth;
        private int mapHeight;
        private string[,] nameArray;
        public int tileSpacing = 64;
        private int wallHeight = 40; // This must be the same as defined in the MeshManager.
        DateTime lastAtmosDisplayPush;
        #endregion

        public Map(SS3DNetserver _netServer)
        {
            netServer = _netServer;
        }

        #region Startup
        public bool InitMap(string mapName)
        {
            if (!LoadMap(mapName))
            {
                return false;
            }

            ParseNameArray();
            InitializeAtmos();

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
            tileArray = new Tile[mapWidth, mapHeight];

            for (int z = 0; z < mapHeight; z++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    switch (nameArray[x, z])
                    {
                        case "wall":
                            tileArray[x, z] = new Tiles.Wall.Wall();
                            break;
                        case "floor":
                            tileArray[x, z] = new Tiles.Floor.Floor();
                            break;
                        case "space":
                            tileArray[x, z] = new Tiles.Floor.Space();
                            break;
                        default:
                            break;
                    }
                }
            }
        }

        private void InitializeAtmos()
        {
            for (int z = 0; z < mapHeight; z++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    tileArray[x,z].gasCell = new Tiles.Atmos.GasCell(tileArray[x,z],x,z,tileArray, mapWidth, mapHeight);
                }
            }

            tileArray[2, 2].gasCell.AddGas(5000, Tiles.Atmos.GasType.Oxygen);
        }

        public void UpdateAtmos()
        {
            for (int x = 0; x < mapWidth; x++)
                for(int y = 0; y < mapHeight;y++)
                {
                    tileArray[x, y].gasCell.CalculateNextGasAmount();
                }
            for(int x = 0;x<mapWidth;x++)
                for (int y = 0; y < mapHeight; y++)
                {
                    tileArray[x, y].gasCell.Update();
                }

            //Prepare gas display message
            if ((DateTime.Now - lastAtmosDisplayPush).TotalMilliseconds > 333)
            {
                bool sendUpdate = false;
                NetOutgoingMessage message = netServer.netServer.CreateMessage();
                message.Write((byte)NetMessage.AtmosDisplayUpdate);
                for (int x = 0; x < mapWidth; x++)
                    for (int y = 0; y < mapHeight; y++)
                    {
                        byte[] displayBytes = tileArray[x, y].gasCell.PackDisplayBytes();
                        if (displayBytes.Length == 0) //if there are no changes, continue.
                            continue;
                        sendUpdate = true;
                        foreach (byte displayByte in displayBytes)
                        {
                            message.Write(x);
                            message.Write(y);
                            message.Write(displayByte);
                            Console.Write("Gas update: x: " + x.ToString() + " y: " + y.ToString() + "byte: " + Convert.ToString(displayByte,2) + "\n");
                        }
                    }
                if (sendUpdate)
                {
                    netServer.SendMessageToAll(message, NetDeliveryMethod.Unreliable);// Gas updates aren't a big deal.
                    Console.Write("Sending Gas update\n");
                }
                lastAtmosDisplayPush = DateTime.Now;
            }
        }

        public TileType[,] GetMapForSending()
        {
            TileType[,] mapObjectType = new TileType[mapWidth, mapHeight];

            for (int x = 0; x < mapWidth; x++)
            {
                for (int z = 0; z < mapHeight; z++)
                {
                    mapObjectType[x, z] = tileArray[x, z].tileType;
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

            Tile tile = GenerateNewTile(newType);

            tileArray[x, z] = tile;
            return true;
        }

        public Tile GenerateNewTile(TileType type)
        {
            switch (type)
            {
                case TileType.Space:
                    Tiles.Floor.Space space = new Tiles.Floor.Space();
                    return space;
                case TileType.Floor:
                    Tiles.Floor.Floor floor = new Tiles.Floor.Floor();
                    return floor;
                case TileType.Wall:
                    Tiles.Wall.Wall wall = new Tiles.Wall.Wall();
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

        public Point GetTileArrayPositionFromWorldPosition(Vector2 pos)
        {
            return GetTileArrayPositionFromWorldPosition(pos.X, pos.Y);
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


        private TileType GetObjectTypeFromWorldPosition(Vector2 pos)
        {
            Point arrayPosition = GetTileArrayPositionFromWorldPosition(pos.X, pos.Y);
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
                return tileArray[x, z].tileType;
            }
        }
        #endregion


        #region BASIC collision things

        public bool CheckCollision(Vector2 pos)
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

        public TileType GetObjectTypeAt(Vector2 pos)
        {
            return GetObjectTypeFromWorldPosition(pos);
        }

        public bool IsFloorUnder(Vector2 pos)
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
