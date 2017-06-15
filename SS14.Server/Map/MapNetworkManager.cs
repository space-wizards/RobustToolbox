using System;
using Lidgren.Network;
using SS14.Server.Interfaces.Network;
using SS14.Shared;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;

namespace SS14.Server.Map
{
    [IoCTarget]
    public class MapNetworkManager : IMapNetworkManager
    {
        public MapNetworkManager()
        {
            IoCManager.Resolve<IMapManager>().TileChanged += MapManagerOnTileChanged;
        }

        ~MapNetworkManager()
        {
            IoCManager.Resolve<IMapManager>().TileChanged -= MapManagerOnTileChanged;
        }

        private void MapManagerOnTileChanged(TileRef tileRef, Tile oldTile)
        {
            throw new NotImplementedException();
        }

        public static NetOutgoingMessage CreateMapMessage(MapMessage messageType)
        {
            NetOutgoingMessage message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            message.Write((byte)NetMessage.MapMessage);
            message.Write((byte)messageType);
            return message;
        }

        public void SendMap(IMapManager mapManager, NetConnection connection)
        {
            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map");
            NetOutgoingMessage mapMessage = CreateMapMessage(MapMessage.SendTileMap);

            mapMessage.Write((int)1); // Format version.  Who knows, it could come in handy.

            // Tile definition mapping
            var tileDefManager = IoCManager.Resolve<ITileDefinitionManager>();
            mapMessage.Write((int)tileDefManager.Count);
            for (int tileId = 0; tileId < tileDefManager.Count; ++tileId)
                mapMessage.Write((string)tileDefManager[tileId].Name);

            // Map chunks
            mapMessage.Write((int)mapManager.Chunks.Count);
            foreach (var chunk in mapManager.Chunks)
            {
                mapMessage.Write((int)chunk.Key.X);
                mapMessage.Write((int)chunk.Key.Y);

                foreach (var tile in chunk.Value.Tiles)
                    mapMessage.Write((uint)tile);
            }

            IoCManager.Resolve<ISS14NetServer>().SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map finished with message size: " +
                           mapMessage.LengthBytes + " bytes");
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

        public static void NetworkUpdateTile(TileRef tile)
        {
            NetOutgoingMessage message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            message.Write((byte)NetMessage.MapMessage);
            message.Write((byte)MapMessage.TurfUpdate);

            message.Write((int)tile.X);
            message.Write((int)tile.Y);
            message.Write((uint)tile.Tile);
            IoCManager.Resolve<ISS14NetServer>().SendToAll(message);
        }
        
        public void HandleNetworkMessage(IMapManager mapManager, NetIncomingMessage message)
        {
            var messageType = (MapMessage)message.ReadByte();
            switch (messageType)
            {
                case MapMessage.TurfClick:
                    //HandleTurfClick(message);
                    break;
                default:
                    break;
            }
        }
    }
}
