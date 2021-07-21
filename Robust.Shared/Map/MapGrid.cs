using System;
using System.Collections.Generic;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map
{
    /// <inheritdoc />
    internal class MapGrid : IMapGridInternal
    {
        /// <summary>
        ///     Game tick that the map was created.
        /// </summary>
        [ViewVariables]
        public GameTick CreatedTick { get; }

        /// <summary>
        ///     Last game tick that the map was modified.
        /// </summary>
        [ViewVariables]
        public GameTick LastModifiedTick { get; private set; }

        /// <inheritdoc />
        public GameTick CurTick => _mapManager.GameTiming.CurTick;

        /// <inheritdoc />
        [ViewVariables]
        public MapId ParentMapId { get; set; }

        [ViewVariables]
        public EntityUid GridEntityId { get; internal set; }

        /// <summary>
        ///     Grid chunks than make up this grid.
        /// </summary>
        private readonly Dictionary<Vector2i, IMapChunkInternal> _chunks = new();

        private readonly IMapManagerInternal _mapManager;
        private readonly IEntityManager _entityManager;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MapGrid"/> class.
        /// </summary>
        /// <param name="mapManager">Reference to the <see cref="MapManager"/> that will manage this grid.</param>
        /// <param name="entityManager"></param>
        /// <param name="gridIndex">Index identifier of this grid.</param>
        /// <param name="chunkSize">The dimension of this square chunk.</param>
        /// <param name="snapSize">Distance in world units between the lines on the conceptual snap grid.</param>
        /// <param name="parentMapId">Parent map identifier.</param>
        internal MapGrid(IMapManagerInternal mapManager, IEntityManager entityManager, GridId gridIndex, ushort chunkSize, MapId parentMapId)
        {
            _mapManager = mapManager;
            _entityManager = entityManager;
            Index = gridIndex;
            ChunkSize = chunkSize;
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
        [ViewVariables]
        public Box2 WorldBounds => LocalBounds.Translated(WorldPosition);

        /// <inheritdoc />
        [ViewVariables]
        public Box2 LocalBounds { get; private set; }

        public bool SuppressCollisionRegeneration { get; set; }

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
        public ushort TileSize { get; } = 1;

        /// <inheritdoc />
        [ViewVariables]
        public Vector2 WorldPosition
        {
            get
            {
                //TODO: Make grids real parents of entities.
                if(GridEntityId.IsValid())
                    return _mapManager.EntityManager.GetEntity(GridEntityId).Transform.WorldPosition;
                return Vector2.Zero;
            }
            set
            {
                _mapManager.EntityManager.GetEntity(GridEntityId).Transform.WorldPosition = value;
                LastModifiedTick = _mapManager.GameTiming.CurTick;
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
                    return _mapManager.EntityManager.GetEntity(GridEntityId).Transform.WorldMatrix;

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
                    return _mapManager.EntityManager.GetEntity(GridEntityId).Transform.InvWorldMatrix;

                return Matrix3.Identity;
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
            _mapManager.RaiseOnTileChanged(tileRef, oldTile);
        }

        /// <inheritdoc />
        public void NotifyChunkCollisionRegenerated(MapChunk chunk)
        {
            // TODO: Ideally we wouldn't have LocalBounds on the grid and we could just treat it like a physics object
            // (eventually, well into the future).
            // For now we'll just attach a fixture to each chunk.

            // Not raising directed because the grid's EntityUid isn't set yet.
            IoCManager
                .Resolve<IEntityManager>()
                .EventBus
                .RaiseEvent(EventSource.Local, new RegenerateChunkCollisionEvent(chunk));

            UpdateAABB();
        }

        #region TileAccess

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
            return output.GetTileRef((ushort)chunkTileIndices.X, (ushort)chunkTileIndices.Y);
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetAllTiles(bool ignoreSpace = true)
        {
            foreach (var kvChunk in _chunks)
            {
                foreach (var tileRef in kvChunk.Value)
                {
                    if (!ignoreSpace || !tileRef.Tile.IsEmpty)
                        yield return tileRef;
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
        public IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true, Predicate<TileRef>? predicate = null)
        {
            var localArea = new Box2(WorldToLocal(worldArea.BottomLeft), WorldToLocal(worldArea.TopRight));
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
                        var tile = chunk.GetTileRef((ushort)chunkTile.X, (ushort)chunkTile.Y);

                        if (ignoreEmpty && tile.Tile.IsEmpty)
                            continue;

                        if (predicate == null || predicate(tile))
                        {
                            yield return tile;

                        }
                    }
                    else if (!ignoreEmpty)
                    {
                        var tile = new TileRef(ParentMapId, Index, x, y, new Tile());

                        if (predicate == null || predicate(tile))
                        {
                            yield return tile;
                        }
                    }
                }
            }
        }

        /// <inheritdoc />
        public IEnumerable<TileRef> GetTilesIntersecting(Circle worldArea, bool ignoreEmpty = true, Predicate<TileRef>? predicate = null)
        {
            var aabb = new Box2(worldArea.Position.X - worldArea.Radius, worldArea.Position.Y - worldArea.Radius, worldArea.Position.X + worldArea.Radius, worldArea.Position.Y + worldArea.Radius);

            foreach (var tile in GetTilesIntersecting(aabb, ignoreEmpty))
            {
                var local = GridTileToLocal(tile.GridIndices);
                var gridId = tile.GridIndex;

                if (!_mapManager.TryGetGrid(gridId, out var grid))
                {
                    continue;
                }

                var to = new EntityCoordinates(grid.GridEntityId, worldArea.Position);

                if (!local.TryDistance(_entityManager, to, out var distance))
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
            return GetChunk(new Vector2i(xIndex, yIndex));
        }

        /// <inheritdoc />
        public IMapChunkInternal GetChunk(Vector2i chunkIndices)
        {
            if (_chunks.TryGetValue(chunkIndices, out var output))
                return output;

            return _chunks[chunkIndices] = new MapChunk(this, chunkIndices.X, chunkIndices.Y, ChunkSize);
        }

        /// <inheritdoc />
        public IReadOnlyDictionary<Vector2i, IMapChunkInternal> GetMapChunks()
        {
            return _chunks;
        }

        #endregion ChunkAccess

        #region SnapGridAccess

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetAnchoredEntities(EntityCoordinates coords)
        {
            return GetAnchoredEntities(TileIndicesFor(coords));
        }

        /// <inheritdoc />
        public IEnumerable<EntityUid> GetAnchoredEntities(Vector2i pos)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);
            return chunk.GetSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y);
        }

        /// <inheritdoc />
        public Vector2i TileIndicesFor(EntityCoordinates coords)
        {
            DebugTools.Assert(ParentMapId == coords.GetMapId(_entityManager));

            var local = WorldToLocal(coords.ToMapPos(_entityManager));
            return SnapGridLocalCellFor(local);
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

        private (IMapChunk, Vector2i) ChunkAndOffsetForTile(Vector2i pos)
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

        /// <summary>
        ///     Transforms entity coordinates to tile indices relative to grid origin.
        /// </summary>
        public Vector2i CoordinatesToTile(EntityCoordinates coords)
        {
            DebugTools.Assert(ParentMapId == coords.GetMapId(_entityManager));

            var local = WorldToLocal(coords.ToMapPos(_entityManager));
            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new Vector2i(x, y);
        }

        /// <summary>
        ///     Transforms global world coordinates to chunk indices relative to grid origin.
        /// </summary>
        public Vector2i LocalToChunkIndices(EntityCoordinates gridPos)
        {
            var local = WorldToLocal(gridPos.ToMapPos(_entityManager));
            var x = (int)Math.Floor(local.X / (TileSize * ChunkSize));
            var y = (int)Math.Floor(local.Y / (TileSize * ChunkSize));
            return new Vector2i(x, y);
        }

        public bool CollidesWithGrid(Vector2i indices)
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

            return new Vector2(locX, locY) + WorldPosition;
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
            tile = chunk.GetTileRef(cTileIndices);
            return true;
        }

        /// <inheritdoc />
        public bool TryGetTileRef(EntityCoordinates coords, out TileRef tile)
        {
            return TryGetTileRef(CoordinatesToTile(coords), out tile);
        }

        #endregion Transforms
    }
}
