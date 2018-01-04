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
        public readonly int MapIndex;
        public readonly int GridIndex;
        private readonly Tile _tile;
        private readonly MapGrid.Indices _gridTile;

        internal TileRef(int argMap, int gridIndex, int xIndex, int yIndex, Tile tile)
        {
            MapIndex = argMap;
            _gridTile = new MapGrid.Indices(xIndex, yIndex);
            GridIndex = gridIndex;
            _tile = tile;
        }

        internal TileRef(int argMap, int gridIndex, MapGrid.Indices gridTile, Tile tile)
        {
            MapIndex = argMap;
            _gridTile = gridTile;
            GridIndex = gridIndex;
            _tile = tile;
        }

        public int X => _gridTile.X;
        public int Y => _gridTile.Y;
        public LocalCoordinates LocalPos => IoCManager.Resolve<IMapManager>().GetMap(MapIndex).GetGrid(GridIndex).GridTileToLocal(_gridTile);
        public ushort TileSize => IoCManager.Resolve<IMapManager>().GetMap(MapIndex).GetGrid(GridIndex).TileSize;
        public Tile Tile
        {
            get => _tile;
            set
            {
                IMapGrid grid = IoCManager.Resolve<IMapManager>().GetMap(MapIndex).GetGrid(GridIndex);
                grid.SetTile(new LocalCoordinates(_gridTile.X, _gridTile.Y, grid), value);
            }
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
            return IoCManager.Resolve<IMapManager>().GetMap(MapIndex).GetGrid(GridIndex).IndicesToTile(currenttile, out steptile);
        }
    }
}
