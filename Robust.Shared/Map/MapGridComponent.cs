using System;
using System.Collections.Generic;
using Robust.Shared.GameObjects;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Maths;
using Robust.Shared.Physics;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.Map
{
    /// <summary>
    ///     Represents a map grid inside the ECS system.
    /// </summary>
    public interface IMapGridComponent : IComponent
    {
        [Obsolete("Use EntityUids instead")]
        GridId GridIndex { get; }
        IMapGrid Grid { get; }
    }

    /// <inheritdoc cref="IMapGridComponent"/>
    [ComponentReference(typeof(IMapGridComponent))]
    [NetworkedComponent]
    internal sealed class MapGridComponent : Component, IMapGridComponent
    {
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;

        // This field is used for deserialization internally in the map loader.
        // If you want to remove this, you would have to restructure the map save file.
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("index")]
#pragma warning disable CS0618
        private GridId _gridIndex = GridId.Invalid;
#pragma warning restore CS0618

        private IMapGrid? _mapGrid;

        /// <inheritdoc />
        [Obsolete("Use EntityUid instead")]
        public GridId GridIndex
        {
            get => _gridIndex;
            internal set => _gridIndex = value;
        }

        [DataField("chunkSize")]
        private ushort _chunkSize = 16;

        /// <inheritdoc />
        [ViewVariables]
        public IMapGrid Grid
        {
            get => _mapGrid ?? throw new InvalidOperationException();
            private set => _mapGrid = value;
        }

        /// <summary>
        ///     The length of a side of the square chunk in number of tiles.
        /// </summary>
        [ViewVariables]
        public ushort ChunkSize => _chunkSize;

        /// <summary>
        ///     The bounding box of the grid in local coordinates.
        /// </summary>
        [ViewVariables]
        public Box2 LocalAABB { get; internal set; }

        /// <summary>
        ///     Last game tick that the map was modified.
        /// </summary>
        [ViewVariables]
        internal GameTick LastTileModifiedTick { get; set; }

        internal bool GridClassDeleting { get; set; }

        /// <summary>
        ///     Grid chunks than make up this grid.
        /// </summary>
        internal readonly Dictionary<Vector2i, MapChunk> _chunks = new();

        /// <summary>
        /// Map DynamicTree proxy to lookup for grid intersection.
        /// </summary>
        internal DynamicTree.Proxy MapProxy = DynamicTree.Proxy.Free;

        protected override void Initialize()
        {
            base.Initialize();
            var xform = _entMan.GetComponent<TransformComponent>(Owner);
            var mapId = xform.MapID;

            if (_mapManager.HasMapEntity(mapId))
            {
                xform.AttachParent(_mapManager.GetMapEntityIdOrThrow(mapId));
            }
        }

        protected override void OnRemove()
        {
            if (_mapGrid != null)
                _mapManager.TrueGridDelete((MapGrid)_mapGrid);

            base.OnRemove();
        }

        /// <inheritdoc />
        public override ComponentState GetComponentState()
        {
            return new MapGridComponentState(_gridIndex, _chunkSize);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MapGridComponentState state)
                return;

            _gridIndex = state.GridIndex;
            _chunkSize = state.ChunkSize;
        }

        public MapGrid AllocMapGrid(ushort chunkSize, ushort tileSize)
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.Added);

#pragma warning disable CS0618
            var grid = new MapGrid(_mapManager, _entMan, Owner);
#pragma warning restore CS0618
            _chunkSize = chunkSize;
            TileSize = tileSize;

            Grid = grid;

            _mapManager.OnGridAllocated(this, grid);
            return grid;
        }

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        public ushort TileSize { get; set; } = 1;

        public static void ApplyMapGridState(NetworkedMapManager networkedMapManager, IMapGridComponent gridComp, GameStateMapData.ChunkDatum[] chunkUpdates)
        {
            var grid = (MapGrid)gridComp.Grid;
            networkedMapManager.SuppressOnTileChanged = true;
            var modified = new List<(Vector2i position, Tile tile)>();
            foreach (var chunkData in chunkUpdates)
            {
                if (chunkData.IsDeleted())
                    continue;

                var chunk = grid.GetChunk(chunkData.Index);
                chunk.SuppressCollisionRegeneration = true;
                DebugTools.Assert(chunkData.TileData.Length == grid.ChunkSize * grid.ChunkSize);

                var counter = 0;
                for (ushort x = 0; x < grid.ChunkSize; x++)
                {
                    for (ushort y = 0; y < grid.ChunkSize; y++)
                    {
                        var tile = chunkData.TileData[counter++];
                        if (chunk.GetTile(x, y) == tile)
                            continue;

                        chunk.SetTile(x, y, tile);
                        modified.Add((new Vector2i(chunk.X * grid.ChunkSize + x, chunk.Y * grid.ChunkSize + y), tile));
                    }
                }
            }

            if (modified.Count != 0)
            {
                MapManager.InvokeGridChanged(networkedMapManager, grid, modified);
            }

            foreach (var chunkData in chunkUpdates)
            {
                if (chunkData.IsDeleted())
                {
                    grid.RemoveChunk(chunkData.Index);
                    continue;
                }

                var chunk = grid.GetChunk(chunkData.Index);
                chunk.SuppressCollisionRegeneration = false;
                grid.RegenerateCollision(chunk);
            }

            networkedMapManager.SuppressOnTileChanged = false;
        }
    }

    /// <summary>
    ///     Serialized state of a <see cref="MapGridComponentState"/>.
    /// </summary>
#pragma warning disable CS0618
    [Serializable, NetSerializable]
    internal sealed class MapGridComponentState : ComponentState
    {
        /// <summary>
        ///     Index of the grid this component is linked to.
        /// </summary>
        public GridId GridIndex { get; }

        /// <summary>
        ///     The size of the chunks in the map grid.
        /// </summary>
        public ushort ChunkSize { get; }

        /// <summary>
        ///     Constructs a new instance of <see cref="MapGridComponentState"/>.
        /// </summary>
        /// <param name="gridIndex">Index of the grid this component is linked to.</param>
        /// <param name="chunkSize">The size of the chunks in the map grid.</param>
        public MapGridComponentState(GridId gridIndex, ushort chunkSize)
        {
            GridIndex = gridIndex;
            ChunkSize = chunkSize;
        }
    }
#pragma warning restore CS0618
}
