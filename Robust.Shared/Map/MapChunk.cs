using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal class MapChunk : IMapChunkInternal
    {
        /// <summary>
        /// New SnapGrid cells are allocated with this capacity.
        /// </summary>
        private const int SnapCellStartingCapacity = 1;

        private readonly IMapGridInternal _grid;
        private readonly Vector2i _gridIndices;

        private readonly Tile[,] _tiles;
        private readonly SnapGridCell[,] _snapGrid;

        private Box2i _cachedBounds;
        private IList<Box2> _colBoxes;

        /// <inheritdoc />
        public GameTick LastTileModifiedTick { get; private set; }

        /// <inheritdoc />
        public GameTick LastAnchoredModifiedTick { get; set; }

        public IEnumerable<Box2> CollisionBoxes => _colBoxes;

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
            LastTileModifiedTick = grid.CurTick;
            _gridIndices = new Vector2i(x, y);
            ChunkSize = chunkSize;

            _tiles = new Tile[ChunkSize, ChunkSize];
            _snapGrid = new SnapGridCell[ChunkSize, ChunkSize];
            _colBoxes = new List<Box2>(0);
        }

        /// <inheritdoc />
        public ushort ChunkSize { get; }

        /// <inheritdoc />
        public int X => _gridIndices.X;

        /// <inheritdoc />
        public int Y => _gridIndices.Y;

        /// <inheritdoc />
        public Vector2i Indices => _gridIndices;

        /// <inheritdoc />
        public TileRef GetTileRef(ushort xIndex, ushort yIndex)
        {
            if (xIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

            if (yIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

            var indices = ChunkTileToGridTile(new Vector2i(xIndex, yIndex));
            return new TileRef(_grid.ParentMapId, _grid.Index, indices, _tiles[xIndex, yIndex]);
        }

        /// <inheritdoc />
        public TileRef GetTileRef(Vector2i indices)
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

                    var indices = ChunkTileToGridTile(new Vector2i(x, y));
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

            var gridTile = ChunkTileToGridTile(new Vector2i(xIndex, yIndex));
            var newTileRef = new TileRef(_grid.ParentMapId, _grid.Index, gridTile, tile);
            var oldTile = _tiles[xIndex, yIndex];
            LastTileModifiedTick = _grid.CurTick;

            _tiles[xIndex, yIndex] = tile;

            if (!SuppressCollisionRegeneration)
            {
                RegenerateCollision();
            }

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

                    var gridTile = ChunkTileToGridTile(new Vector2i(x, y));
                    yield return new TileRef(_grid.ParentMapId, _grid.Index, gridTile.X, gridTile.Y, _tiles[x, y]);
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        /// <inheritdoc />
        public Vector2i GridTileToChunkTile(Vector2i gridTile)
        {
            var size = ChunkSize;
            var x = MathHelper.Mod(gridTile.X, size);
            var y = MathHelper.Mod(gridTile.Y, size);
            return new Vector2i(x, y);
        }

        /// <inheritdoc />
        public Vector2i ChunkTileToGridTile(Vector2i chunkTile)
        {
            return chunkTile + _gridIndices * ChunkSize;
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetSnapGridCell(ushort xCell, ushort yCell)
        {
            if (xCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xCell), "Tile indices out of bounds.");

            if (yCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yCell), "Tile indices out of bounds.");

            var cell = _snapGrid[xCell, yCell];
            var list = cell.Center;

            if (list == null)
            {
                return Array.Empty<EntityUid>();
            }

            return list;
        }

        /// <inheritdoc />
        public void AddToSnapGridCell(ushort xCell, ushort yCell, EntityUid euid)
        {
            if (xCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xCell), "Tile indices out of bounds.");

            if (yCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yCell), "Tile indices out of bounds.");

            ref var cell = ref _snapGrid[xCell, yCell];
            cell.Center ??= new List<EntityUid>(SnapCellStartingCapacity);

            DebugTools.Assert(!cell.Center.Contains(euid));
            cell.Center.Add(euid);
            LastAnchoredModifiedTick = _grid.CurTick;
        }

        /// <inheritdoc />
        public void RemoveFromSnapGridCell(ushort xCell, ushort yCell, EntityUid euid)
        {
            if (xCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xCell), "Tile indices out of bounds.");

            if (yCell >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yCell), "Tile indices out of bounds.");

            ref var cell = ref _snapGrid[xCell, yCell];
            cell.Center?.Remove(euid);
            LastAnchoredModifiedTick = _grid.CurTick;
        }

        public IEnumerable<EntityUid> GetAllAnchoredEnts()
        {
            foreach (var cell in _snapGrid)
            {
                if(cell.Center is null)
                    continue;

                foreach (var euid in cell.Center)
                {
                    yield return euid;
                }
            }
        }

        public bool SuppressCollisionRegeneration { get; set; }

        public void RegenerateCollision()
        {
            // generate collision rects
            GridChunkPartition.PartitionChunk(this, ref _colBoxes, out _cachedBounds);
            _grid.NotifyChunkCollisionRegenerated();
        }

        /// <inheritdoc />
        public Box2i CalcLocalBounds()
        {
            return _cachedBounds;
        }

        public Box2 CalcWorldBounds()
        {
            var worldPos = _grid.WorldPosition + Indices * _grid.TileSize * ChunkSize;
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
        public bool CollidesWithChunk(Vector2i localIndices)
        {
            return _tiles[localIndices.X, localIndices.Y].TypeId != Tile.Empty.TypeId;
        }

        /// <inheritdoc />
        public bool CollidesWithChunk(Box2 pos)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override string ToString()
        {
            return $"Chunk {_gridIndices}";
        }

        private struct SnapGridCell
        {
            public List<EntityUid>? Center;
        }
    }
}
