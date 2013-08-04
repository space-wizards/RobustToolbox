using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using Lidgren.Network;
using SS13.IoC;
using SS13_Shared;
using SS13_Shared.ServerEnums;
using ServerInterfaces.Map;
using ServerInterfaces.Network;
using ServerInterfaces.Tiles;
using ServerServices.Atmos;
using ServerServices.Log;
using ServerServices.Tiles;

namespace ServerServices.Map
{
    public class MapManager : IMapManager
    {
        #region Variables

        private DateTime lastAtmosDisplayPush;
        private int mapHeight;
        private int mapWidth;
        private Tile[,] tileArray;
        public int tileSpacing = 64;
        private Dictionary<byte, string> tileStringTable = new Dictionary<byte, string>();

        #endregion

        #region Startup

        public bool InitMap(string mapName)
        {
            BuildTileTable();
            if (!LoadMap(mapName))
                NewMap();

            return true;
        }

        #endregion

        #region IMapManager Members

        /// <summary>
        /// This function takes the gas cell from one tile and moves it to another, reconnecting all of the references in adjacent tiles.
        /// Use this when a new tile is generated at a map location.
        /// </summary>
        /// <param name="fromTile">Tile to move gas information/cell from</param>
        /// <param name="toTile">Tile to move gas information/cell to</param>
        public void MoveGasCell(ITile fromTile, ITile toTile)
        {
            GasCell g = (fromTile as Tile).gasCell;
            (toTile as Tile).gasCell = g;
            g.AttachToTile((toTile as Tile));
        }

        public void Shutdown()
        {
            //ServiceManager.Singleton.RemoveService(this);
            tileArray = null;
        }

        public int GetMapWidth()
        {
            return mapWidth;
        }

        public int GetMapHeight()
        {
            return mapHeight;
        }

        #endregion

        #region Tile helper function

        public Point GetTileArrayPositionFromWorldPosition(float x, float z)
        {
            if (x < 0 || z < 0)
                return new Point(-1, -1);
            if (x >= mapWidth*tileSpacing || z >= mapWidth*tileSpacing)
                return new Point(-1, -1);

            // We use floor here, because even if we're at pos 10.999999, we're still on tile 10 in the array.
            var xPos = (int) Math.Floor(x/tileSpacing);
            var zPos = (int) Math.Floor(z/tileSpacing);

            return new Point(xPos, zPos);
        }

        public bool IsWorldPositionInBounds(Vector2 pos)
        {
            Point tpos = GetTileArrayPositionFromWorldPosition(pos);
            if (tpos.X == -1 && tpos.Y == -1)
                return false;
            return true;
        }

        public ITile GetTileFromWorldPosition(Vector2 pos)
        {
            Point arrayPos = GetTileArrayPositionFromWorldPosition(pos);
            return GetTileAt(arrayPos.X, arrayPos.Y);
        }

        public Point GetTileArrayPositionFromWorldPosition(Vector2 pos)
        {
            return GetTileArrayPositionFromWorldPosition(pos.X, pos.Y);
        }

        public Type GetTileTypeFromWorldPosition(float x, float y)
        {
            Point arrayPosition = GetTileArrayPositionFromWorldPosition(x, y);
            return GetTileTypeFromWorldPosition(new Vector2(x, y));
        }

        public bool IsSaneArrayPosition(int x, int y)
        {
            if (x < 0 || y < 0)
                return false;
            if (x > mapWidth - 1 || y > mapWidth - 1)
                return false;
            return true;
        }

        private Type GetTileTypeFromWorldPosition(Vector2 pos)
        {
            Point arrayPosition = GetTileArrayPositionFromWorldPosition(pos.X, pos.Y);
            if (arrayPosition.Y < 0 || arrayPosition.Y < 0)
            {
                return null;
            }
            else
            {
                return GetObjectTypeFromArrayPosition(arrayPosition.X, arrayPosition.Y);
            }
        }

