using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Maths;
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
        public LocalCoordinates LocalPos => _manager.GetGrid(_gridIndex).GridTileToLocal(_gridTile);
        public WorldCoordinates WorldPos => _manager.GetGrid(_gridIndex).GridTileToWorld(_gridTile);
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

        public bool GetStep(Direction dir, out TileRef steptile)
        {
            MapGrid.Indices currenttile = _gridTile;
            MapGrid.Indices shift;
            switch (dir)
            {
                case Direction.East:
                    shift = new MapGrid.Indices(1, 0);
                    break;
                case Direction.West:
                    shift = new MapGrid.Indices(-1, 0);
                    break;
                case Direction.North:
                    shift = new MapGrid.Indices(0, 1);
                    break;
                case Direction.South:
                    shift = new MapGrid.Indices(0, -1);
                    break;
                case Direction.NorthEast:
                    shift = new MapGrid.Indices(1, 1);
                    break;
                case Direction.SouthEast:
                    shift = new MapGrid.Indices(1, -1);
                    break;
                case Direction.NorthWest:
                    shift = new MapGrid.Indices(-1, 1);
                    break;
                case Direction.SouthWest:
                    shift = new MapGrid.Indices(-1, -1);
                    break;
                default:
                    steptile = new TileRef();
                    return false;
            }
            currenttile += shift;
            return _manager.GetGrid(_gridIndex).IndicesToTile(currenttile, out steptile);
        }
    }
}
