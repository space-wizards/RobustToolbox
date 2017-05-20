using Lidgren.Network;

namespace SS14.Client.Interfaces.Map
{
    public class Chunk
    {
        public static readonly int ChunkSize = 16;

        public uint Version { get; private set; }
        public void IncrementVersion() { unchecked { ++Version; } }

        public Tile[] Tiles { get; private set; }

        public Chunk()
        {
            Version = 0;
            Tiles = new Tile[ChunkSize * ChunkSize];
        }

        public void ReceiveChunkData(NetIncomingMessage message)
        {
            for (int i = 0; i < Tiles.Length; ++i)
            {
                Tile tile = (Tile)message.ReadUInt32();
                Tiles[i] = tile;
            }
        }
    }
}
