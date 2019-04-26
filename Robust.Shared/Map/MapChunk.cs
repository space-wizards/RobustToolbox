using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal class MapChunk : IMapChunkInternal
    {
        private readonly IMapGridInternal _grid;
        private readonly MapIndices _gridIndices;

        private readonly Tile[,] _tiles;
        private readonly SnapGridCell[,] _snapGrid;

        /// <inheritdoc />
        public GameTick LastModifiedTick { get; private set; }

        /// <summary>
        ///     Constructs an instance of a MapGrid chunk.
        /// </summary>
        /// <param name="grid"></param>
        /// <param name="x"></param>
        /// <param name="y"></param>
        /// <param name="chunkSize"></param>
        public MapChunk(IMapGridInternal grid, int x, int y, ushort chunkSize)
        {
            _grid = grid;
            LastModifiedTick = grid.CurTick;
            _gridIndices = new MapIndices(x, y);
            ChunkSize = chunkSize;

            _tiles = new Tile[ChunkSize, ChunkSize];
            _snapGrid = new SnapGridCell[ChunkSize, ChunkSize];
        }

        /// <inheritdoc />
        public ushort ChunkSize { get; }

        /// <inheritdoc />
        public int X => _gridIndices.X;

        /// <inheritdoc />
        public int Y => _gridIndices.Y;

        /// <inheritdoc />
        public MapIndices Indices => _gridIndices;
        
        /// <inheritdoc />
        public TileRef GetTileRef(ushort xIndex, ushort yIndex)
        {
            if (xIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

            if (yIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

            var indices = ChunkTileToGridTile(new MapIndices(xIndex, yIndex));
            return new TileRef(_grid.ParentMapId, _grid.Index, indices, _tiles[xIndex, yIndex]);
        }

        /// <inheritdoc />
        public TileRef GetTileRef(MapIndices indices)
        {
            if (indices.X >= ChunkSize || indices.X < 0 || indices.Y >= ChunkSize || indices.Y < 0)
                throw new ArgumentOutOfRangeException(nameof(indices), "Tile indices out of bounds.");

            var chunkIndices = ChunkTileToGridTile(indices);
            return new TileRef(_grid.ParentMapId, _grid.Index, chunkIndices, _tiles[indices.X, indices.Y]);
        }

        /// <inheritdoc />
        public Tile GetTile(ushort xIndex, ushort yIndex)
        {
            if (xIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

            if (yIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

            return _tiles[xIndex, yIndex];
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true)
        {
            for (var x = 0; x < ChunkSize; x++)
            for (var y = 0; y < ChunkSize; y++)
            {
                if (ignoreEmpty && _tiles[x, y].IsEmpty)
                    continue;

                var indices = ChunkTileToGridTile(new MapIndices(x, y));
                yield return new TileRef(_grid.ParentMapId, _grid.Index, indices.X, indices.Y, _tiles[x, y]);
            }
        }

        /// <inheritdoc />
        public void SetTile(ushort xIndex, ushort yIndex, Tile tile)
        {
            if (xIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

            if (yIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

            // same tile, no point to continue
            if (_tiles[xIndex, yIndex].TypeId == tile.TypeId)
                return;

            var gridTile = ChunkTileToGridTile(new MapIndices(xIndex, yIndex));
            var newTileRef = new TileRef(_grid.ParentMapId, _grid.Index, gridTile, tile);
            var oldTile = _tiles[xIndex, yIndex];
            LastModifiedTick = _grid.CurTick;
            _grid.NotifyTileChanged(newTileRef, oldTile);

            _tiles[xIndex, yIndex] = tile;
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
                if (_tiles[x, y].IsEmpty)
                    continue;

                var gridTile = ChunkTileToGridTile(new MapIndices(x, y));
                yield return new TileRef(_grid.ParentMapId, _grid.Index, gridTile.X, gridTile.Y, _tiles[x, y]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public MapIndices GridTileToChunkTile(MapIndices gridTile)
        {
            var size = ChunkSize;
            var x = MathHelper.Mod(gridTile.X, size);
            var y = MathHelper.Mod(gridTile.Y, size);
            return new MapIndices(x, y);
        }

        /// <inheritdoc />
        public IEnumerable<SnapGridComponent> GetSnapGridCell(ushort xCell, ushort yCell, SnapGridOffset offset)
        {
            if (xCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xCell), "Tile indices out of bounds.");

            if (yCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yCell), "Tile indices out of bounds.");

            var cell = _snapGrid[xCell, yCell];
            var list = offset == SnapGridOffset.Center ? cell.Center : cell.Edge;

            if (list == null)
                yield break;

            foreach (var element in list)
            {
                yield return element;
            }
        }

        /// <inheritdoc />
        public void AddToSnapGridCell(ushort xCell, ushort yCell, SnapGridOffset offset, SnapGridComponent snap)
        {
            if (xCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xCell), "Tile indices out of bounds.");

            if (yCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yCell), "Tile indices out of bounds.");

            ref var cell = ref _snapGrid[xCell, yCell];
            if (offset == SnapGridOffset.Center)
            {
                if (cell.Center == null)
                {
                    cell.Center = new List<SnapGridComponent>(1);
                }
                cell.Center.Add(snap);
            }
            else
            {
                if (cell.Edge == null)
                {
                    cell.Edge = new List<SnapGridComponent>(1);
                }
                cell.Edge.Add(snap);
            }
        }

        /// <inheritdoc />
        public void RemoveFromSnapGridCell(ushort xCell, ushort yCell, SnapGridOffset offset, SnapGridComponent snap)
        {
            if (xCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xCell), "Tile indices out of bounds.");

            if (yCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yCell), "Tile indices out of bounds.");

            ref var cell = ref _snapGrid[xCell, yCell];
            if (offset == SnapGridOffset.Center)
            {
                cell.Center?.Remove(snap);
            }
            else
            {
                cell.Edge?.Remove(snap);
            }
        }

        /// <summary>
        ///     Translates chunk tile indices to grid tile indices.
        /// </summary>
        /// <param name="chunkTile">The indices relative to the chunk origin.</param>
        /// <returns>The indices relative to the grid origin.</returns>
        private MapIndices ChunkTileToGridTile(MapIndices chunkTile)
        {
            return chunkTile + _gridIndices * ChunkSize;
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Chunk {_gridIndices}";
        }

        private struct SnapGridCell
        {
            public List<SnapGridComponent> Center;
            public List<SnapGridComponent> Edge;
        }
    }
}
