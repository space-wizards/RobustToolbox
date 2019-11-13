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

        private Box2i _cachedBounds;

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
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    if (ignoreEmpty && _tiles[x, y].IsEmpty)
                        continue;

                    var indices = ChunkTileToGridTile(new MapIndices(x, y));
                    yield return new TileRef(_grid.ParentMapId, _grid.Index, indices.X, indices.Y, _tiles[x, y]);
                }
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

            _tiles[xIndex, yIndex] = tile;
            CheckBounds(new Vector2i(xIndex, yIndex), tile.IsEmpty);

            _grid.NotifyTileChanged(newTileRef, oldTile);
        }

        /// <summary>
        ///     Returns an enumerator that iterates through all grid tiles.
        /// </summary>
        /// <returns></returns>
        public IEnumerator<TileRef> GetEnumerator()
        {
            for (var x = 0; x < ChunkSize; x++)
            {
                for (var y = 0; y < ChunkSize; y++)
                {
                    if (_tiles[x, y].IsEmpty)
                        continue;

                    var gridTile = ChunkTileToGridTile(new MapIndices(x, y));
                    yield return new TileRef(_grid.ParentMapId, _grid.Index, gridTile.X, gridTile.Y, _tiles[x, y]);
                }
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
        public MapIndices ChunkTileToGridTile(MapIndices chunkTile)
        {
            return chunkTile + _gridIndices * ChunkSize;
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
            {
                return Array.Empty<SnapGridComponent>();
            }

            return list;
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

        private void CheckBounds(in Vector2i indices, bool empty)
        {
            var tileBounds = new Box2i(0,0,1, 1).Translated(indices);

            if (!empty)
            {
                // placing tiles can only expand the bounds, it is easier to just blind union
                _cachedBounds = _cachedBounds.Size.Equals(Vector2i.Zero) ? tileBounds : _cachedBounds.Union(tileBounds);
            }
            else
            {
                // removing tiles can only shrink the bounds, this is not an easy thing to detect
                ReduceBoundsX(_tiles, tileBounds, ref _cachedBounds);
                ReduceBoundsY(_tiles, tileBounds, ref _cachedBounds);
            }
        }

        private static void ReduceBoundsX(in Tile[,] tiles, Box2i tBounds, ref Box2i cBounds)
        {
            if (tBounds.Left != cBounds.Left && tBounds.Right != cBounds.Right)
                return; // nothing to do, we are not on an edge

            var left = tBounds.Left == cBounds.Left;

            // removing a tile can shrink the side more than one tile
            while (cBounds.Width > 0)
            {
                // check if we are the only tile holding the side out
                if(!AnyTileOnY(tiles, new Vector2i(cBounds.Bottom, cBounds.Top), tBounds.BottomLeft))
                    return; // our removal does not modify the edge, we are done here

                // shrink the chunk bounds
                int newLeft;
                int newRight;
                if (left)
                {
                    newLeft = cBounds.Left + 1;
                    newRight = cBounds.Right;
                    tBounds = tBounds.Translated(new Vector2i(1, 0));
                }
                else
                {
                    newLeft = cBounds.Left;
                    newRight = cBounds.Right - 1;
                    tBounds = tBounds.Translated(new Vector2i(-1, 0));
                }

                cBounds = new Box2i(newLeft, cBounds.Bottom, newRight, cBounds.Top);
            }
        }

        private static void ReduceBoundsY(in Tile[,] tiles, Box2i tBounds, ref Box2i cBounds)
        {
            if (tBounds.Bottom != cBounds.Bottom && tBounds.Top != cBounds.Top)
                return; // nothing to do, we are not on an edge

            var bottom = tBounds.Bottom == cBounds.Bottom; // which side we are moving

            // removing a tile can shrink the side more than one tile
            while (cBounds.Height > 0)
            {
                // check if we are the only tile holding the side out
                if (!AnyTileOnX(tiles, new Vector2i(cBounds.Left, cBounds.Right), tBounds.BottomLeft))
                    return; // our removal does not modify the edge

                // shrink the chunk bounds
                int newBottom;
                int newTop;
                if (bottom)
                {
                    newBottom = cBounds.Bottom + 1;
                    newTop = cBounds.Top;
                    tBounds = tBounds.Translated(new Vector2i(0, 1));
                }
                else
                {
                    newBottom = cBounds.Bottom;
                    newTop = cBounds.Top - 1;
                    tBounds = tBounds.Translated(new Vector2i(0, -1));
                }

                cBounds = new Box2i(cBounds.Left, newBottom, cBounds.Right, newTop);
            }
        }

        private static bool AnyTileOnX(in Tile[,] tiles, Vector2i extents, in Vector2i indices)
        {
            var y = indices.Y;
            for (var x = extents.X; x < extents.Y; x++)
            {
                if(tiles[x, y].IsEmpty)
                    continue;

                return false;
            }

            return true;
        }

        private static bool AnyTileOnY(in Tile[,] tiles, Vector2i extents, in Vector2i indices)
        {
            var x = indices.X;
            for (var y = extents.X; y < extents.Y; y++)
            {
                if(tiles[x, y].IsEmpty)
                    continue;

                return false;
            }

            return true;
        }

        /// <inheritdoc />
        public Box2i CalcLocalBounds()
        {
            return _cachedBounds;
        }

        public Box2 CalcWorldBounds()
        {
            var worldPos = _grid.WorldPosition + (Vector2i)Indices * _grid.TileSize * ChunkSize;
            var localBounds = CalcLocalBounds();
            var ts = _grid.TileSize;

            var scaledLocalBounds = new Box2(
                localBounds.Left * ts,
                localBounds.Bottom * ts,
                localBounds.Right * ts,
                localBounds.Top * ts);

            return scaledLocalBounds.Translated(worldPos);
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
