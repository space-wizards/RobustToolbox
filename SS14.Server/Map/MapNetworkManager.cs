using Lidgren.Network;
using SS14.Server.Interfaces.Network;
using SS14.Shared;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Log;
using SS14.Shared.Map;

namespace SS14.Server.Map
{
    /// <summary>
    ///     This handles all translations between network messages and the MapManager on the server
    ///     side. This class is instantiated through the IoC Resource Locator.
    ///     TODO: This is a temporary class. Once the Client and Server NetworkManagers are merged
    ///     to a unified shared version, this class should be merged with its twin in SS14.Client.
    /// </summary>
    [IoCTarget]
    public class MapNetworkManager : IMapNetworkManager
    {
        /// <summary>
        ///     The accepted version of the network message map format.
        /// </summary>
        private const int MAP_VERSION = 1;

        /// <summary>
        ///     Default private constructor.
        /// </summary>
        public MapNetworkManager()
        {
            IoCManager.Resolve<IMapManager>().OnTileChanged += MapMgrOnTileChanged;
        }

        /// <summary>
        ///     Handles and processes a incoming network message.
        /// </summary>
        /// <param name="mapManager">The mapManager to apply to message to.</param>
        /// <param name="message">The message to handle.</param>
        public void HandleNetworkMessage(IMapManager mapManager, NetIncomingMessage message)
        {
            var messageType = (MapMessage) message.ReadByte();
            switch (messageType)
            {
                case MapMessage.TurfClick:
                    //HandleTurfClick(message);
                    break;
            }
        }

        /// <summary>
        ///     Serializes the MapManager and TileDefMgr into an outgoing message to send to a client.
        /// </summary>
        /// <param name="mapManager">The mapManager to apply to message to.</param>
        /// <param name="connection">The connection to make the message on.</param>
        public void SendMap(IMapManager mapManager, NetConnection connection)
        {
            //TODO: This should be a part of the network message, so that multiple maps(z-levels) are possible.
            const int MAP_INDEX = 0;

            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map");
            var mapMessage = CreateMapMessage(MapMessage.SendTileMap);

            mapMessage.Write(MAP_VERSION); // Format version.  Who knows, it could come in handy.

            // Tile definition mapping
            var tileDefManager = IoCManager.Resolve<ITileDefinitionManager>();
            mapMessage.Write(tileDefManager.Count);

            foreach (var tileDef in tileDefManager)
                mapMessage.Write(tileDef.Name);

            // Map chunks
            var grid = mapManager.GetGrid(MAP_INDEX);
            mapMessage.Write(grid.ChunkCount);
            foreach (var chunk in grid.GetMapChunks())
            {
                mapMessage.Write(chunk.X);
                mapMessage.Write(chunk.Y);

                foreach (var tile in chunk)
                    mapMessage.Write((uint) tile.Tile);
            }

            IoCManager.Resolve<ISS14NetServer>().SendMessage(mapMessage, connection, NetDeliveryMethod.ReliableOrdered);
            LogManager.Log(connection.RemoteEndPoint.Address + ": Sending map finished with message size: " +
                           mapMessage.LengthBytes + " bytes");
        }

        /// <summary>
        ///     Default finalizer.
        /// </summary>
        ~MapNetworkManager()
        {
            IoCManager.Resolve<IMapManager>().OnTileChanged -= MapMgrOnTileChanged;
        }

        /// <summary>
        ///     Event handler for when a tile is modified in the MapManager.
        /// </summary>
        /// <param name="tileRef">A reference to the new tile.</param>
        /// <param name="oldTile">The old tile being modified.</param>
        private static void MapMgrOnTileChanged(int gridId, TileRef tileRef, Tile oldTile)
        {
            var netMgr = IoCManager.Resolve<ISS14NetServer>();

            var message = netMgr.CreateMessage();
            message.Write((byte) NetMessage.MapMessage);
            message.Write((byte) MapMessage.TurfUpdate);

            message.Write(tileRef.X);
            message.Write(tileRef.Y);
            message.Write((uint) tileRef.Tile);

            netMgr.SendToAll(message);
        }

        /// <summary>
        ///     Creates an empty MapMessage.
        /// </summary>
        /// <param name="messageType">The type of message to create.</param>
        /// <returns></returns>
        private static NetOutgoingMessage CreateMapMessage(MapMessage messageType)
        {
            var message = IoCManager.Resolve<ISS14NetServer>().CreateMessage();
            message.Write((byte) NetMessage.MapMessage);
            message.Write((byte) messageType);
            return message;
        } // TODO HOOK ME BACK UP WITH ENTITY SYSTEM


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
        }*/
    }
}
