using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using BKSystem.IO;
using ServerServices.Tiles;
using Lidgren.Network;

using SS13_Shared;
using System.Drawing;
using ServerInterfaces;
using ServerServices.Log;
using SS13.IoC;
using ServerInterfaces.Network;
using ServerInterfaces.Map;
using ServerInterfaces.Tiles;
using System.Linq;
using System.Reflection;
using SS13_Shared.ServerEnums;

namespace ServerServices.Map
{
    public class MapManager : IMapManager
    {
        #region Variables
        private Tile[,] tileArray;
        private int mapWidth;
        private int mapHeight;
        public int tileSpacing = 64;
        DateTime lastAtmosDisplayPush;
        Dictionary<byte, string> tileStringTable = new Dictionary<byte, string>();
        #endregion

        public MapManager()
        {
        }

        #region Startup
        public bool InitMap(string mapName)
        {
            BuildTileTable();
            if (!LoadMap(mapName))
                NewMap();

            InitializeAtmos();
            return true;
        }
        #endregion

        public void BuildTileTable()
        {
            Type type = typeof(Tile);
            List<Assembly> asses = AppDomain.CurrentDomain.GetAssemblies().ToList();
            List<Type> types = asses.SelectMany(t => t.GetTypes()).Where(p => type.IsAssignableFrom(p) && !p.IsAbstract).ToList();

            if (types.Count > 255)
                throw new ArgumentOutOfRangeException("types.Count", "Can not load more than 255 types of tiles.");

            tileStringTable = types.ToDictionary(x => (byte)types.FindIndex(y => y == x), x => x.Name);
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
            MapMessage messageType = (MapMessage)message.ReadByte();
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

        private void DestroyTile(Point arrayPosition)
        {
            if (IsSaneArrayPosition(arrayPosition.X, arrayPosition.Y))
            {
                var t = tileArray[arrayPosition.X, arrayPosition.Y];
                var g = t.gasCell;
                var newTile = GenerateNewTile(arrayPosition.X, arrayPosition.Y, "Floor") as Tile; //Ugly
                tileArray[arrayPosition.X, arrayPosition.Y] = newTile;
                newTile.gasCell = g;
                g.AttachToTile(newTile);
                NetworkUpdateTile(arrayPosition.X, arrayPosition.Y);
            }                
        }

        private void HandleTurfUpdate(NetIncomingMessage message)
        {
            short x = message.ReadInt16();
            short y = message.ReadInt16();

            string typeStr =  GetTileString((byte)message.ReadByte());

            if (IsSaneArrayPosition(x, y))
            {
                Tiles.Atmos.GasCell g = tileArray[x, y].gasCell;
                var t = GenerateNewTile(x, y, typeStr) as Tile;
                tileArray[x, y] = t as Tile;
                tileArray[x, y].gasCell = g;
                g.AttachToTile(t);
                NetworkUpdateTile(x, y);
            }
        }

        public void NetworkUpdateTile(int x, int y)
        {
            if (!IsSaneArrayPosition(x, y))
                return;

            NetOutgoingMessage message = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            message.Write((byte)NetMessage.MapMessage);
            message.Write((byte)MapMessage.TurfUpdate);
            message.Write((short)x);
            message.Write((short)y);
            message.Write((byte)GetTileIndex(tileArray[x, y].GetType().Name));
            message.Write((byte)tileArray[x, y].tileState);
            IoCManager.Resolve<ISS13NetServer>().SendToAll(message);
        }
        #endregion

        #region Map loading/sending
        private bool LoadMap(string filename)
        {
            if (!File.Exists(filename))
                return false;

            FileStream fs = new FileStream(filename, FileMode.Open);
            StreamReader sr = new StreamReader(fs);

            mapWidth = int.Parse(sr.ReadLine());
            mapHeight = int.Parse(sr.ReadLine());

            tileArray = new Tile[mapWidth, mapHeight];

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    string tileName = sr.ReadLine();
                    tileArray[x, y] = (Tile)GenerateNewTile(x, y, tileName);
                }
            }

            sr.Close();
            fs.Close();

            return true;
        }

