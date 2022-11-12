using System;
using System.Collections.Generic;
using System.IO;
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
    [Serializable, NetSerializable]
    public readonly struct ChunkDatum
    {
        public readonly Vector2i Index;

        // Definitely wasteful to send EVERY tile.
        // Optimize away future coder.
        // Also it's stored row-major.
        public readonly Tile[] TileData;

        public bool IsDeleted()
        {
            return TileData == default;
        }

        private ChunkDatum(Vector2i index, Tile[] tileData)
        {
            Index = index;
            TileData = tileData;
        }

        public static ChunkDatum CreateModified(Vector2i index, Tile[] tileData)
        {
            return new ChunkDatum(index, tileData);
        }

        public static ChunkDatum CreateDeleted(Vector2i index)
        {
            return new ChunkDatum(index, default!);
        }
    }

    /// <summary>
    ///     Represents a map grid inside the ECS system.
    /// </summary>
    [NetworkedComponent]
    public sealed partial class MapGridComponent : Component
    {
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;

        [DataField("chunkSize")] private ushort _chunkSize = 16;

        /// <summary>
        ///     The length of a side of the square chunk in number of tiles.
        /// </summary>
        [ViewVariables]
        public ushort ChunkSize
        {
            get => _chunkSize;
            set => _chunkSize = value;
        }

        /// <summary>
        ///     The bounding box of the grid in local coordinates.
        /// </summary>
        [ViewVariables]
        public Box2 LocalAABB { get; private set; }

        /// <summary>
        ///     Last game tick that the map was modified.
        /// </summary>
        [ViewVariables]
        private GameTick LastTileModifiedTick { get; set; }

        private readonly List<(GameTick tick, Vector2i indices)> _chunkDeletionHistory = new();

        /// <summary>
        ///     Grid chunks than make up this grid.
        /// </summary>
        private readonly Dictionary<Vector2i, MapChunk> _chunks = new();

        /// <summary>
        /// Map DynamicTree proxy to lookup for grid intersection.
        /// </summary>
        internal DynamicTree.Proxy MapProxy = DynamicTree.Proxy.Free;

        protected override void Initialize()
        {
            base.Initialize();
            var xformQuery = _entMan.GetEntityQuery<TransformComponent>();
            var xform = xformQuery.GetComponent(Owner);
            var mapId = xform.MapID;

            if (_mapManager.HasMapEntity(mapId))
            {
                xform.AttachParent(_mapManager.GetMapEntityIdOrThrow(mapId));
            }

            _entMan.EntitySysManager.GetEntitySystem<SharedTransformSystem>().SetGridId(xform, Owner, xformQuery);
        }

        internal static string SerializeTiles(MapChunk chunk)
        {
            // number of bytes written per tile, because sizeof(Tile) is useless.
            const int structSize = 4;

            var nTiles = chunk.ChunkSize * chunk.ChunkSize * structSize;
            var barr = new byte[nTiles];

            using (var stream = new MemoryStream(barr))
            using (var writer = new BinaryWriter(stream))
            {
                for (ushort y = 0; y < chunk.ChunkSize; y++)
                {
                    for (ushort x = 0; x < chunk.ChunkSize; x++)
                    {
                        var tile = chunk.GetTile(x, y);
                        writer.Write(tile.TypeId);
                        writer.Write((byte)tile.Flags);
                        writer.Write(tile.Variant);
                    }
                }
            }

            return Convert.ToBase64String(barr);
        }

        /// <inheritdoc />
        public override void HandleComponentState(ComponentState? curState, ComponentState? nextState)
        {
            base.HandleComponentState(curState, nextState);

            if (curState is not MapGridComponentState state)
                return;

            _chunkSize = state.ChunkSize;
        }

        public MapGridComponent AllocMapGrid(ushort chunkSize, ushort tileSize)
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.Added);

            _chunkSize = chunkSize;
            TileSize = tileSize;
            LastTileModifiedTick = _gameTiming.CurTick;

            return this;
        }

        /// <summary>
        ///     The length of the side of a square tile in world units.
        /// </summary>
        public ushort TileSize { get; set; } = 1;

        internal void ApplyMapGridState(SharedMapSystem mapSystem, List<ChunkDatum> chunkUpdates)
        {
            mapSystem.SuppressOnTileChanged = true;
            var modified = new List<(Vector2i position, Tile tile)>();
            foreach (var chunkData in chunkUpdates)
            {
                if (chunkData.IsDeleted())
                    continue;

                var chunk = GetChunk(chunkData.Index);
                chunk.SuppressCollisionRegeneration = true;
                DebugTools.Assert(chunkData.TileData.Length == ChunkSize * ChunkSize);

                var counter = 0;
                for (ushort x = 0; x < ChunkSize; x++)
                {
                    for (ushort y = 0; y < ChunkSize; y++)
                    {
                        var tile = chunkData.TileData[counter++];
                        if (chunk.GetTile(x, y) == tile)
                            continue;

                        chunk.SetTile(x, y, tile);
                        modified.Add((new Vector2i(chunk.X * ChunkSize + x, chunk.Y * ChunkSize + y),
                            tile));
                    }
                }
            }

            if (modified.Count != 0 && Running)
            {
                _entMan.EventBus.RaiseLocalEvent(Owner, new GridModifiedEvent(this, modified), true);
            }

            foreach (var chunkData in chunkUpdates)
            {
                if (chunkData.IsDeleted())
                {
                    RemoveChunk(chunkData.Index);
                    continue;
                }

                var chunk = GetChunk(chunkData.Index);
                chunk.SuppressCollisionRegeneration = false;
                RegenerateCollision(chunk);
            }

            mapSystem.SuppressOnTileChanged = false;
        }

        public static void CullChunkDeletionHistory(IEntityManager entityManager, GameTick upToTick)
        {
            foreach (var gridComp in entityManager.EntityQuery<MapGridComponent>())
            {
                gridComp._chunkDeletionHistory.RemoveAll(t => t.tick < upToTick);
            }
        }

        public static List<ChunkDatum>? GetDeltaChunkData(MapGridComponent gridComp, GameTick fromTick)
        {
            if (gridComp.LastTileModifiedTick < fromTick)
                return null;

            var chunkData = new List<ChunkDatum>();

            foreach (var (tick, indices) in gridComp._chunkDeletionHistory)
            {
                if (tick < fromTick)
                    continue;

                chunkData.Add(ChunkDatum.CreateDeleted(indices));
            }

            foreach (var (index, chunk) in gridComp.GetMapChunks())
            {
                if (chunk.LastTileModifiedTick < fromTick)
                    continue;

                var tileBuffer = new Tile[gridComp.ChunkSize * (uint)gridComp.ChunkSize];

                // Flatten the tile array.
                // NetSerializer doesn't do multi-dimensional arrays.
                // This is probably really expensive.
                for (var x = 0; x < gridComp.ChunkSize; x++)
                {
                    for (var y = 0; y < gridComp.ChunkSize; y++)
                    {
                        tileBuffer[x * gridComp.ChunkSize + y] = chunk.GetTile((ushort)x, (ushort)y);
                    }
                }

                chunkData.Add(ChunkDatum.CreateModified(index, tileBuffer));
            }

            return chunkData;
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
        ///     The size of the chunks in the map grid.
        /// </summary>
        public ushort ChunkSize { get; }

        public List<ChunkDatum>? ChunkDatums { get; }

        /// <summary>
        ///     Constructs a new instance of <see cref="MapGridComponentState"/>.
        /// </summary>
        /// <param name="gridIndex">Index of the grid this component is linked to.</param>
        /// <param name="chunkSize">The size of the chunks in the map grid.</param>
        /// <param name="chunkDatums"></param>
        public MapGridComponentState(ushort chunkSize, List<ChunkDatum>? chunkDatums)
        {
            ChunkSize = chunkSize;
            ChunkDatums = chunkDatums;
        }
    }
#pragma warning restore CS0618
}
