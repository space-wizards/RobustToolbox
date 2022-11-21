using System;
using System.Collections.Generic;
using Robust.Shared.GameStates;
using Robust.Shared.IoC;
using Robust.Shared.Map;
using Robust.Shared.Maths;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.Manager.Attributes;
using Robust.Shared.Utility;
using Robust.Shared.ViewVariables;

namespace Robust.Shared.GameObjects
{
    [RegisterComponent]
    [NetworkedComponent]
    public sealed class MapGridComponent : Component
    {
        [Dependency] private readonly IEntityManager _entMan = default!;
        [Dependency] private readonly IMapManagerInternal _mapManager = default!;

        // This field is used for deserialization internally in the map loader.
        // If you want to remove this, you would have to restructure the map save file.
        [DataField("index")]
        internal int GridIndex = 0;
        // the grid section now writes the grid's EntityUID. as long as existing maps get updated (just a load+save),
        // this can be removed

        private IMapGrid? _mapGrid;

        [DataField("chunkSize")]
        internal ushort ChunkSize = 16;

        [ViewVariables]
        public IMapGrid Grid
        {
            get => _mapGrid ?? throw new InvalidOperationException();
            private set => _mapGrid = value;
        }

        internal MapGrid AllocMapGrid(ushort chunkSize, ushort tileSize)
        {
            DebugTools.Assert(LifeStage == ComponentLifeStage.Added);

            var grid = new MapGrid(_mapManager, _entMan, Owner, chunkSize);
            grid.TileSize = tileSize;

            Grid = grid;

            _mapManager.OnGridAllocated(this, grid);
            return grid;
        }

        internal static void ApplyMapGridState(NetworkedMapManager networkedMapManager, MapGridComponent gridComp, GameStateMapData.ChunkDatum[] chunkUpdates)
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
