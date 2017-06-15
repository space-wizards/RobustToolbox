using Lidgren.Network;

namespace SS14.Shared.Map
{
    /// <summary>
    /// A square section of the map.
    /// </summary>
    public class Chunk
    {
        /// <summary>
        /// The number of tiles per dimension of the chunk.
        /// </summary>
        public const int CHUNK_SIZE = 16;

        public uint Version { get; private set; }
        public void IncrementVersion() { unchecked { ++Version; } }

        public Tile[] Tiles { get; private set; }

        public Chunk()
        {
            Version = 0;
            Tiles = new Tile[CHUNK_SIZE * CHUNK_SIZE];
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
