using Lidgren.Network;
using SS14.Server.Interfaces.Map;
using SS14.Server.Interfaces.Network;
using SS14.Server.Interfaces.Tiles;
using SS14.Server.Services.Atmos;
using SS14.Server.Services.Log;
using SS14.Server.Services.Tiles;
using SS14.Shared;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
using SS14.Shared.ServerEnums;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;

namespace SS14.Server.Services.Map
{
    public class MapManager : IMapManager
    {
        #region Variables

        private DateTime lastAtmosDisplayPush;
        private int mapHeight;
        private int mapWidth;
        public int tileSpacing = 64;
        private const int wallThickness = 24;
        private Dictionary<byte, string> tileStringTable = new Dictionary<byte, string>();
        private QuadTree<Tile> _groundArray;
        private QuadTree<Tile> _wallArray;
        private RectangleF worldArea;

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
            if (fromTile == null)
                return;
            GasCell g = (fromTile as Tile).gasCell;
            (toTile as Tile).gasCell = g;
            g.AttachToTile((toTile as Tile));
        }

        public void Shutdown()
        {
            //ServiceManager.Singleton.RemoveService(this);
            _groundArray = null;
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

        public int GetTileSpacing()
        {
            return tileSpacing;
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
            return GetTypeAt(Type.GetType("ServerServices.Tiles." + type, false), pos);
        }


        #endregion

        #region Map altering

        public Tile ChangeTile(Vector2 pos, string newType, Direction dir = Direction.North)
        {
            var tile = GenerateNewTile(pos, newType, dir) as Tile;
            if (tile == null)
                return null;
            //Transfer the gas cell from the old tile to the new tile.
            Tile t = (Tile)GetTypeAt(newType, pos);
            if (t != null && t.GasPermeable && tile.GasPermeable)
            {
                MoveGasCell(t, tile);
            }
            else
            {
                tile.GasCell = new GasCell((Tile)tile);
            }

            RemoveTile(t);
            AddTile(tile);
            UpdateTile(tile);
            return tile;
        }

        public ITile GenerateNewTile(Vector2 pos, string typeName, Direction dir = Direction.North)
        {
            Type tileType = Type.GetType("ServerServices.Tiles." + typeName, false);

            if (tileType == null) throw new ArgumentException("Invalid Tile Type specified : '" + typeName + "' .");
            RectangleF rect = new RectangleF();

            Tile t = (Tile)GetTypeAt(tileType, pos);
            Tile tile = null;
            if (t != null && t.dir == dir)
            {
                t.RaiseChangedEvent(tileType);
                rect = t.Bounds;
            }
            else
            {
                if (typeName != "Wall")
                {
                    rect = new RectangleF(pos.X, pos.Y, tileSpacing, tileSpacing);
                    tile = (Tile)Activator.CreateInstance(tileType, rect, this);
                }
                else
                {
                    if (dir == Direction.North) // NS (vertical) wall
                    {
                        rect = new RectangleF(pos.X, pos.Y, wallThickness, tileSpacing);
                    }
                    else // EW (horizontal) wall
                    {
                        rect = new RectangleF(pos.X, pos.Y, tileSpacing, wallThickness);
                    }
                    tile = (Tile)Activator.CreateInstance(tileType, rect, this, dir);
                }
            }


            return (ITile)tile;
        }


        #endregion

        #region networking

        public NetOutgoingMessage CreateMapMessage(MapMessage messageType)
        {
            NetOutgoingMessage message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
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

            foreach (Tile t in GetAllTilesIn(new Rectangle(0, 0, mapWidth * tileSpacing, mapHeight * tileSpacing)))
            {
                mapMessage.Write(t.WorldPosition.X);
                mapMessage.Write(t.WorldPosition.Y);
                mapMessage.Write(GetTileIndex((t.GetType().Name)));
                mapMessage.Write((byte)t.TileState);
                if (t.GetType().Name == "Wall") mapMessage.Write((byte)t.dir);
            }

            IoCManager.Resolve<ISS14NetServer>().SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map finished with message size: " +
                           mapMessage.LengthBytes + " bytes");
        }