        private Type GetObjectTypeFromArrayPosition(int x, int z)
        {
            if (x < 0 || z < 0 || x >= mapWidth || z >= mapHeight)
            {
                return null;
            }
            else
            {
                return tileArray[x, z].GetType();
            }
        }

        #endregion

        #region Map altering

        public bool ChangeTile(int x, int z, Type newType)
        {
            if (x < 0 || z < 0)
                return false;

            if (x > mapWidth || z > mapWidth)
                return false;

            var args = new object[3];
            args[0] = x;
            args[1] = z;
            args[2] = this;
            object newTile = Activator.CreateInstance(newType, args);
            var castTile = (Tile) newTile;

            if (tileArray[x, z] != null)
                tileArray[x, z].RaiseChangedEvent(castTile.GetType());

            MoveGasCell(tileArray[x, z], castTile); //Transfer the gas cell from the old tile to the new tile.

            tileArray[x, z] = castTile;

            return true;
        }

        public ITile GenerateNewTile(int x, int y, string typeName)
        {
            Type tileType = Type.GetType("ServerServices.Tiles." + typeName, false);

            if (tileType == null) throw new ArgumentException("Invalid Tile Type specified : '" + typeName + "' .");

            if (tileArray[x, y] != null) //If theres a tile, activate it's changed event.
                tileArray[x, y].RaiseChangedEvent(tileType);

            return (ITile) Activator.CreateInstance(tileType, x, y, this);
        }

        public bool ChangeTile(int x, int z, string newType)
        {
            if (x < 0 || z < 0)
                return false;

            if (x > mapWidth || z > mapWidth)
                return false;

            var tile = GenerateNewTile(x, z, newType) as Tile;
            //Transfer the gas cell from the old tile to the new tile.

            MoveGasCell(tileArray[x, z], tile);

            tileArray[x, z] = tile;
            return true;
        }

        #endregion

        #region networking

        public NetOutgoingMessage CreateMapMessage(MapMessage messageType)
        {
            NetOutgoingMessage message = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            message.Write((byte) NetMessage.MapMessage);
            message.Write((byte) messageType);
            return message;
        }

        public void SendMap(NetConnection connection)
        {
            SendTileIndex(connection); //Send index of byte -> str to save space.

            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map");
            NetOutgoingMessage mapMessage = CreateMapMessage(MapMessage.SendTileMap);

            int mapWidth = GetMapWidth();
            int mapHeight = GetMapHeight();

            mapMessage.Write(mapWidth);
            mapMessage.Write(mapHeight);

            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    Tile t = tileArray[x, y]; //Bypassing stuff.
                    mapMessage.Write(GetTileIndex((t.GetType().Name)));
                    mapMessage.Write((byte) t.TileState);
                }
            }

