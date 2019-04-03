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
        public readonly MapIndices GridTile;
        private readonly Tile _tile;

        internal TileRef(MapId argMap, GridId gridIndex, int xIndex, int yIndex, Tile tile)
        {
            MapIndex = argMap;
            GridTile = new MapIndices(xIndex, yIndex);
            GridIndex = gridIndex;
            _tile = tile;
        }

        internal TileRef(MapId argMap, GridId gridIndex, MapIndices gridTile, Tile tile)
        {
            MapIndex = argMap;
            GridTile = gridTile;
            GridIndex = gridIndex;
            _tile = tile;
        }

        public int X => GridTile.X;
        public int Y => GridTile.Y;
        public GridCoordinates LocalPos => IoCManager.Resolve<IMapManager>().GetMap(MapIndex).GetGrid(GridIndex).GridTileToLocal(GridTile);
        public ushort TileSize => IoCManager.Resolve<IMapManager>().GetMap(MapIndex).GetGrid(GridIndex).TileSize;
        public Tile Tile
        {
            get => _tile;
            set
            {
                IMapGrid grid = IoCManager.Resolve<IMapManager>().GetMap(MapIndex).GetGrid(GridIndex);
                grid.SetTile(new GridCoordinates(GridTile.X, GridTile.Y, grid), value);
            }
        }

        public ITileDefinition TileDef => IoCManager.Resolve<ITileDefinitionManager>()[Tile.TileId];

        /// <inheritdoc />
        public override string ToString()
        {
            return $"TileRef: {X},{Y} ({Tile})";
        }

        public bool GetStep(Direction dir, out TileRef steptile)
        {
            MapIndices currenttile = GridTile;
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
