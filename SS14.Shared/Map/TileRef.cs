
namespace SS14.Shared.Map
{
    [System.Diagnostics.DebuggerDisplay("TileRef: {X},{Y}")]
    public struct TileRef
    {
        private readonly MapManager manager;
        private readonly uint mapIndex;
        private readonly Chunk chunk;
        private readonly uint x;           
        private readonly uint y;
        
        internal TileRef(MapManager manager, uint mapIndex, Chunk chunk, uint x, uint y)
        {
            this.manager = manager;
            this.x = x;
            this.y = y;
            this.chunk = chunk;
            this.mapIndex = mapIndex;
        }

        public uint X { get { return x; } }
        public uint Y { get { return y; } }
        public uint TileSize => manager.TileSize;
        public Tile Tile
        {
            get => chunk.Tiles[x,y];
            set => chunk.SetTile(x, y, value);
        }

        /*
        public TileRef North { get { return new TileRef(manager, x, y - 1); } }
        public TileRef South { get { return new TileRef(manager, x, y + 1); } }
        public TileRef East { get { return new TileRef(manager, x + 1, y); } }
        public TileRef West { get { return new TileRef(manager, x - 1, y); } }
        */
    }
}