            IoCManager.Resolve<ISS13NetServer>().SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map finished with message size: " +
                           mapMessage.LengthBytes + " bytes");
        }

        /// <summary>
        /// Send message to all clients.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(NetOutgoingMessage message)
        {
            IoCManager.Resolve<ISS13NetServer>().SendToAll(message);
        }

        public void SendTileIndex(NetConnection connection)
        {
            NetOutgoingMessage mapMessage = CreateMapMessage(MapMessage.SendTileIndex);

            mapMessage.Write((byte) tileStringTable.Count);

            foreach (var curr in tileStringTable)
            {
                mapMessage.Write(curr.Key);
                mapMessage.Write(curr.Value);
            }

            IoCManager.Resolve<ISS13NetServer>().SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
        }

        #endregion

        public void BuildTileTable()
        {
            Type type = typeof (Tile);
            List<Assembly> asses = AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<Type> types =
                asses.SelectMany(t => t.GetTypes()).Where(p => type.IsAssignableFrom(p) && !p.IsAbstract).ToList();

            if (types.Count > 255)
                throw new ArgumentOutOfRangeException("types.Count", "Can not load more than 255 types of tiles.");

            tileStringTable = types.ToDictionary(x => (byte) types.FindIndex(y => y == x), x => x.Name);
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

        #region Networking

        public void HandleNetworkMessage(NetIncomingMessage message)
        {
            var messageType = (MapMessage) message.ReadByte();
            switch (messageType)
            {
                case MapMessage.TurfClick:
                    //HandleTurfClick(message);
                    break;
                case MapMessage.TurfUpdate:
                    HandleTurfUpdate(message);
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

        public void DestroyTile(Point arrayPosition)
        {
            if (IsSaneArrayPosition(arrayPosition.X, arrayPosition.Y))
            {
                Tile t = tileArray[arrayPosition.X, arrayPosition.Y];
                GasCell g = t.gasCell;
                var newTile = GenerateNewTile(arrayPosition.X, arrayPosition.Y, "Floor") as Tile; //Ugly
                tileArray[arrayPosition.X, arrayPosition.Y] = newTile;
                newTile.gasCell = g;
                g.AttachToTile(newTile);
                NetworkUpdateTile(arrayPosition.X, arrayPosition.Y);
            }
        }

        public void NetworkUpdateTile(int x, int y)
        {
            if (!IsSaneArrayPosition(x, y))
                return;

            NetOutgoingMessage message = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            message.Write((byte) NetMessage.MapMessage);
            message.Write((byte) MapMessage.TurfUpdate);
            message.Write((short) x);
            message.Write((short) y);
            message.Write(GetTileIndex(tileArray[x, y].GetType().Name));
            message.Write((byte) tileArray[x, y].TileState);
            IoCManager.Resolve<ISS13NetServer>().SendToAll(message);
        }

        private void HandleTurfUpdate(NetIncomingMessage message)
        {
            short x = message.ReadInt16();
            short y = message.ReadInt16();

            string typeStr = GetTileString(message.ReadByte());

            if (IsSaneArrayPosition(x, y))
            {
                GasCell g = tileArray[x, y].gasCell;
                var t = GenerateNewTile(x, y, typeStr) as Tile;
                tileArray[x, y] = t;
                tileArray[x, y].gasCell = g;
                g.AttachToTile(t);
                NetworkUpdateTile(x, y);
            }
        }

        #endregion

        #region Map loading/sending

        public void SaveMap()
        {
            string fileName = "SavedMap";

            var fs = new FileStream(fileName, FileMode.Create);
            var sw = new StreamWriter(fs);
            LogManager.Log("Saving map: W: " + mapWidth + " H: " + mapHeight);

            sw.WriteLine(mapWidth);
            sw.WriteLine(mapHeight);

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    sw.WriteLine(tileArray[x, y].GetType().Name);
                }
            }
            LogManager.Log("Done saving map.");

            sw.Close();
            fs.Close();
        }

        public ITile GetTileAt(int x, int y)
        {
            if (!IsSaneArrayPosition(x, y))
                return null;
            return tileArray[x, y];
        }

        private bool LoadMap(string filename)
        {
            if (!File.Exists(filename))
                return false;

            var fs = new FileStream(filename, FileMode.Open);
            var sr = new StreamReader(fs);

            mapWidth = int.Parse(sr.ReadLine());
            mapHeight = int.Parse(sr.ReadLine());

            tileArray = new Tile[mapWidth,mapHeight];

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    string tileName = sr.ReadLine();
                    tileArray[x, y] = (Tile) GenerateNewTile(x, y, tileName);
                }
            }

            sr.Close();
            fs.Close();

            return true;
        }

        private void NewMap()
        {
            LogManager.Log("Cannot find map. Generating blank map.", LogLevel.Warning);
            mapWidth = 50;
            mapHeight = 50;
            tileArray = new Tile[mapWidth,mapHeight];
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    tileArray[x, y] = new Floor(x, y, this);
                }
            }
        }

        #endregion
    }
}