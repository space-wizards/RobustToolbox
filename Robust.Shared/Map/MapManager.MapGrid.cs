using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects.Components.Transform;
using Robust.Shared.Interfaces.Map;
using Robust.Shared.Maths;
using Robust.Shared.Timing;

namespace Robust.Shared.Map
{
    /// <inheritdoc cref="IMapManager"/>
    public partial class MapManager
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
            public GameTick LastModifiedTick { get; set; }

            public GameTick CurTick => _mapManager.GameTiming.CurTick;

            /// <inheritdoc />
            public bool IsDefaultGrid => ParentMap.DefaultGrid == this;

            /// <inheritdoc />
            public IMap ParentMap => _mapManager.GetMap(ParentMapId);

            /// <inheritdoc />
            public MapId ParentMapId { get; set; }

            /// <summary>
            ///     Grid chunks than make up this grid.
            /// </summary>
            internal readonly Dictionary<MapIndices, MapChunk> _chunks = new Dictionary<MapIndices, MapChunk>();

            private readonly IMapManagerInternal _mapManager;
            private Vector2 _worldPosition;

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

            private Box2 LocalBounds = new Box2();

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
                get => _worldPosition;
                set
                {
                    _worldPosition = value;
                    LastModifiedTick = _mapManager.GameTiming.CurTick;
                }
            }

            /// <summary>
            /// Expands the AABB for this grid when a new tile is added. If the tile is already inside the existing AABB,
            /// nothing happens. If it is outside, the AABB is expanded to fit the new tile.
            /// </summary>
            /// <param name="gridTile">The new tile to check.</param>
            /// <param name="empty"></param>
            public void UpdateAABB(MapIndices gridTile, bool empty)
            {
                var tileBounds = Box2.UnitCentered;
                var localTilePos = GridTileToLocal(gridTile).Position;
                tileBounds = tileBounds.Scale(TileSize).Translated(localTilePos);

                if (!empty)
                    LocalBounds = LocalBounds.Union(tileBounds); // expand if needed
                else
                {
                    // check if at corner of bounds, only corners would change the grid bounds
                    bool atCorner =
                        tileBounds.BottomLeft == LocalBounds.BottomLeft ||
                        tileBounds.BottomRight == LocalBounds.BottomRight ||
                        tileBounds.TopLeft == LocalBounds.TopLeft ||
                        tileBounds.TopRight == LocalBounds.TopRight;

                    if (!atCorner) // removing did nothing
                        return;

                    // get walk directions

                    // walk the direction to find if we are the only tile in the row/column

                    // if only one, shrink bound side

                    // else, do nothing

                }
            }

            /// <inheritdoc />
            public void NotifyTileChanged(in TileRef tileRef, in Tile oldTile)
            {
                LastModifiedTick = _mapManager.GameTiming.CurTick;
                UpdateAABB(tileRef.GridIndices, tileRef.Tile.IsEmpty);
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
            public TileRef GetTile(GridCoordinates worldPos)
            {
                return GetTile(WorldToTile(worldPos));
            }

            /// <inheritdoc />
            public TileRef GetTile(MapIndices tileCoordinates)
            {
                var chunkIndices = GridTileToGridChunk(tileCoordinates);

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
                SetTile(localTile.X, localTile.Y, tile);
            }

            private void SetTile(int xIndex, int yIndex, Tile tile)
            {
                var (chunk, chunkTile) = ChunkAndOffsetForTile(new MapIndices(xIndex, yIndex));
                chunk.SetTile((ushort)chunkTile.X, (ushort)chunkTile.Y, tile);
            }

            /// <inheritdoc />
            public void SetTile(GridCoordinates worldPos, ushort tileId, ushort tileData = 0)
            {
                SetTile(worldPos, new Tile(tileId, tileData));
            }

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
                        var gridChunk = GridTileToGridChunk(new MapIndices(x, y));

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

            #endregion TileAccess

            #region ChunkAccess

            /// <summary>
            ///     The total number of allocated chunks in the grid.
            /// </summary>
            public int ChunkCount => _chunks.Count;

            /// <inheritdoc />
            public IMapChunk GetChunk(int xIndex, int yIndex)
            {
                return GetChunk(new MapIndices(xIndex, yIndex));
            }

            /// <inheritdoc />
            public IMapChunk GetChunk(MapIndices chunkIndices)
            {
                if (_chunks.TryGetValue(chunkIndices, out var output))
                    return output;

                return _chunks[chunkIndices] = new MapChunk(this, chunkIndices.X, chunkIndices.Y, ChunkSize);
            }

            /// <inheritdoc />
            public IEnumerable<IMapChunk> GetMapChunks()
            {
                foreach (var kvChunk in _chunks)
                {
                    yield return kvChunk.Value;
                }
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
            public MapIndices SnapGridCellFor(GridCoordinates worldPos, SnapGridOffset offset)
            {
                var local = worldPos.ConvertToGrid(_mapManager, this);
                if (offset == SnapGridOffset.Edge)
                {
                    local = local.Offset(new Vector2(TileSize / 2f, TileSize / 2f));
                }
                var x = (int)Math.Floor(local.X / TileSize);
                var y = (int)Math.Floor(local.Y / TileSize);
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
                var gridChunkIndices = GridTileToGridChunk(pos);
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
                return new GridCoordinates(posLocal.Position + WorldPosition, _mapManager.GetGrid(posLocal.GridID).ParentMap);
            }

            /// <inheritdoc />
            public Vector2 ConvertToWorld(Vector2 posLocal)
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
            public MapIndices WorldToTile(GridCoordinates posWorld)
            {
                var local = posWorld.ConvertToGrid(_mapManager, this);
                var x = (int)Math.Floor(local.X / TileSize);
                var y = (int)Math.Floor(local.Y / TileSize);
                return new MapIndices(x, y);
            }

            /// <summary>
            ///     Transforms global world coordinates to chunk indices relative to grid origin.
            /// </summary>
            public MapIndices WorldToChunk(GridCoordinates posWorld)
            {
                var local = posWorld.ConvertToGrid(_mapManager, this);
                var x = (int)Math.Floor(local.X / (TileSize * ChunkSize));
                var y = (int)Math.Floor(local.Y / (TileSize * ChunkSize));
                return new MapIndices(x, y);
            }

            /// <inheritdoc />
            public MapIndices GridTileToGridChunk(MapIndices gridTile)
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

            /// <inheritdoc />
            public bool IndicesToTile(MapIndices indices, out TileRef tile)
            {
                var chunkIndices = new MapIndices(indices.X / ChunkSize, indices.Y / ChunkSize);
                if (!_chunks.ContainsKey(chunkIndices))
                {
                    tile = new TileRef(); //Nothing should ever use or access this, bool check should occur first
                    return false;
                }
                var chunk = _chunks[chunkIndices];
                tile = chunk.GetTileRef(new MapIndices(indices.X % ChunkSize, indices.Y % ChunkSize));
                return true;
            }

            #endregion Transforms
        }
    }
}
