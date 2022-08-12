using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

// GridId obsolete
#pragma warning disable CS0618

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal sealed class MapGrid : IMapGridInternal
    {
        /// <summary>
        ///     Last game tick that the map was modified.
        /// </summary>
        [ViewVariables]
        public GameTick LastTileModifiedTick { get; internal set; }

        /// <inheritdoc />
        [ViewVariables]
        public MapId ParentMapId
        {
            get
            {
                //TODO: Make grids real parents of entities.
                if(GridEntityId.IsValid())
                {
                    return _entityManager.GetComponent<TransformComponent>(GridEntityId).MapID;
                }

                throw new InvalidOperationException($"Tried to access the {nameof(ParentMapId)} of an unbound grid.");
            }
            set
            {
                var mapEnt = _mapManager.GetMapEntityId(value);
                var worldPos = WorldPosition;

                // this should teleport the grid to the map
                _entityManager.GetComponent<TransformComponent>(GridEntityId).Coordinates =
                    new EntityCoordinates(mapEnt, worldPos);

                LastTileModifiedTick = _mapManager.GameTiming.CurTick;
            }
        }

        [ViewVariables]
        public EntityUid GridEntityId { get; internal set; }

        /// <summary>
        /// Map DynamicTree proxy to lookup for grid intersection.
        /// </summary>
        internal DynamicTree.Proxy MapProxy = DynamicTree.Proxy.Free;

        /// <summary>
        ///     Grid chunks than make up this grid.
        /// </summary>
        private readonly Dictionary<Vector2i, MapChunk> _chunks = new();

        private readonly IMapManagerInternal _mapManager;
        private readonly IEntityManager _entityManager;

        public bool Deleting;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapGrid"/> class.
        /// </summary>
        /// <param name="mapManager">Reference to the <see cref="MapManager"/> that will manage this grid.</param>
        /// <param name="entityManager"></param>
        /// <param name="gridIndex">Index identifier of this grid.</param>
        /// <param name="chunkSize">The dimension of this square chunk.</param>
        internal MapGrid(IMapManagerInternal mapManager, IEntityManager entityManager, GridId gridIndex, ushort chunkSize)
        {
            _mapManager = mapManager;
            _entityManager = entityManager;
            Index = gridIndex;
            ChunkSize = chunkSize;
            LastTileModifiedTick = _mapManager.GameTiming.CurTick;
        }

        /// <inheritdoc />
        [ViewVariables]
        public Box2Rotated WorldBounds
        {
            get
            {
                var worldAABB = LocalAABB.Translated(WorldPosition);
                return new Box2Rotated(worldAABB, WorldRotation, worldAABB.Center);
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        public Box2 WorldAABB =>
            new Box2Rotated(LocalAABB, WorldRotation, Vector2.Zero)
                .CalcBoundingBox()
                .Translated(WorldPosition);

        /// <inheritdoc />
        [ViewVariables]
        public Box2 LocalAABB { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public ushort ChunkSize { get; }

        /// <inheritdoc />
        [ViewVariables]
        [Obsolete("Use EntityUids instead")]
        public GridId Index { get; }

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        [ViewVariables]
        public ushort TileSize { get; set; } = 1;

        /// <inheritdoc />
        [ViewVariables]
        [Obsolete("Use Transform System + GridEntityId")]
        public Vector2 WorldPosition
        {
            get
            {
                //TODO: Make grids real parents of entities.
                if(GridEntityId.IsValid())
                {
                    return _entityManager.GetComponent<TransformComponent>(GridEntityId).WorldPosition;
                }

                return Vector2.Zero;
            }
            set
            {
                _entityManager.GetComponent<TransformComponent>(GridEntityId).WorldPosition = value;
                LastTileModifiedTick = _mapManager.GameTiming.CurTick;
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        [Obsolete("Use Transform System + GridEntityId")]
        public Angle WorldRotation
        {
            get
            {
                //TODO: Make grids real parents of entities.
                if(GridEntityId.IsValid())
                {
                    return _entityManager.GetComponent<TransformComponent>(GridEntityId).WorldRotation;
                }

                return Angle.Zero;
            }
            set
            {
                _entityManager.GetComponent<TransformComponent>(GridEntityId).WorldRotation = value;
                LastTileModifiedTick = _mapManager.GameTiming.CurTick;
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        [Obsolete("Use Transform System + GridEntityId")]
        public Matrix3 WorldMatrix
        {
            get
            {
                //TODO: Make grids real parents of entities.
                if(GridEntityId.IsValid())
                    return _entityManager.GetComponent<TransformComponent>(GridEntityId).WorldMatrix;

                return Matrix3.Identity;
            }
        }

        /// <inheritdoc />
        [ViewVariables]
        [Obsolete("Use Transform System + GridEntityId")]
        public Matrix3 InvWorldMatrix
        {
            get
            {
                //TODO: Make grids real parents of entities.
                if(GridEntityId.IsValid())
                    return _entityManager.GetComponent<TransformComponent>(GridEntityId).InvWorldMatrix;

                return Matrix3.Identity;
            }
        }

        #region TileAccess

        /// <inheritdoc />
        public TileRef GetTileRef(MapCoordinates coords)
        {
            return GetTileRef(CoordinatesToTile(coords));
        }

        /// <inheritdoc />
        public TileRef GetTileRef(EntityCoordinates coords)
        {
            return GetTileRef(CoordinatesToTile(coords));
        }

        /// <inheritdoc />
        public TileRef GetTileRef(Vector2i tileCoordinates)
        {
            var chunkIndices = GridTileToChunkIndices(tileCoordinates);

            if (!_chunks.TryGetValue(chunkIndices, out var output))
            {
                // Chunk doesn't exist, return a tileRef to an empty (space) tile.
                return new TileRef(Index, GridEntityId, tileCoordinates.X, tileCoordinates.Y, default);
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
        public TileRef GetTileRef(MapChunk mapChunk, ushort xIndex, ushort yIndex)
        {
            if (xIndex >= mapChunk.ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

            if (yIndex >= mapChunk.ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

            var indices = mapChunk.ChunkTileToGridTile(new Vector2i(xIndex, yIndex));
            return new TileRef(Index, GridEntityId, indices, mapChunk.GetTile(xIndex, yIndex));
        }

        /// <inheritdoc />
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
                        yield return new TileRef(Index, GridEntityId, gridX, gridY, tile);
                    }
                }
            }
        }

        /// <inheritdoc />
        public GridTileEnumerator GetAllTilesEnumerator(bool ignoreEmpty = true)
        {
            return new GridTileEnumerator(Index, GridEntityId, _chunks.GetEnumerator(), ChunkSize, ignoreEmpty);
        }

        /// <inheritdoc />
        public void SetTile(EntityCoordinates coords, Tile tile)
        {
            var localTile = CoordinatesToTile(coords);
            SetTile(new Vector2i(localTile.X, localTile.Y), tile);
        }

        /// <inheritdoc />
        public void SetTile(Vector2i gridIndices, Tile tile)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(gridIndices);
            chunk.SetTile((ushort)chunkTile.X, (ushort)chunkTile.Y, tile);
        }

        /// <inheritdoc />
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

        public IEnumerable<TileRef> GetLocalTilesIntersecting(Box2Rotated localArea, bool ignoreEmpty = true, Predicate<TileRef>? predicate = null)
        {
            var localAABB = localArea.CalcBoundingBox();
            return GetLocalTilesIntersecting(localAABB, ignoreEmpty, predicate);
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetTilesIntersecting(Box2Rotated worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            var matrix = InvWorldMatrix;
            var localArea = matrix.TransformBox(worldArea);

            foreach (var tile in GetLocalTilesIntersecting(localArea, ignoreEmpty, predicate))
            {
                yield return tile;
            }
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true, Predicate<TileRef>? predicate = null)
        {
            var matrix = InvWorldMatrix;
            var localArea = matrix.TransformBox(worldArea);

            foreach (var tile in GetLocalTilesIntersecting(localArea, ignoreEmpty, predicate))
            {
                yield return tile;
            }
        }

        public IEnumerable<TileRef> GetLocalTilesIntersecting(Box2 localArea, bool ignoreEmpty, Predicate<TileRef>? predicate)
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
                        var tile = new TileRef(Index, GridEntityId, x, y, new Tile());

                        if (predicate == null || predicate(tile))
                            yield return tile;
                    }
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetTilesIntersecting(Circle worldArea, bool ignoreEmpty = true, Predicate<TileRef>? predicate = null)
        {
            var aabb = new Box2(worldArea.Position.X - worldArea.Radius, worldArea.Position.Y - worldArea.Radius, worldArea.Position.X + worldArea.Radius, worldArea.Position.Y + worldArea.Radius);
            var circleGridPos = new EntityCoordinates(GridEntityId, WorldToLocal(worldArea.Position));

            foreach (var tile in GetTilesIntersecting(aabb, ignoreEmpty))
            {
                var local = GridTileToLocal(tile.GridIndices);

                if (!local.TryDistance(_entityManager, circleGridPos, out var distance))
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
        ///     The total number of allocated chunks in the grid.
        /// </summary>
        public int ChunkCount => _chunks.Count;

        /// <inheritdoc />
        public MapChunk GetChunk(int xIndex, int yIndex)
        {
            return GetChunk(new Vector2i(xIndex, yIndex));
        }

        /// <inheritdoc />
        public void RemoveChunk(Vector2i origin)
        {
            if (!_chunks.TryGetValue(origin, out var chunk)) return;

            chunk.Fixtures.Clear();
            _chunks.Remove(origin);

            _mapManager.ChunkRemoved(Index, chunk);

            if (_chunks.Count == 0)
            {
                _entityManager.EventBus.RaiseLocalEvent(GridEntityId, new EmptyGridEvent {GridId = Index}, true);
            }
        }

        public bool TryGetChunk(Vector2i chunkIndices, [NotNullWhen(true)] out MapChunk? chunk)
        {
            return _chunks.TryGetValue(chunkIndices, out chunk);
        }

        /// <inheritdoc />
        public MapChunk GetChunk(Vector2i chunkIndices)
        {
            if (_chunks.TryGetValue(chunkIndices, out var output))
                return output;

            var newChunk = new MapChunk(chunkIndices.X, chunkIndices.Y, ChunkSize);
            newChunk.LastTileModifiedTick = _mapManager.GameTiming.CurTick;
            newChunk.TileModified += OnTileModified;
            return _chunks[chunkIndices] = newChunk;
        }

        /// <inheritdoc />
        public bool HasChunk(Vector2i chunkIndices)
        {
            return _chunks.ContainsKey(chunkIndices);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<Vector2i, MapChunk> GetMapChunks()
        {
            return _chunks;
        }

        internal struct ChunkEnumerator
        {
            private Dictionary<Vector2i, MapChunk> _chunks;
            private Vector2i _chunkLB;
            private Vector2i _chunkRT;

            private int _xIndex;
            private int _yIndex;

            internal ChunkEnumerator(Dictionary<Vector2i, MapChunk> chunks, Box2 localAABB, int chunkSize)
            {
                _chunks = chunks;

                _chunkLB = new Vector2i((int)Math.Floor(localAABB.Left / chunkSize), (int)Math.Floor(localAABB.Bottom / chunkSize));
                _chunkRT = new Vector2i((int)Math.Floor(localAABB.Right / chunkSize), (int)Math.Floor(localAABB.Top / chunkSize));

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

        /// <inheritdoc />
        public ChunkEnumerator GetMapChunks(Box2 worldAABB)
        {
            var localAABB = InvWorldMatrix.TransformBox(worldAABB);
            return new ChunkEnumerator(_chunks, localAABB, ChunkSize);
        }

        /// <inheritdoc />
        public ChunkEnumerator GetMapChunks(Box2Rotated worldArea)
        {
            var matrix = InvWorldMatrix;
            var localArea = matrix.TransformBox(worldArea);
            return new ChunkEnumerator(_chunks, localArea, ChunkSize);
        }

        public ChunkEnumerator GetLocalMapChunks(Box2 localAABB)
        {
            return new ChunkEnumerator(_chunks, localAABB, ChunkSize);
        }

        #endregion ChunkAccess

        #region SnapGridAccess

        /// <inheritdoc />
        public int AnchoredEntityCount(Vector2i pos)
        {
            var gridChunkPos = GridTileToChunkIndices(pos);

            if (!_chunks.TryGetValue(gridChunkPos, out var chunk)) return 0;

            var (x, y) = chunk.GridTileToChunkTile(pos);
            return chunk.GetSnapGrid((ushort) x, (ushort) y)?.Count ?? 0; // ?
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetAnchoredEntities(MapCoordinates coords)
        {
            return GetAnchoredEntities(TileIndicesFor(coords));
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetAnchoredEntities(EntityCoordinates coords)
        {
            return GetAnchoredEntities(TileIndicesFor(coords));
        }

        /// <inheritdoc />
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
            var snapgrid = chunk.GetSnapGrid((ushort)chunkTile.X, (ushort)chunkTile.Y);

            return snapgrid == null ?
                AnchoredEntitiesEnumerator.Empty :
                new AnchoredEntitiesEnumerator(snapgrid.GetEnumerator());
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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

        /// <inheritdoc />
        public Vector2i TileIndicesFor(EntityCoordinates coords)
        {
            DebugTools.Assert(ParentMapId == coords.GetMapId(_entityManager));

            return SnapGridLocalCellFor(LocalToGrid(coords));
        }

        /// <inheritdoc />
        public Vector2i TileIndicesFor(MapCoordinates worldPos)
        {
            DebugTools.Assert(ParentMapId == worldPos.MapId);

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
            var snapgrid = chunk.GetSnapGrid((ushort) chunkTile.X, (ushort) chunkTile.Y);
            return snapgrid?.Contains(euid) == true;
        }

        /// <inheritdoc />
        public bool AddToSnapGridCell(Vector2i pos, EntityUid euid)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);

            if (chunk.GetTile((ushort) chunkTile.X, (ushort) chunkTile.Y).IsEmpty)
                return false;

            chunk.AddToSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, euid);
            return true;
        }

        /// <inheritdoc />
        public bool AddToSnapGridCell(EntityCoordinates coords, EntityUid euid)
        {
            return AddToSnapGridCell(TileIndicesFor(coords), euid);
        }

        /// <inheritdoc />
        public void RemoveFromSnapGridCell(Vector2i pos, EntityUid euid)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
            chunk.RemoveFromSnapGridCell((ushort)chunkTile.X, (ushort) chunkTile.Y, euid);
        }

        /// <inheritdoc />
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
                    throw new NotImplementedException();
            }
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetInDir(EntityCoordinates position, Direction dir)
        {
            var pos = SnapGridPosAt(TileIndicesFor(position), dir);
            return GetAnchoredEntities(pos);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetOffset(EntityCoordinates coords, Vector2i offset)
        {
            var pos = TileIndicesFor(coords) + offset;
            return GetAnchoredEntities(pos);
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetLocal(EntityCoordinates coords)
        {
            return GetAnchoredEntities(TileIndicesFor(coords));
        }

        /// <inheritdoc />
        public EntityCoordinates DirectionToGrid(EntityCoordinates coords, Direction direction)
        {
            return GridTileToLocal(SnapGridPosAt(TileIndicesFor(coords), direction));
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetCardinalNeighborCells(EntityCoordinates coords)
        {
            var position = TileIndicesFor(coords);
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
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public Vector2 WorldToLocal(Vector2 posWorld)
        {
            return InvWorldMatrix.Transform(posWorld);
        }

        /// <inheritdoc />
        public EntityCoordinates MapToGrid(MapCoordinates posWorld)
        {
            if(posWorld.MapId != ParentMapId)
                throw new ArgumentException($"Grid {Index} is on map {ParentMapId}, but coords are on map {posWorld.MapId}.", nameof(posWorld));

            if (!_mapManager.TryGetGrid(Index, out var grid))
            {
                return new EntityCoordinates(_mapManager.GetMapEntityId(posWorld.MapId), (posWorld.X, posWorld.Y));
            }

            return new EntityCoordinates(grid.GridEntityId, WorldToLocal(posWorld.Position));
        }

        /// <inheritdoc />
        public Vector2 LocalToWorld(Vector2 posLocal)
        {
            return WorldMatrix.Transform(posLocal);
        }

        public Vector2i WorldToTile(Vector2 posWorld)
        {
            var local = WorldToLocal(posWorld);
            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new Vector2i(x, y);
        }

        /// <inheritdoc />
        public Vector2i CoordinatesToTile(MapCoordinates coords)
        {
            DebugTools.Assert(ParentMapId == coords.MapId);

            var local = WorldToLocal(coords.Position);

            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new Vector2i(x, y);
        }

        /// <inheritdoc />
        public Vector2i CoordinatesToTile(EntityCoordinates coords)
        {
            DebugTools.Assert(ParentMapId == coords.GetMapId(_entityManager));
            var local = LocalToGrid(coords);

            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new Vector2i(x, y);
        }

        /// <inheritdoc />
        public Vector2i LocalToChunkIndices(EntityCoordinates gridPos)
        {
            var local = LocalToGrid(gridPos);

            var x = (int)Math.Floor(local.X / (TileSize * ChunkSize));
            var y = (int)Math.Floor(local.Y / (TileSize * ChunkSize));
            return new Vector2i(x, y);
        }

        public Vector2 LocalToGrid(EntityCoordinates position)
        {
            return position.EntityId == GridEntityId ? position.Position : WorldToLocal(position.ToMapPos(_entityManager));
        }

        public bool CollidesWithGrid(Vector2i indices)
        {
            var chunkIndices = GridTileToChunkIndices(indices);
            if (!_chunks.TryGetValue(chunkIndices, out var chunk))
                return false;

            var cTileIndices = chunk.GridTileToChunkTile(indices);
            return chunk.GetTile((ushort) cTileIndices.X, (ushort) cTileIndices.Y).TypeId != Tile.Empty.TypeId;
        }

        /// <inheritdoc />
        public Vector2i GridTileToChunkIndices(Vector2i gridTile)
        {
            var x = (int)Math.Floor(gridTile.X / (float)ChunkSize);
            var y = (int)Math.Floor(gridTile.Y / (float)ChunkSize);

            return new Vector2i(x, y);
        }

        /// <inheritdoc />
        public EntityCoordinates GridTileToLocal(Vector2i gridTile)
        {
            return new(GridEntityId, (gridTile.X * TileSize + (TileSize / 2f), gridTile.Y * TileSize + (TileSize / 2f)));
        }

        public Vector2 GridTileToWorldPos(Vector2i gridTile)
        {
            var locX = gridTile.X * TileSize + (TileSize / 2f);
            var locY = gridTile.Y * TileSize + (TileSize / 2f);

            return WorldMatrix.Transform(new Vector2(locX, locY));
        }

        public MapCoordinates GridTileToWorld(Vector2i gridTile)
        {
            return new(GridTileToWorldPos(gridTile), ParentMapId);
        }

        /// <inheritdoc />
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

        /// <inheritdoc />
        public bool TryGetTileRef(EntityCoordinates coords, out TileRef tile)
        {
            return TryGetTileRef(CoordinatesToTile(coords), out tile);
        }

        /// <inheritdoc />
        public bool TryGetTileRef(Vector2 worldPos, out TileRef tile)
        {
            return TryGetTileRef(WorldToTile(worldPos), out tile);
        }

        #endregion Transforms

        /// <summary>
        /// Regenerate collision for multiple chunks at once; faster than doing it individually.
        /// </summary>
        public void RegenerateCollision(IReadOnlySet<MapChunk> chunks)
        {
            var chunkRectangles = new Dictionary<MapChunk, List<Box2i>>(chunks.Count);
            var removedChunks = new List<MapChunk>();
            var fixtureSystem = EntitySystem.Get<FixtureSystem>();
            _entityManager.EntitySysManager.TryGetEntitySystem(out SharedGridFixtureSystem? system);

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
                    RemoveChunk(mapChunk.Indices);
                    // Gone. Reduced to atoms
                    foreach (var fixture in mapChunk.Fixtures)
                    {
                        fixtureSystem.DestroyFixture(fixture, false);
                    }

                    removedChunks.Add(mapChunk);
                }
            }

            LocalAABB = new Box2();
            foreach (var chunk in _chunks.Values)
            {
                var chunkBounds = chunk.CachedBounds;

                if(chunkBounds.Size.Equals(Vector2i.Zero))
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

            _mapManager.OnGridBoundsChange(GridEntityId, this);
            // May have been deleted from the bulk update above!
            if (!_entityManager.Deleted(GridEntityId))
                system?.RegenerateCollision(GridEntityId, chunkRectangles, removedChunks);
        }

        /// <summary>
        /// Regenerates the chunk local bounds of this chunk.
        /// </summary>
        public void RegenerateCollision(MapChunk mapChunk)
        {
            // Even if the chunk is still removed still need to make sure bounds are updated (for now...)
            if (mapChunk.FilledTiles == 0)
            {
                var fixtureSystem = EntitySystem.Get<FixtureSystem>();
                foreach (var fixture in mapChunk.Fixtures)
                {
                    fixtureSystem.DestroyFixture(fixture);
                }

                RemoveChunk(mapChunk.Indices);
            }

            // generate collision rectangles for this chunk based on filled tiles.
            GridChunkPartition.PartitionChunk(mapChunk, out var localBounds, out var rectangles);
            mapChunk.CachedBounds = localBounds;

            LocalAABB = new Box2();
            foreach (var chunk in _chunks.Values)
            {
                var chunkBounds = chunk.CachedBounds;

                if(chunkBounds.Size.Equals(Vector2i.Zero))
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

            if (!_entityManager.EntitySysManager.TryGetEntitySystem(out SharedGridFixtureSystem? system) ||
                _entityManager.Deleted(GridEntityId)) return;

            // TODO: Move this to the component when we combine.
            _mapManager.OnGridBoundsChange(GridEntityId, this);

            system.RegenerateCollision(GridEntityId, mapChunk, rectangles);
        }

        /// <summary>
        /// Calculate the world space AABB for this chunk.
        /// </summary>
        public Box2 CalcWorldAABB(MapChunk mapChunk)
        {
            var rotation = WorldRotation;
            var position = WorldPosition;
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

        private void OnTileModified(MapChunk mapChunk, Vector2i tileIndices, Tile newTile, Tile oldTile, bool shapeChanged)
        {
            // As the collision regeneration can potentially delete the chunk we'll notify of the tile changed first.
            var gridTile = mapChunk.ChunkTileToGridTile(tileIndices);
            mapChunk.LastTileModifiedTick = _mapManager.GameTiming.CurTick;
            LastTileModifiedTick = _mapManager.GameTiming.CurTick;

            // The map serializer currently sets tiles of unbound grids as part of the deserialization process
            // It properly sets SuppressOnTileChanged so that the event isn't spammed for every tile on the grid.
            // ParentMapId is not able to be accessed on unbound grids, so we can't even call this function for unbound grids.
            if(!_mapManager.SuppressOnTileChanged)
            {
                var newTileRef = new TileRef(Index, GridEntityId, gridTile, newTile);
                _mapManager.RaiseOnTileChanged(newTileRef, oldTile);
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
        public GridId GridId { get; init;  }
    }

    /// <summary>
    /// Returns all tiles on a grid.
    /// </summary>
    public struct GridTileEnumerator
    {
        private GridId _gridId;
        private EntityUid _gridUid;
        private Dictionary<Vector2i, MapChunk>.Enumerator _chunkEnumerator;
        private readonly ushort _chunkSize;
        private int _index;
        private bool _ignoreEmpty;

        internal GridTileEnumerator(GridId gridId, EntityUid gridUid, Dictionary<Vector2i, MapChunk>.Enumerator chunkEnumerator, ushort chunkSize, bool ignoreEmpty)
        {
            _gridId = gridId;
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

            var x = (ushort) (_index / _chunkSize);
            var y = (ushort) (_index % _chunkSize);
            var tile = chunk.GetTile(x, y);
            _index++;

            if (_ignoreEmpty && tile.IsEmpty)
            {
                return MoveNext(out tileRef);
            }

            var gridX = x + chunkOrigin.X * _chunkSize;
            var gridY = y + chunkOrigin.Y * _chunkSize;
            tileRef = new TileRef(_gridId, _gridUid, gridX, gridY, tile);
            return true;
        }
    }
}