        /// <summary>
        /// Send message to all clients.
        /// </summary>
        /// <param name="message"></param>
        public void SendMessage(NetOutgoingMessage message)
        {
            IoCManager.Resolve<ISS14NetServer>().SendToAll(message);
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

            IoCManager.Resolve<ISS14NetServer>().SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
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

        public void DestroyTile(ITile t)
        {
            if (RemoveTile((Tile)t))
            {
                NetworkUpdateTile((Tile)t);
                UpdateTile((Tile)t);
            }
        }

        private void AddTile(Tile t)
        {
            if (t.GetType().Name == "Wall")
            {
                _wallArray.Insert(t);
            }
            else
            {
                _groundArray.Insert(t);
            }
        }

        private bool RemoveTile(Tile t)
        {
            if (t == null)
                return false;
            if (t.GetType().Name == "Wall")
            {
                _wallArray.Remove(t);
            }
            else
            {
                _groundArray.Remove(t);
            }

            return true;
        }

        public void UpdateTile(Tile t)
        {
            if (t == null || t.gasCell == null)
                return;
            t.gasCell.SetNeighbours(this);
            
            foreach (Tile u in GetAllTilesIn(new RectangleF(t.WorldPosition.X - tileSpacing, t.WorldPosition.Y - tileSpacing, tileSpacing * 2, tileSpacing * 2)))
            {
                u.gasCell.SetNeighbours(this);
            }

        }


        public void NetworkUpdateTile(ITile t)
        {
            if (t == null)
                return;
            NetOutgoingMessage message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            message.Write((byte) NetMessage.MapMessage);
            message.Write((byte) MapMessage.TurfUpdate);
            message.Write(t.WorldPosition.X);
            message.Write(t.WorldPosition.Y);
            message.Write(GetTileIndex(t.GetType().Name));
            message.Write((byte) t.TileState);
            if (t.GetType().Name == "Wall") message.Write((byte)t.dir);
            IoCManager.Resolve<ISS14NetServer>().SendToAll(message);
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

            foreach (Tile t in GetAllTilesIn(new Rectangle(0, 0, mapWidth * tileSpacing, mapHeight * tileSpacing)))
            {
                sw.WriteLine(t.WorldPosition.X);
                sw.WriteLine(t.WorldPosition.Y);
                sw.WriteLine(GetTileIndex(t.GetType().Name));
            }

            LogManager.Log("Done saving map.");

            sw.Close();
            fs.Close();
        }

        private Rectangle TilePos(Tile T)
        {
            return new Rectangle((int)(T.WorldPosition.X), (int)(T.WorldPosition.Y), (int)(tileSpacing), (int)(tileSpacing));
        }

        public RectangleF GetWorldArea()
        {
            return worldArea;
        }

        private bool LoadMap(string filename)
        {
            if (!File.Exists(filename))
                return false;

            var fs = new FileStream(filename, FileMode.Open);
            var sr = new StreamReader(fs);

            mapWidth = int.Parse(sr.ReadLine());
            mapHeight = int.Parse(sr.ReadLine());

            worldArea = new RectangleF(0, 0, mapWidth * tileSpacing, mapHeight * tileSpacing);

            _groundArray = new QuadTree<Tile>(new SizeF(tileSpacing * 2f, tileSpacing * 2f), 4);
            _wallArray = new QuadTree<Tile>(new SizeF(tileSpacing * 2f, tileSpacing * 2f), 4);


            while (!sr.EndOfStream)
            {
                float x = float.Parse(sr.ReadLine());
                float y = float.Parse(sr.ReadLine());
                byte i = byte.Parse(sr.ReadLine());

                AddTile((Tile)GenerateNewTile(new Vector2(x, y), GetTileString(i)));
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
            _groundArray = new QuadTree<Tile>(new SizeF(tileSpacing * 2f, tileSpacing * 2f), 4);
            _wallArray = new QuadTree<Tile>(new SizeF(tileSpacing * 2f, tileSpacing * 2f), 4);

            worldArea = new RectangleF(0, 0, mapWidth * tileSpacing, mapHeight * tileSpacing);

            for (int y = 0; y < mapHeight; y++)
            {
                for (int x = 0; x < mapWidth; x++)
                {
                    AddTile(new Floor(new RectangleF(x * tileSpacing, y * tileSpacing, tileSpacing, tileSpacing), this));
                }
            }
        }

        #endregion
    }
}