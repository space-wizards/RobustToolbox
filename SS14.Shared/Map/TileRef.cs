using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;
using SS14.Shared.Maths;

namespace SS14.Shared.Map
{
    /// <summary>
    /// A reference to a tile.
    /// </summary>
    public struct TileRef
    {
        public readonly MapId MapIndex;
        public readonly GridId GridIndex;
        private readonly Tile _tile;
        private readonly MapIndices _gridTile;

        internal TileRef(MapId argMap, GridId gridIndex, int xIndex, int yIndex, Tile tile)
        {
            MapIndex = argMap;
            _gridTile = new MapIndices(xIndex, yIndex);
            GridIndex = gridIndex;
            _tile = tile;
        }

        internal TileRef(MapId argMap, GridId gridIndex, MapIndices gridTile, Tile tile)
        {
            MapIndex = argMap;
            _gridTile = gridTile;
            GridIndex = gridIndex;
            _tile = tile;
        }

        public int X => _gridTile.X;
        public int Y => _gridTile.Y;
        public GridLocalCoordinates LocalPos => IoCManager.Resolve<IMapManager>().GetMap(MapIndex).GetGrid(GridIndex).GridTileToLocal(_gridTile);
        public ushort TileSize => IoCManager.Resolve<IMapManager>().GetMap(MapIndex).GetGrid(GridIndex).TileSize;
        public Tile Tile
        {
            get => _tile;
            set
            {
                IMapGrid grid = IoCManager.Resolve<IMapManager>().GetMap(MapIndex).GetGrid(GridIndex);
                grid.SetTile(new GridLocalCoordinates(_gridTile.X, _gridTile.Y, grid), value);
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
            MapIndices currenttile = _gridTile;
            MapIndices shift;
            switch (dir)
            {
                case Direction.East:
                    shift = new MapIndices(1, 0);
                    break;
                case Direction.West:
                    shift = new MapIndices(-1, 0);
                    break;
                case Direction.North:
                    shift = new MapIndices(0, 1);
                    break;
                case Direction.South:
                    shift = new MapIndices(0, -1);
                    break;
                case Direction.NorthEast:
                    shift = new MapIndices(1, 1);
                    break;
                case Direction.SouthEast:
                    shift = new MapIndices(1, -1);
                    break;
                case Direction.NorthWest:
                    shift = new MapIndices(-1, 1);
                    break;
                case Direction.SouthWest:
                    shift = new MapIndices(-1, -1);
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
