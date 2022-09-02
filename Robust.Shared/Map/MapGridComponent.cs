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
    [NetworkedComponent]
    public sealed partial class MapGridComponent : Component
    {
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        // This field is used for deserialization internally in the map loader.
        // If you want to remove this, you would have to restructure the map save file.
        [ViewVariables(VVAccess.ReadOnly)]
        [DataField("index")]
#pragma warning disable CS0618
        private GridId _gridIndex = GridId.Invalid;
#pragma warning restore CS0618

        /// <inheritdoc />
        [Obsolete("Use EntityUid instead")]
        public GridId GridIndex
        {
            get => _gridIndex;
            internal set => _gridIndex = value;
        }

        [DataField("chunkSize")]
        private ushort _chunkSize = 16;

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
            _mapManager.TrueGridDelete(this);

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

        public MapGridComponent AllocMapGrid(ushort chunkSize, ushort tileSize)
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.Added);

            _chunkSize = chunkSize;
            TileSize = tileSize;
            LastTileModifiedTick = _gameTiming.CurTick;

            _mapManager.OnGridAllocated(this, this);
            return this;
        }

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        public ushort TileSize { get; set; } = 1;

        internal static void ApplyMapGridState(NetworkedMapManager networkedMapManager, MapGridComponent gridComp,
            GameStateMapData.ChunkDatum[] chunkUpdates)
        {
            networkedMapManager.SuppressOnTileChanged = true;
            var modified = new List<(Vector2i position, Tile tile)>();
            foreach (var chunkData in chunkUpdates)
            {
                if (chunkData.IsDeleted())
                    continue;

                var chunk = gridComp.GetChunk(chunkData.Index);
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
            {
                MapManager.InvokeGridChanged(networkedMapManager, gridComp, modified);
            }

            foreach (var chunkData in chunkUpdates)
            {
                if (chunkData.IsDeleted())
                {
                    gridComp.RemoveChunk(chunkData.Index);
                    continue;
                }

                var chunk = gridComp.GetChunk(chunkData.Index);
                chunk.SuppressCollisionRegeneration = false;
                gridComp.RegenerateCollision(chunk);
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
