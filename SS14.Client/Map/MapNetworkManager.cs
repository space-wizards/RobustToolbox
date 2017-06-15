using Lidgren.Network;
using SFML.System;
using SS14.Shared;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Map;

namespace SS14.Client.Map
{
    [IoCTarget]
    public class MapNetworkManager : IMapNetworkManager
    {
        public void HandleNetworkMessage(IMapManager mapManager, NetIncomingMessage message)
        {
            var messageType = (MapMessage)message.ReadByte();
            switch (messageType)
            {
                case MapMessage.TurfUpdate:
                    HandleTurfUpdate(message);
                    break;
                case MapMessage.SendTileMap:
                    LoadTileMap(mapManager, message);
                    break;
                case MapMessage.TurfClick:
                        break;

            }
        }

        public void SendMap(IMapManager mapManager, NetConnection connection)
        {
            // not implemented clientside
        }

        private static void LoadTileMap(IMapManager mapManager, NetIncomingMessage message)
        {
            int version = message.ReadInt32();
            if (version != 1)
                return;

            var tileDefMgr = IoCManager.Resolve<ITileDefinitionManager>();
            tileDefMgr.RegisterServerTileMapping(message);

            int chunkCount = message.ReadInt32();
            for (int i = 0; i < chunkCount; ++i)
            {
                int x = message.ReadInt32();
                int y = message.ReadInt32();
                var chunkPos = new Vector2i(x, y);

                Chunk chunk;
                if (!mapManager.Chunks.TryGetValue(chunkPos, out chunk))
                    mapManager.Chunks[chunkPos] = chunk = new Chunk();

                chunk.ReceiveChunkData(message);
            }

            MapRenderer.RebuildSprites(tileDefMgr);
        }

        private static void HandleTurfUpdate(NetIncomingMessage message)
        {
            var mapManager = IoCManager.Resolve<IMapManager>();
            int x = message.ReadInt32();
            int y = message.ReadInt32();
            Tile tile = (Tile)message.ReadUInt32();

            mapManager.Tiles[x, y] = tile;
        }
    }
}