        public void SaveMap()
        {
            string fileName = "SavedMap";

            FileStream fs = new FileStream(fileName, FileMode.Create);
            StreamWriter sw = new StreamWriter(fs);
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

        private void NewMap()
        {
            LogManager.Log("Cannot find map. Generating blank map.", LogLevel.Warning);
            mapWidth = 50;
            mapHeight = 50;
            tileArray = new Tile[mapWidth, mapHeight];
            for (int x = 0; x < mapWidth; x++)
            {
                for (int y = 0; y < mapHeight; y++)
                {
                    tileArray[x,y] = new Floor(x, y, this);
                }
            }
        }

        public ITile GetTileAt(int x, int y)
        {
            if (!IsSaneArrayPosition(x, y))
                return null;
            return tileArray[x, y];
        }

        #endregion

        #region Atmos

        private void InitializeAtmos()
        {
            for (int z = 0; z < mapHeight; z++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    tileArray[x, z].gasCell = new Tiles.Atmos.GasCell(tileArray[x, z], x, z, tileArray, mapWidth, mapHeight);
                    if (tileArray[x, z].startWithAtmos)
                    {
                        tileArray[x, z].gasCell.AddGas(20, GasType.Oxygen);
                        tileArray[x, z].gasCell.AddGas(80, GasType.Nitrogen);
                    }
                }
            }
        }

        private struct AtmosRecord
        {
            int x;
            int y;
            byte display;

            public AtmosRecord(int _x, int _y, byte _display)
            {
                x = _x;
                y = _y;
                display = _display;
            }

            public void pack(NetOutgoingMessage message)
            {
                message.Write(x);
                message.Write(y);
                message.Write(display);
            }
        }

        public void AddGasAt(Point position, GasType type, int amount)
        {
            tileArray[position.X, position.Y].gasCell.AddGas(amount, type);
        }

        public void UpdateAtmos()
        {
            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                {
                    tileArray[x, y].gasCell.CalculateNextGasAmount();
                }

            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                {
                    if (tileArray[x, y].tileState == TileState.Dead && !tileArray[x, y].gasPermeable)
                        DestroyTile(new Point(x, y));
                    tileArray[x, y].gasCell.Update();
                }

            //Prepare gas display message
            if ((DateTime.Now - lastAtmosDisplayPush).TotalMilliseconds > 1000)
            {
                bool sendUpdate = false;
                //How many types of gasses do we have?
                int numberOfGasTypes = Enum.GetValues(typeof(GasType)).Length;
                var records = new BitStream(mapHeight*mapWidth*numberOfGasTypes);
                for (var x = 0; x < mapWidth; x++)
                    for (var y = 0; y < mapHeight; y++)
                    {
                        int displayBitsWritten = tileArray[x, y].gasCell.PackDisplayBytes(records);
                        //The func will always write the same number of bytes as there are gas types.
                        if(displayBitsWritten > numberOfGasTypes)
                            sendUpdate = true;
                    }

                if (sendUpdate)
                {
                    SendAtmosUpdatePacket(records);
                }
                lastAtmosDisplayPush = DateTime.Now;
            }
        }

        /// <summary>
        /// Compresses byte array to new byte array.
        /// </summary>
        private byte[] Compress(BitStream rawStream)
        {
            rawStream.Position = 0;
            var raw = new byte[rawStream.Length8];
            for (int i = 0; i < rawStream.Length8; i++)
            {
                rawStream.Read(out raw[i]);
            }
            var byteLength = (int)rawStream.Length8;
            if (byteLength == 0)
            {
                LogManager.Log("Gas data unchanged. Not sending that shit.", LogLevel.Debug);
                return null;
            }

            LogManager.Log(byteLength + " bytes of gas data to send before compression.", LogLevel.Debug);
            using (MemoryStream memory = new MemoryStream())
            {
                using (GZipStream gzip = new GZipStream(memory, CompressionMode.Compress, true))
                {
                    gzip.Write(raw, 0, raw.Length);
                }
                return memory.ToArray();
            }
        }

