using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Map.Events;
using Robust.Shared.Maths;
using Robust.Shared.Network;
using Robust.Shared.Physics;
using Robust.Shared.Physics.Systems;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map.Components
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class MapGridComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IGameTiming _timing = default!;
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;
        [Dependency] private readonly INetManager _netManager = default!;

        // This field is used for deserialization internally in the map loader.
        // If you want to remove this, you would have to restructure the map save file.
        [DataField("index")] internal int GridIndex = 0;
        // the grid section now writes the grid's EntityUID. as long as existing maps get updated (just a load+save),
        // this can be removed

        [DataField("chunkSize")] internal ushort ChunkSize = 16;

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        [DataField("tileSize")]
        public ushort TileSize { get; internal set; } = 1;

        [ViewVariables] internal readonly List<(GameTick tick, Vector2i indices)> ChunkDeletionHistory = new();

        /// <summary>
        ///     Last game tick that the map was modified.
        /// </summary>
        [ViewVariables]
        public GameTick LastTileModifiedTick { get; internal set; }

        [Obsolete("Use .Owner")]
        [ViewVariables] public EntityUid GridEntityId => Owner;

        /// <summary>
        /// Map DynamicTree proxy to lookup for grid intersection.
        /// </summary>
        internal DynamicTree.Proxy MapProxy = DynamicTree.Proxy.Free;

        /// <summary>
        ///     Grid chunks than make up this grid.
        /// </summary>
        [DataField("chunks")]
        internal readonly Dictionary<Vector2i, MapChunk> Chunks = new();

        [ViewVariables]
        public Box2 LocalAABB { get; private set; }

        internal void RemoveChunk(Vector2i origin)
        {
            if (!Chunks.TryGetValue(origin, out var chunk))
                return;

            if (_netManager.IsServer)
                ChunkDeletionHistory.Add((_timing.CurTick, chunk.Indices));

            chunk.Fixtures.Clear();
            Chunks.Remove(origin);

            if (Chunks.Count == 0)
                _entMan.EventBus.RaiseLocalEvent(Owner, new EmptyGridEvent { GridId = Owner }, true);
        }

        internal static void ApplyMapGridState(NetworkedMapManager networkedMapManager, MapGridComponent gridComp,
            GameStateMapData.ChunkDatum[] chunkUpdates)
        {
            networkedMapManager.SuppressOnTileChanged = true;
            var modified = new List<(Vector2i position, Tile tile)>();

            foreach (var chunkData in chunkUpdates)
            {
                if (chunkData.IsDeleted())
                    continue;

                var chunk = gridComp.GetOrAddChunk(chunkData.Index);
                chunk.SuppressCollisionRegeneration = true;
                DebugTools.Assert(chunkData.TileData.Length == gridComp.ChunkSize * gridComp.ChunkSize);

                var counter = 0;
                for (ushort x = 0; x < gridComp.ChunkSize; x++)
                {
                    for (ushort y = 0; y < gridComp.ChunkSize; y++)
                    {
                        var tile = chunkData.TileData[counter++];
                        if (chunk.GetTile(x, y) == tile)
                            continue;

                        chunk.SetTile(x, y, tile);
                        modified.Add((new Vector2i(chunk.X * gridComp.ChunkSize + x, chunk.Y * gridComp.ChunkSize + y), tile));
                    }
                }
            }

            if (modified.Count != 0)
                MapManager.InvokeGridChanged(networkedMapManager, gridComp, modified);

            foreach (var chunkData in chunkUpdates)
            {
                if (chunkData.IsDeleted())
                {
                    gridComp.RemoveChunk(chunkData.Index);
                    continue;
                }

                var chunk = gridComp.GetOrAddChunk(chunkData.Index);
                chunk.SuppressCollisionRegeneration = false;
                gridComp.RegenerateCollision(chunk);
            }

            networkedMapManager.SuppressOnTileChanged = false;
        }

        /// <summary>
        /// Regenerate collision for multiple chunks at once; faster than doing it individually.
        /// </summary>
        internal void RegenerateCollision(IReadOnlySet<MapChunk> chunks)
        {
            if (_entMan.HasComponent<MapComponent>(Owner))
                return;

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
            foreach (var chunk in Chunks.Values)
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

            // May have been deleted from the bulk update above!
            if (_entMan.Deleted(Owner))
                return;

            // TODO: Move this to the component when we combine.
            _entMan.EntitySysManager.GetEntitySystem<SharedPhysicsSystem>().WakeBody(Owner);
            _mapManager.OnGridBoundsChange(Owner, this);
            system?.RegenerateCollision(Owner, chunkRectangles, removedChunks);
        }

        /// <summary>
        /// Regenerates the chunk local bounds of this chunk.
        /// </summary>
        internal void RegenerateCollision(MapChunk mapChunk)
        {
            RegenerateCollision(new HashSet<MapChunk> { mapChunk });
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

            if (!Chunks.TryGetValue(chunkIndices, out var output))
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

        /// <inheritdoc />
        public IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true)
        {
            foreach (var kvChunk in Chunks)
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

        /// <inheritdoc />
        public GridTileEnumerator GetAllTilesEnumerator(bool ignoreEmpty = true)
        {
            return new GridTileEnumerator(Owner, Chunks.GetEnumerator(), ChunkSize, ignoreEmpty);
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
            // Ideally we'd to this here for consistency but apparently tile modified does it or something.
            // Yeah it's noodly.
            // RegenerateCollision(chunk);
        }

        /// <inheritdoc />
        public void SetTiles(List<(Vector2i GridIndices, Tile Tile)> tiles)
        {
            if (tiles.Count == 0) return;

            var chunks = new HashSet<MapChunk>(Math.Max(1, tiles.Count / ChunkSize));

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

        /// <inheritdoc />
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

        /// <inheritdoc />
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

                    if (Chunks.TryGetValue(gridChunk, out var chunk))
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

        /// <inheritdoc />
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
        ///     The total number of allocated chunks in the grid.
        /// </summary>
        public int ChunkCount => Chunks.Count;

        /// <inheritdoc />
        internal MapChunk GetOrAddChunk(int xIndex, int yIndex)
        {
            return GetOrAddChunk(new Vector2i(xIndex, yIndex));
        }

        internal bool TryGetChunk(Vector2i chunkIndices, [NotNullWhen(true)] out MapChunk? chunk)
        {
            return Chunks.TryGetValue(chunkIndices, out chunk);
        }

        /// <inheritdoc />
        internal MapChunk GetOrAddChunk(Vector2i chunkIndices)
        {
            if (Chunks.TryGetValue(chunkIndices, out var output))
                return output;

            var newChunk = new MapChunk(chunkIndices.X, chunkIndices.Y, ChunkSize);
            newChunk.LastTileModifiedTick = _mapManager.GameTiming.CurTick;

            if (Initialized)
                newChunk.TileModified += OnTileModified;

            return Chunks[chunkIndices] = newChunk;
        }

        /// <inheritdoc />
        public bool HasChunk(Vector2i chunkIndices)
        {
            return Chunks.ContainsKey(chunkIndices);
        }

        /// <inheritdoc />
        internal IReadOnlyDictionary<Vector2i, MapChunk> GetMapChunks()
        {
            return Chunks;
        }

        /// <inheritdoc />
        internal ChunkEnumerator GetMapChunks(Box2 worldAABB)
        {
            var localAABB = _entMan.GetComponent<TransformComponent>(Owner).InvWorldMatrix
                .TransformBox(worldAABB);
            return new ChunkEnumerator(Chunks, localAABB, ChunkSize);
        }

        /// <inheritdoc />
        internal ChunkEnumerator GetMapChunks(Box2Rotated worldArea)
        {
            var matrix = _entMan.GetComponent<TransformComponent>(Owner).InvWorldMatrix;
            var localArea = matrix.TransformBox(worldArea);
            return new ChunkEnumerator(Chunks, localArea, ChunkSize);
        }

        internal ChunkEnumerator GetLocalMapChunks(Box2 localAABB)
        {
            return new ChunkEnumerator(Chunks, localAABB, ChunkSize);
        }

        #endregion ChunkAccess

        #region SnapGridAccess

        /// <inheritdoc />
        public int AnchoredEntityCount(Vector2i pos)
        {
            var gridChunkPos = GridTileToChunkIndices(pos);

            if (!Chunks.TryGetValue(gridChunkPos, out var chunk)) return 0;

            var (x, y) = chunk.GridTileToChunkTile(pos);
            return chunk.GetSnapGrid((ushort)x, (ushort)y)?.Count ?? 0; // ?
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

            if (!Chunks.TryGetValue(gridChunkPos, out var chunk)) return Enumerable.Empty<EntityUid>();

            var chunkTile = chunk.GridTileToChunkTile(pos);
            return chunk.GetSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y);
        }

        public AnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(Vector2i pos)
        {
            var gridChunkPos = GridTileToChunkIndices(pos);

            if (!Chunks.TryGetValue(gridChunkPos, out var chunk)) return AnchoredEntitiesEnumerator.Empty;

            var chunkTile = chunk.GridTileToChunkTile(pos);
            var snapgrid = chunk.GetSnapGrid((ushort)chunkTile.X, (ushort)chunkTile.Y);

            return snapgrid == null
                ? AnchoredEntitiesEnumerator.Empty
                : new AnchoredEntitiesEnumerator(snapgrid.GetEnumerator());
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
#if DEBUG
            var mapId = _entMan.GetComponent<TransformComponent>(Owner).MapID;
            DebugTools.Assert(mapId == coords.GetMapId(_entMan));
#endif

            return SnapGridLocalCellFor(LocalToGrid(coords));
        }

        /// <inheritdoc />
        public Vector2i TileIndicesFor(MapCoordinates worldPos)
        {
#if DEBUG
            var mapId = _entMan.GetComponent<TransformComponent>(Owner).MapID;
            DebugTools.Assert(mapId == worldPos.MapId);
#endif

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
            var snapgrid = chunk.GetSnapGrid((ushort)chunkTile.X, (ushort)chunkTile.Y);
            return snapgrid?.Contains(euid) == true;
        }

        /// <inheritdoc />
        public bool AddToSnapGridCell(Vector2i pos, EntityUid euid)
        {
            var (chunk, chunkTile) = ChunkAndOffsetForTile(pos);

            if (chunk.GetTile((ushort)chunkTile.X, (ushort)chunkTile.Y).IsEmpty)
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
            chunk.RemoveFromSnapGridCell((ushort)chunkTile.X, (ushort)chunkTile.Y, euid);
        }

        /// <inheritdoc />
        public void RemoveFromSnapGridCell(EntityCoordinates coords, EntityUid euid)
        {
            RemoveFromSnapGridCell(TileIndicesFor(coords), euid);
        }

        private (MapChunk, Vector2i) ChunkAndOffsetForTile(Vector2i pos)
        {
            var gridChunkIndices = GridTileToChunkIndices(pos);
            var chunk = GetOrAddChunk(gridChunkIndices);
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
            var matrix = _entMan.GetComponent<TransformComponent>(Owner).InvWorldMatrix;
            return matrix.Transform(posWorld);
        }

        /// <inheritdoc />
        public EntityCoordinates MapToGrid(MapCoordinates posWorld)
        {
            var mapId = _entMan.GetComponent<TransformComponent>(Owner).MapID;

            if (posWorld.MapId != mapId)
                throw new ArgumentException(
                    $"Grid {Owner} is on map {mapId}, but coords are on map {posWorld.MapId}.",
                    nameof(posWorld));

            if (!_mapManager.TryGetGrid(Owner, out var grid))
            {
                return new EntityCoordinates(_mapManager.GetMapEntityId(posWorld.MapId), (posWorld.X, posWorld.Y));
            }

            return new EntityCoordinates(grid.Owner, WorldToLocal(posWorld.Position));
        }

        /// <inheritdoc />
        public Vector2 LocalToWorld(Vector2 posLocal)
        {
            var matrix = _entMan.GetComponent<TransformComponent>(Owner).WorldMatrix;
            return matrix.Transform(posLocal);
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
#if DEBUG
            var mapId = _entMan.GetComponent<TransformComponent>(Owner).MapID;
            DebugTools.Assert(mapId == coords.MapId);
#endif

            var local = WorldToLocal(coords.Position);

            var x = (int)Math.Floor(local.X / TileSize);
            var y = (int)Math.Floor(local.Y / TileSize);
            return new Vector2i(x, y);
        }

        /// <inheritdoc />
        public Vector2i CoordinatesToTile(EntityCoordinates coords)
        {
#if DEBUG
            var mapId = _entMan.GetComponent<TransformComponent>(Owner).MapID;
            DebugTools.Assert(mapId == coords.GetMapId(_entMan));
#endif
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
            return position.EntityId == Owner
                ? position.Position
                : WorldToLocal(position.ToMapPos(_entMan));
        }

        public bool CollidesWithGrid(Vector2i indices)
        {
            var chunkIndices = GridTileToChunkIndices(indices);
            if (!Chunks.TryGetValue(chunkIndices, out var chunk))
                return false;

            var cTileIndices = chunk.GridTileToChunkTile(indices);
            return chunk.GetTile((ushort)cTileIndices.X, (ushort)cTileIndices.Y).TypeId != Tile.Empty.TypeId;
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
            return new(Owner,
                (gridTile.X * TileSize + (TileSize / 2f), gridTile.Y * TileSize + (TileSize / 2f)));
        }

        public Vector2 GridTileToWorldPos(Vector2i gridTile)
        {
            var locX = gridTile.X * TileSize + (TileSize / 2f);
            var locY = gridTile.Y * TileSize + (TileSize / 2f);
            var xform = _entMan.GetComponent<TransformComponent>(Owner);

            return xform.WorldMatrix.Transform(new Vector2(locX, locY));
        }

        public MapCoordinates GridTileToWorld(Vector2i gridTile)
        {
            var parentMapId = _entMan.GetComponent<TransformComponent>(Owner).MapID;

            return new(GridTileToWorldPos(gridTile), parentMapId);
        }

        /// <inheritdoc />
        public bool TryGetTileRef(Vector2i indices, out TileRef tile)
        {
            var chunkIndices = GridTileToChunkIndices(indices);
            if (!Chunks.TryGetValue(chunkIndices, out var chunk))
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
        /// Calculate the world space AABB for this chunk.
        /// </summary>
        internal Box2 CalcWorldAABB(MapChunk mapChunk)
        {
            var (position, rotation) =
                _entMan.GetComponent<TransformComponent>(Owner).GetWorldPositionRotation();

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

        internal void OnTileModified(MapChunk mapChunk, Vector2i tileIndices, Tile newTile, Tile oldTile,
            bool shapeChanged)
        {
            // As the collision regeneration can potentially delete the chunk we'll notify of the tile changed first.
            var gridTile = mapChunk.ChunkTileToGridTile(tileIndices);
            mapChunk.LastTileModifiedTick = _timing.CurTick;
            LastTileModifiedTick = _timing.CurTick;

            // The map serializer currently sets tiles of unbound grids as part of the deserialization process
            // It properly sets SuppressOnTileChanged so that the event isn't spammed for every tile on the grid.
            // ParentMapId is not able to be accessed on unbound grids, so we can't even call this function for unbound grids.
            if (!_mapManager.SuppressOnTileChanged)
            {
                var newTileRef = new TileRef(Owner, gridTile, newTile);
                _mapManager.RaiseOnTileChanged(newTileRef, oldTile);
            }

            if (shapeChanged && !mapChunk.SuppressCollisionRegeneration)
            {
                RegenerateCollision(mapChunk);
            }
        }
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    internal sealed class MapGridComponentState : ComponentState
    {
        /// <summary>
        ///     The size of the chunks in the map grid.
        /// </summary>
        public ushort ChunkSize { get; }

        /// <summary>
        ///     Constructs a new instance of <see cref="MapGridComponentState"/>.
        /// </summary>
        /// <param name="chunkSize">The size of the chunks in the map grid.</param>
        public MapGridComponentState(ushort chunkSize)
        {
            ChunkSize = chunkSize;
        }
    }
}
