using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Maths;

namespace SS14.Shared.Map
{
    public partial class MapManager
    {
        /// <summary>
        ///     A square section of the map.
        /// </summary>
        internal class Chunk : IMapChunk
        {
            private const int ChunkVersion = 1;
            private readonly MapGrid _grid;
            private readonly MapIndices _gridIndices;
            private readonly MapManager _mapManager;

            private readonly Tile[,] _tiles;
            private readonly SnapGridCell[,] _snapGrid;

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
                _gridIndices = new MapIndices(x, y);

                _tiles = new Tile[ChunkSize, ChunkSize];
                _snapGrid = new SnapGridCell[ChunkSize, ChunkSize];
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

                var indices = ChunkTileToGridTile(new MapIndices(xTile, yTile));
                return new TileRef(_grid.MapID, _grid.Index, indices.X, indices.Y, _tiles[xTile, yTile]);
            }
            public TileRef GetTile(MapIndices indices)
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

                        var indices = ChunkTileToGridTile(new MapIndices(x, y));
                        yield return new TileRef(_grid.MapID, _grid.Index, indices.X, indices.Y, _tiles[x, y]);
                    }
            }

            /// <inheritdoc />
            public void SetTile(ushort xChunkTile, ushort yChunkTile, Tile tile)
            {
                if (xChunkTile >= ChunkSize || yChunkTile >= ChunkSize)
                    throw new ArgumentException("Tile indices out of bounds.");

                // same tile, no point to continue
                if (_tiles[xChunkTile, yChunkTile].TileId == tile.TileId)
                    return;

                var gridTile = ChunkTileToGridTile(new MapIndices(xChunkTile, yChunkTile));
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
                        var gridTile = ChunkTileToGridTile(new MapIndices(x, y));
                        yield return new TileRef(_grid.MapID, _grid.Index, gridTile.X, gridTile.Y, _tiles[x, y]);
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
            public void SetTile(ushort xChunkTile, ushort yChunkTile, ushort tileId, ushort tileData = 0)
            {
                SetTile(xChunkTile, yChunkTile, new Tile(tileId, tileData));
            }


            public IEnumerable<SnapGridComponent> GetSnapGridCell(ushort xCell, ushort yCell, SnapGridOffset offset)
            {
                var cell = _snapGrid[xCell, yCell];
                List<SnapGridComponent> list;
                if (offset == SnapGridOffset.Center)
                {
                    list = cell.Center;
                }
                else
                {
                    list = cell.Edge;
                }

                if (list != null)
                {
                    foreach (var element in list)
                    {
                        yield return element;
                    }
                }
            }

            public void AddToSnapGridCell(ushort xCell, ushort yCell, SnapGridOffset offset, SnapGridComponent snap)
            {
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

            public void RemoveFromSnapGridCell(ushort xCell, ushort yCell, SnapGridOffset offset, SnapGridComponent snap)
            {
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
                return $"Chunk {_gridIndices}, {ChunkSize}";
            }

            private struct SnapGridCell
            {
                public List<SnapGridComponent> Center;
                public List<SnapGridComponent> Edge;
            }
        }
    }
}
