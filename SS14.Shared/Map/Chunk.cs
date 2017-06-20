using SS14.Shared.Interfaces.Map;
using System;
using System.Collections;
using System.Collections.Generic;

namespace SS14.Shared.Map
{
    /// <summary>
    /// A square section of the map.
    /// </summary>
    internal class Chunk : IMapChunk
    {
        private const int CHUNK_VERSION = 1;

        public ushort ChunkSize { get; }
        public uint Version => CHUNK_VERSION;
        public int X => _gridIndices.X;
        public int Y => _gridIndices.Y;

        private readonly Tile[,] Tiles;
        private readonly MapManager _mapManager;
        private readonly MapGrid _grid;
        private readonly MapGrid.Indices _gridIndices;


        /// <summary>
        /// Constructs an instance of a MapGrid chunk.
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="grid"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="chunkChunkSize"></param>
        public Chunk(MapManager manager, MapGrid grid, int x, int y, ushort chunkChunkSize)
        {
            _mapManager = manager;
            ChunkSize = chunkChunkSize;
            _grid = grid;
            _gridIndices = new MapGrid.Indices(x, y);

            Tiles = new Tile[ChunkSize, ChunkSize];
        }

        /// <summary>
        /// Returns the tile at the given indices. The tile indices are relative locations to the chunk origin,
        /// NOT local to the grid.
        /// </summary>
        /// <param name="xTile">The X tile index relative to the chunk origin.</param>
        /// <param name="yTile">The Y tile index relative to the chunk origin.</param>
        /// <returns>A reference to a tile.</returns>
        public TileRef GetTile(ushort xTile, ushort yTile)
        {
            // array out of bounds
            if (xTile >= ChunkSize || yTile >= ChunkSize)
                throw new Exception("Tile indices out of bounds.");

            var indices = LocalToGrid(new MapGrid.Indices(xTile, yTile));
            return new TileRef(_mapManager, _grid.Index, indices.X, indices.Y, Tiles[xTile, yTile]);
        }

        [Obsolete("Enumerate over the chunk instead.")]
        public IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true)
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    if (Tiles[x, y].IsEmpty)
                        continue;

                    var indices = LocalToGrid(new MapGrid.Indices(x, y));
                    var chunkTile = GridTileToChunkTile(indices);
                    yield return new TileRef(_mapManager, _grid.Index, indices.X, indices.Y, Tiles[chunkTile.X, chunkTile.Y]);
                }
            }
        }

        public void SetTile(ushort xTileIndex, ushort yTileIndex, Tile tile)
        {
            if (xTileIndex >= ChunkSize || yTileIndex >= ChunkSize)
                return;
            var gridTile = ChunkTileToGridTile(new MapGrid.Indices(xTileIndex, yTileIndex));
            var chunkTile = GridTileToChunkTile(gridTile);
            var tileRef = new TileRef(_mapManager, _grid.Index, gridTile.X, gridTile.Y, Tiles[chunkTile.X, chunkTile.Y]);
            var oldTile = Tiles[xTileIndex, yTileIndex];
            _mapManager.RaiseOnTileChanged(_grid.Index, tileRef, oldTile);
            _grid.UpdateAABB(gridTile);
            Tiles[xTileIndex, yTileIndex] = tile;
        }
        
        public void SetTile(ushort xTileIndex, ushort yTileIndex, ushort tileId, ushort tileData = 0)
        {
            if (xTileIndex >= ChunkSize || yTileIndex >= ChunkSize)
                return;

            Tiles[xTileIndex, yTileIndex] = new Tile(tileId, tileData);
        }

        public IEnumerator<TileRef> GetEnumerator()
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var indices = LocalToGrid(new MapGrid.Indices(x, y));
                    var chunkTile = GridTileToChunkTile(indices);
                    yield return new TileRef(_mapManager, _grid.Index, indices.X, indices.Y, Tiles[chunkTile.X, chunkTile.Y]);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <summary>
        /// Translates chunk tile indices to grid tile indices.
        /// </summary>
        /// <param name="local">The indices relative to the chunk origin.</param>
        /// <returns>The indices relative to the grid origin.</returns>
        private MapGrid.Indices LocalToGrid(MapGrid.Indices local)
        {
            return local + (_gridIndices * ChunkSize);
        }


        /// <inheritdoc />
        public MapGrid.Indices GridTileToChunkTile(MapGrid.Indices gridTileIndices)
        {
            var size = ChunkSize;
            var x =  Math.Abs(gridTileIndices.X % size);
            var y = Math.Abs(gridTileIndices.Y % size);
            return new MapGrid.Indices(x, y);
        }

        public MapGrid.Indices ChunkTileToGridTile(MapGrid.Indices chunkTileIndices)
        {
            return chunkTileIndices + _gridIndices * ChunkSize;
        }
    }
}
