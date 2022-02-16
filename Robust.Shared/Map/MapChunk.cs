using System;
using System.Collections;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics.Dynamics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     A square section of a <see cref="IMapGrid"/>.
    /// </summary>
    internal sealed class MapChunk : IEnumerable<TileRef>
    {
        /// <summary>
        /// New SnapGrid cells are allocated with this capacity.
        /// </summary>
        private const int SnapCellStartingCapacity = 1;

        public GridId GridId => _grid.Index;

        private readonly IMapGridInternal _grid;
        private readonly Vector2i _gridIndices;

        private readonly Tile[,] _tiles;
        private readonly SnapGridCell[,] _snapGrid;

        // We'll keep a running count of how many tiles are non-empty.
        // If this ever hits 0 then we know the chunk can be deleted.
        // The alternative is that every time we SetTile we iterate every tile in the chunk.
        internal int ValidTiles { get; private set; }

        private Box2i _cachedBounds;

        public List<Fixture> Fixtures { get; set; } = new();

        /// <summary>
        /// The last game simulation tick that a tile on this chunk was modified.
        /// </summary>
        public GameTick LastTileModifiedTick { get; private set; }

        public GameTick LastAnchoredModifiedTick { get; set; }

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
        }

        /// <summary>
        ///     The number of tiles per side of the square chunk.
        /// </summary>
        public ushort ChunkSize { get; }

        /// <summary>
        ///     The X index of this chunk inside the <see cref="IMapGrid"/>.
        /// </summary>
        public int X => _gridIndices.X;

        /// <summary>
        ///     The Y index of this chunk inside the <see cref="IMapGrid"/>.
        /// </summary>
        public int Y => _gridIndices.Y;

        /// <summary>
        ///     The positional indices of this chunk in the <see cref="IMapGrid"/>.
        /// </summary>
        public Vector2i Indices => _gridIndices;

        /// <summary>
        ///     Returns the tile at the given indices.
        /// </summary>
        /// <param name="xIndex">The X tile index relative to the chunk origin.</param>
        /// <param name="yIndex">The Y tile index relative to the chunk origin.</param>
        /// <returns>A reference to a tile.</returns>
        public TileRef GetTileRef(ushort xIndex, ushort yIndex)
        {
            if (xIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

            if (yIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

            var indices = ChunkTileToGridTile(new Vector2i(xIndex, yIndex));
            return new TileRef(_grid.ParentMapId, _grid.Index, indices, _tiles[xIndex, yIndex]);
        }

        /// <summary>
        ///     Returns the tile reference at the given indices.
        /// </summary>
        /// <param name="indices">The tile indices relative to the chunk origin.</param>
        /// <returns>A reference to a tile.</returns>
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

        /// <summary>
        ///     Returns all of the tiles in the chunk, while optionally filtering empty files.
        ///     Returned order is guaranteed to be row-major.
        /// </summary>
        /// <param name="ignoreEmpty">Will empty (space) tiles be added to the collection?</param>
        /// <returns></returns>
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

        /// <summary>
        ///     Replaces a single tile inside of the chunk.
        /// </summary>
        /// <param name="xIndex">The X tile index relative to the chunk.</param>
        /// <param name="yIndex">The Y tile index relative to the chunk.</param>
        /// <param name="tile">The new tile to insert.</param>
        public void SetTile(ushort xIndex, ushort yIndex, Tile tile)
        {
            if (xIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

            if (yIndex >= ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

            // same tile, no point to continue
            if (_tiles[xIndex, yIndex].TypeId == tile.TypeId)
                return;

            var oldIsEmpty = _tiles[xIndex, yIndex].IsEmpty;
            var oldValidTiles = ValidTiles;

            if (oldIsEmpty != tile.IsEmpty)
            {
                if (oldIsEmpty)
                {
                    ValidTiles += 1;
                }
                else
                {
                    ValidTiles -= 1;
                }
            }

            DebugTools.Assert(ValidTiles >= 0);
            var gridTile = ChunkTileToGridTile(new Vector2i(xIndex, yIndex));
            var newTileRef = new TileRef(_grid.ParentMapId, _grid.Index, gridTile, tile);
            var oldTile = _tiles[xIndex, yIndex];
            LastTileModifiedTick = _grid.CurTick;

            _tiles[xIndex, yIndex] = tile;

            // As the collision regeneration can potentially delete the chunk we'll notify of the tile changed first.
            _grid.NotifyTileChanged(newTileRef, oldTile);

            if (!SuppressCollisionRegeneration && oldValidTiles != ValidTiles)
            {
                RegenerateCollision();
            }
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

        /// <summary>
        ///     Transforms Tile indices relative to the grid into tile indices relative to this chunk.
        /// </summary>
        /// <param name="gridTile">Tile indices relative to the grid.</param>
        /// <returns>Tile indices relative to this chunk.</returns>
        public Vector2i GridTileToChunkTile(Vector2i gridTile)
        {
            var size = ChunkSize;
            var x = MathHelper.Mod(gridTile.X, size);
            var y = MathHelper.Mod(gridTile.Y, size);
            return new Vector2i(x, y);
        }

        /// <summary>
        ///     Translates chunk tile indices to grid tile indices.
        /// </summary>
        /// <param name="chunkTile">The indices relative to the chunk origin.</param>
        /// <returns>The indices relative to the grid origin.</returns>
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
                if (cell.Center is null)
                    continue;

                foreach (var euid in cell.Center)
                {
                    yield return euid;
                }
            }
        }

        /// <summary>
        /// Like <see cref="GetAllAnchoredEnts"/>... but fast.
        /// </summary>
        public void FastGetAllAnchoredEnts(EntityUidQueryCallback callback)
        {
            foreach (var cell in _snapGrid)
            {
                if (cell.Center is null)
                    continue;

                foreach (var euid in cell.Center)
                {
                    callback(euid);
                }
            }
        }

        public bool SuppressCollisionRegeneration { get; set; }

        public void RegenerateCollision()
        {
            // Even if the chunk is still removed still need to make sure bounds are updated (for now...)
            if (ValidTiles == 0)
            {
                var grid = (IMapGridInternal) IoCManager.Resolve<IMapManager>().GetGrid(GridId);

                grid.RemoveChunk(_gridIndices);
            }

            // generate collision rects
            GridChunkPartition.PartitionChunk(this, out _cachedBounds, out var rectangles);

            _grid.UpdateAABB();

            // TryGet because unit tests YAY
            if (ValidTiles > 0 && EntitySystem.TryGet(out SharedGridFixtureSystem? system))
                system.RegenerateCollision(this, rectangles);
        }

        /// <inheritdoc />
        public Box2i CalcLocalBounds()
        {
            return _cachedBounds;
        }


        /// <summary>
        /// Calculate the bounds of this chunk.
        /// </summary>
        /// <remarks>
        /// TODO: We can rely on the fixture instead for the bounds in the future but we also need to
        /// update the rendering to account for it. Better working on accurate grid bounds after rotation IMO.
        /// </remarks>
        public Box2Rotated CalcWorldBounds(Vector2? gridPos = null, Angle? gridRot = null)
        {
            gridRot ??= _grid.WorldRotation;
            gridPos ??= _grid.WorldPosition;
            var worldPos = gridPos.Value + gridRot.Value.RotateVec(Indices * _grid.TileSize * ChunkSize);

            var localBounds = CalcLocalBounds();
            var ts = _grid.TileSize;

            var scaledLocalBounds = new Box2Rotated(new Box2(
                localBounds.Left * ts,
                localBounds.Bottom * ts,
                localBounds.Right * ts,
                localBounds.Top * ts).Translated(worldPos), gridRot.Value, worldPos);

            return scaledLocalBounds;
        }

        /// <summary>
        /// Calculate the AABB for this chunk.
        /// </summary>
        public Box2 CalcWorldAABB(Vector2? gridPos = null, Angle? gridRot = null)
        {
            var bounds = CalcWorldBounds(gridPos, gridRot);
            return bounds.CalcBoundingBox();
        }

        /// <summary>
        /// Tests if a point is on top of a non-empty tile.
        /// </summary>
        /// <param name="localIndices">Local tile indices</param>
        public bool CollidesWithChunk(Vector2i localIndices)
        {
            return _tiles[localIndices.X, localIndices.Y].TypeId != Tile.Empty.TypeId;
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

    internal sealed class RegenerateChunkCollisionEvent : EntityEventArgs
    {
        public MapChunk Chunk { get; }

        public RegenerateChunkCollisionEvent(MapChunk chunk)
        {
            Chunk = chunk;
        }
    }

    internal sealed class ChunkRemovedEvent : EntityEventArgs
    {
        public MapChunk Chunk = default!;
    }
}
