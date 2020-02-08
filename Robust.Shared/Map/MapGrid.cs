using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal class MapGrid : IMapGridInternal
    {
        /// <summary>
        ///     Game tick that the map was created.
        /// </summary>
        public GameTick CreatedTick { get; }

        /// <summary>
        ///     Last game tick that the map was modified.
        /// </summary>
        public GameTick LastModifiedTick { get; private set; }

        /// <inheritdoc />
        public GameTick CurTick => _mapManager.GameTiming.CurTick;

        /// <inheritdoc />
        public bool IsDefaultGrid => _mapManager.GetDefaultGridId(ParentMapId) == Index;

        /// <inheritdoc />
        public MapId ParentMapId { get; set; }

        public EntityUid GridEntityId { get; internal set; }

        /// <summary>
        ///     Grid chunks than make up this grid.
        /// </summary>
        private readonly Dictionary<MapIndices, IMapChunkInternal> _chunks = new Dictionary<MapIndices, IMapChunkInternal>();

        private readonly IMapManagerInternal _mapManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapGrid"/> class.
        /// </summary>
        /// <param name="mapManager">Reference to the <see cref="MapManager"/> that will manage this grid.</param>
        /// <param name="gridIndex">Index identifier of this grid.</param>
        /// <param name="chunkSize">The dimension of this square chunk.</param>
        /// <param name="snapSize">Distance in world units between the lines on the conceptual snap grid.</param>
        /// <param name="parentMapId">Parent map identifier.</param>
        internal MapGrid(IMapManagerInternal mapManager, GridId gridIndex, ushort chunkSize, float snapSize,
            MapId parentMapId)
        {
            _mapManager = mapManager;
            Index = gridIndex;
            ChunkSize = chunkSize;
            SnapSize = snapSize;
            ParentMapId = parentMapId;
            LastModifiedTick = CreatedTick = _mapManager.GameTiming.CurTick;
        }

        /// <summary>
        ///     Disposes the grid.
        /// </summary>
        public void Dispose()
        {
            // Nothing for now.
        }

        /// <inheritdoc />
        public Box2 WorldBounds => LocalBounds.Translated(WorldPosition);

        /// <inheritdoc />
        public Box2 LocalBounds { get; private set; }

        public bool SuppressCollisionRegeneration { get; set; }

        /// <inheritdoc />
        public ushort ChunkSize { get; }

        /// <inheritdoc />
        public float SnapSize { get; }

        /// <inheritdoc />
        public GridId Index { get; }

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        public ushort TileSize { get; } = 1;

        /// <inheritdoc />
        public Vector2 WorldPosition
        {
            get
            {
                if(IsDefaultGrid) // Default grids cannot be moved.
                    return Vector2.Zero;

                //TODO: Make grids real parents of entities.
                if(GridEntityId.IsValid())
                    return _mapManager.EntityManager.GetEntity(GridEntityId).Transform.WorldPosition;
                return Vector2.Zero;
            }
            set
            {
                if (IsDefaultGrid) // Default grids cannot be moved.
                    return;

                _mapManager.EntityManager.GetEntity(GridEntityId).Transform.WorldPosition = value;
                LastModifiedTick = _mapManager.GameTiming.CurTick;
            }
        }

        /// <summary>
        /// Expands the AABB for this grid when a new tile is added. If the tile is already inside the existing AABB,
        /// nothing happens. If it is outside, the AABB is expanded to fit the new tile.
        /// </summary>
        private void UpdateAABB()
        {
            LocalBounds = new Box2();
            foreach (var chunk in _chunks.Values)
            {
                var chunkBounds = chunk.CalcLocalBounds();

                if(chunkBounds.Size.Equals(Vector2i.Zero))
                    continue;

                if (LocalBounds.Size == Vector2.Zero)
                {
                    var gridBounds = chunkBounds.Translated(chunk.Indices * chunk.ChunkSize);
                    LocalBounds = gridBounds;
                }
                else
                {
                    var gridBounds = chunkBounds.Translated(chunk.Indices * chunk.ChunkSize);
                    LocalBounds = LocalBounds.Union(gridBounds);
                }
            }
        }

        /// <inheritdoc />
        public void NotifyTileChanged(in TileRef tileRef, in Tile oldTile)
        {
            LastModifiedTick = _mapManager.GameTiming.CurTick;
            UpdateAABB();
            _mapManager.RaiseOnTileChanged(tileRef, oldTile);
        }

        /// <inheritdoc />
        public bool OnSnapCenter(Vector2 position)
        {
            return (FloatMath.CloseTo(position.X % SnapSize, 0) && FloatMath.CloseTo(position.Y % SnapSize, 0));
        }

        /// <inheritdoc />
        public bool OnSnapBorder(Vector2 position)
        {
            return (FloatMath.CloseTo(position.X % SnapSize, SnapSize / 2) && FloatMath.CloseTo(position.Y % SnapSize, SnapSize / 2));
        }

        #region TileAccess

        /// <inheritdoc />
        public TileRef GetTileRef(GridCoordinates worldPos)
        {
            return GetTileRef(WorldToTile(worldPos));
        }

        /// <inheritdoc />
        public TileRef GetTileRef(MapIndices tileCoordinates)
        {
            var chunkIndices = GridTileToChunkIndices(tileCoordinates);

            if (!_chunks.TryGetValue(chunkIndices, out var output))
            {
                // Chunk doesn't exist, return a tileRef to an empty (space) tile.
                return new TileRef(ParentMapId, Index, tileCoordinates.X, tileCoordinates.Y, default);
            }

            var chunkTileIndices = output.GridTileToChunkTile(tileCoordinates);
            return output.GetTileRef((ushort)chunkTileIndices.X, (ushort)chunkTileIndices.Y);
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetAllTiles(bool ignoreSpace = true)
        {
            foreach (var kvChunk in _chunks)
            {
                foreach (var tileRef in kvChunk.Value)
                {
                    if (!tileRef.Tile.IsEmpty)
                        yield return tileRef;
                }
            }
        }

        /// <inheritdoc />
        public void SetTile(GridCoordinates worldPos, Tile tile)
        {
            var localTile = WorldToTile(worldPos);
            SetTile(new MapIndices(localTile.X, localTile.Y), tile);
        }

        /// <inheritdoc />
        public void SetTile(MapIndices gridIndices, Tile tile)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(gridIndices);
            chunk.SetTile((ushort)chunkTile.X, (ushort)chunkTile.Y, tile);
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true, Predicate<TileRef> predicate = null)
        {
            //TODO: needs world -> local -> tile translations.
            var gridTileLb = new MapIndices((int)Math.Floor(worldArea.Left), (int)Math.Floor(worldArea.Bottom));
            var gridTileRt = new MapIndices((int)Math.Floor(worldArea.Right), (int)Math.Floor(worldArea.Top));

            var tiles = new List<TileRef>();

            for (var x = gridTileLb.X; x <= gridTileRt.X; x++)
            {
                for (var y = gridTileLb.Y; y <= gridTileRt.Y; y++)
                {
                    var gridChunk = GridTileToChunkIndices(new MapIndices(x, y));

                    if (_chunks.TryGetValue(gridChunk, out var chunk))
                    {
                        var chunkTile = chunk.GridTileToChunkTile(new MapIndices(x, y));
                        var tile = chunk.GetTileRef((ushort)chunkTile.X, (ushort)chunkTile.Y);

                        if (ignoreEmpty && tile.Tile.IsEmpty)
                            continue;

                        if (predicate == null || predicate(tile))
                        {
                            tiles.Add(tile);
                        }
                    }
                    else if (!ignoreEmpty)
                    {
                        var tile = new TileRef(ParentMapId, Index, x, y, new Tile());

                        if (predicate == null || predicate(tile))
                        {
                            tiles.Add(tile);
                        }
                    }
                }
            }
            return tiles;
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetTilesIntersecting(Circle worldArea, bool ignoreEmpty = true, Predicate<TileRef> predicate = null)
        {
            var aabb = new Box2(worldArea.Position.X - worldArea.Radius, worldArea.Position.Y - worldArea.Radius, worldArea.Position.X + worldArea.Radius, worldArea.Position.Y + worldArea.Radius);

            foreach(var tile in GetTilesIntersecting(aabb, ignoreEmpty))
            {
                if (GridTileToLocal(tile.GridIndices).Distance(_mapManager, new GridCoordinates(worldArea.Position,tile.GridIndex)) <= worldArea.Radius)
                {
                    if (predicate == null || predicate(tile))
                    {
                        yield return tile;
                    }
                }
            }
        }

        #endregion TileAccess

        #region ChunkAccess

        public void RegenerateCollision()
        {
            throw new NotImplementedException();
        }

        /// <summary>
        ///     The total number of allocated chunks in the grid.
        /// </summary>
        public int ChunkCount => _chunks.Count;

        /// <inheritdoc />
        public IMapChunkInternal GetChunk(int xIndex, int yIndex)
        {
            return GetChunk(new MapIndices(xIndex, yIndex));
        }

        /// <inheritdoc />
        public IMapChunkInternal GetChunk(MapIndices chunkIndices)
        {
            if (_chunks.TryGetValue(chunkIndices, out var output))
                return output;

            return _chunks[chunkIndices] = new MapChunk(this, chunkIndices.X, chunkIndices.Y, ChunkSize);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<MapIndices, IMapChunkInternal> GetMapChunks()
        {
            return _chunks;
        }

        #endregion ChunkAccess

        #region SnapGridAccess

        /// <inheritdoc />
        public IEnumerable<SnapGridComponent> GetSnapGridCell(GridCoordinates worldPos, SnapGridOffset offset)
        {
            return GetSnapGridCell(SnapGridCellFor(worldPos, offset), offset);
        }

        /// <inheritdoc />
        public IEnumerable<SnapGridComponent> GetSnapGridCell(MapIndices pos, SnapGridOffset offset)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
            return chunk.GetSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, offset);
        }

        /// <inheritdoc />
        public MapIndices SnapGridCellFor(GridCoordinates gridPos, SnapGridOffset offset)
        {
            DebugTools.Assert(ParentMapId == _mapManager.GetGrid(gridPos.GridID).ParentMapId);

            var local = WorldToLocal(gridPos.ToMapPos(_mapManager));
            return SnapGridCellFor(local, offset);
        }

        /// <inheritdoc />
        public MapIndices SnapGridCellFor(MapCoordinates worldPos, SnapGridOffset offset)
        {
            DebugTools.Assert(ParentMapId == worldPos.MapId);

            var localPos = WorldToLocal(worldPos.Position);
            return SnapGridCellFor(localPos, offset);
        }

        /// <inheritdoc />
        public MapIndices SnapGridCellFor(Vector2 localPos, SnapGridOffset offset)
        {
            if (offset == SnapGridOffset.Edge)
            {
                localPos += new Vector2(TileSize / 2f, TileSize / 2f);
            }
            var x = (int)Math.Floor(localPos.X / TileSize);
            var y = (int)Math.Floor(localPos.Y / TileSize);
            return new MapIndices(x, y);
        }

        /// <inheritdoc />
        public void AddToSnapGridCell(MapIndices pos, SnapGridOffset offset, SnapGridComponent snap)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
            chunk.AddToSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, offset, snap);
        }

        /// <inheritdoc />
        public void AddToSnapGridCell(GridCoordinates worldPos, SnapGridOffset offset, SnapGridComponent snap)
        {
            AddToSnapGridCell(SnapGridCellFor(worldPos, offset), offset, snap);
        }

        /// <inheritdoc />
        public void RemoveFromSnapGridCell(MapIndices pos, SnapGridOffset offset, SnapGridComponent snap)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
            chunk.RemoveFromSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, offset, snap);
        }

        /// <inheritdoc />
        public void RemoveFromSnapGridCell(GridCoordinates worldPos, SnapGridOffset offset, SnapGridComponent snap)
        {
            RemoveFromSnapGridCell(SnapGridCellFor(worldPos, offset), offset, snap);
        }

        private (IMapChunk, MapIndices) ChunkAndOffsetForTile(MapIndices pos)
        {
            var gridChunkIndices = GridTileToChunkIndices(pos);
            var chunk = GetChunk(gridChunkIndices);
            var chunkTile = chunk.GridTileToChunkTile(pos);
            return (chunk, chunkTile);
        }

        #endregion

        #region Transforms

        /// <inheritdoc />
        public Vector2 WorldToLocal(Vector2 posWorld)
        {
            return posWorld - WorldPosition;
        }

        /// <inheritdoc />
        public GridCoordinates LocalToWorld(GridCoordinates posLocal)
        {
            return new GridCoordinates(posLocal.Position + WorldPosition,
                _mapManager.GetDefaultGridId(_mapManager.GetGrid(posLocal.GridID).ParentMapId));
        }

        /// <inheritdoc />
        public Vector2 LocalToWorld(Vector2 posLocal)
        {
            return posLocal + WorldPosition;
        }

        public MapIndices WorldToTile(Vector2 posWorld)
        {
            var local = WorldToLocal(posWorld);
            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new MapIndices(x, y);
        }

        /// <summary>
        ///     Transforms global world coordinates to tile indices relative to grid origin.
        /// </summary>
        public MapIndices WorldToTile(GridCoordinates gridPos)
        {
            var local = WorldToLocal(gridPos.ToMapPos(_mapManager));
            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new MapIndices(x, y);
        }

        /// <summary>
        ///     Transforms global world coordinates to chunk indices relative to grid origin.
        /// </summary>
        public MapIndices LocalToChunkIndices(GridCoordinates gridPos)
        {
            var local = WorldToLocal(gridPos.ToMapPos(_mapManager));
            var x = (int)Math.Floor(local.X / (TileSize * ChunkSize));
            var y = (int)Math.Floor(local.Y / (TileSize * ChunkSize));
            return new MapIndices(x, y);
        }

        public bool CollidesWithGrid(MapIndices indices)
        {
            var chunkIndices = GridTileToChunkIndices(indices);
            if (!_chunks.TryGetValue(chunkIndices, out var chunk))
                return false;

            var cTileIndices = chunk.GridTileToChunkTile(indices);
            return chunk.CollidesWithChunk(cTileIndices);
        }

        public bool CollidesWithGrid(Box2 aabb)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public MapIndices GridTileToChunkIndices(MapIndices gridTile)
        {
            var x = (int)Math.Floor(gridTile.X / (float)ChunkSize);
            var y = (int)Math.Floor(gridTile.Y / (float)ChunkSize);

            return new MapIndices(x, y);
        }

        /// <inheritdoc />
        public GridCoordinates GridTileToLocal(MapIndices gridTile)
        {
            return new GridCoordinates(gridTile.X * TileSize + (TileSize / 2f), gridTile.Y * TileSize + (TileSize / 2f), this);
        }

        public Vector2 GridTileToWorldPos(MapIndices gridTile)
        {
            var locX = gridTile.X * TileSize + (TileSize / 2f);
            var locY = gridTile.Y * TileSize + (TileSize / 2f);

            return new Vector2(locX, locY) + WorldPosition;
        }

        public MapCoordinates GridTileToWorld(MapIndices gridTile)
        {
            return new MapCoordinates(GridTileToWorldPos(gridTile), ParentMapId);
        }

        /// <inheritdoc />
        public bool TryGetTileRef(MapIndices indices, out TileRef tile)
        {
            var chunkIndices = GridTileToChunkIndices(indices);
            if (!_chunks.TryGetValue(chunkIndices, out var chunk))
            {
                tile = default;
                return false;
            }

            var cTileIndices = chunk.GridTileToChunkTile(indices);
            tile = chunk.GetTileRef(cTileIndices);
            return true;
        }

        #endregion Transforms
    }
}
