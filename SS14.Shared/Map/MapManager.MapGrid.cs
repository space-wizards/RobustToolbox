using System;
using System.Collections.Generic;
using SS14.Shared.GameObjects.Components.Transform;
using SS14.Shared.Interfaces.Map;
using SS14.Shared.Maths;

namespace SS14.Shared.Map
{
    public partial class MapManager
    {
        public class MapGrid : IMapGrid
        {
            public bool IsDefaultGrid => Map.DefaultGrid == this;
            public IMap Map => _mapManager.GetMap(MapID);
            public MapId MapID { get; private set; }
            private readonly MapManager _mapManager;
            private readonly Dictionary<MapIndices, Chunk> _chunks = new Dictionary<MapIndices, Chunk>();

            internal MapGrid(MapManager mapManager, GridId gridIndex, ushort chunkSize, float snapsize, MapId mapID)
            {
                _mapManager = mapManager;
                Index = gridIndex;
                ChunkSize = chunkSize;
                SnapSize = snapsize;
                MapID = mapID;
            }

            /// <summary>
            /// Disposes the grid.
            /// </summary>
            public void Dispose()
            {
                // Nothing for now.
            }

            /// <inheritdoc />
            public Box2 AABBWorld { get; private set; }

            /// <inheritdoc />
            public ushort ChunkSize { get; }

            /// <inheritdoc />
            public float SnapSize { get; }

            public GridId Index { get; }

            /// <summary>
            ///     The length of the side of a square tile in world units.
            /// </summary>
            public ushort TileSize { get; set; } = 1;

            /// <inheritdoc />
            public Vector2 WorldPosition { get; set; }

            /// <summary>
            /// Expands the AABB for this grid when a new tile is added. If the tile is already inside the existing AABB,
            /// nothing happens. If it is outside, the AABB is expanded to fit the new tile.
            /// </summary>
            /// <param name="gridTile">The new tile to check.</param>
            public void UpdateAABB(MapIndices gridTile)
            {
                var worldPos = GridTileToLocal(gridTile).ToWorld();

                if (AABBWorld.Contains(worldPos.Position))
                    return;

                // rect union
                var a = AABBWorld;
                var b = worldPos;

                var min_x = Math.Min(a.Left, b.X);
                var max_x = Math.Max(a.Right, b.X);

                var min_y = Math.Min(a.Top, b.Y);
                var max_y = Math.Max(a.Bottom, b.Y);

                AABBWorld = Box2.FromDimensions(min_x, min_y, max_x - min_x, max_y - min_y);
            }

            public bool OnSnapCenter(Vector2 position)
            {
                return (FloatMath.CloseTo(position.X % SnapSize, 0) && FloatMath.CloseTo(position.Y % SnapSize, 0));
            }

            public bool OnSnapBorder(Vector2 position)
            {
                return (FloatMath.CloseTo(position.X % SnapSize, SnapSize / 2) && FloatMath.CloseTo(position.Y % SnapSize, SnapSize / 2));
            }

            #region TileAccess

