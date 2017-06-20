
using System.Diagnostics;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.IoC;

namespace SS14.Shared.Map
{
    /// <summary>
    /// A reference to a tile.
    /// </summary>
    [DebuggerDisplay("TileRef: {X},{Y}")]
    public struct TileRef
    {
        private readonly MapManager _manager;
        private readonly int _gridIndex;
        private readonly int _x;           
        private readonly int _y;
        private readonly Tile _tile;
        
        internal TileRef(MapManager manager, int gridIndex,int xIndex, int yIndex, Tile tile)
        {
            _manager = manager;
            _x = xIndex;
            _y = yIndex;
            _gridIndex = gridIndex;
            _tile = tile;
        }

        public int X => _x;
        public int Y => _y;
        public ushort TileSize => _manager.TileSize;
        public Tile Tile
        {
            get => _tile;
            set => _manager.GetGrid(_gridIndex).SetTile(_x, _y, value);
        }

        public ITileDefinition TileDef => IoCManager.Resolve<ITileDefinitionManager>()[Tile.TileId];
    }
}
