using System.Collections;
using System.Collections.Generic;
using SS14.Shared.Interfaces.Map;

namespace SS14.Shared.Map
{
    /// <summary>
    /// A square section of the map.
    /// </summary>
    public class Chunk : IMapChunk
    {
        private const int CHUNK_VERSION = 1;

        public uint Size { get; }
        public uint Version => CHUNK_VERSION;
        public int X { get; }
        public int Y { get; }

        public readonly Tile[,] Tiles;

        private MapManager _mapManager;
        private uint _mapIndex;
        private int _xPos, _yPos;

        /// <summary>
        /// Default Constructor.
        /// </summary>
        public Chunk(MapManager manager, uint mapIndex, int x, int y, uint chunkSize)
        {
            _mapManager = manager;
            _xPos = x;
            _yPos = y;
            Size = chunkSize;
            _mapIndex = mapIndex;

            Tiles = new Tile[Size, Size];
        }

        public TileRef GetTile(uint xTile, uint yTile)
        {
            // array out of bounds
            if (xTile >= Size || yTile >= Size)
                return default(TileRef);

            return new TileRef(_mapManager, _mapIndex, this, xTile, yTile);
        }

        public IEnumerable<TileRef> GetAllTiles()
        {
            foreach (var tile in Tiles)
            {
                yield return new TileRef();
            }
        }

        public void SetTile(uint xTileIndex, uint yTileIndex, Tile tile)
        {
            if (xTileIndex >= Size || yTileIndex >= Size)
                return;

            Tiles[xTileIndex, yTileIndex] = tile;
        }
        
        public void SetTile(uint xTileIndex, uint yTileIndex, ushort tileId, ushort tileData = 0)
        {
            if (xTileIndex >= Size || yTileIndex >= Size)
                return;

            Tiles[xTileIndex, yTileIndex] = new Tile(tileId, tileData);
        }

        public IEnumerator<TileRef> GetEnumerator()
        {
            for(var x=0;x<Size;x++)
            for (int y = 0; y < Size; y++)
            {
                yield return new TileRef();
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