        private NetOutgoingMessage CreateAtmosUpdatePacket(BitStream records)
        {
            var recs = Compress(records);
            if (recs == null)
                return null;
            var msg = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            msg.Write((byte)NetMessage.AtmosDisplayUpdate);
            msg.Write((int)records.Length);
            msg.Write(recs.Length);
            msg.Write(recs);
            return msg;
        }

        private void SendAtmosUpdatePacket(BitStream records)
        {
            var msg = CreateAtmosUpdatePacket(records);
            if (msg == null)
                return;
            LogManager.Log("Sending Gas update packet of size " + msg.LengthBytes + " bytes\n", LogLevel.Debug);
            IoCManager.Resolve<ISS13NetServer>().SendToAll(msg, NetDeliveryMethod.Unreliable);
        }

        private void SendAtmosUpdatePacket(List<AtmosRecord> records)
        {
            int recordsCount = records.Count;
            int recordsInPacket = 0;
            int position = 0;

            /*
            while (recordsCount > 0)
            {
                if (recordsCount >= 50)
                    recordsInPacket = 50;
                else
                    recordsInPacket = recordsCount;
                recordsCount -= recordsInPacket;
                NetOutgoingMessage message = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
                message.Write((byte)NetMessage.AtmosDisplayUpdate);
                message.Write(recordsInPacket);
                for (int i = 0 + position; i < recordsInPacket + position; i++)
                {
                    records[i].pack(message);
                }
                IoCManager.Resolve<ISS13NetServer>().SendToAll(message, NetDeliveryMethod.Unreliable);// Gas updates aren't a big deal.
                LogManager.Log("Sending Gas update with " + recordsInPacket + " records\n", LogLevel.Debug);
                position += recordsInPacket;
            }*/
        }

        public void SendAtmosStateTo(NetConnection client)
        {
            /*NetOutgoingMessage message = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            message.Write((byte)NetMessage.AtmosDisplayUpdate);

            List<AtmosRecord> records = new List<AtmosRecord>();
            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                {
                    byte[] displayBytes = tileArray[x, y].gasCell.PackDisplayBytes(true);
                    if (displayBytes.Length == 0) //if there are no changes, continue.
                        continue;
                    foreach (byte displayByte in displayBytes)
                    {
                        records.Add(new AtmosRecord(x, y, displayByte));
                    }
                }

            message.Write(records.Count);
            foreach (AtmosRecord rec in records)
            {
                rec.pack(message);
            }
            IoCManager.Resolve<ISS13NetServer>().SendMessage(message, client, NetDeliveryMethod.Unreliable);// Gas updates aren't a big deal.
            //LogManager.Log("Sending Gas update to " + SS13Server.Singleton.playerManager.GetSessionByConnection(client).name + "\n", LogLevel.Debug);*/
        
            int numberOfGasTypes = Enum.GetValues(typeof(GasType)).Length;
            var records = new BitStream(mapHeight * mapWidth * numberOfGasTypes);
            int displayBitsWritten = 0;
            for (int x = 0; x < mapWidth; x++)
                for (int y = 0; y < mapHeight; y++)
                {
                    //byte[] displayBytes = tileArray[x, y].gasCell.PackDisplayBytes();
                    displayBitsWritten = tileArray[x, y].gasCell.PackDisplayBytes(records, true);
                }
            var msg = CreateAtmosUpdatePacket(records);
            if (msg == null)
                return;
            IoCManager.Resolve<ISS13NetServer>().SendMessage(msg, client, NetDeliveryMethod.ReliableUnordered);

        }

        /// <summary>
        /// This function takes the gas cell from one tile and moves it to another, reconnecting all of the references in adjacent tiles.
        /// Use this when a new tile is generated at a map location.
        /// </summary>
        /// <param name="fromTile">Tile to move gas information/cell from</param>
        /// <param name="toTile">Tile to move gas information/cell to</param>
        public void MoveGasCell(ITile fromTile, ITile toTile)
        {
            Tiles.Atmos.GasCell g = (fromTile as Tile).gasCell;
            (toTile as Tile).gasCell = g;
            g.AttachToTile((toTile as Tile));
        }

        public float GetGasAmount(Point position, GasType type)
        {
            var t = (Tile)GetTileAt(position.X, position.Y);
            return t.gasCell.GasMixture.gasses[type];
        }

