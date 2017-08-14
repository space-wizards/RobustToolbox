using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using Vector2f = OpenTK.Vector2;

namespace SS14.Shared.Map
{
    /// <summary>
    /// A reference to a tile.
    /// </summary>
    public struct TileRef
    {
        private readonly MapManager _manager;
        private readonly int _gridIndex;
        private readonly Tile _tile;
        private readonly MapGrid.Indices _gridTile;
        
        internal TileRef(MapManager manager, int gridIndex, int xIndex, int yIndex, Tile tile)
        {
            _manager = manager;
            _gridTile = new MapGrid.Indices(xIndex, yIndex);
            _gridIndex = gridIndex;
            _tile = tile;
        }

        internal TileRef(MapManager manager, int gridIndex, MapGrid.Indices gridTile, Tile tile)
        {
            _manager = manager;
            _gridTile = gridTile;
            _gridIndex = gridIndex;
            _tile = tile;
        }

        public int X => _gridTile.X;
        public int Y => _gridTile.Y;
        public Vector2f LocalPos => _manager.GetGrid(_gridIndex).GridTileToLocal(_gridTile);
        public Vector2f WorldPos => _manager.GetGrid(_gridIndex).GridTileToWorld(_gridTile);
        public ushort TileSize => _manager.TileSize;
        public Tile Tile
        {
            get => _tile;
            set => _manager.GetGrid(_gridIndex).SetTile(_gridTile.X, _gridTile.Y, value);
        }

        public ITileDefinition TileDef => IoCManager.Resolve<ITileDefinitionManager>()[Tile.TileId];

        /// <inheritdoc />
        public override string ToString()
        {
            return $"TileRef: {X},{Y}";
        }
    }
}
