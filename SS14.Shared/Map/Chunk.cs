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

        private readonly Tile[,] _tiles;
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
        /// <param name="chunkSize"></param>
        public Chunk(MapManager manager, MapGrid grid, int x, int y, ushort chunkSize)
        {
            _mapManager = manager;
            ChunkSize = chunkSize;
            _grid = grid;
            _gridIndices = new MapGrid.Indices(x, y);

            _tiles = new Tile[ChunkSize, ChunkSize];
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

            var indices = ChunkTileToGridTile(new MapGrid.Indices(xTile, yTile));
            return new TileRef(_mapManager, _grid.Index, indices.X, indices.Y, _tiles[xTile, yTile]);
        }

        [Obsolete("Enumerate over the chunk instead.")]
        public IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true)
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    if (_tiles[x, y].IsEmpty)
                        continue;

                    var indices = ChunkTileToGridTile(new MapGrid.Indices(x, y));
                    yield return new TileRef(_mapManager, _grid.Index, indices.X, indices.Y, _tiles[x, y]);
                }
            }
        }

        public void SetTile(ushort xChunkTile, ushort yChunkTile, Tile tile)
        {
            if (xChunkTile >= ChunkSize || yChunkTile >= ChunkSize)
                throw new Exception("Tile indices out of bounds.");

            var gridTile = ChunkTileToGridTile(new MapGrid.Indices(xChunkTile, yChunkTile));

            var newTileRef = new TileRef(_mapManager, _grid.Index, gridTile.X, gridTile.Y, tile);
            var oldTile = _tiles[xChunkTile, yChunkTile];
            _mapManager.RaiseOnTileChanged(_grid.Index, newTileRef, oldTile);
            _grid.UpdateAABB(gridTile);

            _tiles[xChunkTile, yChunkTile] = tile;
        }

        /// <inheritdoc />
        public void SetTile(ushort xChunkTile, ushort yChunkTile, ushort tileId, ushort tileData = 0)
        {
            SetTile(xChunkTile, yChunkTile, new Tile(tileId, tileData));
        }

        public IEnumerator<TileRef> GetEnumerator()
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    var gridTile = ChunkTileToGridTile(new MapGrid.Indices(x, y));
                    yield return new TileRef(_mapManager, _grid.Index, gridTile.X, gridTile.Y, _tiles[x, y]);
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
        /// <param name="chunkTile">The indices relative to the chunk origin.</param>
        /// <returns>The indices relative to the grid origin.</returns>
        private MapGrid.Indices ChunkTileToGridTile(MapGrid.Indices chunkTile)
        {
            return chunkTile + (_gridIndices * ChunkSize);
        }
        
        /// <inheritdoc />
        public MapGrid.Indices GridTileToChunkTile(MapGrid.Indices gridTile)
        {
            var size = ChunkSize;
            var x =  Mod(gridTile.X, size);
            var y = Mod(gridTile.Y, size);
            return new MapGrid.Indices(x, y);
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Chunk {_gridIndices}, {ChunkSize}";
        }

        /// <summary>
        /// This method provides floored modulus.
        /// C-like languages use truncated modulus for their '%' operator.
        /// </summary>
        /// <param name="n">The dividend.</param>
        /// <param name="d">The divisor.</param>
        /// <returns>The remainder.</returns>
        [System.Diagnostics.DebuggerStepThrough]
        private static int Mod(double n, int d)
        {
            return (int)(n - (int)Math.Floor(n / d) * d);
        }
    }
}
