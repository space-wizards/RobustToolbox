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
        private SharedMapSystem MapSystem => _entManager.System<SharedMapSystem>();

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
            return MapSystem.GetTileRef(Owner, this, coords);
        }

        public TileRef GetTileRef(EntityCoordinates coords)
        {
            return MapSystem.GetTileRef(Owner, this, coords);
        }

        public TileRef GetTileRef(Vector2i tileCoordinates)
        {
            return MapSystem.GetTileRef(Owner, this, tileCoordinates);
        }

        public IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true)
        {
            return MapSystem.GetAllTiles(Owner, this, ignoreEmpty);
        }

        public GridTileEnumerator GetAllTilesEnumerator(bool ignoreEmpty = true)
        {
            return MapSystem.GetAllTilesEnumerator(Owner, this, ignoreEmpty);
        }

        public void SetTile(EntityCoordinates coords, Tile tile)
        {
            MapSystem.SetTile(Owner, this, coords, tile);
        }

        public void SetTile(Vector2i gridIndices, Tile tile)
        {
            MapSystem.SetTile(Owner, this, gridIndices, tile);
        }

        public void SetTiles(List<(Vector2i GridIndices, Tile Tile)> tiles)
        {
            MapSystem.SetTiles(Owner, this, tiles);
        }

        public IEnumerable<TileRef> GetLocalTilesIntersecting(Box2Rotated localArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return MapSystem.GetLocalTilesIntersecting(Owner, this, localArea, ignoreEmpty, predicate);
        }

        public IEnumerable<TileRef> GetTilesIntersecting(Box2Rotated worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return MapSystem.GetTilesIntersecting(Owner, this, worldArea, ignoreEmpty, predicate);
        }

        public IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return MapSystem.GetTilesIntersecting(Owner, this, worldArea, ignoreEmpty, predicate);
        }

        public IEnumerable<TileRef> GetLocalTilesIntersecting(Box2 localArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return MapSystem.GetLocalTilesIntersecting(Owner, this, localArea, ignoreEmpty, predicate);
        }

        public IEnumerable<TileRef> GetTilesIntersecting(Circle worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return MapSystem.GetTilesIntersecting(Owner, this, worldArea, ignoreEmpty, predicate);
        }

        #endregion TileAccess

        #region ChunkAccess

        internal bool TryGetChunk(Vector2i chunkIndices, [NotNullWhen(true)] out MapChunk? chunk)
        {
            return MapSystem.TryGetChunk(Owner, this, chunkIndices, out chunk);
        }

        internal IReadOnlyDictionary<Vector2i, MapChunk> GetMapChunks()
        {
            return MapSystem.GetMapChunks(Owner, this);
        }

        internal ChunkEnumerator GetMapChunks(Box2Rotated worldArea)
        {
            return MapSystem.GetMapChunks(Owner, this, worldArea);
        }

        #endregion ChunkAccess

        #region SnapGridAccess

        public IEnumerable<EntityUid> GetAnchoredEntities(MapCoordinates coords)
        {
            return MapSystem.GetAnchoredEntities(Owner, this, coords);
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(Vector2i pos)
        {
            return MapSystem.GetAnchoredEntities(Owner, this, pos);
        }

        public AnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(Vector2i pos)
        {
            return MapSystem.GetAnchoredEntitiesEnumerator(Owner, this, pos);
        }

        public IEnumerable<EntityUid> GetLocalAnchoredEntities(Box2 localAABB)
        {
            return MapSystem.GetLocalAnchoredEntities(Owner, this, localAABB);
        }

        public IEnumerable<EntityUid> GetAnchoredEntities(Box2 worldAABB)
        {
            return MapSystem.GetAnchoredEntities(Owner, this, worldAABB);
        }

        public Vector2i TileIndicesFor(EntityCoordinates coords)
        {
            return MapSystem.TileIndicesFor(Owner, this, coords);
        }

        public Vector2i TileIndicesFor(MapCoordinates worldPos)
        {
            return MapSystem.TileIndicesFor(Owner, this, worldPos);
        }

        public IEnumerable<EntityUid> GetInDir(EntityCoordinates position, Direction dir)
        {
            return MapSystem.GetInDir(Owner, this, position, dir);
        }

        public IEnumerable<EntityUid> GetLocal(EntityCoordinates coords)
        {
            return MapSystem.GetLocal(Owner, this, coords);
        }

        public IEnumerable<EntityUid> GetCardinalNeighborCells(EntityCoordinates coords)
        {
            return MapSystem.GetCardinalNeighborCells(Owner, this, coords);
        }

        public IEnumerable<EntityUid> GetCellsInSquareArea(EntityCoordinates coords, int n)
        {
            return MapSystem.GetCellsInSquareArea(Owner, this, coords, n);
        }

        #endregion

        public Vector2 WorldToLocal(Vector2 posWorld)
        {
            return MapSystem.WorldToLocal(Owner, this, posWorld);
        }

        public EntityCoordinates MapToGrid(MapCoordinates posWorld)
        {
            return MapSystem.MapToGrid(Owner, posWorld);
        }

        public Vector2 LocalToWorld(Vector2 posLocal)
        {
            return MapSystem.LocalToWorld(Owner, this, posLocal);
        }

        public Vector2i WorldToTile(Vector2 posWorld)
        {
            return MapSystem.WorldToTile(Owner, this, posWorld);
        }

        public Vector2i LocalToTile(EntityCoordinates coordinates)
        {
            return MapSystem.LocalToTile(Owner, this, coordinates);
        }

        public Vector2i CoordinatesToTile(EntityCoordinates coords)
        {
            return MapSystem.CoordinatesToTile(Owner, this, coords);
        }

        public bool CollidesWithGrid(Vector2i indices)
        {
            return MapSystem.CollidesWithGrid(Owner, this, indices);
        }

        public Vector2i GridTileToChunkIndices(Vector2i gridTile)
        {
            return MapSystem.GridTileToChunkIndices(Owner, this, gridTile);
        }

        public EntityCoordinates GridTileToLocal(Vector2i gridTile)
        {
            return MapSystem.GridTileToLocal(Owner, this, gridTile);
        }

        public Vector2 GridTileToWorldPos(Vector2i gridTile)
        {
            return MapSystem.GridTileToWorldPos(Owner, this, gridTile);
        }

        public MapCoordinates GridTileToWorld(Vector2i gridTile)
        {
            return MapSystem.GridTileToWorld(Owner, this, gridTile);
        }

        public bool TryGetTileRef(Vector2i indices, out TileRef tile)
        {
            return MapSystem.TryGetTileRef(Owner, this, indices, out tile);
        }

        public bool TryGetTileRef(EntityCoordinates coords, out TileRef tile)
        {
            return MapSystem.TryGetTileRef(Owner, this, coords, out tile);
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
