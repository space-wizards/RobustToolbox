namespace SS14.Server.Interfaces.Map
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
    }
}