        public float GetGasTotal(Point position)
        {
            var t = (Tile) GetTileAt(position.X, position.Y);
            return t.gasCell.TotalGas;
        }

        public Vector2 GetGasVelocity(Point position)
        {
            var t = (Tile)GetTileAt(position.X, position.Y);
            if(t != null)
                return t.gasCell.GasVel;
            return new Vector2(0, 0);
        }
        #endregion

        #region Map altering
        public bool ChangeTile(int x, int z, string newType)
        {
            if (x < 0 || z < 0)
                return false;

            if (x > mapWidth || z > mapWidth)
                return false;

            Tile tile = GenerateNewTile(x, z, newType) as Tile; //Transfer the gas cell from the old tile to the new tile.

            MoveGasCell(tileArray[x, z], tile);

            tileArray[x, z] = tile;
            return true;
        }

        public bool ChangeTile(int x, int z, Type newType)
        {
            if (x < 0 || z < 0)
                return false;

            if (x > mapWidth || z > mapWidth)
                return false;

            object[] args = new object[3];
            args[0] = x;
            args[1] = z;
            args[2] = this;
            object newTile = Activator.CreateInstance(newType, args);
            Tile castTile = (Tile)newTile;

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

            return (ITile)Activator.CreateInstance(tileType, x, y, this);
        }
        #endregion

        #region networking
        public NetOutgoingMessage CreateMapMessage(MapMessage messageType)
        {
            NetOutgoingMessage message = IoCManager.Resolve<ISS13NetServer>().CreateMessage();
            message.Write((byte)NetMessage.MapMessage);
            message.Write((byte)messageType);
            return message;
        }

        public void SendTileIndex(NetConnection connection)
        {
            var mapMessage = CreateMapMessage(MapMessage.SendTileIndex);

            mapMessage.Write((byte)tileStringTable.Count);

            foreach (var curr in tileStringTable)
            {
                mapMessage.Write(curr.Key);
                mapMessage.Write(curr.Value);
            }

            IoCManager.Resolve<ISS13NetServer>().SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
        }

        public void SendMap(NetConnection connection)
        {
            SendTileIndex(connection); //Send index of byte -> str to save space.

            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map");
            var mapMessage = CreateMapMessage(MapMessage.SendTileMap);

            var mapWidth = GetMapWidth();
            var mapHeight = GetMapHeight();

            mapMessage.Write(mapWidth);
            mapMessage.Write(mapHeight);

            for (var x = 0; x < mapWidth; x++)
            {
                for (var y = 0; y < mapHeight; y++)
                {
                    var t = tileArray[x, y]; //Bypassing stuff.
                    mapMessage.Write((byte)GetTileIndex((t.GetType().Name)));
                    mapMessage.Write((byte)t.tileState);
                }
            }

            IoCManager.Resolve<ISS13NetServer>().SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map finished with message size: " + mapMessage.LengthBytes + " bytes");
        }

        /// <summary>
        /// Send message to all clients.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(NetOutgoingMessage message)
        {
            IoCManager.Resolve<ISS13NetServer>().SendToAll(message);
        }
        #endregion

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


        #region Tile helper function
        public Point GetTileArrayPositionFromWorldPosition(float x, float z)
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

        public bool IsWorldPositionInBounds(Vector2 pos)
        {
            var tpos = GetTileArrayPositionFromWorldPosition(pos);
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

        private Type GetTileTypeFromWorldPosition(Vector2 pos)
        {
            Point arrayPosition = GetTileArrayPositionFromWorldPosition(pos.X, pos.Y);
            if (arrayPosition.Y < 0 || arrayPosition.Y < 0)
            {
                return null;
            }
            else
            {
                return GetObjectTypeFromArrayPosition((int)arrayPosition.X, (int)arrayPosition.Y);
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

        public bool IsSaneArrayPosition(int x, int y)
        {
            if (x < 0 || y < 0)
                return false;
            if (x > mapWidth - 1|| y > mapWidth - 1)
                return false;
            return true;
        }
        #endregion

    }
}
