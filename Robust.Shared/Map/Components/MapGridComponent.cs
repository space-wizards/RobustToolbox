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
    public sealed partial class MapGridComponent : Component
    {
        [Dependency] private readonly IEntityManager _entManager = default!;
        private SharedMapSystem MapSystem => _entManager.System<SharedMapSystem>();

        // This field is used for deserialization internally in the map loader.
        // If you want to remove this, you would have to restructure the map save file.
        [DataField("index")] internal int GridIndex;
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
        internal Dictionary<Vector2i, MapChunk> Chunks = new();

        [ViewVariables]
        public Box2 LocalAABB { get; internal set; }

        /// <summary>
        /// Set to enable or disable grid splitting.
        /// You must ensure you handle this properly and check for splits afterwards if relevant!
        /// </summary>
        [ViewVariables(VVAccess.ReadWrite), DataField("canSplit")]
        public bool CanSplit = true;

        #region TileAccess

        [Obsolete("Use the MapSystem method")]
        public TileRef GetTileRef(EntityCoordinates coords)
        {
            return MapSystem.GetTileRef(Owner, this, coords);
        }

        [Obsolete("Use the MapSystem method")]
        public TileRef GetTileRef(Vector2i tileCoordinates)
        {
            return MapSystem.GetTileRef(Owner, this, tileCoordinates);
        }

        [Obsolete("Use the MapSystem method")]
        public IEnumerable<TileRef> GetAllTiles(bool ignoreEmpty = true)
        {
            return MapSystem.GetAllTiles(Owner, this, ignoreEmpty);
        }

        [Obsolete("Use the MapSystem method")]
        public GridTileEnumerator GetAllTilesEnumerator(bool ignoreEmpty = true)
        {
            return MapSystem.GetAllTilesEnumerator(Owner, this, ignoreEmpty);
        }

        [Obsolete("Use the MapSystem method")]
        public void SetTile(EntityCoordinates coords, Tile tile)
        {
            MapSystem.SetTile(Owner, this, coords, tile);
        }

        [Obsolete("Use the MapSystem method")]
        public void SetTile(Vector2i gridIndices, Tile tile)
        {
            MapSystem.SetTile(Owner, this, gridIndices, tile);
        }

        [Obsolete("Use the MapSystem method")]
        public void SetTiles(List<(Vector2i GridIndices, Tile Tile)> tiles)
        {
            MapSystem.SetTiles(Owner, this, tiles);
        }

        [Obsolete("Use the MapSystem method")]
        public IEnumerable<TileRef> GetTilesIntersecting(Box2 worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return MapSystem.GetTilesIntersecting(Owner, this, worldArea, ignoreEmpty, predicate);
        }

        [Obsolete("Use the MapSystem method")]
        public IEnumerable<TileRef> GetLocalTilesIntersecting(Box2 localArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return MapSystem.GetLocalTilesIntersecting(Owner, this, localArea, ignoreEmpty, predicate);
        }

        [Obsolete("Use the MapSystem method")]
        public IEnumerable<TileRef> GetTilesIntersecting(Circle worldArea, bool ignoreEmpty = true,
            Predicate<TileRef>? predicate = null)
        {
            return MapSystem.GetTilesIntersecting(Owner, this, worldArea, ignoreEmpty, predicate);
        }

        #endregion TileAccess

        #region ChunkAccess

        [Obsolete("Use the MapSystem method")]
        internal bool TryGetChunk(Vector2i chunkIndices, [NotNullWhen(true)] out MapChunk? chunk)
        {
            return MapSystem.TryGetChunk(Owner, this, chunkIndices, out chunk);
        }

        [Obsolete("Use the MapSystem method")]
        internal IReadOnlyDictionary<Vector2i, MapChunk> GetMapChunks()
        {
            return MapSystem.GetMapChunks(Owner, this);
        }

        [Obsolete("Use the MapSystem method")]
        internal ChunkEnumerator GetMapChunks(Box2Rotated worldArea)
        {
            return MapSystem.GetMapChunks(Owner, this, worldArea);
        }

        #endregion ChunkAccess

        #region SnapGridAccess

        [Obsolete("Use the MapSystem method")]
        public IEnumerable<EntityUid> GetAnchoredEntities(MapCoordinates coords)
        {
            return MapSystem.GetAnchoredEntities(Owner, this, coords);
        }

        [Obsolete("Use the MapSystem method")]
        public IEnumerable<EntityUid> GetAnchoredEntities(Vector2i pos)
        {
            return MapSystem.GetAnchoredEntities(Owner, this, pos);
        }

        [Obsolete("Use the MapSystem method")]
        public AnchoredEntitiesEnumerator GetAnchoredEntitiesEnumerator(Vector2i pos)
        {
            return MapSystem.GetAnchoredEntitiesEnumerator(Owner, this, pos);
        }

        [Obsolete("Use the MapSystem method")]
        public Vector2i TileIndicesFor(EntityCoordinates coords)
        {
            return MapSystem.TileIndicesFor(Owner, this, coords);
        }

        [Obsolete("Use the MapSystem method")]
        public Vector2i TileIndicesFor(MapCoordinates worldPos)
        {
            return MapSystem.TileIndicesFor(Owner, this, worldPos);
        }

        [Obsolete("Use the MapSystem method")]
        public IEnumerable<EntityUid> GetCellsInSquareArea(EntityCoordinates coords, int n)
        {
            return MapSystem.GetCellsInSquareArea(Owner, this, coords, n);
        }

        #endregion

        [Obsolete("Use the MapSystem method")]
        public Vector2 WorldToLocal(Vector2 posWorld)
        {
            return MapSystem.WorldToLocal(Owner, this, posWorld);
        }

        [Obsolete("Use the MapSystem method")]
        public EntityCoordinates MapToGrid(MapCoordinates posWorld)
        {
            return MapSystem.MapToGrid(Owner, posWorld);
        }

        [Obsolete("Use the MapSystem method")]
        public Vector2 LocalToWorld(Vector2 posLocal)
        {
            return MapSystem.LocalToWorld(Owner, this, posLocal);
        }

        [Obsolete("Use the MapSystem method")]
        public Vector2i WorldToTile(Vector2 posWorld)
        {
            return MapSystem.WorldToTile(Owner, this, posWorld);
        }

        [Obsolete("Use the MapSystem method")]
        public Vector2i LocalToTile(EntityCoordinates coordinates)
        {
            return MapSystem.LocalToTile(Owner, this, coordinates);
        }

        [Obsolete("Use the MapSystem method")]
        public Vector2i CoordinatesToTile(EntityCoordinates coords)
        {
            return MapSystem.CoordinatesToTile(Owner, this, coords);
        }

        [Obsolete("Use the MapSystem method")]
        public EntityCoordinates GridTileToLocal(Vector2i gridTile)
        {
            return MapSystem.GridTileToLocal(Owner, this, gridTile);
        }

        [Obsolete("Use the MapSystem method")]
        public Vector2 GridTileToWorldPos(Vector2i gridTile)
        {
            return MapSystem.GridTileToWorldPos(Owner, this, gridTile);
        }

        [Obsolete("Use the MapSystem method")]
        public bool TryGetTileRef(Vector2i indices, out TileRef tile)
        {
            return MapSystem.TryGetTileRef(Owner, this, indices, out tile);
        }

        [Obsolete("Use the MapSystem method")]
        public bool TryGetTileRef(EntityCoordinates coords, out TileRef tile)
        {
            return MapSystem.TryGetTileRef(Owner, this, coords, out tile);
        }
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    internal sealed class MapGridComponentState(ushort chunkSize, Dictionary<Vector2i, ChunkDatum> fullGridData, GameTick lastTileModifiedTick) : ComponentState
    {
        /// <summary>
        ///     The size of the chunks in the map grid.
        /// </summary>
        public ushort ChunkSize = chunkSize;

        /// <summary>
        /// Networked chunk data containing the full grid state.
        /// </summary>
        public Dictionary<Vector2i, ChunkDatum> FullGridData = fullGridData;

        /// <summary>
        /// Last game tick that the tile on the grid was modified.
        /// </summary>
        public GameTick LastTileModifiedTick = lastTileModifiedTick;
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
    [Serializable, NetSerializable]
    internal sealed class MapGridComponentDeltaState(ushort chunkSize, Dictionary<Vector2i, ChunkDatum>? chunkData, GameTick lastTileModifiedTick)
        : ComponentState, IComponentDeltaState<MapGridComponentState>
    {
        /// <summary>
        ///     The size of the chunks in the map grid.
        /// </summary>
        public readonly ushort ChunkSize = chunkSize;

        /// <summary>
        /// Networked chunk data.
        /// </summary>
        public readonly Dictionary<Vector2i, ChunkDatum>? ChunkData = chunkData;

        /// <summary>
        /// Last game tick that the tile on the grid was modified.
        /// </summary>
        public GameTick LastTileModifiedTick = lastTileModifiedTick;

        public void ApplyToFullState(MapGridComponentState state)
        {
            state.ChunkSize = ChunkSize;

            if (ChunkData == null)
                return;

            foreach (var (index, data) in ChunkData)
            {
                if (data.IsDeleted())
                    state.FullGridData.Remove(index);
                else
                    state.FullGridData[index] = data;
            }

            state.LastTileModifiedTick = LastTileModifiedTick;
        }

        public MapGridComponentState CreateNewFullState(MapGridComponentState state)
        {
            if (ChunkData == null)
                return new(ChunkSize, state.FullGridData, state.LastTileModifiedTick);

            var newState = new MapGridComponentState(ChunkSize, state.FullGridData.ShallowClone(), LastTileModifiedTick);
            ApplyToFullState(newState);
            return newState;
        }
    }
}
