using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map.Enumerators;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
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
        [Dependency] private readonly IEntityManager _entManager = default!;
        private SharedMapSystem _mapSystem => _entManager.System<SharedMapSystem>();

        // This field is used for deserialization internally in the map loader.
        // If you want to remove this, you would have to restructure the map save file.
        [DataField("index")] internal int GridIndex = 0;
        // the grid section now writes the grid's EntityUID. as long as existing maps get updated (just a load+save),
        // this can be removed

        [DataField("chunkSize")] internal ushort ChunkSize = 16;

        [ViewVariables]
        public int ChunkCount => Chunks.Count;

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        [DataField("tileSize")]
        public ushort TileSize { get; internal set; } = 1;

        public Vector2 TileSizeVector => new(TileSize, TileSize);

        public Vector2 TileSizeHalfVector => new(TileSize / 2f, TileSize / 2f);

        [ViewVariables] internal readonly List<(GameTick tick, Vector2i indices)> ChunkDeletionHistory = new();

        /// <summary>
        ///     Last game tick that the map was modified.
        /// </summary>
        [ViewVariables]
        public GameTick LastTileModifiedTick { get; internal set; }

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
        public Box2 LocalAABB { get; internal set; }

        /// <summary>
        /// Set to enable or disable grid splitting.
        /// You must ensure you handle this properly and check for splits afterwards if relevant!
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("canSplit")]
        public bool CanSplit = true;

        #region TileAccess

        public TileRef GetTileRef(MapCoordinates coords)
        {
            return _mapSystem.GetTileRef(Owner, this, coords);
        }

        public TileRef GetTileRef(EntityCoordinates coords)
        {
            return _mapSystem.GetTileRef(Owner, this, coords);
        }

        public TileRef GetTileRef(Vector2i tileCoordinates)
        {
            return _mapSystem.GetTileRef(Owner, this, tileCoordinates);
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
            return _mapSystem.GetTileRef(Owner, this, mapChunk, xIndex, yIndex);
        }

        public IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true)
        {
            return _mapSystem.GetAllTiles(Owner, this, ignoreEmpty);
        }

        public GridTileEnumerator GetAllTilesEnumerator(bool ignoreEmpty = true)
        {
            return _mapSystem.GetAllTilesEnumerator(Owner, this, ignoreEmpty);
        }

        public void SetTile(EntityCoordinates coords, Tile tile)
        {
            _mapSystem.SetTile(Owner, this, coords, tile);
        }

        public void SetTile(Vector2i gridIndices, Tile tile)
        {
            _mapSystem.SetTile(Owner, this, gridIndices, tile);
        }

        public void SetTiles(List<(Vector2i GridIndices, Tile Tile)> tiles)
        {
            _mapSystem.SetTiles(Owner, this, tiles);
        }

        public IEnumerable<TileRef> GetLocalTilesIntersecting(Box2Rotated localArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return _mapSystem.GetLocalTilesIntersecting(Owner, this, localArea, ignoreEmpty, predicate);
        }

        public IEnumerable<TileRef> GetTilesIntersecting(Box2Rotated worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return _mapSystem.GetTilesIntersecting(Owner, this, worldArea, ignoreEmpty, predicate);
        }

        public IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return _mapSystem.GetTilesIntersecting(Owner, this, worldArea, ignoreEmpty, predicate);
        }

        public IEnumerable<TileRef> GetLocalTilesIntersecting(Box2 localArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return _mapSystem.GetLocalTilesIntersecting(Owner, this, localArea, ignoreEmpty, predicate);
        }

        public IEnumerable<TileRef> GetTilesIntersecting(Circle worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return _mapSystem.GetTilesIntersecting(Owner, this, worldArea, ignoreEmpty, predicate);
        }

        #endregion TileAccess

        #region ChunkAccess

        internal MapChunk GetOrAddChunk(int xIndex, int yIndex)
        {
            return _mapSystem.GetOrAddChunk(Owner, this, xIndex, yIndex);
        }

        internal bool TryGetChunk(Vector2i chunkIndices, [NotNullWhen(true)] out MapChunk? chunk)
        {
            return _mapSystem.TryGetChunk(Owner, this, chunkIndices, out chunk);
        }

        internal MapChunk GetOrAddChunk(Vector2i chunkIndices)
        {
            return _mapSystem.GetOrAddChunk(Owner, this, chunkIndices);
        }

        public bool HasChunk(Vector2i chunkIndices)
        {
            return _mapSystem.HasChunk(Owner, this, chunkIndices);
        }

        internal IReadOnlyDictionary<Vector2i, MapChunk> GetMapChunks()
        {
            return _mapSystem.GetMapChunks(Owner, this);
        }

        internal ChunkEnumerator GetMapChunks(Box2 worldAABB)
        {
            return _mapSystem.GetMapChunks(Owner, this, worldAABB);
        }

        internal ChunkEnumerator GetMapChunks(Box2Rotated worldArea)
        {
            return _mapSystem.GetMapChunks(Owner, this, worldArea);
        }

        internal ChunkEnumerator GetLocalMapChunks(Box2 localAABB)
        {
            return _mapSystem.GetLocalMapChunks(Owner, this, localAABB);
        }

        #endregion ChunkAccess

        #region SnapGridAccess

        public IEnumerable<EntityUid> GetAnchoredEntities(MapCoordinates coords)
        {
            return _mapSystem.GetAnchoredEntities(Owner, this, coords);
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(EntityCoordinates coords)
        {
            return _mapSystem.GetAnchoredEntities(Owner, this, coords);
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(Vector2i pos)
        {
            return _mapSystem.GetAnchoredEntities(Owner, this, pos);
        }

        public AnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(Vector2i pos)
        {
            return _mapSystem.GetAnchoredEntitiesEnumerator(Owner, this, pos);
        }

        public IEnumerable<EntityUid> GetLocalAnchoredEntities(Box2 localAABB)
        {
            return _mapSystem.GetLocalAnchoredEntities(Owner, this, localAABB);
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(Box2 worldAABB)
        {
            return _mapSystem.GetAnchoredEntities(Owner, this, worldAABB);
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(Box2Rotated worldBounds)
        {
            return _mapSystem.GetAnchoredEntities(Owner, this, worldBounds);
        }

        public Vector2i TileIndicesFor(EntityCoordinates coords)
        {
            return _mapSystem.TileIndicesFor(Owner, this, coords);
        }

        public Vector2i TileIndicesFor(MapCoordinates worldPos)
        {
            return _mapSystem.TileIndicesFor(Owner, this, worldPos);
        }

        public bool IsAnchored(EntityCoordinates coords, EntityUid euid)
        {
            return _mapSystem.IsAnchored(Owner, this, coords, euid);
        }

        public bool AddToSnapGridCell(EntityUid gridUid, MapGridComponent grid, Vector2i pos, EntityUid euid)
        {
            return _mapSystem.AddToSnapGridCell(gridUid, grid, pos, euid);
        }

        public bool AddToSnapGridCell(EntityUid gridUid, MapGridComponent grid, EntityCoordinates coords, EntityUid euid)
        {
            return _mapSystem.AddToSnapGridCell(gridUid, grid, coords, euid);
        }

        public void RemoveFromSnapGridCell(EntityUid gridUid, MapGridComponent grid, Vector2i pos, EntityUid euid)
        {
            _mapSystem.RemoveFromSnapGridCell(gridUid, grid, pos, euid);
        }

        public void RemoveFromSnapGridCell(EntityUid gridUid, MapGridComponent grid, EntityCoordinates coords, EntityUid euid)
        {
            _mapSystem.RemoveFromSnapGridCell(gridUid, grid, coords, euid);
        }

        public IEnumerable<EntityUid> GetInDir(EntityCoordinates position, Direction dir)
        {
            return _mapSystem.GetInDir(Owner, this, position, dir);
        }

        public IEnumerable<EntityUid> GetOffset(EntityCoordinates coords, Vector2i offset)
        {
            return _mapSystem.GetOffset(Owner, this, coords, offset);
        }

        public IEnumerable<EntityUid> GetLocal(EntityCoordinates coords)
        {
            return _mapSystem.GetLocal(Owner, this, coords);
        }

        public EntityCoordinates DirectionToGrid(EntityCoordinates coords, Direction direction)
        {
            return _mapSystem.DirectionToGrid(Owner, this, coords, direction);
        }

        public IEnumerable<EntityUid> GetCardinalNeighborCells(EntityCoordinates coords)
        {
            return _mapSystem.GetCardinalNeighborCells(Owner, this, coords);
        }

        public IEnumerable<EntityUid> GetCellsInSquareArea(EntityCoordinates coords, int n)
        {
            return _mapSystem.GetCellsInSquareArea(Owner, this, coords, n);
        }

        #endregion

        public Vector2 WorldToLocal(Vector2 posWorld)
        {
            return _mapSystem.WorldToLocal(Owner, this, posWorld);
        }

        public EntityCoordinates MapToGrid(MapCoordinates posWorld)
        {
            return _mapSystem.MapToGrid(Owner, posWorld);
        }

        public Vector2 LocalToWorld(Vector2 posLocal)
        {
            return _mapSystem.LocalToWorld(Owner, this, posLocal);
        }

        public Vector2i WorldToTile(Vector2 posWorld)
        {
            return _mapSystem.WorldToTile(Owner, this, posWorld);
        }

        public Vector2i LocalToTile(EntityCoordinates coordinates)
        {
            return _mapSystem.LocalToTile(Owner, this, coordinates);
        }

        public Vector2i CoordinatesToTile(MapCoordinates coords)
        {
            return _mapSystem.CoordinatesToTile(Owner, this, coords);
        }

        public Vector2i CoordinatesToTile(EntityCoordinates coords)
        {
            return _mapSystem.CoordinatesToTile(Owner, this, coords);
        }

        public Vector2i LocalToChunkIndices(EntityCoordinates gridPos)
        {
            return _mapSystem.LocalToChunkIndices(Owner, this, gridPos);
        }

        public Vector2 LocalToGrid(EntityCoordinates position)
        {
            return _mapSystem.LocalToGrid(Owner, this, position);
        }

        public bool CollidesWithGrid(Vector2i indices)
        {
            return _mapSystem.CollidesWithGrid(Owner, this, indices);
        }

        public Vector2i GridTileToChunkIndices(Vector2i gridTile)
        {
            return _mapSystem.GridTileToChunkIndices(Owner, this, gridTile);
        }

        public EntityCoordinates GridTileToLocal(Vector2i gridTile)
        {
            return _mapSystem.GridTileToLocal(Owner, this, gridTile);
        }

        public Vector2 GridTileToWorldPos(Vector2i gridTile)
        {
            return _mapSystem.GridTileToWorldPos(Owner, this, gridTile);
        }

        public MapCoordinates GridTileToWorld(Vector2i gridTile)
        {
            return _mapSystem.GridTileToWorld(Owner, this, gridTile);
        }

        public bool TryGetTileRef(Vector2i indices, out TileRef tile)
        {
            return _mapSystem.TryGetTileRef(Owner, this, indices, out tile);
        }

        public bool TryGetTileRef(EntityCoordinates coords, out TileRef tile)
        {
            return _mapSystem.TryGetTileRef(Owner, this, coords, out tile);
        }

        public bool TryGetTileRef(Vector2 worldPos, out TileRef tile)
        {
            return _mapSystem.TryGetTileRef(Owner, this, worldPos, out tile);
        }
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    internal sealed class MapGridComponentState : ComponentState, IComponentDeltaState
    {
        /// <summary>
        ///     The size of the chunks in the map grid.
        /// </summary>
        public ushort ChunkSize;

        /// <summary>
        /// Networked chunk data.
        /// </summary>
        public List<ChunkDatum>? ChunkData;

        /// <summary>
        /// Networked chunk data containing the full grid state.
        /// </summary>
        public Dictionary<Vector2i, Tile[]>? FullGridData;

        public bool FullState => FullGridData != null;

        /// <summary>
        ///     Constructs a new grid component delta state.
        /// </summary>
        public MapGridComponentState(ushort chunkSize, List<ChunkDatum>? chunkData)
        {
            ChunkSize = chunkSize;
            ChunkData = chunkData;
        }

        /// <summary>
        ///     Constructs a new full component state.
        /// </summary>
        public MapGridComponentState(ushort chunkSize, Dictionary<Vector2i, Tile[]> fullGridData)
        {
            ChunkSize = chunkSize;
            FullGridData = fullGridData;
        }

        public void ApplyToFullState(ComponentState fullState)
        {
            var state = (MapGridComponentState)fullState;
            DebugTools.Assert(!FullState && state.FullState);

            state.ChunkSize = ChunkSize;

            if (ChunkData == null)
                return;

            foreach (var data in ChunkData)
            {
                if (data.IsDeleted())
                    state.FullGridData!.Remove(data.Index);
                else
                    state.FullGridData![data.Index] = data.TileData;
            }
        }

        public ComponentState CreateNewFullState(ComponentState fullState)
        {
            var state = (MapGridComponentState)fullState;
            DebugTools.Assert(!FullState && state.FullState);

            var fullGridData = new Dictionary<Vector2i, Tile[]>(state.FullGridData!.Count);

            foreach (var (key, value) in state.FullGridData)
            {
                var arr = fullGridData[key] = new Tile[value.Length];
                Array.Copy(value, arr, value.Length);
            }

            var newState = new MapGridComponentState(ChunkSize, fullGridData);
            ApplyToFullState(newState);
            return newState;
        }
    }
}
