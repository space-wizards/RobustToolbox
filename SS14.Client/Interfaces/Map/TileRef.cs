
namespace SS14.Client.Interfaces.Map
{
    [System.Diagnostics.DebuggerDisplay("TileRef: {X},{Y}")]
    public struct TileRef
    {
        private readonly IMapManager map; // Instance field since there may be multiple grids later.
        private readonly int x;           // Make static if this ends up never happening. ~Yota
        private readonly int y;

        private readonly Chunk chunk;
        private readonly int index;

        public TileRef(IMapManager map, int x, int y) : this(map, x, y, null, 0) { }
        public TileRef(IMapManager map, int x, int y, Chunk chunk, int index)
        {
            this.map = map;
            this.x = x;
            this.y = y;
            this.chunk = chunk;
            this.index = index;
        }

        public int X { get { return x; } }
        public int Y { get { return y; } }
        public Tile Tile
        {
            get
            {
                if (chunk == null)
                {
                    this = map.GetTileRef(x, y);
                    if (chunk == null)
                        return map.Tiles[x, y];
                }

                return chunk.Tiles[index];
            }
            set { map.Tiles[x, y] = value; }
        }
    }
}
