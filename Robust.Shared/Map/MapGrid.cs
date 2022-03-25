using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

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
        public Box2 WorldBounds =>
            new Box2Rotated(LocalBounds, WorldRotation, Vector2.Zero)
                .CalcBoundingBox()
                .Translated(WorldPosition);

        /// <inheritdoc />
        [ViewVariables]
        public Box2 LocalBounds { get; private set; }

        /// <inheritdoc />
        [ViewVariables]
        public ushort ChunkSize { get; }

        /// <inheritdoc />
        [ViewVariables]
        public GridId Index { get; }

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        [ViewVariables]
        public ushort TileSize { get; set; } = 1;

        /// <inheritdoc />
        [ViewVariables]
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
                return new TileRef(ParentMapId, Index, tileCoordinates.X, tileCoordinates.Y, default);
            }

            var chunkTileIndices = output.GridTileToChunkTile(tileCoordinates);
            return GetTileRef(ParentMapId, output, (ushort)chunkTileIndices.X, (ushort)chunkTileIndices.Y);
        }

        /// <summary>
        ///     Returns the tile at the given chunk indices.
        /// </summary>
        /// <param name="mapId">The MapId of the tile. Passed in as this isn't cached on grids.</param>
        /// <param name="mapChunk"></param>
        /// <param name="xIndex">The X tile index relative to the chunk origin.</param>
        /// <param name="yIndex">The Y tile index relative to the chunk origin.</param>
        /// <returns>A reference to a tile.</returns>
        public TileRef GetTileRef(MapId mapId, MapChunk mapChunk, ushort xIndex, ushort yIndex)
        {
            if (xIndex >= mapChunk.ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(xIndex), "Tile indices out of bounds.");

            if (yIndex >= mapChunk.ChunkSize)
                throw new ArgumentOutOfRangeException(nameof(yIndex), "Tile indices out of bounds.");

            var indices = mapChunk.ChunkTileToGridTile(new Vector2i(xIndex, yIndex));
            return new TileRef(ParentMapId, Index, indices, mapChunk.GetTile(xIndex, yIndex));
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetAllTiles(bool ignoreSpace = true)
        {
            foreach (var kvChunk in _chunks)
            {
                var chunk = kvChunk.Value;
                for (ushort x = 0; x < ChunkSize; x++)
                {
                    for (ushort y = 0; y < ChunkSize; y++)
                    {
                        var tile = chunk.GetTile(x, y);

                        if (ignoreSpace && tile.IsEmpty)
                            continue;

                        var (gridX, gridY) = new Vector2i(x, y) + chunk.Indices * ChunkSize;
                        yield return new TileRef(ParentMapId, Index, gridX, gridY, tile);
                    }
                }
            }
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
                RegenerateCollision(chunk);
            }
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

        private IEnumerable<TileRef> GetLocalTilesIntersecting(Box2 localArea, bool ignoreEmpty, Predicate<TileRef>? predicate)
        {
            // TODO: Should move the intersecting calls onto mapmanager system and then allow people to pass in xform / xformquery
            // that way we can avoid the GetComp here.
            var mapId = ParentMapId;
            var gridTileLb = new Vector2i((int)Math.Floor(localArea.Left), (int)Math.Floor(localArea.Bottom));
            var gridTileRt = new Vector2i((int)Math.Floor(localArea.Right), (int)Math.Floor(localArea.Top));

            for (var x = gridTileLb.X; x <= gridTileRt.X; x++)
            {
                for (var y = gridTileLb.Y; y <= gridTileRt.Y; y++)
                {
                    var gridChunk = GridTileToChunkIndices(new Vector2i(x, y));

                    if (_chunks.TryGetValue(gridChunk, out var chunk))
                    {
                        var chunkTile = chunk.GridTileToChunkTile(new Vector2i(x, y));
                        var tile = GetTileRef(mapId, chunk, (ushort)chunkTile.X, (ushort)chunkTile.Y);

                        if (ignoreEmpty && tile.Tile.IsEmpty)
                            continue;

                        if (predicate == null || predicate(tile))
                            yield return tile;
                    }
                    else if (!ignoreEmpty)
                    {
                        var tile = new TileRef(mapId, Index, x, y, new Tile());

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

            _chunks.Remove(origin);

            _mapManager.ChunkRemoved(Index, chunk);

            if (_chunks.Count == 0)
            {
                _entityManager.EventBus.RaiseLocalEvent(GridEntityId, new EmptyGridEvent {GridId = Index});
            }
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
        public void GetMapChunks(Box2 worldAABB, out ChunkEnumerator enumerator)
        {
            var localArea = InvWorldMatrix.TransformBox(worldAABB);
            enumerator = new ChunkEnumerator(_chunks, localArea, ChunkSize);
        }

        /// <inheritdoc />
        public void GetMapChunks(Box2Rotated worldArea, out ChunkEnumerator enumerator)
        {
            var matrix = InvWorldMatrix;
            var localArea = matrix.TransformBox(worldArea);

            enumerator = new ChunkEnumerator(_chunks, localArea, ChunkSize);
        }

        public void GetLocalMapChunks(Box2 localAABB, out ChunkEnumerator enumerator)
        {
            enumerator = new ChunkEnumerator(_chunks, localAABB, ChunkSize);
        }

        #endregion ChunkAccess

        #region SnapGridAccess

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
                    foreach (var cell in GetAnchoredEntities(position + new Vector2i(x, y)))
                    {
                        yield return cell;
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
            tile = GetTileRef(ParentMapId, chunk, (ushort)cTileIndices.X, (ushort)cTileIndices.Y);
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
        /// Regenerates the chunk local bounds of this chunk.
        /// </summary>
        public void RegenerateCollision(MapChunk mapChunk)
        {
            // Even if the chunk is still removed still need to make sure bounds are updated (for now...)
            if (mapChunk.FilledTiles == 0)
            {
                RemoveChunk(mapChunk.Indices);
            }

            // generate collision rectangles for this chunk based on filled tiles.
            GridChunkPartition.PartitionChunk(mapChunk, out var localBounds, out var rectangles);
            mapChunk.CachedBounds = localBounds;

            LocalBounds = new Box2();
            foreach (var chunk in _chunks.Values)
            {
                var chunkBounds = chunk.CachedBounds;

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

            // TryGet because unit tests YAY
            if (mapChunk.FilledTiles > 0 && _entityManager.EntitySysManager.TryGetEntitySystem(out SharedGridFixtureSystem? system))
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
                var newTileRef = new TileRef(ParentMapId, Index, gridTile, newTile);
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
}
