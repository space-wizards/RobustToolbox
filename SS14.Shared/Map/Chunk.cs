using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using SS14.Shared.Interfaces.Map;

namespace SS14.Shared.Map
{
    /// <summary>
    ///     A square section of the map.
    /// </summary>
    internal class Chunk : IMapChunk
    {
        private const int ChunkVersion = 1;
        private readonly MapGrid _grid;
        private readonly MapGrid.Indices _gridIndices;
        private readonly MapManager _mapManager;

        private readonly Tile[,] _tiles;

        /// <summary>
        ///     Constructs an instance of a MapGrid chunk.
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

        /// <inheritdoc />
        public ushort ChunkSize { get; }

        /// <inheritdoc />
        public uint Version => ChunkVersion;

        /// <inheritdoc />
        public int X => _gridIndices.X;

        /// <inheritdoc />
        public int Y => _gridIndices.Y;

        /// <summary>
        ///     Returns the tile at the given indices. The tile indices are relative locations to the chunk origin,
        ///     NOT local to the grid.
        /// </summary>
        /// <param name="xTile">The X tile index relative to the chunk origin.</param>
        /// <param name="yTile">The Y tile index relative to the chunk origin.</param>
        /// <returns>A reference to a tile.</returns>
        public TileRef GetTile(ushort xTile, ushort yTile)
        {
            // array out of bounds
            if (xTile >= ChunkSize || yTile >= ChunkSize)
                throw new ArgumentOutOfRangeException("Tile indices out of bounds.");

            var indices = ChunkTileToGridTile(new MapGrid.Indices(xTile, yTile));
            return new TileRef(_grid.MapID, _grid.Index, indices.X, indices.Y, _tiles[xTile, yTile]);
        }
        public TileRef GetTile(MapGrid.Indices indices)
        {
            // array out of bounds
            if (indices.X >= ChunkSize || indices.X < 0 || indices.Y >= ChunkSize || indices.Y < 0)
                throw new ArgumentOutOfRangeException("Tile indices out of bounds.");

            return new TileRef(_grid.MapID, _grid.Index, indices.X, indices.Y, _tiles[indices.X, indices.Y]);
        }

        /// <inheritdoc />
        [Obsolete("Enumerate over the chunk instead.")]
        public IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true)
        {
            for (var x = 0; x < ChunkSize; x++)
                for (var y = 0; y < ChunkSize; y++)
                {
                    if (_tiles[x, y].IsEmpty)
                        continue;

                    var indices = ChunkTileToGridTile(new MapGrid.Indices(x, y));
                    yield return new TileRef(_grid.MapID, _grid.Index, indices.X, indices.Y, _tiles[x, y]);
                }
        }

        /// <inheritdoc />
        public void SetTile(ushort xChunkTile, ushort yChunkTile, Tile tile)
        {
            if (xChunkTile >= ChunkSize || yChunkTile >= ChunkSize)
                throw new ArgumentException("Tile indices out of bounds.");

            // same tile, no point to continue
            if(_tiles[xChunkTile, yChunkTile].TileId == tile.TileId)
                return;

            var gridTile = ChunkTileToGridTile(new MapGrid.Indices(xChunkTile, yChunkTile));
            var newTileRef = new TileRef(_grid.MapID, _grid.Index, gridTile.X, gridTile.Y, tile);
            var oldTile = _tiles[xChunkTile, yChunkTile];
            _mapManager.RaiseOnTileChanged(newTileRef, oldTile);
            _grid.UpdateAABB(gridTile);

            _tiles[xChunkTile, yChunkTile] = tile;
        }

        /// <summary>
        ///     Returns an enumerator that iterates through all grid tiles.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TileRef> GetEnumerator()
        {
            for (var x = 0; x < ChunkSize; x++)
                for (var y = 0; y < ChunkSize; y++)
                {
                    var gridTile = ChunkTileToGridTile(new MapGrid.Indices(x, y));
                    yield return new TileRef(_grid.MapID, _grid.Index, gridTile.X, gridTile.Y, _tiles[x, y]);
                }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public MapGrid.Indices GridTileToChunkTile(MapGrid.Indices gridTile)
        {
            var size = ChunkSize;
            var x = Mod(gridTile.X, size);
            var y = Mod(gridTile.Y, size);
            return new MapGrid.Indices(x, y);
        }

        /// <inheritdoc />
        public void SetTile(ushort xChunkTile, ushort yChunkTile, ushort tileId, ushort tileData = 0)
        {
            SetTile(xChunkTile, yChunkTile, new Tile(tileId, tileData));
        }

        /// <summary>
        ///     Translates chunk tile indices to grid tile indices.
        /// </summary>
        /// <param name="chunkTile">The indices relative to the chunk origin.</param>
        /// <returns>The indices relative to the grid origin.</returns>
        private MapGrid.Indices ChunkTileToGridTile(MapGrid.Indices chunkTile)
        {
            return chunkTile + _gridIndices * ChunkSize;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Chunk {_gridIndices}, {ChunkSize}";
        }

        /// <summary>
        ///     This method provides floored modulus.
        ///     C-like languages use truncated modulus for their '%' operator.
        /// </summary>
        /// <param name="n">The dividend.</param>
        /// <param name="d">The divisor.</param>
        /// <returns>The remainder.</returns>
        [DebuggerStepThrough]
        private static int Mod(double n, int d)
        {
            return (int)(n - (int)Math.Floor(n / d) * d);
        }
    }
}