            /// <inheritdoc />
            public TileRef GetTile(GridLocalCoordinates worldPos)
            {
                var chunkIndices = WorldToChunk(worldPos);
                var gridTileIndices = WorldToTile(worldPos);

                Chunk output;
                if (_chunks.TryGetValue(chunkIndices, out output))
                {
                    var chunkTileIndices = output.GridTileToChunkTile(gridTileIndices);
                    return output.GetTile((ushort)chunkTileIndices.X, (ushort)chunkTileIndices.Y);
                }
                return new TileRef(MapID, Index, gridTileIndices.X, gridTileIndices.Y, default(Tile));
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
            public void SetTile(GridLocalCoordinates worldPos, Tile tile)
            {
                var localTile = WorldToTile(worldPos);
                SetTile(localTile.X, localTile.Y, tile);
            }

            /// <inheritdoc />
            public void SetTile(int xIndex, int yIndex, Tile tile)
            {
                var (chunk, chunkTile) = ChunkAndOffsetForTile(new MapIndices(xIndex, yIndex));
                chunk.SetTile((ushort)chunkTile.X, (ushort)chunkTile.Y, tile);
            }

            /// <inheritdoc />
            public void SetTile(GridLocalCoordinates worldPos, ushort tileId, ushort tileData = 0)
            {
                SetTile(worldPos, new Tile(tileId, tileData));
            }

            /// <inheritdoc />
            public IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true, Predicate<TileRef> predicate = null)
            {
                //TODO: needs world -> local -> tile translations.
                var gridTileLt = new MapIndices((int)Math.Floor(worldArea.Left), (int)Math.Floor(worldArea.Top));
                var gridTileRb = new MapIndices((int)Math.Floor(worldArea.Right), (int)Math.Floor(worldArea.Bottom));

                var tiles = new List<TileRef>();

                for (var x = gridTileLt.X; x <= gridTileRb.X; x++)
                {
                    for (var y = gridTileLt.Y; y <= gridTileRb.Y; y++)
                    {
                        var gridChunk = GridTileToGridChunk(new MapIndices(x, y));
                        Chunk chunk;
                        if (_chunks.TryGetValue(gridChunk, out chunk))
                        {
                            var chunkTile = chunk.GridTileToChunkTile(new MapIndices(x, y));
                            var tile = chunk.GetTile((ushort)chunkTile.X, (ushort)chunkTile.Y);

                            if (ignoreEmpty && tile.Tile.IsEmpty)
                                continue;

                            if (predicate == null || predicate(tile))
                            {
                                tiles.Add(tile);
                            }
                        }
                        else if (!ignoreEmpty)
                        {
                            var tile = new TileRef(MapID, Index, x, y, new Tile());

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
            /// The total number of allocated chunks in the grid.
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
                Chunk output;
                if (_chunks.TryGetValue(chunkIndices, out output))
                    return output;

                return _chunks[chunkIndices] = new Chunk(_mapManager, this, chunkIndices.X, chunkIndices.Y, ChunkSize);
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

            public IEnumerable<SnapGridComponent> GetSnapGridCell(GridLocalCoordinates worldPos, SnapGridOffset offset)
            {
                return GetSnapGridCell(SnapGridCellFor(worldPos, offset), offset);
            }

            public IEnumerable<SnapGridComponent> GetSnapGridCell(MapIndices pos, SnapGridOffset offset)
            {
                var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
                return chunk.GetSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, offset);
            }

            public MapIndices SnapGridCellFor(GridLocalCoordinates worldPos, SnapGridOffset offset)
            {
                var local = worldPos.ConvertToGrid(this);
                if (offset == SnapGridOffset.Edge)
                {
                    local = local.Offset(new Vector2(TileSize / 2f, TileSize / 2f));
                }
                var x = (int)Math.Floor(local.X / TileSize);
                var y = (int)Math.Floor(local.Y / TileSize);
                return new MapIndices(x, y);
            }

            public void AddToSnapGridCell(MapIndices pos, SnapGridOffset offset, SnapGridComponent snap)
            {
                var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
                chunk.AddToSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, offset, snap);
            }

            public void AddToSnapGridCell(GridLocalCoordinates worldPos, SnapGridOffset offset, SnapGridComponent snap)
            {
                AddToSnapGridCell(SnapGridCellFor(worldPos, offset), offset, snap);
            }

            public void RemoveFromSnapGridCell(MapIndices pos, SnapGridOffset offset, SnapGridComponent snap)
            {
                var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
                chunk.RemoveFromSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, offset, snap);
            }

            public void RemoveFromSnapGridCell(GridLocalCoordinates worldPos, SnapGridOffset offset, SnapGridComponent snap)
            {
                RemoveFromSnapGridCell(SnapGridCellFor(worldPos, offset), offset, snap);
            }


            (IMapChunk, MapIndices) ChunkAndOffsetForTile(MapIndices pos)
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
            public GridLocalCoordinates LocalToWorld(GridLocalCoordinates posLocal)
            {
                return new GridLocalCoordinates(posLocal.Position + WorldPosition, posLocal.MapID);
            }

            public Vector2 ConvertToWorld(Vector2 localpos)
            {
                return localpos + WorldPosition;
            }

            /// <summary>
            /// Transforms global world coordinates to tile indices relative to grid origin.
            /// </summary>
            /// <returns></returns>
            public MapIndices WorldToTile(GridLocalCoordinates posWorld)
            {
                var local = posWorld.ConvertToGrid(this);
                var x = (int)Math.Floor(local.X / TileSize);
                var y = (int)Math.Floor(local.Y / TileSize);
                return new MapIndices(x, y);
            }

            /// <summary>
            /// Transforms global world coordinates to chunk indices relative to grid origin.
            /// </summary>
            /// <param name="localPos">The position in the world.</param>
            /// <returns></returns>
            public MapIndices WorldToChunk(GridLocalCoordinates posWorld)
            {
                var local = posWorld.ConvertToGrid(this);
                var x = (int)Math.Floor(local.X / (TileSize * ChunkSize));
                var y = (int)Math.Floor(local.Y / (TileSize * ChunkSize));
                return new MapIndices(x, y);
            }

            /// <summary>
            /// Transforms grid tile indices to grid chunk indices.
            /// </summary>
            /// <param name="gridTile"></param>
            /// <returns></returns>
            public MapIndices GridTileToGridChunk(MapIndices gridTile)
            {
                var x = (int)Math.Floor(gridTile.X / (float)ChunkSize);
                var y = (int)Math.Floor(gridTile.Y / (float)ChunkSize);

                return new MapIndices(x, y);
            }

            /// <inheritdoc />
            public GridLocalCoordinates GridTileToLocal(MapIndices gridTile)
            {
                return new GridLocalCoordinates(gridTile.X * TileSize + (TileSize / 2), gridTile.Y * TileSize + (TileSize / 2), this);
            }

            /// <inheritdoc />
            public bool IndicesToTile(MapIndices indices, out TileRef tile)
            {
                MapIndices chunkindices = new MapIndices(indices.X / ChunkSize, indices.Y / ChunkSize);
                if (!_chunks.ContainsKey(chunkindices))
                {
                    tile = new TileRef(); //Nothing should ever use or access this, bool check should occur first
                    return false;
                }
                Chunk chunk = _chunks[chunkindices];
                tile = chunk.GetTile(new MapIndices(indices.X % ChunkSize, indices.Y % ChunkSize));
                return true;
            }

            #endregion Transforms
        }
    }
}
