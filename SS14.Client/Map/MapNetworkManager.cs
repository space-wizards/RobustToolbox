using Lidgren.Network;
using SS14.Shared;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace SS14.Client.Map
{
    /// <summary>
    ///     This handles all translations between network messages and the MapManager on the client
    ///     side. This class is instantiated through the IoC Resource Locator.
    ///     TODO: This is a temporary class. Once the Client and Server NetworkManagers are merged
    ///     to a unified shared version, this class should be merged with its twin in SS14.Server.
    /// </summary>
    public class MapNetworkManager : IMapNetworkManager
    {
        /// <summary>
        ///     The accepted version of the NetworkMessage map format.
        /// </summary>
        private const int MAP_VERSION = 1;

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
                case MapMessage.TurfUpdate:
                    HandleTileUpdate(mapManager, message);
                    break;
                case MapMessage.SendTileMap:
                    var tileDefMgr = IoCManager.Resolve<ITileDefinitionManager>();
                    HandleTileMap(mapManager, tileDefMgr, message);
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
            // not implemented client side
        }

        /// <summary>
        ///     Deserializes an IMapManager and ITileDefinitionManager from a properly formatted NetMessage.
        /// </summary>
        /// <param name="mapManager">The target MapManager to deserialize the message into.</param>
        /// <param name="tileDefMgr">The TileDefManager to deserialize the message into.</param>
        /// <param name="message">The message containing a serialized map and tileDefines.</param>
        private static void HandleTileMap(IMapManager mapManager, ITileDefinitionManager tileDefMgr, NetIncomingMessage message)
        {
            var version = message.ReadInt32();
            if (version != MAP_VERSION)
                return;

            tileDefMgr.RegisterServerTileMapping(message);

            //TODO: This should be a part of the network message, so that multiple maps(z-levels) are possible.
            const int MAP_INDEX = 0;

            var chunkCount = message.ReadInt32();
            for (var i = 0; i < chunkCount; ++i)
            {
                var x = message.ReadInt32();
                var y = message.ReadInt32();
                var chunkPos = new MapGrid.Indices(x, y);

                var chunk = mapManager.GetGrid(MAP_INDEX).GetChunk(chunkPos);
                HandleChunkData(chunk, message);
            }
        }

        /// <summary>
        ///     Deserializes a chunk.
        /// </summary>
        /// <param name="chunk">The chunk to deserialize.</param>
        /// <param name="message">The NetMessage to read from.</param>
        private static void HandleChunkData(IMapChunk chunk, NetBuffer message)
        {
            for (ushort x = 0; x < chunk.ChunkSize; x++)
            for (ushort y = 0; y < chunk.ChunkSize; y++)
                chunk.SetTile(x, y, (Tile) message.ReadUInt32());
        }

        /// <summary>
        ///     Updates a single tile from the network message.
        /// </summary>
        /// <param name="mapManager">The target MapManager to update.</param>
        /// <param name="message">The message containing the info.</param>
        private static void HandleTileUpdate(IMapManager mapManager, NetBuffer message)
        {
            var x = message.ReadInt32();
            var y = message.ReadInt32();
            var tile = (Tile) message.ReadUInt32();

            //TODO: This should be a part of the network message, so that multiple maps(z-levels) are possible.
            const int MAP_INDEX = 0;

            mapManager.GetGrid(MAP_INDEX).SetTile(x, y, tile);
        }
    }
}
