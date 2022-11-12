using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Utility;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     This is a collection of tiles in a grid format.
    /// </summary>
    public sealed partial class MapGridComponent
    {
        #region TileAccess

        /// <summary>
        ///     Gets a tile a the given world coordinates. This will not create a new chunk.
        /// </summary>
        /// <param name="coords">The location of the tile in coordinates.</param>
        /// <returns>The tile at the world coordinates.</returns>
        public TileRef GetTileRef(MapCoordinates coords)
        {
            return GetTileRef(CoordinatesToTile(coords));
        }

        /// <summary>
        ///     Gets a tile a the given world coordinates. This will not create a new chunk.
        /// </summary>
        /// <param name="coords">The location of the tile in coordinates.</param>
        /// <returns>The tile at the world coordinates.</returns>
        public TileRef GetTileRef(EntityCoordinates coords)
        {
            return GetTileRef(CoordinatesToTile(coords));
        }

        /// <summary>
        ///     Gets a tile a the given grid indices. This will not create a new chunk.
        /// </summary>
        /// <param name="tileCoordinates">The location of the tile in coordinates.</param>
        /// <returns>The tile at the tile coordinates.</returns>
        public TileRef GetTileRef(Vector2i tileCoordinates)
        {
            var chunkIndices = GridTileToChunkIndices(tileCoordinates);

            if (!_chunks.TryGetValue(chunkIndices, out var output))
            {
                // Chunk doesn't exist, return a tileRef to an empty (space) tile.
                return new TileRef(Owner, tileCoordinates.X, tileCoordinates.Y, default);
            }

            var chunkTileIndices = output.GridTileToChunkTile(tileCoordinates);
            return GetTileRef(output, (ushort)chunkTileIndices.X, (ushort)chunkTileIndices.Y);
        }

        /// <summary>
        ///     Returns the tile at the given chunk indices.
        /// </summary>
        /// <param name="mapChunk"></param>
        /// <param name="xIndex">The X tile index relative to the chunk origin.</param>
        /// <param name="yIndex">The Y tile index relative to the chunk origin.</param>
        /// <returns>A reference to a tile.</returns>
        internal TileRef GetTileRef(MapChunk mapChunk, ushort xIndex, ushort yIndex)
        {
            if (xIndex >= mapChunk.ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

            if (yIndex >= mapChunk.ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

            var indices = mapChunk.ChunkTileToGridTile(new Vector2i(xIndex, yIndex));
            return new TileRef(Owner, indices, mapChunk.GetTile(xIndex, yIndex));
        }

        /// <summary>
        ///     Returns all tiles in the grid, in row-major order [xTileIndex, yTileIndex].
        /// </summary>
        /// <returns>All tiles in the chunk.</returns>
        public IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true)
        {
            foreach (var kvChunk in _chunks)
            {
                var chunk = kvChunk.Value;
                for (ushort x = 0; x < ChunkSize; x++)
                {
                    for (ushort y = 0; y < ChunkSize; y++)
                    {
                        var tile = chunk.GetTile(x, y);

                        if (ignoreEmpty && tile.IsEmpty)
                            continue;

                        var (gridX, gridY) = new Vector2i(x, y) + chunk.Indices * ChunkSize;
                        yield return new TileRef(Owner, gridX, gridY, tile);
                    }
                }
            }
        }

        /// <summary>
        ///     Returns an enumerator that gets all tiles in the grid without empty ones, in row-major order [xTileIndex, yTileIndex].
        /// </summary>
        public GridTileEnumerator GetAllTilesEnumerator(bool ignoreEmpty = true)
        {
            return new GridTileEnumerator(Owner, _chunks.GetEnumerator(), ChunkSize, ignoreEmpty);
        }

        /// <summary>
        ///     Replaces a single tile inside of the grid.
        /// </summary>
        /// <param name="coords"></param>
        /// <param name="tile">The tile to insert at the coordinates.</param>
        public void SetTile(EntityCoordinates coords, Tile tile)
        {
            var localTile = CoordinatesToTile(coords);
            SetTile(new Vector2i(localTile.X, localTile.Y), tile);
        }

        /// <summary>
        ///     Modifies a single tile inside of the chunk.
        /// </summary>
        /// <param name="gridIndices"></param>
        /// <param name="tile">The tile to insert at the coordinates.</param>
        public void SetTile(Vector2i gridIndices, Tile tile)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(gridIndices);
            chunk.SetTile((ushort)chunkTile.X, (ushort)chunkTile.Y, tile);
        }

        /// <summary>
        ///     Modifies many tiles inside of a chunk. Avoids regenerating collision until the end.
        /// </summary>
        /// <param name="tiles"></param>
        public void SetTiles(List<(Vector2i GridIndices, Tile Tile)> tiles)
        {
            if (tiles.Count == 0) return;

            var chunks = new HashSet<MapChunk>();

            foreach (var (gridIndices, tile) in tiles)
            {
                var (chunk, chunkTile) = ChunkAndOffsetForTile(gridIndices);
                chunks.Add(chunk);
                chunk.SuppressCollisionRegeneration = true;
                chunk.SetTile((ushort)chunkTile.X, (ushort)chunkTile.Y, tile);
            }

            foreach (var chunk in chunks)
            {
                chunk.SuppressCollisionRegeneration = false;
            }

            RegenerateCollision(chunks);
        }

        public IEnumerable<TileRef> GetLocalTilesIntersecting(Box2Rotated localArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            var localAABB = localArea.CalcBoundingBox();
            return GetLocalTilesIntersecting(localAABB, ignoreEmpty, predicate);
        }

        public IEnumerable<TileRef> GetTilesIntersecting(Box2Rotated worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            var matrix = _entMan.GetComponent<TransformComponent>(Owner).InvWorldMatrix;
            var localArea = matrix.TransformBox(worldArea);

            foreach (var tile in GetLocalTilesIntersecting(localArea, ignoreEmpty, predicate))
            {
                yield return tile;
            }
        }

        /// <summary>
        ///     Returns all tiles inside the area that match the predicate.
        /// </summary>
        /// <param name="worldArea">An area in the world to search for tiles.</param>
        /// <param name="ignoreEmpty">Will empty tiles be returned?</param>
        /// <param name="predicate">Optional predicate to filter the files.</param>
        /// <returns></returns>
        public IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            var matrix = _entMan.GetComponent<TransformComponent>(Owner).InvWorldMatrix;
            var localArea = matrix.TransformBox(worldArea);

            foreach (var tile in GetLocalTilesIntersecting(localArea, ignoreEmpty, predicate))
            {
                yield return tile;
            }
        }

        public IEnumerable<TileRef> GetLocalTilesIntersecting(Box2 localArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            // TODO: Should move the intersecting calls onto mapmanager system and then allow people to pass in xform / xformquery
            // that way we can avoid the GetComp here.
            var gridTileLb = new Vector2i((int)Math.Floor(localArea.Left), (int)Math.Floor(localArea.Bottom));
            // If we have 20.1 we want to include that tile but if we have 20 then we don't.
            var gridTileRt = new Vector2i((int)Math.Ceiling(localArea.Right), (int)Math.Ceiling(localArea.Top));

            for (var x = gridTileLb.X; x < gridTileRt.X; x++)
            {
                for (var y = gridTileLb.Y; y < gridTileRt.Y; y++)
                {
                    var gridChunk = GridTileToChunkIndices(new Vector2i(x, y));

                    if (_chunks.TryGetValue(gridChunk, out var chunk))
                    {
                        var chunkTile = chunk.GridTileToChunkTile(new Vector2i(x, y));
                        var tile = GetTileRef(chunk, (ushort)chunkTile.X, (ushort)chunkTile.Y);

                        if (ignoreEmpty && tile.Tile.IsEmpty)
                            continue;

                        if (predicate == null || predicate(tile))
                            yield return tile;
                    }
                    else if (!ignoreEmpty)
                    {
                        var tile = new TileRef(Owner, x, y, new Tile());

                        if (predicate == null || predicate(tile))
                            yield return tile;
                    }
                }
            }
        }

        public IEnumerable<TileRef> GetTilesIntersecting(Circle worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            var aabb = new Box2(worldArea.Position.X - worldArea.Radius, worldArea.Position.Y - worldArea.Radius,
                worldArea.Position.X + worldArea.Radius, worldArea.Position.Y + worldArea.Radius);
            var circleGridPos = new EntityCoordinates(Owner, WorldToLocal(worldArea.Position));

            foreach (var tile in GetTilesIntersecting(aabb, ignoreEmpty))
            {
                var local = GridTileToLocal(tile.GridIndices);

                if (!local.TryDistance(_entMan, circleGridPos, out var distance))
                {
                    continue;
                }

                if (distance <= worldArea.Radius)
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

        /// <summary>
        ///     The total number of chunks contained on this grid.
        /// </summary>
        public int ChunkCount => _chunks.Count;

        /// <summary>
        ///     Returns the chunk at the given indices. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="xIndex">The X index of the chunk in this grid.</param>
        /// <param name="yIndex">The Y index of the chunk in this grid.</param>
        /// <returns>The existing or new chunk.</returns>
        internal MapChunk GetChunk(int xIndex, int yIndex)
        {
            return GetChunk(new Vector2i(xIndex, yIndex));
        }

        /// <summary>
        /// Removes the chunk with the specified origin.
        /// </summary>
        internal void RemoveChunk(Vector2i origin)
        {
            if (!_chunks.TryGetValue(origin, out var chunk)) return;

            chunk.Fixtures.Clear();
            _chunks.Remove(origin);

            _chunkDeletionHistory.Add((_gameTiming.CurTick, chunk.Indices));

            if (_chunks.Count == 0)
            {
                _entMan.EventBus.RaiseLocalEvent(Owner, new EmptyGridEvent { GridId = Owner }, true);
            }
        }

        /// <summary>
        ///     Tries to return a chunk at the given indices.
        /// </summary>
        /// <param name="chunkIndices">The indices of the chunk in this grid.</param>
        /// <param name="chunk">The existing chunk.</param>
        /// <returns>If the chunk exists.</returns>
        internal bool TryGetChunk(Vector2i chunkIndices, [NotNullWhen(true)] out MapChunk? chunk)
        {
            return _chunks.TryGetValue(chunkIndices, out chunk);
        }

        /// <summary>
        ///     Returns the chunk at the given indices. If the chunk does not exist,
        ///     then a new one is generated that is filled with empty space.
        /// </summary>
        /// <param name="chunkIndices">The indices of the chunk in this grid.</param>
        /// <returns>The existing or new chunk.</returns>
        internal MapChunk GetChunk(Vector2i chunkIndices)
        {
            if (_chunks.TryGetValue(chunkIndices, out var output))
                return output;

            var newChunk = new MapChunk(chunkIndices.X, chunkIndices.Y, ChunkSize)
            {
                LastTileModifiedTick = _gameTiming.CurTick
            };

            var mapSystem = _entMan.EntitySysManager.GetEntitySystem<SharedMapSystem>();
            newChunk.TileModified += (mapChunk, tileIndices, newTile, oldTile, shapeChanged) => OnTileModified(mapSystem, mapChunk, tileIndices, newTile, oldTile, shapeChanged);
            return _chunks[chunkIndices] = newChunk;
        }

        /// <summary>
        ///     Returns whether a chunk exists with the specified indices.
        /// </summary>
        public bool HasChunk(Vector2i chunkIndices)
        {
            return _chunks.ContainsKey(chunkIndices);
        }

        /// <summary>
        ///     Returns all chunks in this grid. This will not generate new chunks.
        /// </summary>
        /// <returns>All chunks in the grid.</returns>
        internal IReadOnlyDictionary<Vector2i, MapChunk> GetMapChunks()
        {
            return _chunks;
        }

        internal struct ChunkEnumerator
        {
            private readonly Dictionary<Vector2i, MapChunk> _chunks;
            private readonly Vector2i _chunkLB;
            private readonly Vector2i _chunkRT;

            private int _xIndex;
            private int _yIndex;

            internal ChunkEnumerator(Dictionary<Vector2i, MapChunk> chunks, Box2 localAABB, int chunkSize)
            {
                _chunks = chunks;

                _chunkLB = new Vector2i((int)Math.Floor(localAABB.Left / chunkSize),
                    (int)Math.Floor(localAABB.Bottom / chunkSize));
                _chunkRT = new Vector2i((int)Math.Floor(localAABB.Right / chunkSize),
                    (int)Math.Floor(localAABB.Top / chunkSize));

                _xIndex = _chunkLB.X;
                _yIndex = _chunkLB.Y;
            }

            public bool MoveNext([NotNullWhen(true)] out MapChunk? chunk)
            {
                if (_yIndex > _chunkRT.Y)
                {
                    _yIndex = _chunkLB.Y;
                    _xIndex += 1;
                }

                for (var x = _xIndex; x <= _chunkRT.X; x++)
                {
                    for (var y = _yIndex; y <= _chunkRT.Y; y++)
                    {
                        var gridChunk = new Vector2i(x, y);
                        if (!_chunks.TryGetValue(gridChunk, out chunk)) continue;
                        _xIndex = x;
                        _yIndex = y + 1;
                        return true;
                    }

                    _yIndex = _chunkLB.Y;
                }

                chunk = null;
                return false;
            }
        }

        /// <summary>
        ///     Returns all the <see cref="MapChunk"/> intersecting the worldAABB.
        /// </summary>
        internal ChunkEnumerator GetMapChunks(Box2 worldAABB)
        {
            var localAABB = _entMan.GetComponent<TransformComponent>(Owner).InvWorldMatrix.TransformBox(worldAABB);
            return new ChunkEnumerator(_chunks, localAABB, ChunkSize);
        }

        /// <summary>
        ///     Returns all the <see cref="MapChunk"/> intersecting the rotated world box.
        /// </summary>
        internal ChunkEnumerator GetMapChunks(Box2Rotated worldArea)
        {
            var matrix = _entMan.GetComponent<TransformComponent>(Owner).InvWorldMatrix;
            var localArea = matrix.TransformBox(worldArea);
            return new ChunkEnumerator(_chunks, localArea, ChunkSize);
        }

        internal ChunkEnumerator GetLocalMapChunks(Box2 localAABB)
        {
            return new ChunkEnumerator(_chunks, localAABB, ChunkSize);
        }

        #endregion ChunkAccess

        #region SnapGridAccess

        public int AnchoredEntityCount(Vector2i pos)
        {
            var gridChunkPos = GridTileToChunkIndices(pos);

            if (!_chunks.TryGetValue(gridChunkPos, out var chunk)) return 0;

            var (x, y) = chunk.GridTileToChunkTile(pos);
            return chunk.GetSnapGrid((ushort)x, (ushort)y)?.Count ?? 0; // ?
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(MapCoordinates coords)
        {
            return GetAnchoredEntities(TileIndicesFor(coords));
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(EntityCoordinates coords)
        {
            return GetAnchoredEntities(TileIndicesFor(coords));
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(Vector2i pos)
        {
            // Because some content stuff checks neighboring tiles (which may not actually exist) we won't just
            // create an entire chunk for it.
            var gridChunkPos = GridTileToChunkIndices(pos);

            if (!_chunks.TryGetValue(gridChunkPos, out var chunk)) return Enumerable.Empty<EntityUid>();

            var chunkTile = chunk.GridTileToChunkTile(pos);
            return chunk.GetSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y);
        }

        public AnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(Vector2i pos)
        {
            var gridChunkPos = GridTileToChunkIndices(pos);

            if (!_chunks.TryGetValue(gridChunkPos, out var chunk)) return AnchoredEntitiesEnumerator.Empty;

            var chunkTile = chunk.GridTileToChunkTile(pos);
            var snapGrid = chunk.GetSnapGrid((ushort)chunkTile.X, (ushort)chunkTile.Y);

            return snapGrid == null
                ? AnchoredEntitiesEnumerator.Empty
                : new AnchoredEntitiesEnumerator(snapGrid.GetEnumerator());
        }

        public IEnumerable<EntityUid> GetLocalAnchoredEntities(Box2 localAABB)
        {
            foreach (var tile in GetLocalTilesIntersecting(localAABB, true, null))
            {
                foreach (var ent in GetAnchoredEntities(tile.GridIndices))
                {
                    yield return ent;
                }
            }
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(Box2 worldAABB)
        {
            foreach (var tile in GetTilesIntersecting(worldAABB))
            {
                foreach (var ent in GetAnchoredEntities(tile.GridIndices))
                {
                    yield return ent;
                }
            }
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(Box2Rotated worldBounds)
        {
            foreach (var tile in GetTilesIntersecting(worldBounds))
            {
                foreach (var ent in GetAnchoredEntities(tile.GridIndices))
                {
                    yield return ent;
                }
            }
        }

        public Vector2i TileIndicesFor(EntityCoordinates coords)
        {
            DebugTools.Assert(_entMan.GetComponent<TransformComponent>(Owner).MapID == coords.GetMapId(_entMan));

            return SnapGridLocalCellFor(LocalToGrid(coords));
        }

        public Vector2i TileIndicesFor(MapCoordinates worldPos)
        {
            DebugTools.Assert(_entMan.GetComponent<TransformComponent>(Owner).MapID == worldPos.MapId);

            var localPos = WorldToLocal(worldPos.Position);
            return SnapGridLocalCellFor(localPos);
        }

        private Vector2i SnapGridLocalCellFor(Vector2 localPos)
        {
            var x = (int)Math.Floor(localPos.X / TileSize);
            var y = (int)Math.Floor(localPos.Y / TileSize);
            return new Vector2i(x, y);
        }

        public bool IsAnchored(EntityCoordinates coords, EntityUid euid)
        {
            var tilePos = TileIndicesFor(coords);
            var (chunk, chunkTile) = ChunkAndOffsetForTile(tilePos);
            var snapGrid = chunk.GetSnapGrid((ushort)chunkTile.X, (ushort)chunkTile.Y);
            return snapGrid?.Contains(euid) == true;
        }

        public bool AddToSnapGridCell(Vector2i pos, EntityUid euid)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);

            if (chunk.GetTile((ushort)chunkTile.X, (ushort)chunkTile.Y).IsEmpty)
                return false;

            chunk.AddToSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, euid);
            return true;
        }

        public bool AddToSnapGridCell(EntityCoordinates coords, EntityUid euid)
        {
            return AddToSnapGridCell(TileIndicesFor(coords), euid);
        }

        public void RemoveFromSnapGridCell(Vector2i pos, EntityUid euid)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
            chunk.RemoveFromSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, euid);
        }

        public void RemoveFromSnapGridCell(EntityCoordinates coords, EntityUid euid)
        {
            RemoveFromSnapGridCell(TileIndicesFor(coords), euid);
        }

        private (MapChunk, Vector2i) ChunkAndOffsetForTile(Vector2i pos)
        {
            var gridChunkIndices = GridTileToChunkIndices(pos);
            var chunk = GetChunk(gridChunkIndices);
            var chunkTile = chunk.GridTileToChunkTile(pos);
            return (chunk, chunkTile);
        }

        private static Vector2i SnapGridPosAt(Vector2i position, Direction dir, int dist = 1)
        {
            switch (dir)
            {
                case Direction.East:
                    return position + new Vector2i(dist, 0);
                case Direction.SouthEast:
                    return position + new Vector2i(dist, -dist);
                case Direction.South:
                    return position + new Vector2i(0, -dist);
                case Direction.SouthWest:
                    return position + new Vector2i(-dist, -dist);
                case Direction.West:
                    return position + new Vector2i(-dist, 0);
                case Direction.NorthWest:
                    return position + new Vector2i(-dist, dist);
                case Direction.North:
                    return position + new Vector2i(0, dist);
                case Direction.NorthEast:
                    return position + new Vector2i(dist, dist);
                default:
                    throw new ArgumentOutOfRangeException(nameof(dir), dir, null);
            }
        }

        /// <summary>
        ///     Returns an enumerable over all the entities which are one tile over in a certain direction.
        /// </summary>
        public IEnumerable<EntityUid> GetInDir(EntityCoordinates position, Direction dir)
        {
            var pos = SnapGridPosAt(TileIndicesFor(position), dir);
            return GetAnchoredEntities(pos);
        }

        public IEnumerable<EntityUid> GetOffset(EntityCoordinates coords, Vector2i offset)
        {
            var pos = TileIndicesFor(coords) + offset;
            return GetAnchoredEntities(pos);
        }

        public IEnumerable<EntityUid> GetLocal(EntityCoordinates coords)
        {
            return GetAnchoredEntities(TileIndicesFor(coords));
        }

        public EntityCoordinates DirectionToGrid(EntityCoordinates coords, Direction direction)
        {
            return GridTileToLocal(SnapGridPosAt(TileIndicesFor(coords), direction));
        }

        public IEnumerable<EntityUid> GetCardinalNeighborCells(EntityCoordinates coords)
        {
            var position = TileIndicesFor(coords);
            // ReSharper disable EnforceForeachStatementBraces
            foreach (var cell in GetAnchoredEntities(position))
                yield return cell;
            foreach (var cell in GetAnchoredEntities(position + new Vector2i(0, 1)))
                yield return cell;
            foreach (var cell in GetAnchoredEntities(position + new Vector2i(0, -1)))
                yield return cell;
            foreach (var cell in GetAnchoredEntities(position + new Vector2i(1, 0)))
                yield return cell;
            foreach (var cell in GetAnchoredEntities(position + new Vector2i(-1, 0)))
                yield return cell;
            // ReSharper restore EnforceForeachStatementBraces
        }

        public IEnumerable<EntityUid> GetCellsInSquareArea(EntityCoordinates coords, int n)
        {
            var position = TileIndicesFor(coords);

            for (var y = -n; y <= n; ++y)
            for (var x = -n; x <= n; ++x)
            {
                var enumerator = GetAnchoredEntitiesEnumerator(position + new Vector2i(x, y));

                while (enumerator.MoveNext(out var cell))
                {
                    yield return cell.Value;
                }
            }
        }

        #endregion

        #region Transforms

        /// <summary>
        ///     Transforms world-space coordinates from the global origin to the grid local origin.
        /// </summary>
        /// <param name="posWorld">The world-space coordinates with global origin.</param>
        /// <returns>The world-space coordinates with local origin.</returns>
        public Vector2 WorldToLocal(Vector2 posWorld)
        {
            return _entMan.GetComponent<TransformComponent>(Owner).InvWorldMatrix.Transform(posWorld);
        }

        /// <summary>
        /// Transforms map coordinates to grid coordinates.
        /// </summary>
        public EntityCoordinates MapToGrid(MapCoordinates posWorld)
        {
            if (posWorld.MapId != _entMan.GetComponent<TransformComponent>(Owner).MapID)
            {
                throw new ArgumentException($"Grid {Owner} is on map {_entMan.GetComponent<TransformComponent>(Owner).MapID}, but coords are on map {posWorld.MapId}.", nameof(posWorld));
            }

            if (!_mapManager.EntityManager.TryGetComponent<MapGridComponent>((EntityUid?) Owner, out var grid))
            {
                return new EntityCoordinates(_mapManager.GetMapEntityId(posWorld.MapId), (posWorld.X, posWorld.Y));
            }

            return new EntityCoordinates(grid.Owner, WorldToLocal(posWorld.Position));
        }

        /// <summary>
        ///     Transforms local vectors into world space vectors
        /// </summary>
        /// <param name="posLocal">The local vector with this grid as origin.</param>
        /// <returns>The world-space vector with global origin.</returns>
        public Vector2 LocalToWorld(Vector2 posLocal)
        {
            return _entMan.GetComponent<TransformComponent>(Owner).WorldMatrix.Transform(posLocal);
        }

        /// <summary>
        ///     Transforms World position into grid tile indices.
        /// </summary>
        /// <param name="posWorld">Position in the world.</param>
        /// <returns>Indices of a tile on the grid.</returns>
        public Vector2i WorldToTile(Vector2 posWorld)
        {
            var local = WorldToLocal(posWorld);
            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new Vector2i(x, y);
        }

        public Vector2i CoordinatesToTile(MapCoordinates coords)
        {
            DebugTools.Assert(_entMan.GetComponent<TransformComponent>(Owner).MapID == coords.MapId);

            var local = WorldToLocal(coords.Position);

            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new Vector2i(x, y);
        }

        /// <summary>
        ///     Transforms EntityCoordinates to a local tile location.
        /// </summary>
        /// <param name="coords"></param>
        /// <returns></returns>
        public Vector2i CoordinatesToTile(EntityCoordinates coords)
        {
            DebugTools.Assert(_entMan.GetComponent<TransformComponent>(Owner).MapID == coords.GetMapId(_entMan));
            var local = LocalToGrid(coords);

            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new Vector2i(x, y);
        }

        /// <summary>
        /// Transforms EntityCoordinates to chunk indices relative to grid origin.
        /// </summary>
        public Vector2i LocalToChunkIndices(EntityCoordinates gridPos)
        {
            var local = LocalToGrid(gridPos);

            var x = (int)Math.Floor(local.X / (TileSize * ChunkSize));
            var y = (int)Math.Floor(local.Y / (TileSize * ChunkSize));
            return new Vector2i(x, y);
        }

        public Vector2 LocalToGrid(EntityCoordinates position)
        {
            return position.EntityId == Owner
                ? position.Position
                : WorldToLocal(position.ToMapPos(_entMan));
        }

        public bool CollidesWithGrid(Vector2i indices)
        {
            var chunkIndices = GridTileToChunkIndices(indices);
            if (!_chunks.TryGetValue(chunkIndices, out var chunk))
                return false;

            var cTileIndices = chunk.GridTileToChunkTile(indices);
            return chunk.GetTile((ushort)cTileIndices.X, (ushort)cTileIndices.Y).TypeId != Tile.Empty.TypeId;
        }

        /// <summary>
        /// Transforms grid tile indices to chunk indices.
        /// </summary>
        public Vector2i GridTileToChunkIndices(Vector2i gridTile)
        {
            var x = (int)Math.Floor(gridTile.X / (float)ChunkSize);
            var y = (int)Math.Floor(gridTile.Y / (float)ChunkSize);

            return new Vector2i(x, y);
        }

        /// <summary>
        ///     Transforms grid-space tile indices to local coordinates.
        ///     The resulting coordinates are centered on the tile.
        /// </summary>
        /// <param name="gridTile"></param>
        /// <returns></returns>
        public EntityCoordinates GridTileToLocal(Vector2i gridTile)
        {
            return new(Owner,
                (gridTile.X * TileSize + (TileSize / 2f), gridTile.Y * TileSize + (TileSize / 2f)));
        }

        /// <summary>
        ///     Transforms grid-space tile indices to map coordinate position.
        ///     The resulting coordinates are centered on the tile.
        /// </summary>
        public Vector2 GridTileToWorldPos(Vector2i gridTile)
        {
            var locX = gridTile.X * TileSize + (TileSize / 2f);
            var locY = gridTile.Y * TileSize + (TileSize / 2f);

            return _entMan.GetComponent<TransformComponent>(Owner).WorldMatrix.Transform(new Vector2(locX, locY));
        }

        /// <summary>
        ///     Transforms grid-space tile indices to map coordinates.
        ///     The resulting coordinates are centered on the tile.
        /// </summary>
        public MapCoordinates GridTileToWorld(Vector2i gridTile)
        {
            return new(GridTileToWorldPos(gridTile), _entMan.GetComponent<TransformComponent>(Owner).MapID);
        }

        /// <summary>
        ///     Transforms grid indices into a tile reference, returns false if no tile is found.
        /// </summary>
        /// <param name="indices">The Grid Tile indices.</param>
        /// <param name="tile"></param>
        /// <returns></returns>
        public bool TryGetTileRef(Vector2i indices, out TileRef tile)
        {
            var chunkIndices = GridTileToChunkIndices(indices);
            if (!_chunks.TryGetValue(chunkIndices, out var chunk))
            {
                tile = default;
                return false;
            }

            var cTileIndices = chunk.GridTileToChunkTile(indices);
            tile = GetTileRef(chunk, (ushort)cTileIndices.X, (ushort)cTileIndices.Y);
            return true;
        }

        /// <summary>
        ///     Transforms coordinates into a tile reference, returns false if no tile is found.
        /// </summary>
        /// <param name="coords">The coordinates.</param>
        /// <param name="tile"></param>
        /// <returns></returns>
        public bool TryGetTileRef(EntityCoordinates coords, out TileRef tile)
        {
            return TryGetTileRef(CoordinatesToTile(coords), out tile);
        }

        /// <summary>
        ///     Transforms a world position into a tile reference, returns false if no tile is found.
        /// </summary>
        public bool TryGetTileRef(Vector2 worldPos, out TileRef tile)
        {
            return TryGetTileRef(WorldToTile(worldPos), out tile);
        }

        #endregion Transforms

        /// <summary>
        /// Regenerate collision for multiple chunks at once; faster than doing it individually.
        /// </summary>
        internal void RegenerateCollision(IReadOnlySet<MapChunk> chunks)
        {
            var chunkRectangles = new Dictionary<MapChunk, List<Box2i>>(chunks.Count);
            var removedChunks = new List<MapChunk>();
            var fixtureSystem = _entMan.EntitySysManager.GetEntitySystem<FixtureSystem>();
            _entMan.EntitySysManager.TryGetEntitySystem(out SharedGridFixtureSystem? system);

            foreach (var mapChunk in chunks)
            {
                // Even if the chunk is still removed still need to make sure bounds are updated (for now...)
                // generate collision rectangles for this chunk based on filled tiles.
                GridChunkPartition.PartitionChunk(mapChunk, out var localBounds, out var rectangles);
                mapChunk.CachedBounds = localBounds;

                if (mapChunk.FilledTiles > 0)
                    chunkRectangles.Add(mapChunk, rectangles);
                else
                {
                    // Gone. Reduced to atoms
                    // Need to do this before RemoveChunk because it clears fixtures.
                    foreach (var fixture in mapChunk.Fixtures)
                    {
                        fixtureSystem.DestroyFixture(fixture, false);
                    }

                    RemoveChunk(mapChunk.Indices);
                    removedChunks.Add(mapChunk);
                }
            }

            LocalAABB = new Box2();
            foreach (var chunk in _chunks.Values)
            {
                var chunkBounds = chunk.CachedBounds;

                if (chunkBounds.Size.Equals(Vector2i.Zero))
                    continue;

                if (LocalAABB.Size == Vector2.Zero)
                {
                    var gridBounds = chunkBounds.Translated(chunk.Indices * chunk.ChunkSize);
                    LocalAABB = gridBounds;
                }
                else
                {
                    var gridBounds = chunkBounds.Translated(chunk.Indices * chunk.ChunkSize);
                    LocalAABB = LocalAABB.Union(gridBounds);
                }
            }

            LocalAABB.Scale(TileSize);

            _mapManager.OnGridBoundsChange(Owner, this);
            // May have been deleted from the bulk update above!
            if (!_entMan.Deleted(Owner))
                system?.RegenerateCollision(Owner, chunkRectangles, removedChunks);
        }

        /// <summary>
        /// Regenerates the chunk local bounds of this chunk.
        /// </summary>
        internal void RegenerateCollision(MapChunk mapChunk)
        {
            RegenerateCollision(new HashSet<MapChunk>() {mapChunk});
        }

        /// <summary>
        /// Calculate the world space AABB for this chunk.
        /// </summary>
        internal Box2 CalcWorldAABB(MapChunk mapChunk)
        {
            var (position, rotation) = _entMan.GetComponent<TransformComponent>(Owner).GetWorldPositionRotation();

            var chunkPosition = mapChunk.Indices;
            var tileScale = TileSize;
            var chunkScale = mapChunk.ChunkSize;

            var worldPos = position + rotation.RotateVec(chunkPosition * tileScale * chunkScale);

            return new Box2Rotated(
                ((Box2)mapChunk.CachedBounds
                    .Scale(tileScale))
                .Translated(worldPos),
                rotation, worldPos).CalcBoundingBox();
        }

        private void OnTileModified(SharedMapSystem mapSystem, MapChunk mapChunk, Vector2i tileIndices, Tile newTile,
            Tile oldTile,
            bool shapeChanged)
        {
            // As the collision regeneration can potentially delete the chunk we'll notify of the tile changed first.
            var gridTile = mapChunk.ChunkTileToGridTile(tileIndices);
            mapChunk.LastTileModifiedTick = _gameTiming.CurTick;
            LastTileModifiedTick = _gameTiming.CurTick;

            // The map serializer currently sets tiles of unbound grids as part of the deserialization process
            // It properly sets SuppressOnTileChanged so that the event isn't spammed for every tile on the grid.
            // ParentMapId is not able to be accessed on unbound grids, so we can't even call this function for unbound grids.
            if (!mapSystem.SuppressOnTileChanged)
            {
                var newTileRef = new TileRef(Owner, gridTile, newTile);
                if (!mapSystem.SuppressOnTileChanged)
                {
                    var euid = newTileRef.GridUid;
                    _entMan.EventBus.RaiseLocalEvent(euid, new TileChangedEvent(euid, newTileRef, oldTile), true);
                }
            }

            if (shapeChanged && !mapChunk.SuppressCollisionRegeneration)
            {
                RegenerateCollision(mapChunk);
            }
        }
    }

    /// <summary>
    /// Raised whenever a grid becomes empty due to no more tiles with data.
    /// </summary>
    public sealed class EmptyGridEvent : EntityEventArgs
    {
        public EntityUid GridId { get; init; }
    }

    /// <summary>
    /// Returns all tiles on a grid.
    /// </summary>
    public struct GridTileEnumerator
    {
        private readonly EntityUid _gridUid;
        private Dictionary<Vector2i, MapChunk>.Enumerator _chunkEnumerator;
        private readonly ushort _chunkSize;
        private int _index;
        private readonly bool _ignoreEmpty;

        internal GridTileEnumerator(EntityUid gridUid,
            Dictionary<Vector2i, MapChunk>.Enumerator chunkEnumerator, ushort chunkSize, bool ignoreEmpty)
        {
            _gridUid = gridUid;
            _chunkEnumerator = chunkEnumerator;
            _chunkSize = chunkSize;
            _index = _chunkSize * _chunkSize;
            _ignoreEmpty = ignoreEmpty;
        }

        public bool MoveNext([NotNullWhen(true)] out TileRef? tileRef)
        {
            if (_index == _chunkSize * _chunkSize)
            {
                if (!_chunkEnumerator.MoveNext())
                {
                    tileRef = null;
                    return false;
                }

                _index = 0;
            }

            var (chunkOrigin, chunk) = _chunkEnumerator.Current;

            var x = (ushort)(_index / _chunkSize);
            var y = (ushort)(_index % _chunkSize);
            var tile = chunk.GetTile(x, y);
            _index++;

            if (_ignoreEmpty && tile.IsEmpty)
            {
                return MoveNext(out tileRef);
            }

            var gridX = x + chunkOrigin.X * _chunkSize;
            var gridY = y + chunkOrigin.Y * _chunkSize;
            tileRef = new TileRef(_gridUid, gridX, gridY, tile);
            return true;
        }
    }
}
